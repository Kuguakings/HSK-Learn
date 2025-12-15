// DraggableWord.cs (新版)
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class DraggableWord : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string wordText;
    public Transform originalParent; // 【修改】改为public，方便管理器访问

    private Vector3 startPosition;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private Coroutine animationCoroutine; // 【新增】用于控制动画协程

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (animationCoroutine != null) return; // 【新增】如果正在动画中，则不允许拖拽

        startPosition = transform.position;
        if (transform.parent.GetComponent<SentenceSlot>() == null)
        {
            originalParent = transform.parent;
        }

        transform.SetParent(rootCanvas.transform, true);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (animationCoroutine != null) return;
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (animationCoroutine != null) return;

        // 【修改】拖拽结束后，如果父物体还是Canvas（意味着没有落在有效槽位上）
        // 则让它停留在当前位置，但父物体回归到原始的生成区
        if (transform.parent == rootCanvas.transform)
        {
            transform.SetParent(originalParent);
        }
        canvasGroup.blocksRaycasts = true;
    }

    // 【新增】一个带动画的移动方法
    public void AnimateMoveTo(Vector3 targetPosition, float duration)
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(MoveCoroutine(targetPosition, duration));
    }

    private IEnumerator MoveCoroutine(Vector3 targetPosition, float duration)
    {
        Vector3 startPos = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / duration); // 使用平滑曲线
            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
        animationCoroutine = null; // 动画结束
    }
}