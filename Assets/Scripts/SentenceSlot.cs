/// <summary>
/// 句子槽位组件 / Sentence Slot Component
/// 用于句子排序游戏，接收拖拽的词语 / Used in sentence ordering game, receives dragged words
/// </summary>
using UnityEngine;
using UnityEngine.EventSystems;

public class SentenceSlot : MonoBehaviour, IDropHandler
{
    // 公开属性 / Public Properties
    public int slotOrder;  // 槽位的顺序索引 / Slot order index
    public DraggableWord currentWord { get; private set; }  // 当前槽位中的词语 / Current word in this slot

    // 私有引用 / Private References
    private LinkupGameManager gameManager;  // 游戏管理器引用 / Game manager reference

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
                // ���޸ġ�֪ͨ���������������ӵķ����߼�
                gameManager.HandleWordPlacement(droppedWord, this);
            }
        }
    }

    // �������������ķ��ú��Ƴ�����
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