// 这个脚本不需要挂载到任何游戏对象上，它只是一个数据的定义。
// [System.Serializable] 这行代码让这个数据结构可以在Unity的Inspector窗口中显示，方便调试。
[System.Serializable]
public class WordData
{
    public int groupId; // 组ID，例如“苹果”这一组的所有卡片ID都是1
    public string hanzi;
    public string pinyin;
    public string english;
}