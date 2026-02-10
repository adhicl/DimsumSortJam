using System.Collections;
using Commons;
using DG.Tweening;
using Models;
using UnityEngine;
using Zenject;

public class PowerUpAnimationEffect : MonoBehaviour
{
    private static readonly int OpenUp = Animator.StringToHash("OpenUp");
    [Inject] GameSetting gameSetting;

    [SerializeField] private Animator boxAnimation;
    [SerializeField] private SpriteRenderer boxSprite;
    [SerializeField] private Transform boxParticle;
    
    public Sprite magnifierSprite;
    public Sprite packageSprite;
    public Sprite reloadSprite;
    
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
            positionDimsum.transform.DOShakeRotation(5f, 30f);
        }

        yield return new WaitForSeconds(2f);
        
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
        
        boxParticle.gameObject.SetActive(true);
        boxParticle.localScale = Vector3.zero;
        boxParticle.DOScale(Vector3.one * 0.3f, 0.5f);
        
        yield return new WaitForSeconds(.2f); 
        foreach (var positionDimsum in positionDimsums)
        {
            positionDimsum.DrawToTop();
            positionDimsum.transform.DOShakeRotation(5f, 30f);
        }

        yield return new WaitForSeconds(2f);
        
        foreach (var positionDimsum in positionDimsums)
        {
            positionDimsum.BackToBottom();
        }
        this.gameObject.SetActive(false);
    }
    
    public void DoAnimateRefresh(MDimSum[] positionDimsums)
    {
        boxParticle.gameObject.SetActive(false);
        this.gameObject.SetActive(true);
        
        boxSprite.sprite = reloadSprite;
        boxAnimation.SetTrigger(OpenUp);

        StartCoroutine(DoAnimateRefreshRoutine(positionDimsums));
    }

    private IEnumerator DoAnimateRefreshRoutine(MDimSum[] positionDimsums)
    {
        yield return new WaitForSeconds(1f);
        
        boxParticle.gameObject.SetActive(true);
        boxParticle.localScale = Vector3.zero;
        boxParticle.DOScale(Vector3.one * 0.3f, 0.5f);
        
        yield return new WaitForSeconds(.2f); 
        // foreach (var positionDimsum in positionDimsums)
        // {
        //     positionDimsum.DrawToTop();
        //     positionDimsum.transform.DOShakeRotation(5f, 0.5f);
        // }

        yield return new WaitForSeconds(2f);
        
        // foreach (var positionDimsum in positionDimsums)
        // {
        //     positionDimsum.BackToBottom();
        // }
        this.gameObject.SetActive(false);
    }
}
