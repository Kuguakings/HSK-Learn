/// <summary>
/// 词语方块组件 / Word Tile Component
/// 处理单个方块的点击、选中状态、动画效果 / Handles individual tile clicks, selection state, and animations
/// </summary>
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Button), typeof(CanvasGroup), typeof(LayoutElement))]
public class WordTile : MonoBehaviour, IPointerClickHandler
{
    [Header("组件引用 / Component References")]
    [SerializeField] private TextMeshProUGUI wordText;       // 显示文字的组件 / Text display component
    [SerializeField] private Image backgroundImage;          // 背景图片 / Background image

    [Header("状态颜色 / State Colors")]
    [SerializeField] private Color selectedColor = new Color(1f, 0.92f, 0.016f);  // 选中颜色(黄色) / Selected color (yellow)
    [SerializeField] private Color errorColor = new Color(1f, 0.4f, 0.4f);        // 错误颜色(红色) / Error color (red)
    private Color normalColor = Color.white;  // 默认颜色 / Default color - 修复：初始化默认值 / Fix: Initialize with default value

    // 公开数据 / Public Data
    public TileInfo info;  // 方块信息 / Tile information
    
    // 内部引用 / Internal References
    private GameManager gameManager;
    private Button button;
    private CanvasGroup canvasGroup;
    private bool isSelected = false;  // 是否被选中 / Is selected

    /// <summary>
    /// 初始化组件引用 / Initialize Component References
    /// </summary>
    private void Awake()
    {
        button = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        // 修复：安全获取背景颜色，提供默认值 / Fix: Safely get background color with default
        if (backgroundImage != null)
        {
            normalColor = backgroundImage.color;
        }
        else
        {
            Debug.LogWarning($"WordTile [{gameObject.name}]: backgroundImage为空，使用默认白色 / backgroundImage is null, using default white color");
        }
    }

    /// <summary>
    /// 设置方块数据 / Setup Tile Data
    /// </summary>
    /// <param name="tileInfo">方块信息 / Tile information</param>
    /// <param name="manager">游戏管理器引用 / Game manager reference</param>
    /// <param name="isVisible">是否可见 / Whether visible</param>
    public void Setup(TileInfo tileInfo, GameManager manager, bool isVisible = true)
    {
        this.gameManager = manager;
        
        // 如果不可见，直接禁用 / If not visible, disable directly
        if (!isVisible)
        {
            gameObject.SetActive(false);
            return;
        }
        
        // 设置数据并显示文字 / Set data and display text
        this.info = tileInfo;
        if (wordText != null)
        {
            wordText.text = info.text;
        }
        gameObject.name = "Tile_" + info.text;
    }

    /// <summary>
    /// 处理点击事件 / Handle Click Event
    /// 左键选中，右键取消选中 / Left click to select, right click to deselect
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 检查有效性 / Check validity
        if (gameManager == null || !button.interactable) return;
        
        // 左键: 选中 / Left click: Select
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (!isSelected)
            {
                SetVisualSelected(true);
                gameManager.OnTileSelected(this);
            }
        }
        // 右键: 取消选中 / Right click: Deselect
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (isSelected)
            {
                SetVisualSelected(false);
                gameManager.OnTileDeselected(this);
            }
        }
    }

    /// <summary>
    /// 设置选中状态的视觉表现 / Set Visual Selected State
    /// </summary>
    public void SetVisualSelected(bool selected)
    {
        isSelected = selected;
        if (backgroundImage != null)
        {
            backgroundImage.color = isSelected ? selectedColor : normalColor;
        }
    }

    /// <summary>
    /// 设置是否可互动 / Set Interactable State
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    /// <summary>
    /// 错误闪烁动画 / Error Flash Animation
    /// 当选择错误时，方块会闪烁红色 / Flash red when incorrect selection
    /// </summary>
    public IEnumerator FlashError()
    {
        SetInteractable(false);
        backgroundImage.color = errorColor;
        yield return new WaitForSeconds(0.2f);
        backgroundImage.color = normalColor;
        yield return new WaitForSeconds(0.2f);
        backgroundImage.color = errorColor;
        yield return new WaitForSeconds(0.2f);
        SetVisualSelected(false);
        SetInteractable(true);
        gameManager.OnTileDeselected(this);
    }

    /// <summary>
    /// 淡出动画 / Fade Out Animation
    /// 当匹配成功时，方块逐渐透明并消失 / Gradually fade and disappear when matched
    /// </summary>
    public IEnumerator FadeOut(float duration)
    {
        SetInteractable(false);
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1.0f - (elapsedTime / duration);
            yield return null;
        }
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 移动动画 / Move Animation
    /// 带有位置、旋转、缩放的缓动效果 / Smooth animation with position, rotation, and scale
    /// </summary>
    public IEnumerator MoveTo(Vector3 targetPosition, float duration)
    {
        // 禁用布局系统，自己控制位置 / Disable layout system, control position manually
        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement != null) layoutElement.ignoreLayout = true;

        // 记录初始状态 / Record initial state
        Vector3 startPosition = transform.position;
        Vector3 startScale = transform.localScale;
        Quaternion startRotation = transform.rotation;

        Vector3 popScale = startScale * 1.15f; // 动画过程中会稍微变大一点 / Slightly bigger during animation
        Quaternion targetRotation = Quaternion.Euler(0, 0, Random.Range(-5f, 5f)); // 随机微小旋转 / Random small rotation

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            // 平滑缓动曲线 (Ease Out) / Smooth easing curve
            float easedT = Mathf.Sin(t * Mathf.PI * 0.5f);

            // 位置动画 / Position animation
            transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, easedT);

            // 旋转和缩放动画 (先变大再恢复) / Rotation and scale animation (pop effect)
            if (t < 0.5f)
            {
                // 前半段：变大 / First half: scale up
                transform.localScale = Vector3.Lerp(startScale, popScale, t * 2);
            }
            else
            {
                // 后半段：恢复 / Second half: scale back
                transform.localScale = Vector3.Lerp(popScale, startScale, (t - 0.5f) * 2);
            }
            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, easedT);

            yield return null;
        }

        // 确保动画结束时所有状态都恢复正常 / Ensure all states are reset when animation ends
        transform.position = targetPosition;
        transform.localScale = startScale;
        transform.rotation = Quaternion.identity; // 恢复为无旋转状态 / Reset to no rotation

        if (layoutElement != null) layoutElement.ignoreLayout = false;
    }
}