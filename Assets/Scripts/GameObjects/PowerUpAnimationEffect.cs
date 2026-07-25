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
        boxParticle.gameObject.SetActive(false);
        this.gameObject.SetActive(true);
        
        boxSprite.sprite = magnifierSprite;
        boxAnimation.SetTrigger(OpenUp);

        StartCoroutine(DoAnimateMagnifierRoutine(positionDimsums));
    }

    private IEnumerator DoAnimateMagnifierRoutine(MDimSum[] positionDimsums)
    {
        yield return new WaitForSeconds(1f);
        
        boxParticle.gameObject.SetActive(true);
        boxParticle.localScale = Vector3.zero;
        boxParticle.DOScale(Vector3.one * 0.3f, 0.5f);
        
        yield return new WaitForSeconds(.2f); 
        foreach (var positionDimsum in positionDimsums)
        {
            positionDimsum.DrawToTop();
            positionDimsum.transform.DOShakeRotation(3f, 30f);
            SoundController.Instance.PlayBubbleSoundClips();
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(1f);
        
        foreach (var positionDimsum in positionDimsums)
        {
            positionDimsum.BackToBottom();
        }
        this.gameObject.SetActive(false);
    }
    
    public void DoAnimateSuckPower(MDimSum[] positionDimsums)
    {
        boxParticle.gameObject.SetActive(false);
        this.gameObject.SetActive(true);
        
        boxSprite.sprite = packageSprite;
        boxAnimation.SetTrigger(OpenUp);

        StartCoroutine(DoAnimateSuckPowerRoutine(positionDimsums));
    }

    private IEnumerator DoAnimateSuckPowerRoutine(MDimSum[] positionDimsums)
    {
        yield return new WaitForSeconds(1f);

        int dimsumType = positionDimsums[0].dimsumType;
        
        boxParticle.gameObject.SetActive(true);
        boxParticle.localScale = Vector3.zero;
        boxParticle.DOScale(Vector3.one * 0.3f, 0.5f);
        
        yield return new WaitForSeconds(.2f); 
        foreach (var positionDimsum in positionDimsums)
        {
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
            positionDimsum.BackToBottom();
            positionDimsum.RemoveFromDropPlace();
            dimsumSpawner.Remove(positionDimsum);
        }
        this.gameObject.SetActive(false);
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
        yield return new WaitForSeconds(1f);
        
        boxParticle.gameObject.SetActive(true);
        boxParticle.localScale = Vector3.zero;
        boxParticle.DOScale(Vector3.one * 0.3f, 0.5f);
        
        yield return new WaitForSeconds(.5f); 
        this.gameObject.SetActive(false);
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
        yield return new WaitForSeconds(1f);
        
        boxParticle.gameObject.SetActive(true);
        boxParticle.localScale = Vector3.zero;
        boxParticle.DOScale(Vector3.one * 0.3f, 0.5f);
        
        yield return new WaitForSeconds(.5f);
        this.gameObject.SetActive(false);
    }
}
