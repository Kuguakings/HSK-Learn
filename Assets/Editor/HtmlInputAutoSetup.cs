using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System.Runtime.InteropServices;

[RequireComponent(typeof(TMP_InputField))]
public class HtmlInputBridge : MonoBehaviour, IPointerClickHandler
{
    private TMP_InputField inputField;

    // 【核心修复：引入 Native Prompt】
    [DllImport("__Internal")]
    private static extern void JsShowNativePrompt(string existingText, string objectName, string callbackSuccess);

    void Start()
    {
        inputField = GetComponent<TMP_InputField>();
        // 保持只读，这样点击时不会触发 Unity 自己的虚拟键盘，而是触发我们的弹窗
        inputField.readOnly = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!inputField.interactable) return;

        string currentText = inputField.text;
        string myGameObjectName = gameObject.name;

        Debug.Log($"[HtmlInputBridge] 正在呼叫原生 Prompt，目标: {myGameObjectName}");

#if UNITY_WEBGL && !UNITY_EDITOR
        // 调用 Native Prompt
        JsShowNativePrompt(currentText, myGameObjectName, "OnHtmlInputSuccess");
#else
        Debug.Log("【编辑器模式】临时解开只读模式。");
        inputField.readOnly = false;
        inputField.ActivateInputField();
#endif
    }

    public void OnHtmlInputSuccess(string newText)
    {
        Debug.Log($"[HtmlInputBridge] 收到 Prompt 返回文本: {newText}");
        inputField.text = newText;

        // 手动触发 Unity 的事件，确保其他脚本知道值变了
        inputField.onValueChanged.Invoke(newText);
        inputField.onEndEdit.Invoke(newText);
    }
}