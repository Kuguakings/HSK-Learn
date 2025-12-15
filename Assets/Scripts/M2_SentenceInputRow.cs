// M2_SentenceInputRow.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 模式2编辑器 - 句子输入行组件
/// 绑定到 M2_SentenceInputRow_Prefab 上
/// </summary>
public class M2_SentenceInputRow : MonoBehaviour
{
    [Header("UI 引用")]
    public TextMeshProUGUI sentenceIdText;  // 显示句子ID
    public TMP_InputField sentenceInput;     // 句子输入框
    public Toggle selectionToggle;           // 选择Toggle
    public Button deleteButton;              // 删除按钮（如果有）

    [Header("选择高亮")]
    public Image selectionHighlight;

    private LevelEditorManager editorManager;
    private int sentenceId;
    private bool isManagerUpdatingToggle = false;

    /// <summary>
    /// 初始化设置
    /// </summary>
    public void Setup(LevelEditorManager manager, int id, Mode2Content data)
    {
        this.editorManager = manager;
        this.sentenceId = id;

        // 1. 设置ID显示
        if (sentenceIdText != null)
        {
            sentenceIdText.text = id.ToString();
        }

        // 2. 设置句子内容
        if (sentenceInput != null)
        {
            // 如果有数据，使用第一个单词作为完整句子的代表
            // 实际上应该从所有相同sentenceId的words拼接而来
            sentenceInput.text = data != null ? data.fullSentence : "";
            
            // 绑定输入变化事件
            sentenceInput.onValueChanged.AddListener(OnSentenceChanged);
        }

        // 3. 绑定Toggle事件
        if (selectionToggle != null)
        {
            selectionToggle.onValueChanged.AddListener(OnToggleChanged);
        }

        // 4. 绑定删除按钮（如果有）
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(OnDeleteClicked);
        }

        // 5. 默认不选中
        SetSelected(false);
    }

    private void OnSentenceChanged(string newText)
    {
        if (editorManager != null)
        {
            editorManager.MarkLevelAsDirty();
        }
    }

    private void OnToggleChanged(bool isOn)
    {
        if (isManagerUpdatingToggle) return;

        if (editorManager != null)
        {
            editorManager.M2_OnSentenceToggleChanged(this, isOn);
        }
    }

    private void OnDeleteClicked()
    {
        // TODO: 实现删除逻辑
        if (editorManager != null)
        {
            // editorManager.M2_OnRequestDeleteSentence(this);
        }
    }

    /// <summary>
    /// 获取句子ID
    /// </summary>
    public int GetSentenceId()
    {
        return sentenceId;
    }

    /// <summary>
    /// 获取完整句子内容
    /// </summary>
    public string GetFullSentence()
    {
        return sentenceInput != null ? sentenceInput.text : "";
    }

    /// <summary>
    /// 设置选中状态（由Manager调用）
    /// </summary>
    public void SetSelected(bool selected)
    {
        isManagerUpdatingToggle = true;
        
        if (selectionToggle != null)
        {
            selectionToggle.isOn = selected;
        }

        if (selectionHighlight != null)
        {
            selectionHighlight.gameObject.SetActive(selected);
        }

        isManagerUpdatingToggle = false;
    }

    /// <summary>
    /// 设置完整句子文本
    /// </summary>
    public void SetFullSentenceText(string text)
    {
        if (sentenceInput != null)
        {
            sentenceInput.text = text;
        }
    }

    /// <summary>
    /// 更新显示（如果需要）
    /// </summary>
    public void UpdateDisplay(int newId, string newSentence)
    {
        sentenceId = newId;
        
        if (sentenceIdText != null)
        {
            sentenceIdText.text = newId.ToString();
        }

        if (sentenceInput != null)
        {
            sentenceInput.text = newSentence;
        }
    }

    private void OnDestroy()
    {
        // 清理事件监听
        if (sentenceInput != null)
        {
            sentenceInput.onValueChanged.RemoveListener(OnSentenceChanged);
        }

        if (selectionToggle != null)
        {
            selectionToggle.onValueChanged.RemoveListener(OnToggleChanged);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveListener(OnDeleteClicked);
        }
    }
}
