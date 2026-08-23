using System;
using Commons;
using Controllers;
using DG.Tweening;
using Ricimi;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using Zenject;
using Random = UnityEngine.Random;

namespace UI
{
    /// <summary>
    /// The four power-up buttons above the board. Each one is gated three ways, and all three
    /// have to pass before it lights up:
    ///
    /// <list type="bullet">
    /// <item>the player has reached the level that unlocks it (<see cref="Settings.minLevelPowerup1"/> etc);</item>
    /// <item>for the package and the magnifier, the board is actually holding a match to act on;</item>
    /// <item>the level is running, rather than already won or lost.</item>
    /// </list>
    ///
    /// A blocked button is greyed with <c>interactable</c> rather than hidden, so the row never
    /// reshuffles under the player's thumb mid-level.
    ///
    /// Running out is not one of the gates: an empty power-up stays pressable and opens the buy
    /// popup instead, and the "+" badge on the button does the same.
    /// </summary>
    public class PlayTopBar : MonoBehaviour
    {
        public GameSetting gameSetting;

        [SerializeField] private Button powerUpBtn1;
        [SerializeField] private Button powerUpBtn2;
        [SerializeField] private Button powerUpBtn3;
        [SerializeField] private Button powerUpBtn4;

        [SerializeField] private GameObject powerupActive1;
        [SerializeField] private GameObject powerupActive2;
        [SerializeField] private GameObject powerupActive3;
        [SerializeField] private GameObject powerupActive4;

        [SerializeField] private GameObject powerupInactive1;
        [SerializeField] private GameObject powerupInactive2;
        [SerializeField] private GameObject powerupInactive3;
        [SerializeField] private GameObject powerupInactive4;

        [SerializeField] private TextMeshProUGUI totalPowerup1Text;
        [SerializeField] private TextMeshProUGUI totalPowerup2Text;
        [SerializeField] private TextMeshProUGUI totalPowerup3Text;
        [SerializeField] private TextMeshProUGUI totalPowerup4Text;

        [SerializeField] private TextMeshProUGUI levelPowerup1Text;
        [SerializeField] private TextMeshProUGUI levelPowerup2Text;
        [SerializeField] private TextMeshProUGUI levelPowerup3Text;
        [SerializeField] private TextMeshProUGUI levelPowerup4Text;

        [SerializeField] private Button addPowerup1Btn;
        [SerializeField] private Button addPowerup2Btn;
        [SerializeField] private Button addPowerup3Btn;
        [SerializeField] private Button addPowerup4Btn;

        [Tooltip("How often the row re-checks the board for a match, in seconds. The board can " +
                 "change without the player touching a button - a basket completing, a refill " +
                 "landing - so this polls rather than waiting to be told.")]
        [SerializeField] private float refreshInterval = 0.15f;

        [Header("New power-up feedback")]
        [Tooltip("How far the button overshoots when a power-up lands. 0 turns the pop off.")]
        [SerializeField] private float punchScale = 0.35f;

        [Tooltip("How long the pop lasts, in seconds.")]
        [SerializeField] private float punchDuration = 0.45f;

        [Tooltip("Size of the icon that flies to the button, in canvas units. " +
                 "Zero or less skips the flight and pops the button on the spot.")]
        [SerializeField] private Vector2 flyIconSize = new Vector2(140f, 140f);

        [Tooltip("How long the icon takes to swell into view in the middle of the screen.")]
        [SerializeField] private float flyPopDuration = 0.35f;

        [Tooltip("How long it then sits in the middle before setting off, in seconds. " +
                 "This is the beat that stops the whole thing reading as a flicker.")]
        [SerializeField] private float flyHoldDuration = 0.45f;

        [Tooltip("How long the arc across to the button takes, in seconds.")]
        [SerializeField] private float flyDuration = 0.9f;

        [Tooltip("How high the icon arcs on its way to the button, in canvas units. " +
                 "0 travels in a straight line.")]
        [SerializeField] private float flyJumpPower = 160f;

        [Tooltip("How far apart a handful of icons start around the middle, in canvas units.")]
        [SerializeField] private float flyScatter = 90f;

        [Tooltip("Gap between one icon setting off and the next, in seconds.")]
        [SerializeField] private float flyStagger = 0.12f;

        [Tooltip("Most icons flown at once, however many were granted. A bundle can credit a lot.")]
        [SerializeField] private int maxFlyIcons = 5;

        // The serialized fields stay one-per-slot so the prefab wiring survives; these are the
        // same objects gathered into arrays so the logic below can be written once.
        private Button[] _powerUpButtons;
        private Button[] _addButtons;
        private TextMeshProUGUI[] _totalTexts;
        private int[] _minLevels;

        // Whether the power-up acts on three matching dim sum, and so needs one on the board to
        // be worth pressing. Indexed like the arrays above: package, magnifier, shuffle, timer.
        // The shuffle rearranges whatever is left and the timer touches the clock, so neither
        // cares what the board is holding.
        private static readonly bool[] NeedsMatch = { true, true, false, false };

        // The counters and the "+" badges stay as the prefab authored them until the game says
        // otherwise, which is what keeps the first tutorial from opening a shop.
        private bool _countsVisible;
        private float _nextRefresh;

        // What each counter read last time, so a count going *up* can be noticed. Watching the
        // number rather than listening to the buy popup means every source is covered - coins, a
        // rewarded ad, an IAP bundle crediting all four at once - and nothing has to remember to
        // tell the top bar. The first read only records the baseline: loading a save with three
        // power-ups in it is not something the player just earned.
        private int[] _lastCounts;
        private bool _countsBaselined;
        private Tween[] _punchTweens;

        // Power-ups already paid for but not yet celebrated. Noticing the count go up is not the
        // moment to celebrate it: the player is still looking at the buy popup, so icons would fly
        // behind it and the button would pop where it cannot be seen. The gain waits here, held
        // back off the counter too, until the screen is clear.
        private int[] _held;

        // Icons still on their way down to each button. The label prints the count *minus* these,
        // so the number ticks up as each icon lands rather than jumping the moment the coins are
        // taken - the same read as coins flying into the coin pill.
        private int[] _inFlight;
        private RectTransform _flyLayer;

        // Slots asked to celebrate, one bit per slot. Static because the asker is a popup that is
        // destroying itself as it asks, so it has no toolbar reference to call through.
        private static int _pendingPunchMask;

        // Statics survive scene loads, and domain reloads too when those are turned off, so a
        // request left over from a level the player already left would pop a button on entry.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPendingPunches() => _pendingPunchMask = 0;

        /// <summary>
        /// Releases the celebration for a slot: the icons swell into view in the middle of the
        /// screen and arc over to the button, the counter ticks up as they land, and the button
        /// pops with the success clip. Call it when the player is actually back at the board -
        /// after the ad, after the payment, after the popup has finished closing - rather than at
        /// the moment the power-up is credited.
        /// </summary>
        /// <param name="slot">1-based power-up slot, matching <c>GameSetting.GetPowerup</c>.</param>
        public static void RequestPunch(int slot)
        {
            if (slot < 1 || slot > 4) return;
            _pendingPunchMask |= 1 << (slot - 1);
        }

        public void PlayButtonSound()
        {
            SoundController.Instance.PlayButtonClickClip();
        }

        private void Awake()
        {
            _powerUpButtons = new[] { powerUpBtn1, powerUpBtn2, powerUpBtn3, powerUpBtn4 };
            _addButtons = new[] { addPowerup1Btn, addPowerup2Btn, addPowerup3Btn, addPowerup4Btn };
            _totalTexts = new[] { totalPowerup1Text, totalPowerup2Text, totalPowerup3Text, totalPowerup4Text };
            _minLevels = new[]
            {
                Settings.minLevelPowerup1, Settings.minLevelPowerup2,
                Settings.minLevelPowerup3, Settings.minLevelPowerup4
            };
            _lastCounts = new int[_powerUpButtons.Length];
            _punchTweens = new Tween[_powerUpButtons.Length];
            _held = new int[_powerUpButtons.Length];
            _inFlight = new int[_powerUpButtons.Length];

            // Flown icons are parented to the canvas root and pushed to the back of the sibling
            // list, so they pass over the popup that granted them instead of under it.
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) _flyLayer = canvas.rootCanvas.transform as RectTransform;

            // The "+" badge is the one place a player can ask for more without spending what they
            // do not have. Wired in code because these buttons had no listener at all, and a
            // handler that lives next to the slot number cannot be pointed at the wrong slot.
            for (int i = 0; i < _addButtons.Length; i++)
            {
                if (_addButtons[i] == null) continue;
                int slot = i + 1;
                _addButtons[i].onClick.AddListener(() => OpenBuyPopup(slot));
            }
        }

        private void Start()
        {
            // A request outlives the toolbar that should have answered it - quitting a level with
            // the buy popup still closing leaves one behind - and a fresh board must start quiet.
            _pendingPunchMask = 0;

            for (int i = 0; i < _powerUpButtons.Length; i++)
            {
                bool unlocked = gameSetting.currentLevel >= _minLevels[i];
                SetActiveSafe(ActiveRoot(i), unlocked);
                SetActiveSafe(InactiveRoot(i), !unlocked);
                if (LevelText(i) != null) LevelText(i).text = $"Lv. {_minLevels[i]}";
            }

            if (gameSetting.currentLevel != 0) SetUpPowerUpButtons();

            // Even before the counters are live the buttons still need their unlock state, and
            // the match gate has to be right on the very first frame or the magnifier is lit over
            // an empty board for a moment.
            RefreshInteractable();
        }

        private void Update()
        {
            // Outside the poll gate: the celebration is the answer to something the player just
            // did, so it should not wait up to a refresh interval to set off.
            FlushPendingPunches();

            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + Mathf.Max(0.02f, refreshInterval);

            RefreshInteractable();
            if (_countsVisible) SetUpPowerUpButtons();
        }

        public void ShowTutorialPowerUp()
        {
            for (int i = 0; i < _powerUpButtons.Length; i++)
            {
                if (gameSetting.currentLevel != _minLevels[i] - 1) continue;
                SetActiveSafe(ActiveRoot(i), true);
                SetActiveSafe(InactiveRoot(i), false);
            }

            SetUpPowerUpButtons();
        }

        /// <summary>
        /// Greys or lights every power-up button. Called on a timer because the two match-driven
        /// power-ups depend on the board, which changes on its own.
        /// </summary>
        private void RefreshInteractable()
        {
            GameController game = GameController.Instance;

            // Asked once rather than per button: it walks every basket, and both buttons that
            // care about it want the same answer in the same frame.
            bool hasMatch = game == null || game.HasReadyMatch;
            bool levelRunning = game == null
                                || (game.gameStatus != Settings.GAME_STATUS.win
                                    && game.gameStatus != Settings.GAME_STATUS.lose);

            // One power-up at a time. The effects run for a couple of seconds and animate the very
            // dim sum a second press would target, so the whole row goes dark until the board has
            // settled - including the slots the player is not mid-way through using.
            bool powerUpRunning = game != null && game.PowerupRunning;

            for (int i = 0; i < _powerUpButtons.Length; i++)
            {
                // The "+" badge sits on top of an empty slot and is a button in its own right, so
                // it would otherwise still be tappable - opening the shop, and pausing the game,
                // over a power-up that is still playing.
                if (_addButtons[i] != null) _addButtons[i].interactable = levelRunning && !powerUpRunning;

                if (_powerUpButtons[i] == null) continue;

                bool unlocked = gameSetting.currentLevel >= _minLevels[i];
                bool usable = unlocked && levelRunning && !powerUpRunning && (!NeedsMatch[i] || hasMatch);

                _powerUpButtons[i].interactable = usable;
            }
        }

        private void SetUpPowerUpButtons()
        {
            _countsVisible = true;

            for (int i = 0; i < _totalTexts.Length; i++)
            {
                int total = gameSetting.GetPowerup(i + 1);
                int gain = _countsBaselined ? total - _lastCounts[i] : 0;
                _lastCounts[i] = total;

                if (gain > 0) _held[i] += gain;

                UpdateSlotLabel(i);
            }

            _countsBaselined = true;
        }

        /// <summary>
        /// Sets off the celebrations that are due. A slot is due either because whoever granted
        /// the power-up asked - the buy popup, once it has finished closing - or because the gain
        /// came from somewhere with no popup in the way at all, such as a level reward, in which
        /// case there is nothing to wait for.
        /// </summary>
        private void FlushPendingPunches()
        {
            int mask = _pendingPunchMask;
            _pendingPunchMask = 0;

            // Popups can stack, so this asks "is the screen clear" rather than "did mine close".
            bool screenClear = !Popup.AnyOpen;

            for (int i = 0; i < _powerUpButtons.Length; i++)
            {
                bool asked = (mask & (1 << i)) != 0;
                if (!asked && !(screenClear && _held[i] > 0)) continue;
                ReleaseGain(i);
            }
        }

        /// <summary>
        /// Pays out one slot's held gain: icons fly down, the counter follows them, and the button
        /// pops when the last one lands. A request with nothing held - the count was already on
        /// the label - still pops, so an asker is never answered with silence.
        /// </summary>
        private void ReleaseGain(int index)
        {
            int amount = _held[index];
            _held[index] = 0;

            if (amount <= 0)
            {
                Punch(index);
                SoundController.Instance.PlaySuccessClip();
                return;
            }

            StartFlight(index, amount);
        }

        /// <summary>
        /// Prints one counter, holding back both what is waiting to be celebrated and what is
        /// still flying down to it, and shows the "+" badge only once there is genuinely nothing
        /// left to spend.
        /// </summary>
        private void UpdateSlotLabel(int index)
        {
            int pending = _held[index] + _inFlight[index];
            int shown = Mathf.Max(0, gameSetting.GetPowerup(index + 1) - pending);
            if (_totalTexts[index] != null) _totalTexts[index].text = $"{shown:N0}";

            // The counter reads zero for as long as the icon is still on its way, and a "+" badge
            // over a slot that has already been paid for invites the player to buy it twice. One
            // that is on its way is not one to be sold again.
            if (_addButtons[index] != null)
                _addButtons[index].gameObject.SetActive(shown <= 0 && pending <= 0);
        }

        /// <summary>
        /// Sends one icon per power-up gained to its button: each one swells into view in the
        /// middle of the screen, holds there long enough to be read, then arcs up and over to the
        /// button. The button pops and the sound plays when the last one lands.
        ///
        /// The middle is where it starts whatever granted it - the buy popup was sitting there,
        /// and a bundle or an ad reward has no better claim to anywhere else - so the icon appears
        /// where the player is already looking.
        /// </summary>
        private void StartFlight(int index, int gain)
        {
            Button button = _powerUpButtons[index];
            Sprite icon = SlotIcon(index);
            int count = Mathf.Clamp(gain, 1, Mathf.Max(1, maxFlyIcons));

            // Nothing to fly with, or the flight is switched off: pop on the spot instead, so the
            // gain is never silent.
            if (button == null || icon == null || _flyLayer == null
                || flyIconSize.x <= 0f || flyIconSize.y <= 0f)
            {
                UpdateSlotLabel(index);
                Punch(index);
                SoundController.Instance.PlaySuccessClip();
                return;
            }

            Vector3 origin = _flyLayer.TransformPoint(Vector3.zero);
            Vector3 destination = button.transform.position;

            // Everything below moves the flyer in world space, but the canvas is drawn by a
            // camera: one of its pixels is a few thousandths of a world unit. Handing DOTween a
            // arc height or an offset straight out of a pixel-denominated field would throw the
            // icon clean off the screen and back, which reads as it flying somewhere else
            // entirely rather than over to the button. Sizes stay in pixels - sizeDelta is a
            // canvas measurement - so only distances need converting.
            float pixelToWorld = _flyLayer.lossyScale.x;
            if (pixelToWorld <= 0f) pixelToWorld = 1f;

            // The whole gain is held off the label, not just the icons flown: a bundle of twenty
            // sends five icons, and the counter should still arrive at twenty. The last icon
            // carries whatever it was not worth drawing a sprite for.
            _inFlight[index] += gain;
            int lastCarries = 1 + (gain - count);

            for (int i = 0; i < count; i++)
            {
                bool isLast = i == count - 1;

                // Only a handful ever fly, so they are spread around the middle rather than
                // stacked on it - a single icon starts dead centre.
                Vector2 kick = count > 1 ? Random.insideUnitCircle * flyScatter * pixelToWorld : Vector2.zero;
                Vector3 start = origin + new Vector3(kick.x, kick.y, 0f);

                GameObject flyer = CreateFlyer(icon, start);
                RectTransform rt = (RectTransform)flyer.transform;
                rt.localScale = Vector3.zero;

                // Targeted and linked to the flyer: a bare Sequence belongs to nothing, so
                // DOTween cannot tell that its object has gone and the tween outlives a scene
                // change still driving a destroyed transform.
                Sequence seq = DOTween.Sequence().SetUpdate(true).SetTarget(rt).SetLink(flyer);

                // A stagger, so a handful arrive one after another instead of as one blob.
                if (i > 0) seq.AppendInterval(i * flyStagger);

                // Swell into view in the middle and sit there a moment: the icon has to be seen
                // before it is worth animating anywhere.
                seq.Append(rt.DOScale(1f, flyPopDuration).SetEase(Ease.OutBack));
                seq.AppendInterval(flyHoldDuration);

                // Then arc over to the button, shrinking as it goes.
                seq.Append(rt.DOJump(destination, flyJumpPower * pixelToWorld, 1, flyDuration)
                    .SetEase(Ease.InOutQuad));
                seq.Join(rt.DOScale(0.55f, flyDuration).SetEase(Ease.InQuad));
                seq.OnComplete(() =>
                {
                    // The toolbar can be gone by the time an icon lands - a level ends, the scene
                    // changes - and the arrival must not touch a dead component.
                    if (this == null) { if (flyer != null) Destroy(flyer); return; }

                    Destroy(flyer);
                    _inFlight[index] = Mathf.Max(0, _inFlight[index] - (isLast ? lastCarries : 1));
                    UpdateSlotLabel(index);

                    if (!isLast) return;
                    Punch(index);
                    SoundController.Instance.PlaySuccessClip();
                });
            }

            // The label has to hold its old value from this moment, not from the next poll.
            UpdateSlotLabel(index);
        }

        private GameObject CreateFlyer(Sprite icon, Vector3 origin)
        {
            var flyer = new GameObject("Powerup-Flyer", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)flyer.transform;
            rt.SetParent(_flyLayer, false);
            rt.SetAsLastSibling();
            rt.sizeDelta = flyIconSize;
            rt.position = origin;

            var image = flyer.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            // It is decoration passing over the popup; it must never eat a tap meant for a button.
            image.raycastTarget = false;
            return flyer;
        }

        /// <summary>
        /// The icon the button itself is wearing. Read off the button rather than serialized
        /// separately, so the thing that flies down can never be a different power-up's art.
        /// </summary>
        private Sprite SlotIcon(int index)
        {
            Button button = _powerUpButtons[index];
            if (button == null) return null;
            Transform icon = button.transform.Find("Active/Icon");
            if (icon == null) return null;
            var image = icon.GetComponent<Image>();
            return image == null ? null : image.sprite;
        }

        /// <summary>Pops one power-up button, so a new one is noticed rather than just appearing.</summary>
        private void Punch(int index)
        {
            if (punchScale <= 0f) return;
            Button button = _powerUpButtons[index];
            if (button == null) return;

            // DOPunchScale springs back to whatever scale it captured when it started, so a
            // second punch landing on top of a running one strands the button at the size the
            // first had reached. Kill and reset before starting another.
            if (_punchTweens[index] != null && _punchTweens[index].IsActive()) _punchTweens[index].Kill();
            Transform target = button.transform;
            target.localScale = Vector3.one;

            // Scale does not feed layout, so popping a button cannot shove the rest of the row
            // sideways inside the toolbar's HorizontalLayoutGroup.
            _punchTweens[index] = target
                .DOPunchScale(Vector3.one * punchScale, punchDuration, 6, 0.8f)
                .SetUpdate(true);
        }

        private void OnDestroy()
        {
            if (_punchTweens == null) return;
            for (int i = 0; i < _punchTweens.Length; i++)
            {
                if (_punchTweens[i] != null && _punchTweens[i].IsActive()) _punchTweens[i].Kill();
            }
        }

        private void OpenBuyPopup(int slot)
        {
            SoundController.Instance.PlayButtonClickClip();
            if (GameController.Instance != null) GameController.Instance.ShowBuyPowerupPopup(slot);
        }

        private GameObject ActiveRoot(int index)
        {
            switch (index)
            {
                case 0: return powerupActive1;
                case 1: return powerupActive2;
                case 2: return powerupActive3;
                default: return powerupActive4;
            }
        }

        private GameObject InactiveRoot(int index)
        {
            switch (index)
            {
                case 0: return powerupInactive1;
                case 1: return powerupInactive2;
                case 2: return powerupInactive3;
                default: return powerupInactive4;
            }
        }

        private TextMeshProUGUI LevelText(int index)
        {
            switch (index)
            {
                case 0: return levelPowerup1Text;
                case 1: return levelPowerup2Text;
                case 2: return levelPowerup3Text;
                default: return levelPowerup4Text;
            }
        }

        private static void SetActiveSafe(GameObject target, bool active)
        {
            if (target != null) target.SetActive(active);
        }
    }
}
