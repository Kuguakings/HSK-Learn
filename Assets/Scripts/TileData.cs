/// <summary>
/// 游戏数据结构定义 / Game Data Structure Definitions
/// 包含消消乐和连连看两种模式的数据结构 / Contains data structures for both Match-3 and Link-up modes
/// </summary>

// === 消消乐（模式1）数据结构 / Match-3 (Mode 1) Data Structures ===

/// <summary>
/// 方块类型枚举 / Tile Type Enumeration
/// </summary>
public enum TileType 
{ 
    Hanzi,    // 汉字 / Chinese characters
    Pinyin,   // 拼音 / Pinyin
    English   // 英文 / English
}

/// <summary>
/// 方块信息类 / Tile Information Class
/// 存储单个方块的完整信息 / Stores complete information for a single tile
/// </summary>
public class TileInfo
{
    public int groupId;      // 分组ID，同组的汉字/拼音/英文可以消除 / Group ID, same group can be matched
    public TileType type;    // 方块类型 / Tile type
    public string text;      // 显示文本 / Display text
}

// === 连连看（模式2）数据结构 / Link-up (Mode 2) Data Structures ===

/// <summary>
/// 词语连接信息类 / Word Link Information Class
/// 用于句子排序游戏 / Used for sentence ordering game
/// </summary>
public class WordLinkInfo
{
    public int sentenceId;   // 句子ID / Sentence ID
    public int wordOrder;    // 词语在句子中的顺序 / Word order in sentence
    public string wordText;  // 词语文本 / Word text
}