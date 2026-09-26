using System.Collections;
using Commons;
using Controllers;
using DG.Tweening;
using Models;
using Spawners;
using UnityEngine;
using Zenject;

public class PowerUpAnimationEffect : MonoBehaviour
{
    private static readonly int OpenUp = Animator.StringToHash("OpenUp");
    public GameSetting gameSetting;
    [Inject] private GameController _gameController;
    [Inject] private DimsumSpawner dimsumSpawner;

    [SerializeField] private Animator boxAnimation;
    [SerializeField] private SpriteRenderer boxSprite;
    [SerializeField] private Transform boxParticle;

    public Sprite magnifierSprite;
    public Sprite packageSprite;
    public Sprite reloadSprite;
    public Sprite timerSprite;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.gameObject.SetActive(false);
        boxParticle.gameObject.SetActive(false);
    }

    public void DoAnimateMagnifier(MDimSum[] positionDimsums)
    {
        // Refused before anything is switched on. Callers check too, but this object being active
        // is what GameController.PowerupRunning reads, so starting an effect with nothing to act
        // on is how the whole power-up row gets stuck.
        if (!HasTarget(positionDimsums)) return;

        boxParticle.gameObject.SetActive(false);
        this.gameObject.SetActive(true);

        boxSprite.sprite = magnifierSprite;
        boxAnimation.SetTrigger(OpenUp);

        StartCoroutine(DoAnimateMagnifierRoutine(positionDimsums));
    }

    private IEnumerator DoAnimateMagnifierRoutine(MDimSum[] positionDimsums)
    {
        try
        {
            yield return new WaitForSeconds(1f);

            boxParticle.gameObject.SetActive(true);
            boxParticle.localScale = Vector3.zero;
            boxParticle.DOScale(Vector3.one * 0.3f, 0.5f);

            yield return new WaitForSeconds(.2f);
            foreach (var positionDimsum in positionDimsums)
            {
                // A second or more passes inside this loop, and the board can clear a piece while
                // it runs, so each entry is re-checked rather than trusted from when it was gathered.
                if (positionDimsum == null) continue;
                positionDimsum.DrawToTop();
                positionDimsum.transform.DOShakeRotation(3f, 30f);
                SoundController.Instance.PlayBubbleSoundClips();
                yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(1f);

            foreach (var positionDimsum in positionDimsums)
            {
                if (positionDimsum == null) continue;
                positionDimsum.BackToBottom();
            }
        }
        finally
        {
            Finish();
        }
    }

    public void DoAnimateSuckPower(MDimSum[] positionDimsums)
    {
        // See DoAnimateMagnifier. This is the one that used to throw: an empty array reached
        // positionDimsums[0] a second into the routine, and the throw skipped the line that
        // switched this object off again, leaving every power-up button dead for the level.
        if (!HasTarget(positionDimsums)) return;

        boxParticle.gameObject.SetActive(false);
        this.gameObject.SetActive(true);

        boxSprite.sprite = packageSprite;
        boxAnimation.SetTrigger(OpenUp);

        StartCoroutine(DoAnimateSuckPowerRoutine(positionDimsums));
    }

    private IEnumerator DoAnimateSuckPowerRoutine(MDimSum[] positionDimsums)
    {
        try
        {
            yield return new WaitForSeconds(1f);

            // Read off the first surviving target rather than element zero, which is what threw.
            int dimsumType = FirstDimsumType(positionDimsums);
            if (dimsumType < 0) yield break;

            boxParticle.gameObject.SetActive(true);
            boxParticle.localScale = Vector3.zero;
            boxParticle.DOScale(Vector3.one * 0.3f, 0.5f);

            yield return new WaitForSeconds(.2f);
            foreach (var positionDimsum in positionDimsums)
            {
                if (positionDimsum == null) continue;
                positionDimsum.DrawToTop();
                positionDimsum.transform.DOJump(this.transform.position, 1.5f, 1, 1f);
                SoundController.Instance.PlayWhooshSoundClips();
                yield return new WaitForSeconds(.1f);
            }

            yield return new WaitForSeconds(1f);

            _gameController.DoAddProgress(3);
            _gameController.CheckClearDimsum(dimsumType);
            foreach (var positionDimsum in positionDimsums)
            {
                if (positionDimsum == null) continue;
                positionDimsum.BackToBottom();
                positionDimsum.RemoveFromDropPlace();
                dimsumSpawner.Remove(positionDimsum);
            }
        }
        finally
        {
            Finish();
        }
    }

    public void DoAnimateRefresh()
    {
        boxParticle.gameObject.SetActive(false);
        this.gameObject.SetActive(true);

        boxSprite.sprite = reloadSprite;
        boxAnimation.SetTrigger(OpenUp);

        StartCoroutine(DoAnimateRefreshRoutine());
    }

    private IEnumerator DoAnimateRefreshRoutine()
    {
        try
        {
            yield return new WaitForSeconds(1f);

            boxParticle.gameObject.SetActive(true);
            boxParticle.localScale = Vector3.zero;
            boxParticle.DOScale(Vector3.one * 0.3f, 0.5f);

            yield return new WaitForSeconds(.5f);
        }
        finally
        {
            Finish();
        }
    }

    public void DoAnimateFreezeTimer()
    {
        boxParticle.gameObject.SetActive(false);
        this.gameObject.SetActive(true);

        boxSprite.sprite = timerSprite;
        boxAnimation.SetTrigger(OpenUp);

        StartCoroutine(DoAnimateFreezeTimerRoutine());
    }

    private IEnumerator DoAnimateFreezeTimerRoutine()
    {
        try
        {
            yield return new WaitForSeconds(1f);

            boxParticle.gameObject.SetActive(true);
            boxParticle.localScale = Vector3.zero;
            boxParticle.DOScale(Vector3.one * 0.3f, 0.5f);

            yield return new WaitForSeconds(.5f);
        }
        finally
        {
            Finish();
        }
    }

    /// <summary>At least one dim sum is still there to animate.</summary>
    private static bool HasTarget(MDimSum[] positionDimsums)
    {
        if (positionDimsums == null) return false;
        foreach (var positionDimsum in positionDimsums)
        {
            if (positionDimsum != null) return true;
        }
        return false;
    }

    /// <summary>Type of the first surviving target, or -1 when they have all gone.</summary>
    private static int FirstDimsumType(MDimSum[] positionDimsums)
    {
        if (positionDimsums == null) return -1;
        foreach (var positionDimsum in positionDimsums)
        {
            if (positionDimsum != null) return positionDimsum.dimsumType;
        }
        return -1;
    }

    /// <summary>
    /// Switches the effect off. Every routine ends here, and does so from a finally block, because
    /// this object being active is exactly what <c>GameController.PowerupRunning</c> reads: a
    /// routine that dies part way through leaves the whole power-up row - and the "+" badges with
    /// it - greyed out until the level is reloaded. One bad frame should cost an animation, not
    /// the rest of the level.
    /// </summary>
    private void Finish()
    {
        this.gameObject.SetActive(false);
    }
}
