using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems; // 引入事件系统

// 增加 IPointerClickHandler 接口来处理点击逻辑
public class AnnouncementItem : MonoBehaviour, IPointerClickHandler
{
    [Header("UI 组件")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI publisherText;
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI tagText;
    public GameObject tagBg;
    // public Button clickBtn; // 【删除】我们不用Button组件了，改用脚本直接监听

    private AnnouncementData data;
    private Action<AnnouncementData> onClickCallback; // 单击回调
    private Action<AnnouncementData> onEditCallback;  // 双击/右键回调

    public void Setup(AnnouncementData data, Action<AnnouncementData> onClick, Action<AnnouncementData> onEdit)
    {
        this.data = data;
        this.onClickCallback = onClick;
        this.onEditCallback = onEdit;

        if (titleText) titleText.text = data.title;

        if (publisherText) publisherText.text = "发布人: " + (string.IsNullOrEmpty(data.authorName) ? "管理员" : data.authorName);

        // 【修改点 1】时间精确到分
        DateTime dt = DateTimeOffset.FromUnixTimeSeconds(data.updatedAt).LocalDateTime;
        if (dateText) dateText.text = dt.ToString("yyyy/MM/dd HH:mm");

        // 标签逻辑
        if (data.tags != null && data.tags.Count > 0 && !string.IsNullOrEmpty(data.tags[0]))
        {
            if (tagBg) tagBg.SetActive(true);
            if (tagText) tagText.text = data.tags[0];
        }
        else
        {
            if (tagBg) tagBg.SetActive(false);
        }
    }

    // 【修改点 2】智能点击检测
    public void OnPointerClick(PointerEventData eventData)
    {
        // 1. 如果是右键，且是管理员 -> 进入编辑
        if (TcbManager.IsAdmin && eventData.button == PointerEventData.InputButton.Right)
        {
            Debug.Log("触发编辑: " + data.title);
            onEditCallback?.Invoke(this.data);
        }
        // 2. 否则（左键单击） -> 进入详情
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            onClickCallback?.Invoke(this.data);
        }
    }
}