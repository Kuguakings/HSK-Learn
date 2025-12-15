// --- “单词消消乐”模式使用的数据结构 ---
public enum TileType { Hanzi, Pinyin, English }

public class TileInfo
{
    public int groupId;
    public TileType type;
    public string text;
}

// --- “词语连连看”模式使用的数据结构 ---
public class WordLinkInfo
{
    public int sentenceId;
    public int wordOrder;
    public string wordText;
}