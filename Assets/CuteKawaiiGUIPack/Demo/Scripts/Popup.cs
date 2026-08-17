// Copyright (C) 2024 ricimi. All rights reserved.
// This code can only be used under the standard Unity Asset Store EULA,
// a copy of which is available at https://unity.com/legal/as-terms.

using System;
using System.Collections;
using Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace Ricimi
{
    // This class is responsible for popup management. Popups follow the traditional behavior of
    // automatically blocking the input on elements behind it and adding a background texture.
    public class Popup : MonoBehaviour
    {
        public Color backgroundColor = new Color(10.0f / 255.0f, 10.0f / 255.0f, 10.0f / 255.0f, 0.6f);

        public float destroyTime = 0.5f;

        private GameObject m_background;
        
        public event Action onClose;

        // How many popups are currently on screen. The play timer reads this so a level's
        // countdown does not drain while the player is sitting in a dialog. Counted rather
        // than a bool because popups can overlap (a shop popup opened from another popup).
        private static int s_openCount;

        /// <summary>True while at least one popup is on screen.</summary>
        public static bool AnyOpen => s_openCount > 0;

        // Statics survive scene loads but not domain reloads; with "Enter Play Mode Options"
        // reload disabled they would survive those too, so reset explicitly on every start.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOpenCount() => s_openCount = 0;

        private bool m_counted;

        public void Open()
        {
            SoundController.Instance.PlayOpenPopupClip();

            if (!m_counted)
            {
                m_counted = true;
                s_openCount++;
            }

            AddBackground();
        }

        // Decremented here rather than in Close() so a popup torn down by a scene load, or
        // destroyed any other way, still releases its hold on the timer.
        private void OnDestroy()
        {
            if (!m_counted) return;
            m_counted = false;
            s_openCount--;
        }

        public void Close()
        {
            SoundController.Instance.PlayButtonClickClip();
                
            var animator = GetComponent<Animator>();
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Open"))
            {
                animator.Play("Close");
            }

            RemoveBackground();
            StartCoroutine(RunPopupDestroy());
        }

        // We destroy the popup automatically 0.5 seconds after closing it.
        // The destruction is performed asynchronously via a coroutine. If you
        // want to destroy the popup at the exact time its closing animation is
        // finished, you can use an animation event instead.
        private IEnumerator RunPopupDestroy()
        {
            yield return new WaitForSeconds(destroyTime);
            
            onClose?.Invoke();
            Destroy(m_background);
            Destroy(gameObject);
        }

        private void AddBackground()
        {
            var bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, backgroundColor);
            bgTex.Apply();

            m_background = new GameObject("PopupBackground");
            var image = m_background.AddComponent<Image>();
            var rect = new Rect(0, 0, bgTex.width, bgTex.height);
            var sprite = Sprite.Create(bgTex, rect, new Vector2(0.5f, 0.5f), 1);
            // Clone the material, which is the default UI material, to avoid changing it permanently.
            image.material = new Material(image.material);
            image.material.mainTexture = bgTex;
            image.sprite = sprite;
            var newColor = image.color;
            image.color = newColor;
            image.canvasRenderer.SetAlpha(0.0f);
            image.CrossFadeAlpha(1.0f, 0.4f, false);

            var canvas = GetComponentInParent<Canvas>();
            m_background.transform.localScale = new Vector3(1, 1, 1);
            m_background.GetComponent<RectTransform>().sizeDelta = canvas.GetComponent<RectTransform>().sizeDelta;
            m_background.transform.SetParent(canvas.transform, false);
            m_background.transform.SetSiblingIndex(transform.GetSiblingIndex());
        }

        private void RemoveBackground()
        {
            var image = m_background.GetComponent<Image>();
            if (image != null)
            {
                image.CrossFadeAlpha(0.0f, 0.2f, false);
            }
        }
    }
}
