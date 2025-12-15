/// <summary>
/// 可拖拽词语组件 / Draggable Word Component
/// 用于句子排序游戏，允许玩家拖拽词语到句子槽位 / Used for sentence ordering game, allows dragging words to sentence slots
/// </summary>
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class DraggableWord : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // 公开数据 / Public Data
    public string wordText;           // 词语文本内容 / Word text content
    public Transform originalParent;  // 原始父物体（用于返回原位置）/ Original parent (for returning to original position)

    // 内部变量 / Internal Variables
    private Vector3 startPosition;              // 开始拖拽时的位置 / Start position when dragging begins
    private CanvasGroup canvasGroup;            // 画布组（控制射线检测）/ Canvas group (controls raycasting)
    private Canvas rootCanvas;                  // 根画布引用 / Root canvas reference
    private Coroutine animationCoroutine;       // 当前运行的动画协程 / Currently running animation coroutine

    /// <summary>
    /// 初始化组件引用 / Initialize Component References
    /// </summary>
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
        
        // 修复：检查根画布是否存在 / Fix: Check if root canvas exists
        if (rootCanvas == null)
        {
            Debug.LogError($"DraggableWord [{gameObject.name}]: 未找到根Canvas！/ Root Canvas not found!");
        }
    }

    /// <summary>
    /// 开始拖拽事件 / Begin Drag Event
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 如果动画正在运行，禁止拖拽 / If animation is running, prevent dragging
        if (animationCoroutine != null) return;

        startPosition = transform.position;
        
        // 如果不在句子槽位中，记录原始父物体 / If not in sentence slot, record original parent
        if (transform.parent != null && transform.parent.GetComponent<SentenceSlot>() == null)
        {
            originalParent = transform.parent;
        }

        // 修复：检查rootCanvas是否为空 / Fix: Check if rootCanvas is null
        if (rootCanvas != null)
        {
            transform.SetParent(rootCanvas.transform, true);
        }
        else
        {
            Debug.LogWarning($"DraggableWord [{gameObject.name}]: rootCanvas为空，无法移动到根画布 / rootCanvas is null, cannot move to root");
        }
        
        canvasGroup.blocksRaycasts = false;  // 允许射线穿过，以便检测下方的槽位 / Allow raycasts to pass through to detect slots below
    }

    /// <summary>
    /// 拖拽过程事件 / Dragging Event
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (animationCoroutine != null) return;
        transform.position = eventData.position;  // 跟随鼠标位置 / Follow mouse position
    }

    /// <summary>
    /// 结束拖拽事件 / End Drag Event
    /// 如果没有放入有效槽位，返回原位置 / If not placed in valid slot, return to original position
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (animationCoroutine != null) return;

        // 修复：添加空引用检查 / Fix: Add null reference checks
        // 如果还在rootCanvas下，说明没有放在有效槽位上，需要返回原位置
        // If still under rootCanvas, not placed in valid slot, need to return to original
        if (rootCanvas != null && transform.parent == rootCanvas.transform)
        {
            if (originalParent != null)
            {
                transform.SetParent(originalParent);
            }
            else
            {
                Debug.LogWarning($"DraggableWord [{gameObject.name}]: originalParent为空，无法返回原位置 / originalParent is null, cannot return to original position");
            }
        }
        
        canvasGroup.blocksRaycasts = true;  // 恢复射线检测 / Restore raycast blocking
    }

    /// <summary>
    /// 动画式移动到目标位置 / Animate Move to Target Position
    /// 用于自动移动到指定位置的平滑动画 / Smooth animation for auto-moving to specified position
    /// </summary>
    public void AnimateMoveTo(Vector3 targetPosition, float duration)
    {
        // 如果有动画正在运行，停止它 / If animation is running, stop it
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(MoveCoroutine(targetPosition, duration));
    }

    /// <summary>
    /// 移动协程 / Move Coroutine
    /// 执行平滑移动动画 / Execute smooth move animation
    /// </summary>
    private IEnumerator MoveCoroutine(Vector3 targetPosition, float duration)
    {
        Vector3 startPos = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / duration); // 使用平滑插值 / Use smooth interpolation
            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        // 确保精确到达目标位置 / Ensure exact arrival at target position
        transform.position = targetPosition;
        animationCoroutine = null; // 清理引用 / Clear reference
    }
}