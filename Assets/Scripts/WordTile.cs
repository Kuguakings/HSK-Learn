using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Button), typeof(CanvasGroup), typeof(LayoutElement))]
public class WordTile : MonoBehaviour, IPointerClickHandler
{
    [Header("组件引用")]
    [SerializeField] private TextMeshProUGUI wordText;
    [SerializeField] private Image backgroundImage;

    [Header("状态颜色")]
    [SerializeField] private Color selectedColor = new Color(1f, 0.92f, 0.016f);
    [SerializeField] private Color errorColor = new Color(1f, 0.4f, 0.4f);
    private Color normalColor;

    public TileInfo info;
    private GameManager gameManager;
    private Button button;
    private CanvasGroup canvasGroup;
    private bool isSelected = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (backgroundImage != null)
        {
            normalColor = backgroundImage.color;
        }
    }

    public void Setup(TileInfo tileInfo, GameManager manager, bool isVisible = true)
    {
        this.gameManager = manager;
        if (!isVisible)
        {
            gameObject.SetActive(false);
            return;
        }
        this.info = tileInfo;
        if (wordText != null)
        {
            wordText.text = info.text;
        }
        gameObject.name = "Tile_" + info.text;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameManager == null || !button.interactable) return;
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (!isSelected)
            {
                SetVisualSelected(true);
                gameManager.OnTileSelected(this);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (isSelected)
            {
                SetVisualSelected(false);
                gameManager.OnTileDeselected(this);
            }
        }
    }

    public void SetVisualSelected(bool selected)
    {
        isSelected = selected;
        if (backgroundImage != null)
        {
            backgroundImage.color = isSelected ? selectedColor : normalColor;
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

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

    // --- 【最终版】MoveTo 函数，包含位置、旋转、缩放的缓动动画 ---
    public IEnumerator MoveTo(Vector3 targetPosition, float duration)
    {
        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement != null) layoutElement.ignoreLayout = true;

        Vector3 startPosition = transform.position;
        Vector3 startScale = transform.localScale;
        Quaternion startRotation = transform.rotation;

        Vector3 popScale = startScale * 1.15f; // 动画过程中会稍微变大一点
        Quaternion targetRotation = Quaternion.Euler(0, 0, Random.Range(-5f, 5f)); // 随机一个微小的旋转角度

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            // 这是一个平滑的缓动曲线 (Ease Out)
            float easedT = Mathf.Sin(t * Mathf.PI * 0.5f);

            // 位置动画
            transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, easedT);

            // 旋转和缩放动画 (先变大再恢复)
            if (t < 0.5f)
            {
                // 前半段：变大
                transform.localScale = Vector3.Lerp(startScale, popScale, t * 2);
            }
            else
            {
                // 后半段：恢复
                transform.localScale = Vector3.Lerp(popScale, startScale, (t - 0.5f) * 2);
            }
            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, easedT);

            yield return null;
        }

        // 确保动画结束时所有状态都恢复正常
        transform.position = targetPosition;
        transform.localScale = startScale;
        transform.rotation = Quaternion.identity; // 恢复为无旋转状态

        if (layoutElement != null) layoutElement.ignoreLayout = false;
    }
}