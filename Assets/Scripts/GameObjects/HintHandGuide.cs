using System.Collections;
using Controllers;
using DG.Tweening;
using Models;
using UI;
using UnityEngine;
using Zenject;

namespace GameObjects
{
    /// <summary>
    /// A hand that shows the player what to do next: it presses a dim sum, drags a faded copy of
    /// it to where it should go, and lets go - or, when the board has no simple move, taps a
    /// power-up button. It loops until the player touches the screen.
    ///
    /// Two set-ups use it:
    /// <list type="bullet">
    /// <item>Level 1 tutorial: a short idle delay, so it is effectively always guiding.</item>
    /// <item>Every other level: an idle hint, shown only after the player has gone a while
    /// without touching anything (assumed stuck or unsure).</item>
    /// </list>
    ///
    /// It reads the live board rather than replaying a script, and re-plans after every move.
    /// What it suggests, best first:
    /// <list type="number">
    /// <item>A move that completes three of a kind. Among those, one whose dish opens a locked
    /// basket or is wanted by a waiting customer is preferred.</item>
    /// <item>(Tutorial only) a move that puts a dim sum next to one of its kind.</item>
    /// <item>A dig: moving a piece off a basket that still has rows waiting underneath, so
    /// emptying it reveals more - offered only when the rest of the board can take its pieces.</item>
    /// <item>(Idle hint only) tapping a power-up button.</item>
    /// </list>
    /// It hides while a finger is down, a match is clearing, a power-up is playing or a popup is
    /// open, and stops for good once the level is decided.
    /// </summary>
    public class HintHandGuide : MonoBehaviour
    {
        [Inject] private GameController _gameController;

        [Header("Art")]
        [SerializeField] private Sprite handSprite;
        [Tooltip("Where the fingertip sits in the hand sprite, 0-1 from the bottom-left corner. " +
                 "The hand is placed so this point lands on the target.")]
        [SerializeField] private Vector2 fingertip = new Vector2(0.215f, 0.79f);
        [Tooltip("World-space width of the hand.")]
        [SerializeField] private float handWidth = 1.1f;
        [Tooltip("Drawn on the UI layer above the game canvas (order 200), so the hand also shows " +
                 "over the power-up buttons.")]
        [SerializeField] private string sortingLayer = "UI";
        [SerializeField] private int sortingOrder = 500;

        [Header("When to show")]
        [Tooltip("Delay before the very first hint. The Level 1 tutorial card opens one second into " +
                 "the scene; starting sooner flashes the hand just before the card covers it.")]
        [SerializeField] private float startDelay = 1.5f;
        [Tooltip("Seconds the board must sit still, untouched, before the first hint. Short for the " +
                 "tutorial; around 8 for the idle hint in normal play.")]
        [SerializeField] private float idleDelay = 0.8f;
        [Tooltip("Pause between repeats once a hint is showing, until the player touches.")]
        [SerializeField] private float repeatGap = 0.3f;

        [Header("What to suggest")]
        [Tooltip("Also suggest building a pair when no match can be finished. Used by the tutorial.")]
        [SerializeField] private bool suggestPairs = true;
        [Tooltip("Tap a power-up button when no simple move exists. Used by the idle hint.")]
        [SerializeField] private bool suggestPowerUps = false;
        [Tooltip("Locked-basket tutorial (Level 4): while a basket is locked, show quickly and " +
                 "only suggest moves that gather three of its unlock dish into one basket. Once " +
                 "it opens, the normal rules and idle delay apply again.")]
        [SerializeField] private bool focusLockedBasket = false;
        [Tooltip("Idle delay used while focusing on a locked basket.")]
        [SerializeField] private float focusIdleDelay = 0.8f;

        [Header("Animation")]
        [SerializeField] private float pressDuration = 0.25f;
        [SerializeField] private float dragDuration = 0.9f;
        [SerializeField] private float holdAtTarget = 0.25f;
        [SerializeField] private float pauseAfterDemo = 0.5f;
        [SerializeField, Range(0f, 1f)] private float ghostAlpha = 0.75f;

        private const int ScoreUnlocksBasket = 2;
        private const int ScoreServesCustomer = 2;

        private Camera _camera;
        private PlayTopBar _topBar;
        private Transform _hand;
        private SpriteRenderer _handRenderer;
        private SpriteRenderer _ghost;
        private Sequence _sequence;

        private struct Hint
        {
            public bool IsTap;
            // Drag hints
            public MDropArea From;
            public int FromSlot;
            public MDimSum Piece;
            public MDropArea To;
            public int ToSlot;
            // Tap hints
            public RectTransform Button;
        }

        private void Start()
        {
            if (_gameController == null) _gameController = FindObjectOfType<GameController>();
            _topBar = FindObjectOfType<PlayTopBar>();
            _camera = Camera.main;

            if (handSprite == null || _gameController == null || _camera == null)
            {
                Debug.LogWarning("[HintHand] Disabled: " +
                                 (handSprite == null ? "no hand sprite assigned."
                                     : _gameController == null ? "no GameController in scene." : "no main camera."));
                gameObject.SetActive(false);
                return;
            }

            BuildVisuals();
            StartCoroutine(GuideLoop());
            Debug.Log($"[HintHand] {name} running (idle {idleDelay}s, pairs {suggestPairs}, power-ups {suggestPowerUps}).");
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
        }

        #region visuals

        private void BuildVisuals()
        {
            // The root sits exactly on the fingertip; the sprite child is offset so its tip lands there.
            _hand = new GameObject("Hand").transform;
            _hand.SetParent(transform, false);

            var art = new GameObject("HandArt").transform;
            art.SetParent(_hand, false);
            _handRenderer = art.gameObject.AddComponent<SpriteRenderer>();
            _handRenderer.sprite = handSprite;
            _handRenderer.sortingLayerName = sortingLayer;
            _handRenderer.sortingOrder = sortingOrder;

            Vector2 spriteSize = handSprite.rect.size / handSprite.pixelsPerUnit;
            float scale = handWidth / spriteSize.x;
            art.localScale = Vector3.one * scale;
            Vector2 pivot = handSprite.pivot / handSprite.pixelsPerUnit;   // from bottom-left
            Vector2 tip = Vector2.Scale(fingertip, spriteSize);
            art.localPosition = (Vector3)((pivot - tip) * scale);

            _ghost = new GameObject("GhostDimsum").AddComponent<SpriteRenderer>();
            _ghost.transform.SetParent(transform, false);
            _ghost.sortingLayerName = sortingLayer;
            _ghost.sortingOrder = sortingOrder - 1;

            SetVisible(false, false);
        }

        private void SetVisible(bool hand, bool ghost)
        {
            if (_handRenderer != null) _handRenderer.enabled = hand;
            if (_ghost != null) _ghost.enabled = ghost;
        }

        private void SetAlpha(SpriteRenderer r, float a)
        {
            var c = r.color;
            c.a = a;
            r.color = c;
        }

        private Tween Fade(SpriteRenderer r, float to, float duration)
        {
            return DOTween.To(() => r.color.a, a => SetAlpha(r, a), to, duration);
        }

        #endregion

        #region loop

        private IEnumerator GuideLoop()
        {
            yield return new WaitForSeconds(startDelay);

            // True once a hint has been shown and the player has not touched since: repeats then
            // come quickly instead of waiting out the full idle delay again.
            bool hinting = false;

            while (true)
            {
                float still = 0f;
                while (still < (hinting ? repeatGap : CurrentIdleDelay()))
                {
                    if (_gameController == null || _gameController.IsLevelDecided)
                    {
                        StopDemo();
                        yield break;
                    }

                    string blocked = WhyNotCalm();
                    // A finger on the screen is the player playing, not the hint being stuck.
                    ReportIfStuck(blocked != null && !blocked.StartsWith("mouse") && !blocked.StartsWith("touch") ? blocked : null);
                    if (blocked == null)
                    {
                        still += Time.deltaTime;
                    }
                    else
                    {
                        still = 0f;
                        hinting = false;
                    }
                    yield return null;
                }

                if (!TryFindHint(out Hint hint))
                {
                    ReportIfStuck("no suggestible move on the board");
                    yield return null;
                    continue;
                }
                ReportIfStuck(null);
                _lastReported = null;

                if (hint.IsTap) PlayTap(hint);
                else PlayDrag(hint);
                hinting = true;

                while (_sequence != null && _sequence.IsActive() && _sequence.IsPlaying())
                {
                    if (!BoardIsCalm() || !HintStillValid(hint))
                    {
                        StopDemo();
                        hinting = false;
                        break;
                    }
                    yield return null;
                }
            }
        }

        private bool BoardIsCalm()
        {
            return WhyNotCalm() == null;
        }

        /// <summary>Null when the hand may show; otherwise what is holding it back.</summary>
        private string WhyNotCalm()
        {
            if (Input.GetMouseButton(0)) return "mouse button held";
            if (Input.touchCount > 0) return "touch on screen (" + Input.touchCount + ")";
            if (!_gameController.AcceptsBoardInput)
                return "board not accepting input (status " + _gameController.gameStatus +
                       ", popup open " + Ricimi.Popup.AnyOpen + ")";
            if (_gameController.PowerupRunning) return "power-up animation running";
            if (_gameController.IsBoardBusy) return "basket clearing or rebuilding";
            if (_gameController.GameBaskets == null) return "baskets not set up";
            return null;
        }

        // Diagnostics: say once, in the console, why the hand has been held back for a while, so
        // a hint that never appears on a device or in a test run explains itself.
        private const float ReportAfterSeconds = 4f;
        private float _blockedFor;
        private string _lastReported;

        private void ReportIfStuck(string reason)
        {
            if (reason == null)
            {
                _blockedFor = 0f;
                return;
            }

            _blockedFor += Time.deltaTime;
            if (_blockedFor < ReportAfterSeconds || reason == _lastReported) return;
            _lastReported = reason;
            Debug.Log($"[HintHand] {name}: waiting {ReportAfterSeconds:0}s+ because {reason}.");
        }

        private static bool HintStillValid(Hint hint)
        {
            if (hint.IsTap) return hint.Button != null && hint.Button.gameObject.activeInHierarchy;
            return hint.From.mDimSums[hint.FromSlot] == hint.Piece
                   && hint.To.mDimSums[hint.ToSlot] == null;
        }

        #endregion

        #region demos

        private void PlayDrag(Hint hint)
        {
            Vector3 from = hint.Piece.transform.position;
            Vector3 to = hint.To.GetDimsumPosition(hint.ToSlot);
            from.z = to.z = -2f;

            var pieceRenderer = hint.Piece.GetComponentInChildren<SpriteRenderer>();
            _ghost.sprite = pieceRenderer != null ? pieceRenderer.sprite : null;
            _ghost.transform.localScale = hint.Piece.transform.lossyScale;

            _hand.position = from;
            _ghost.transform.position = from;
            _hand.localScale = Vector3.one;
            SetAlpha(_handRenderer, 0f);
            SetAlpha(_ghost, 0f);
            SetVisible(true, true);

            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Append(Fade(_handRenderer, 1f, 0.2f))
                // Press: the hand dips and a copy of the piece appears under the finger.
                .Append(_hand.DOScale(0.85f, pressDuration).SetEase(Ease.OutQuad))
                .Join(Fade(_ghost, ghostAlpha, pressDuration))
                // Drag to the slot.
                .Append(_hand.DOMove(to, dragDuration).SetEase(Ease.InOutSine))
                .Join(_ghost.transform.DOMove(to, dragDuration).SetEase(Ease.InOutSine))
                .AppendInterval(holdAtTarget)
                // Release and fade out.
                .Append(_hand.DOScale(1f, pressDuration).SetEase(Ease.OutQuad))
                .Join(Fade(_ghost, 0f, pressDuration))
                .Append(Fade(_handRenderer, 0f, 0.2f))
                .AppendInterval(pauseAfterDemo)
                .OnComplete(() => SetVisible(false, false))
                .SetLink(gameObject);
        }

        private void PlayTap(Hint hint)
        {
            Vector3 at = ScreenUIToWorld(hint.Button);
            at.z = -2f;

            _hand.position = at;
            _hand.localScale = Vector3.one;
            SetAlpha(_handRenderer, 0f);
            SetVisible(true, false);

            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Append(Fade(_handRenderer, 1f, 0.2f))
                .Append(_hand.DOScale(0.8f, pressDuration).SetEase(Ease.OutQuad))
                .Append(_hand.DOScale(1f, pressDuration).SetEase(Ease.OutQuad))
                .Append(_hand.DOScale(0.8f, pressDuration).SetEase(Ease.OutQuad))
                .Append(_hand.DOScale(1f, pressDuration).SetEase(Ease.OutQuad))
                .AppendInterval(holdAtTarget)
                .Append(Fade(_handRenderer, 0f, 0.2f))
                .AppendInterval(pauseAfterDemo)
                .OnComplete(() => SetVisible(false, false))
                .SetLink(gameObject);
        }

        private void StopDemo()
        {
            _sequence?.Kill();
            _sequence = null;
            SetVisible(false, false);
        }

        /// <summary>World position under the centre of a UI element, whatever its canvas mode.</summary>
        private Vector3 ScreenUIToWorld(RectTransform rect)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.rootCanvas.worldCamera
                : null;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, rect.TransformPoint(rect.rect.center));
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            return world;
        }

        #endregion

        #region finding a hint

        private float CurrentIdleDelay()
        {
            return focusLockedBasket && LockedDish() >= 0 ? focusIdleDelay : idleDelay;
        }

        /// <summary>The unlock dish of the first locked basket in play, or -1 if none is locked.</summary>
        private int LockedDish()
        {
            var baskets = _gameController.GameBaskets;
            if (baskets == null) return -1;
            foreach (var b in baskets)
            {
                if (b != null && b.gameObject.activeInHierarchy && b.LockedUnlockType >= 0) return b.LockedUnlockType;
            }
            return -1;
        }

        private bool TryFindHint(out Hint hint)
        {
            if (focusLockedBasket)
            {
                int dish = LockedDish();
                if (dish >= 0 && TryFindGatherMove(dish, out hint)) return true;
            }

            if (TryFindMatchMove(out hint)) return true;
            if (suggestPairs && TryFindPairMove(out hint)) return true;
            if (TryFindDigMove(out hint)) return true;
            if (suggestPowerUps && TryFindPowerUp(out hint)) return true;
            hint = default;
            return false;
        }

        private static bool IsPlayable(MDropArea basket)
        {
            return basket != null && basket.gameObject.activeInHierarchy && !basket.IsClearing
                   && basket.GetOpenBasket() == Commons.DisplayedBasket.Displayed;
        }

        /// <summary>
        /// One move that completes three of a kind: a basket holding a pair of a dish and a free
        /// slot, plus a movable copy of that dish elsewhere. Scored so that unlocking a basket or
        /// serving a customer wins a tie.
        /// </summary>
        private bool TryFindMatchMove(out Hint best)
        {
            best = default;
            int bestScore = -1;
            var baskets = _gameController.GameBaskets;

            foreach (var target in baskets)
            {
                if (!IsPlayable(target)) continue;
                int[] types = target.GetDimsumTypes();
                int empty = System.Array.IndexOf(types, -1);
                if (empty < 0) continue;

                foreach (int type in types)
                {
                    if (type < 0 || Count(types, type) != 2) continue;
                    if (!TryFindMovable(type, target, out MDropArea source, out int slot)) continue;

                    int score = ScoreDish(type);
                    if (score <= bestScore) continue;
                    bestScore = score;
                    best = DragHint(source, slot, target, empty);
                }
            }
            return bestScore >= 0;
        }

        /// <summary>Tutorial helper: put a dim sum next to a lone one of its kind.</summary>
        private bool TryFindPairMove(out Hint hint)
        {
            hint = default;
            foreach (var target in _gameController.GameBaskets)
            {
                if (!IsPlayable(target)) continue;
                int[] types = target.GetDimsumTypes();
                int empty = System.Array.IndexOf(types, -1);
                if (empty < 0) continue;

                foreach (int type in types)
                {
                    if (type < 0 || Count(types, type) != 1) continue;
                    if (!TryFindMovable(type, target, out MDropArea source, out int slot)) continue;
                    hint = DragHint(source, slot, target, empty);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Moving a piece off a basket that still has rows waiting under it. The basket closest to
        /// empty is chosen, and only if the other baskets have room for everything on it - a dig
        /// that cannot be finished just shuffles pieces around. The piece goes to the basket with
        /// the most room, so it blocks as little as possible.
        /// </summary>
        private bool TryFindDigMove(out Hint hint)
        {
            hint = default;
            var baskets = _gameController.GameBaskets;

            MDropArea source = null;
            int sourceSlot = -1, fewest = int.MaxValue;
            foreach (var b in baskets)
            {
                if (!IsPlayable(b) || !b.HasStillTrayLeft() || b.HasFrozenDimsum()) continue;
                int filled = b.TotalFilledDimsums();
                if (filled == 0 || filled >= fewest) continue;
                if (FreeSlotsExcept(b) < filled) continue;

                for (int s = 0; s < b.mDimSums.Length; s++)
                {
                    if (b.mDimSums[s] == null) continue;
                    source = b; sourceSlot = s; fewest = filled;
                    break;
                }
            }
            if (source == null) return false;

            MDropArea target = null;
            int targetSlot = -1, mostRoom = 0;
            foreach (var b in baskets)
            {
                if (b == source || !IsPlayable(b)) continue;
                int[] types = b.GetDimsumTypes();
                int room = Count(types, -1);
                if (room <= mostRoom) continue;
                target = b; targetSlot = System.Array.IndexOf(types, -1); mostRoom = room;
            }
            if (target == null) return false;

            hint = DragHint(source, sourceSlot, target, targetSlot);
            return true;
        }

        /// <summary>
        /// The next step towards three of <paramref name="dish"/> in one basket (which opens the
        /// locked basket showing it):
        /// finish a basket that already holds two; otherwise pick the basket holding the most
        /// copies as the host and bring one more in; if the host has no room, first move one of
        /// its other pieces out.
        /// </summary>
        private bool TryFindGatherMove(int dish, out Hint hint)
        {
            hint = default;
            var baskets = _gameController.GameBaskets;

            // Copies the player can actually pick up, across the board.
            int movableTotal = 0;
            foreach (var b in baskets)
            {
                if (!IsPlayable(b)) continue;
                foreach (var p in b.mDimSums)
                    if (p != null && !p.IsFrozen && p.dimsumType == dish) movableTotal++;
            }

            MDropArea host = null;
            int hostCount = 0, hostRoom = -1;
            foreach (var b in baskets)
            {
                if (!IsPlayable(b)) continue;
                int[] types = b.GetDimsumTypes();
                int count = Count(types, dish);
                if (count == 0) continue;

                int movableHere = 0;
                foreach (var p in b.mDimSums)
                    if (p != null && !p.IsFrozen && p.dimsumType == dish) movableHere++;
                // Enough copies elsewhere to bring this host up to three?
                if (movableTotal - movableHere < 3 - count) continue;

                int room = Count(types, -1);
                if (count > hostCount || (count == hostCount && room > hostRoom))
                {
                    host = b; hostCount = count; hostRoom = room;
                }
            }
            if (host == null) return false;

            int[] hostTypes = host.GetDimsumTypes();
            int emptySlot = System.Array.IndexOf(hostTypes, -1);

            if (emptySlot >= 0)
            {
                // Bring a copy in, taking it from the basket that holds the fewest of them.
                MDropArea source = null;
                int sourceSlot = -1, sourceCount = int.MaxValue;
                foreach (var b in baskets)
                {
                    if (b == host || !IsPlayable(b)) continue;
                    int[] types = b.GetDimsumTypes();
                    int count = Count(types, dish);
                    if (count == 0 || count >= sourceCount) continue;
                    for (int s = 0; s < b.mDimSums.Length; s++)
                    {
                        var p = b.mDimSums[s];
                        if (p == null || p.IsFrozen || p.dimsumType != dish) continue;
                        source = b; sourceSlot = s; sourceCount = count;
                        break;
                    }
                }
                if (source == null) return false;
                hint = DragHint(source, sourceSlot, host, emptySlot);
                return true;
            }

            // Host is full: move one of its other pieces somewhere with room.
            int evictSlot = -1;
            for (int s = 0; s < host.mDimSums.Length; s++)
            {
                var p = host.mDimSums[s];
                if (p != null && !p.IsFrozen && p.dimsumType != dish) { evictSlot = s; break; }
            }
            if (evictSlot < 0) return false;

            MDropArea target = null;
            int targetSlot = -1, mostRoom = 0;
            foreach (var b in baskets)
            {
                if (b == host || !IsPlayable(b)) continue;
                int[] types = b.GetDimsumTypes();
                int room = Count(types, -1);
                if (room <= mostRoom) continue;
                target = b; targetSlot = System.Array.IndexOf(types, -1); mostRoom = room;
            }
            if (target == null) return false;

            hint = DragHint(host, evictSlot, target, targetSlot);
            return true;
        }

        private bool TryFindPowerUp(out Hint hint)
        {
            hint = default;
            if (_topBar == null) return false;
            RectTransform button = _topBar.GetHintPowerUpButton();
            if (button == null) return false;
            hint = new Hint { IsTap = true, Button = button };
            return true;
        }

        private bool TryFindMovable(int type, MDropArea exclude, out MDropArea source, out int slot)
        {
            foreach (var b in _gameController.GameBaskets)
            {
                if (b == exclude || !IsPlayable(b)) continue;
                for (int s = 0; s < b.mDimSums.Length; s++)
                {
                    MDimSum piece = b.mDimSums[s];
                    if (piece == null || piece.IsFrozen || piece.dimsumType != type) continue;
                    source = b;
                    slot = s;
                    return true;
                }
            }
            source = null;
            slot = -1;
            return false;
        }

        private int ScoreDish(int type)
        {
            int score = 0;
            foreach (var b in _gameController.GameBaskets)
            {
                if (b != null && b.gameObject.activeInHierarchy && b.LockedUnlockType == type)
                {
                    score += ScoreUnlocksBasket;
                    break;
                }
            }
            if (_gameController.IsRequestedByCustomer(type)) score += ScoreServesCustomer;
            return score;
        }

        private int FreeSlotsExcept(MDropArea exclude)
        {
            int free = 0;
            foreach (var b in _gameController.GameBaskets)
            {
                if (b == exclude || !IsPlayable(b)) continue;
                free += 3 - b.TotalFilledDimsums();
            }
            return free;
        }

        private static Hint DragHint(MDropArea from, int fromSlot, MDropArea to, int toSlot)
        {
            return new Hint { From = from, FromSlot = fromSlot, Piece = from.mDimSums[fromSlot], To = to, ToSlot = toSlot };
        }

        private static int Count(int[] types, int type)
        {
            int n = 0;
            foreach (int t in types) if (t == type) n++;
            return n;
        }

        #endregion
    }
}
