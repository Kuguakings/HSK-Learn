// SentenceSlot.cs (新版)
using UnityEngine;
using UnityEngine.EventSystems;

public class SentenceSlot : MonoBehaviour, IDropHandler
{
    public int slotOrder; // 【修改】从 requiredOrder 改为 slotOrder
    public DraggableWord currentWord { get; private set; } // 【新增】记录当前槽内的词语

    private LinkupGameManager gameManager;

    public void Setup(LinkupGameManager manager, int order)
    {
        this.gameManager = manager;
        this.slotOrder = order;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            DraggableWord droppedWord = eventData.pointerDrag.GetComponent<DraggableWord>();
            if (droppedWord != null)
            {
                // 【修改】通知主管理器处理复杂的放置逻辑
                gameManager.HandleWordPlacement(droppedWord, this);
            }
        }
    }

    // 【新增】公开的放置和移除方法
    public void PlaceWord(DraggableWord word)
    {
        currentWord = word;
        if (word != null)
        {
            word.transform.SetParent(this.transform);
            word.transform.localPosition = Vector3.zero;
        }
    }

    public void Clear()
    {
        currentWord = null;
    }
}