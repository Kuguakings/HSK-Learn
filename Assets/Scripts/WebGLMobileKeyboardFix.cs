// WebGLMobileKeyboardFix.cs (V4 - 【【【修复了 Awake() 崩溃的最终版】】】)
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_InputField))]
public class WebGLMobileKeyboardFix : MonoBehaviour, IPointerDownHandler
{
    private TMP_InputField inputField;
    private TouchScreenKeyboard keyboard;
    private bool keyboardActive = false;

    // 【【【V4 修复】】】:
    // 我们把 Awake() 换成了 Start()。
    // Start() 只会在这个 GameObject 被激活后（即登录画布显示后）才运行，
    // 这就避免了在非激活状态下添加监听 导致的引擎崩溃。
    void Start()
    {
        inputField = GetComponent<TMP_InputField>();

        // 我们在这里“监听” onEndEdit 事件
        inputField.onEndEdit.AddListener(OnEndEditCallback);
    }

    // (PointerDown 逻辑保持不变)
    public void OnPointerDown(PointerEventData eventData)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (Input.touchSupported && keyboardActive == false)
        {
            keyboard = TouchScreenKeyboard.Open(
                inputField.text,
                (TouchScreenKeyboardType)inputField.keyboardType,
                false, 
                inputField.multiLine,
                inputField.contentType == TMP_InputField.ContentType.Password,
                false, 
                inputField.placeholder.GetComponent<TMP_Text>().text
            );

            if (keyboard != null)
            {
                keyboardActive = true;
            }
        }
#endif
    }

    // (Update 逻辑保持不变)
    void Update()
    {
        if (keyboardActive && keyboard != null)
        {
            inputField.text = keyboard.text;
            if (keyboard.status != TouchScreenKeyboard.Status.Visible)
            {
                keyboardActive = false;
                keyboard = null;
            }
        }
    }

    // (回调函数保持不变)
    private void OnEndEditCallback(string text)
    {
        if (keyboard != null)
        {
            keyboard.active = false;
            keyboardActive = false;
            keyboard = null;
        }
    }

    // 【【【V4 修复】】】: 添加 OnDestroy，确保在物体被销毁时移除监听
    void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onEndEdit.RemoveListener(OnEndEditCallback);
        }
    }
}