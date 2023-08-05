using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.VisualScripting;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] Image HPFillFist;
    [SerializeField] Image HPFillAfter;
    [SerializeField] Image StaminaFill;
    protected override void Awake()
    {
        MakeSingleton(false);
    }
    public void HeathBarUI(float current,float max)
    {
        if(current < 0) current = 0;
        float ratio = current / max;
        HPFillFist.DOFillAmount(ratio, 0.2f);
        StartCoroutine(FillHeathAfter(ratio));
    }
    IEnumerator FillHeathAfter(float ratio)
    {
        yield return new WaitForSeconds(0.3f);
        HPFillAfter.DOFillAmount(ratio, 0.6f);
    }
    public void StaminaBarUI(float current, float max)
    {
        float ratio = current / max;
        StaminaFill.DOFillAmount(ratio, 0.2f);
    }

}
