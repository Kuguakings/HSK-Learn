// M1_EditorCell.cs (V2 - 增加了左键点击编辑功能)
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro; // 【【【 新增 】】】

[RequireComponent(typeof(TMPro.TMP_InputField))]
public class M1_EditorCell : MonoBehaviour, IPointerClickHandler
{
    private LevelEditorManager editorManager;
    private TMP_InputField myInputField; // 【【【 新增 】】】

    // 【【【 新增 Awake() 】】】
    void Awake()
    {
        // 获取对自己输入框的引用
        myInputField = GetComponent<TMP_InputField>();
    }

    public void Setup(LevelEditorManager manager)
    {
        this.editorManager = manager;
    }

    // 【【【【【【【【【【 关键修改 】】】】】】】】】】
    // 我们修改了这个函数，让它能同时处理“左键”和“右键”
    public void OnPointerClick(PointerEventData eventData)
    {
        if (editorManager == null || myInputField == null) return;

        // 1. 检查点击的是否是“右键” (用于删除)
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // (旧逻辑不变)
            editorManager.M1_OnRequestDeleteRow(this.gameObject, eventData.position);
        }
        // 2. 检查点击的是否是“左键” (用于编辑)
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 【【【 新逻辑 】】】
            // 告诉 Manager：“我这个单元格被点击了，请用原生HTML浮窗来编辑我！”
            // 我们把“我自己”(this.gameObject)和“我当前的文本”传递过去
            editorManager.M1_OnRequestEditCell(this.gameObject, myInputField.text);
        }
    }
    // 【【【【【【【【【【 修改结束 】】】】】】】】】】
}