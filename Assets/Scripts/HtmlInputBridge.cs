using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System.Runtime.InteropServices;

[RequireComponent(typeof(TMP_InputField))]
public class HtmlInputBridge : MonoBehaviour, IPointerClickHandler
{
    private TMP_InputField inputField;

    // 【核心修复】引入 Native Prompt
    [DllImport("__Internal")]
    private static extern void JsShowNativePrompt(string existingText, string objectName, string callbackSuccess);

    void Start()
    {
        inputField = GetComponent<TMP_InputField>();
        // 设为只读，点击时只触发我们的弹窗，不触发手机键盘
        inputField.readOnly = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!inputField.interactable) return;

        string currentText = inputField.text;
        string myGameObjectName = gameObject.name;

        Debug.Log($"[HtmlInputBridge] 正在呼叫原生 Prompt: {myGameObjectName}");

#if UNITY_WEBGL && !UNITY_EDITOR
        // 【核心修复】调用原生弹窗
        JsShowNativePrompt(currentText, myGameObjectName, "OnHtmlInputSuccess");
#else
        // 编辑器模式下允许直接输入
        inputField.readOnly = false;
        inputField.ActivateInputField();
#endif
    }

    public void OnHtmlInputSuccess(string newText)
    {
        Debug.Log($"[HtmlInputBridge] 收到返回文本: {newText}");
        inputField.text = newText;
        // 触发事件，通知其他脚本数据变了
        inputField.onValueChanged.Invoke(newText);
        inputField.onEndEdit.Invoke(newText);
    }
}