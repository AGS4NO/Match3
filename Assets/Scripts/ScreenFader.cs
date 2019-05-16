using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(MaskableGraphic))]
public class ScreenFader : MonoBehaviour
{
    public float clearAlpha = 0f;
    public float solidAlpha = 1f;
    public float delay = 0f;
    public float timeToFade = 1f;

    private MaskableGraphic mGraphic;

    // Use this for initialization
    private void Start()
    {
        mGraphic = GetComponent<MaskableGraphic>();
    }

    public void FadeOff()
    {
        StartCoroutine(FadeRoutine(clearAlpha));
    }

    public void FadeOn()
    {
        StartCoroutine(FadeRoutine(solidAlpha));
    }

    private IEnumerator FadeRoutine(float alpha)
    {
        yield return new WaitForSeconds(delay);

        mGraphic.CrossFadeAlpha(alpha, timeToFade, true);
    }
}