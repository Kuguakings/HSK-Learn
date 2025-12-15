// UIElementAnimator.cs (只负责动画的最终版)
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class UIElementAnimator : MonoBehaviour
{
    [Tooltip("元素入场动画的持续时间（秒）")]
    public float animationDuration = 0.4f;

    private CanvasGroup canvasGroup;
    private Vector3 originalScale;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        originalScale = transform.localScale;
    }

    public IEnumerator AnimateInCoroutine()
    {
        // 【修改】移除了播放音效的代码
        float elapsedTime = 0f;
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / animationDuration);
            canvasGroup.alpha = t;
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        transform.localScale = originalScale;
    }
}