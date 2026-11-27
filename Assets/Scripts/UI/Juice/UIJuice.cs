using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Juice
{
    /// <summary>
    /// Marks a popup that the generic <see cref="UIJuice.PlayPopupReveal"/> leaves alone: either it
    /// plays its own entrance (the win screen, the collection panel) or it should simply appear
    /// at once (the settings popups).
    /// </summary>
    public interface IOwnsRevealAnimation { }

    /// <summary>
    /// The shared animation layer for the UI: popups building up piece by piece, buttons that
    /// squash when pressed, numbers that punch when they change, pulses that pull the eye.
    ///
    /// Everything is attached from code - by <see cref="Ricimi.Popup.Open"/>, on every scene load,
    /// and by the Home and in-game screens - so no scene file has to be re-saved to get it.
    /// </summary>
    public static class UIJuice
    {
        // ---------------------------------------------------------------- popups

        /// <summary>
        /// Hides the popup's pieces and pops them back in one after another, just behind the
        /// popup's own open animation. The frame (anything named Background) comes in with the
        /// popup itself; pieces that run their own Animator are left to it.
        /// </summary>
        public static void PlayPopupReveal(GameObject popup)
        {
            if (popup == null) return;
            InstallPressSquash(popup);
            if (popup.GetComponent<IOwnsRevealAnimation>() != null) return;

            var targets = new List<Transform>();
            foreach (Transform child in popup.transform)
            {
                if (!child.gameObject.activeSelf || !(child is RectTransform)) continue;
                if (child.name.StartsWith("Background")) continue;

                // The body is usually several things in one box - stagger those, not the box.
                if (child.name == "Content")
                {
                    foreach (Transform piece in child)
                        if (piece.gameObject.activeSelf && Revealable(piece)) targets.Add(piece);
                    continue;
                }
                if (Revealable(child)) targets.Add(child);
            }

            float delay = 0.12f;
            foreach (var t in targets)
            {
                Vector3 home = t.localScale;
                t.localScale = Vector3.zero;
                t.DOScale(home, 0.35f).SetEase(Ease.OutBack).SetDelay(delay)
                    .SetUpdate(true).SetLink(t.gameObject);
                delay += 0.07f;
            }

            // Watching an ad is the offer worth noticing; let it breathe once everything is in.
            foreach (var gate in popup.GetComponentsInChildren<AdRewardButton>(true))
                ScaleJuice.For(gate).Pulse(0.06f, 1.1f, delay + 0.2f);
        }

        // Animated pieces own their scale, and full-size decorations belong to the frame.
        private static bool Revealable(Transform t)
        {
            if (t.GetComponent<Animator>() != null) return false;
            var rt = t as RectTransform;
            if (rt == null) return false;
            bool fills = rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one
                         && rt.sizeDelta.sqrMagnitude < 400f;
            return !fills;
        }

        /// <summary>
        /// Stagger-pops a list of pieces (rows of a list, buttons of a page). Only the first
        /// <paramref name="maxAnimated"/> wait their turn; the rest appear with the last of them,
        /// so a long list never keeps the player waiting.
        /// </summary>
        public static void PopIn(IList<Transform> items, float startDelay = 0f, float stagger = 0.06f,
            int maxAnimated = 8)
        {
            float delay = startDelay;
            for (int i = 0; i < items.Count; i++)
            {
                Transform t = items[i];
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                t.DOKill(true);
                ScaleJuice juice = t.GetComponent<ScaleJuice>();
                if (t.GetComponent<Animator>() != null || juice != null)
                {
                    // Can't own the scale here; give it a pop on top of whatever drives it.
                    ScaleJuice.For(t).Punch(0.12f, 0.35f, delay);
                }
                else
                {
                    t.localScale = Vector3.zero;
                    t.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetDelay(delay)
                        .SetUpdate(true).SetLink(t.gameObject);
                }
                if (i < maxAnimated - 1) delay += stagger;
            }
        }

        // ---------------------------------------------------------------- buttons

        /// <summary>
        /// Gives every button under <paramref name="root"/> that has no Animator a press squash.
        /// Animated buttons get theirs from the kit's Button controller.
        /// </summary>
        public static void InstallPressSquash(GameObject root)
        {
            if (root == null) return;
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponent<Animator>() != null) continue;
                if (button.GetComponent<PressSquash>() == null) button.gameObject.AddComponent<PressSquash>();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallOnSceneLoads()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects()) InstallPressSquash(root);
        }

        // ---------------------------------------------------------------- numbers

        /// <summary>Makes a label punch whenever what it reads changes.</summary>
        public static PunchOnTextChange PunchOnChange(TMP_Text label, Transform target = null, float strength = 0.25f)
        {
            if (label == null) return null;
            var punch = label.GetComponent<PunchOnTextChange>();
            if (punch == null) punch = label.gameObject.AddComponent<PunchOnTextChange>();
            punch.Configure(target, strength);
            return punch;
        }
    }

    /// <summary>
    /// Scale effects that sit on top of whatever else drives the scale. The kit's buttons run an
    /// Animator that rewrites their scale every frame, which would wipe out an ordinary tween;
    /// this multiplies its own factor in LateUpdate, after the Animator has had its turn, and
    /// works out the underlying scale again each frame, so it never accumulates.
    /// </summary>
    [DisallowMultipleComponent]
    public class ScaleJuice : MonoBehaviour
    {
        private float _pulseAmount, _pulsePeriod = 1f, _pulseFrom;
        private float _punchStrength, _punchDuration, _punchFrom = -1f;
        private float _press, _pressTarget;

        private Selectable _selectable;
        private Vector3 _lastWritten;
        private float _lastFactor = 1f;

        public static ScaleJuice For(Component c)
        {
            var juice = c.GetComponent<ScaleJuice>();
            return juice != null ? juice : c.gameObject.AddComponent<ScaleJuice>();
        }

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }

        /// <summary>A gentle breathing, held while the button can be pressed.</summary>
        public ScaleJuice Pulse(float amount, float period, float startDelay = 0f)
        {
            _pulseAmount = amount;
            _pulsePeriod = Mathf.Max(0.1f, period);
            _pulseFrom = Time.unscaledTime + startDelay;
            return this;
        }

        /// <summary>One springy pop.</summary>
        public ScaleJuice Punch(float strength = 0.2f, float duration = 0.35f, float delay = 0f)
        {
            _punchStrength = strength;
            _punchDuration = Mathf.Max(0.05f, duration);
            _punchFrom = Time.unscaledTime + delay;
            return this;
        }

        /// <summary>Held down (squashed) or released (springs back with a pop).</summary>
        public void SetPressed(bool pressed)
        {
            if (!pressed && _pressTarget > 0f) Punch(0.12f, 0.35f);
            _pressTarget = pressed ? 1f : 0f;
        }

        private void LateUpdate()
        {
            float now = Time.unscaledTime;
            float factor = 1f;

            if (_pulseAmount > 0f && now >= _pulseFrom
                && (_selectable == null || _selectable.IsInteractable()))
            {
                float phase = (now - _pulseFrom) * Mathf.PI * 2f / _pulsePeriod;
                factor *= 1f + _pulseAmount * (0.5f - 0.5f * Mathf.Cos(phase));
            }

            if (_punchFrom >= 0f && now >= _punchFrom)
            {
                float t = (now - _punchFrom) / _punchDuration;
                if (t >= 1f) _punchFrom = -1f;
                else factor *= 1f + _punchStrength * Mathf.Sin(t * Mathf.PI * 3f) * (1f - t);
            }

            _press = Mathf.MoveTowards(_press, _pressTarget, Time.unscaledDeltaTime / 0.08f);
            factor *= 1f - 0.1f * _press;

            Vector3 current = transform.localScale;
            // Unchanged since we wrote it: nothing else drives the scale, so take our factor back
            // off to find the real one. Changed: an Animator or tween wrote a fresh value.
            Vector3 baseScale = current == _lastWritten && _lastFactor != 0f ? current / _lastFactor : current;
            Vector3 next = baseScale * factor;
            transform.localScale = next;
            _lastWritten = next;
            _lastFactor = factor;
        }

        private void OnDisable()
        {
            if (transform.localScale == _lastWritten && _lastFactor != 0f)
                transform.localScale /= _lastFactor;
            _lastFactor = 1f;
            _lastWritten = transform.localScale;
            _press = _pressTarget = 0f;
        }
    }

    /// <summary>Squash on press, spring back on release, for buttons without the kit's Animator.</summary>
    public class PressSquash : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private Selectable _selectable;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_selectable != null && !_selectable.IsInteractable()) return;
            ScaleJuice.For(this).SetPressed(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ScaleJuice.For(this).SetPressed(false);
        }
    }

    /// <summary>Punches a label (or a target of its choosing) every time its text changes.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class PunchOnTextChange : MonoBehaviour
    {
        private TMP_Text _label;
        private Transform _target;
        private float _strength = 0.25f;
        private string _last;
        private bool _primed;

        public void Configure(Transform target, float strength)
        {
            _target = target;
            _strength = strength;
        }

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
        }

        private void LateUpdate()
        {
            string now = _label.text;
            // The first reading is where the label starts, not a change worth celebrating.
            if (!_primed)
            {
                _last = now;
                _primed = true;
                return;
            }
            if (string.Equals(now, _last, System.StringComparison.Ordinal)) return;
            _last = now;
            ScaleJuice.For(_target != null ? _target : transform).Punch(_strength, 0.3f);
        }
    }
}
