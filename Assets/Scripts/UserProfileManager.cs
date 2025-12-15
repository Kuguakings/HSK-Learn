using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Runtime.InteropServices;

/// <summary>
/// 管理主菜单右上角的"用户信息面板"。
/// </summary>
public class UserProfileManager : MonoBehaviour, IPointerClickHandler
{
    [Header("UI 组件")]
    public TextMeshProUGUI usernameText; // 用于显示昵称的 Text
    public TextMeshProUGUI roleText;     // 用于显示角色(管理员/学员)的 Text

    [Header("角色颜色")]
    public Color superAdminColor = new Color(1f, 0.84f, 0f); // 金色
    public Color adminColor = new Color(0f, 0.8f, 1f);       // 蓝色
    public Color userColor = new Color(0.8f, 0.8f, 0.8f);    // 灰色

    // 调用 Native Prompt (原来代码)
    [DllImport("__Internal")]
    private static extern void JsShowNativePrompt(string existingText, string objectName, string callbackSuccess);

    // WebGL专用：调用JavaScript的原生输入框 / WebGL only: Call JavaScript native input
    [DllImport("__Internal")]
    private static extern void JsShowNativePrompt(string existingText, string objectName, string callbackSuccess);

    // WebGL专用：调用JavaScript的原生输入框 / WebGL only: Call JavaScript native input
    [DllImport("__Internal")]
    private static extern void JsShowNativePrompt(string existingText, string objectName, string callbackSuccess);

    void Start()
    {
        // 初始化更新一次 UI
        UpdateUI();
    }

    public void UpdateUI()
    {
        // 1. 更新用户名
        if (!string.IsNullOrEmpty(TcbManager.CurrentNickname))
        {
            usernameText.text = TcbManager.CurrentNickname;
        }
        else
        {
            usernameText.text = "加载中...";
        }

        // 2. 更新角色/权限显示
        if (TcbManager.IsAdmin)
        {
            if (TcbManager.AdminLevel >= 999)
            {
                roleText.text = "超级管理员";
                roleText.color = superAdminColor;
                usernameText.color = superAdminColor;
            }
            else
            {
                roleText.text = "管理员";
                roleText.color = adminColor;
                usernameText.color = adminColor;
            }
        }
        else
        {
            roleText.text = "学员";
            roleText.color = userColor;
            usernameText.color = Color.white; // 普通用户名为白色
        }
    }

    // 处理点击事件
    public void OnPointerClick(PointerEventData eventData)
    {
        // 只有右键才触发改名
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Debug.Log("右键点击用户头像，打开改名窗口...");
            string currentName = TcbManager.CurrentNickname;

#if UNITY_WEBGL && !UNITY_EDITOR
<<<<<<< Updated upstream
<<<<<<< Updated upstream
            // 调用原来代码方式
            JsShowNativePrompt(currentName, gameObject.name, "OnReceiveNewName");
#else
            Debug.LogWarning("编辑器不支持 WebGL 原生输入框功能");
=======
=======
>>>>>>> Stashed changes
            // WebGL构建：调用JavaScript原生输入框 / WebGL build: Call JavaScript native input
            JsShowNativePrompt(currentName, gameObject.name, "OnReceiveNewName");
#else
            Debug.LogWarning("编辑器不支持 WebGL 原生输入框功能 / Editor doesn't support WebGL native input");
<<<<<<< Updated upstream
>>>>>>> Stashed changes
=======
>>>>>>> Stashed changes
#endif
        }
    }

    // 接收 JS 回调的函数
    public void OnReceiveNewName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;

        // 限制长度，防止 UI 溢出
        if (newName.Length > 12) newName = newName.Substring(0, 12);

        // 先更新UI，让用户立即看到反馈
        usernameText.text = newName;

        // 同步到后端数据库
        if (TcbManager.instance != null)
        {
            TcbManager.instance.RequestUpdateUsername(newName);
        }
    }
}