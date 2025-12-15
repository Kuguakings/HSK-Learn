// M2_WordRow.cs (V3.0.4 - Toggle Logic Update)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 【【【模式2 V3.0.4】】】
/// 附加在 M2_WordRow_Prefab (中部“词语编辑”行) 上
/// </summary>
public class M2_WordRow : MonoBehaviour
{
    [Header("UI 引用")]
    public TextMeshProUGUI orderText;
    public TMP_InputField wordInput;
    public Button moveUpButton;
    public Button moveDownButton;
    public Toggle selectionToggle; // <--- 【新增】Toggle 引用

    [Header("选中高亮")]
    public Image selectionHighlight;
    // public Button clickReceiverButton; // <--- 【已移除】旧的点击接收按钮

    private LevelEditorManager editorManager;
    private bool isManagerUpdatingToggle = false; // <--- 【新增】Toggle 锁

    public void Setup(LevelEditorManager manager, int order, string word)
    {
        this.editorManager = manager;

        // 1. 填充数据
        wordInput.text = word;

        // 2. 绑定按钮事件
        if (moveUpButton != null)
        {
            moveUpButton.onClick.AddListener(OnMoveUp);
        }
        if (moveDownButton != null)
        {
            moveDownButton.onClick.AddListener(OnMoveDown);
        }

        // 【新增】绑定 Toggle 事件，替换旧的 clickReceiverButton 逻辑
        if (selectionToggle != null)
        {
            selectionToggle.onValueChanged.AddListener(OnToggleChanged);
        }

        // 3. 绑定“脏检查”
        if (wordInput != null)
        {
            wordInput.onValueChanged.AddListener(OnWordChanged);
        }

        // 4. 默认隐藏高亮
        SetSelected(false);

        // 5. V3.0.3 刷新 Bug 修复 (不变)
        UpdateVisuals(order, false, false);
    }

    private void OnWordChanged(string s)
    {
        if (editorManager != null)
        {
            editorManager.MarkLevelAsDirty();
        }
    }

    // 【新增】Toggle 事件处理器
    private void OnToggleChanged(bool isOn)
    {
        if (isManagerUpdatingToggle) return; // 忽略 Manager 触发的事件

        if (editorManager != null)
        {
            // 通知 Manager 进行“单选”逻辑
            editorManager.M2_OnSelectWordRowToggle(this, isOn);
        }
    }

    private void OnMoveUp()
    {
        if (editorManager != null)
        {
            editorManager.M2_OnRequestMoveWord(this, -1);
        }
    }

    private void OnMoveDown()
    {
        if (editorManager != null)
        {
            editorManager.M2_OnRequestMoveWord(this, 1);
        }
    }

    // 【修改】SetSelected 现在控制 Toggle 状态和锁
    public void SetSelected(bool isSelected)
    {
        // 1. 上锁
        isManagerUpdatingToggle = true;

        // 2. 控制 Toggle 状态
        if (selectionToggle != null)
        {
            selectionToggle.isOn = isSelected;
        }

        // 3. 控制高亮 (使用 .enabled 修复了隐藏整个行的 Bug)
        if (selectionHighlight != null)
        {
            selectionHighlight.enabled = isSelected;
        }

        // 4. 解锁
        isManagerUpdatingToggle = false;
    }

    public void UpdateVisuals(int order, bool isFirst, bool isLast)
    {
        orderText.text = order.ToString();

        if (moveUpButton != null) moveUpButton.interactable = !isFirst;
        if (moveDownButton != null) moveDownButton.interactable = !isLast;
    }

    public string GetWord()
    {
        return wordInput.text;
    }
}