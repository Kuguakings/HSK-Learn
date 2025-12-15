// M2_SentenceInputRow.cs (V3.0.4 - 最终稳定版)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class M2_SentenceInputRow : MonoBehaviour
{
    [Header("UI 引用")]
    public TMP_InputField sentenceIdInput;
    public TMP_InputField fullSentenceInput;
    public Toggle selectionToggle; // 【新】Toggle 引用

    [Header("选中高亮")]
    public Image selectionHighlight;

    private LevelEditorManager editorManager;
    private int internal_sentenceId;
    private string internal_fullSentence; // 【新】用于实时记录输入的私有变量

    private bool isManagerUpdatingToggle = false; // 【新】防止 Toggle 事件循环触发的锁

    void Awake()
    {
        if (fullSentenceInput != null)
        {
            fullSentenceInput.lineType = TMP_InputField.LineType.MultiLineSubmit;
        }
    }

    public void Setup(LevelEditorManager manager, int id, Mode2Content data)
    {
        this.editorManager = manager;
        this.internal_sentenceId = id;

        // 1. 填充数据并初始化内部变量
        sentenceIdInput.text = id.ToString();
        if (data != null)
        {
            fullSentenceInput.text = data.fullSentence;
            internal_fullSentence = data.fullSentence; // 【修复点】
        }
        else
        {
            fullSentenceInput.text = "";
            internal_fullSentence = ""; // 【修复点】
        }

        // 2. 绑定“脏检查”和“实时记录”
        if (fullSentenceInput != null)
        {
            fullSentenceInput.onValueChanged.AddListener(OnSentenceChanged);
        }

        // 3. 绑定 Toggle 事件
        if (selectionToggle != null)
        {
            selectionToggle.onValueChanged.AddListener(OnToggleChanged);
        }

        SetToggleState(false);
    }

    private void OnToggleChanged(bool isOn)
    {
        if (isManagerUpdatingToggle) return; // 忽略 Manager 触发的事件

        if (editorManager != null)
        {
            editorManager.M2_OnSentenceToggleChanged(this, isOn);
        }
    }

    /// <summary>
    /// 当句子被编辑时，【实时记录最新值】
    /// </summary>
    private void OnSentenceChanged(string s)
    {
        internal_fullSentence = s; // 【修复点】: 实时记录最新输入
        if (editorManager != null)
        {
            editorManager.MarkLevelAsDirty();
        }
    }

    /// <summary>
    /// 供 Manager 调用来设置 Toggle 状态 (包含锁机制)
    /// </summary>
    public void SetToggleState(bool isOn)
    {
        isManagerUpdatingToggle = true;

        if (selectionToggle != null)
        {
            selectionToggle.isOn = isOn;
        }

        // 修复了“隐藏整个行”的 Bug
        if (selectionHighlight != null)
        {
            selectionHighlight.enabled = isOn;
        }

        isManagerUpdatingToggle = false;
    }

    public int GetSentenceId()
    {
        return int.TryParse(sentenceIdInput.text, out int id) ? id : internal_sentenceId;
    }

    /// <summary>
    /// 【新】: 返回实时记录的输入内容，修复了“读取空字符串”的 Bug
    /// </summary>
    public string GetFullSentence()
    {
        return internal_fullSentence; // 【修复点】: 返回内部变量
    }

    /// <summary>
    /// 【【【新函数】】】: 供 Manager 调用，用于在保存后更新左侧句子的文本
    /// </summary>
    public void SetFullSentenceText(string text)
    {
        // 1. 更新内部数据 (确保 GetFullSentence() 返回最新值)
        this.internal_fullSentence = text;

        // 2. 更新视觉字段 (左侧输入框)
        if (fullSentenceInput != null)
        {
            fullSentenceInput.text = text;
        }
        // 注意: 这里不需要 MarkLevelAsDirty，因为我们正在保存。
    }
}