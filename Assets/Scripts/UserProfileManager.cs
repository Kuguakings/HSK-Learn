using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Runtime.InteropServices;

/// <summary>
/// 挂载在主菜单右上角的“用户信息面板”上
/// </summary>
public class UserProfileManager : MonoBehaviour, IPointerClickHandler
{
    [Header("UI 引用")]
    public TextMeshProUGUI usernameText; // 拖拽显示昵称的 Text
    public TextMeshProUGUI roleText;     // 拖拽显示身份(管理员/学员)的 Text

    [Header("颜色配置")]
    public Color superAdminColor = new Color(1f, 0.84f, 0f); // 金色
    public Color adminColor = new Color(0f, 0.8f, 1f);       // 蓝色
    public Color userColor = new Color(0.8f, 0.8f, 0.8f);    // 灰色

    // 【核心修复：引入 Native Prompt (原生弹窗)】
    [DllImport("__Internal")]
    private static extern void JsShowNativePrompt(string existingText, string objectName, string callbackSuccess);

    void Start()
    {
        // 初始更新一次 UI
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
            usernameText.color = Color.white; // 普通用户名字白色
        }
    }

    // 处理点击事件
    public void OnPointerClick(PointerEventData eventData)
    {
        // 只有右键点击才触发改名
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Debug.Log("右键点击用户头像，打开改名框...");
            string currentName = TcbManager.CurrentNickname;

#if UNITY_WEBGL && !UNITY_EDITOR
            // 【修复】调用原生浏览器输入框
            JsShowNativePrompt(currentName, gameObject.name, "OnReceiveNewName");
#else
            Debug.LogWarning("编辑器不支持 WebGL 输入框，请打包测试");
#endif
        }
    }

    // 接收 JS 返回的新名字
    public void OnReceiveNewName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;

        // 简单限制长度，防止 UI 爆掉
        if (newName.Length > 12) newName = newName.Substring(0, 12);

        // 本地先更新，让用户感觉很快
        usernameText.text = newName;

        // 发送给后端保存
        if (TcbManager.instance != null)
        {
            TcbManager.instance.RequestUpdateUsername(newName);
        }
    }
}