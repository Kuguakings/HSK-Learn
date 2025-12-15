using UnityEngine;
using TMPro;
using System;
using UnityEngine.EventSystems; // 引入事件系统

public class CommentItem : MonoBehaviour, IPointerClickHandler
{
    public TextMeshProUGUI userText;
    public TextMeshProUGUI contentText;
    public TextMeshProUGUI timeText;

    private AnnouncementComment data;
    // 回调：告诉 Manager 哪条评论被右键点了
    private Action<AnnouncementComment> onRightClickCallback;

    public void Setup(AnnouncementComment data, Action<AnnouncementComment> onRightClick)
    {
        this.data = data;
        this.onRightClickCallback = onRightClick;

        userText.text = data.userNickname + ":";

        // --- 【核心逻辑】变色与小尾巴 ---
        if (!string.IsNullOrEmpty(data.modifiedInfo))
        {
            // 是被管理员修改过的：黄色字 + 尾巴
            contentText.text = $"<color=yellow>{data.content} (由 {data.modifiedInfo} 修改)</color>";
        }
        else
        {
            // 普通评论：原样显示
            contentText.text = data.content;
        }
        // -----------------------------

        DateTime dt = DateTimeOffset.FromUnixTimeSeconds(data.createdAt).LocalDateTime;
        timeText.text = dt.ToString("MM.dd HH:mm");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 只响应右键
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            onRightClickCallback?.Invoke(this.data);
        }
    }
}