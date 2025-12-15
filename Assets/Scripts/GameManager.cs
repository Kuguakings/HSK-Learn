// GameManager.cs - 消消乐游戏管理器 / Match-3 Game Manager
// Manages word matching gameplay, page transitions, and UI interactions
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    #region 内部数据结构 / Internal Data Structures
    /// <summary>
    /// 词语数据类 / Word Data Class
    /// 存储单个词语的所有信息（汉字、拼音、英文）
    /// Stores all information for a single word (Chinese characters, pinyin, English)
    /// </summary>
    public class WordData
    {
        public int groupId;      // 分组ID，相同ID的汉字/拼音/英文属于同一组 / Group ID, same ID means they belong together
        public string hanzi;     // 汉字 / Chinese characters
        public string pinyin;    // 拼音 / Pinyin
        public string english;   // 英文翻译 / English translation
    }
    #endregion

    #region 序列化字段 / Serialized Fields
    [Header("网格配置 / Grid Configuration")]
    public int totalRows = 5;        // 网格行数 / Number of grid rows
    public int totalColumns = 5;     // 网格列数 / Number of grid columns
    
    [Header("游戏预制体元素 / Game Prefab Elements")]
    public GameObject wordTilePrefab;  // 词语方块预制体 / Word tile prefab
    public Transform gridParent;       // 网格父物体 / Grid parent transform
    public Canvas rootCanvas;          // 根画布 / Root canvas
    
    [Header("分页配置 / Pagination Configuration")]
    public int wordsPerPage = 5;       // 每页词语数量 / Words per page
    
    [Header("动画配置 / Animation Configuration")]
    public float fadeDuration = 0.3f;         // 淡入淡出时长 / Fade duration
    public float moveDuration = 0.4f;         // 移动动画时长 / Move animation duration
    public float visualStaggerDelay = 0.03f;  // 视觉错开延迟 / Visual stagger delay
    public float audioStaggerDelay = 0.1f;    // 音频错开延迟 / Audio stagger delay
    
    [Header("游戏UI / Game UI")]
    public TextMeshProUGUI levelTitleText;  // 关卡标题文本 / Level title text
    public TextMeshProUGUI timerText;       // 计时器文本 / Timer text
    public TextMeshProUGUI scoreText;       // 分数文本 / Score text
    public GameObject pausePanel;           // 暂停面板 / Pause panel
    
    [Header("胜利UI / Victory UI")]
    public GameObject levelCompletePanel;         // 关卡完成面板 / Level complete panel
    public GameObject gameCompletePanel;          // 游戏完成面板 / Game complete panel
    public TextMeshProUGUI finalTimeText_LevelComplete;   // 关卡完成最终时间 / Level complete final time
    public TextMeshProUGUI finalScoreText_LevelComplete;  // 关卡完成最终分数 / Level complete final score
    public TextMeshProUGUI finalTimeText_GameComplete;    // 游戏完成最终时间 / Game complete final time
    public TextMeshProUGUI finalScoreText_GameComplete;   // 游戏完成最终分数 / Game complete final score

    [Header("关卡编辑器测试模式 UI面板 / Level Editor Test Mode UI Panels")]
    public GameObject testPlayPausePanel;      // 测试模式暂停面板 / Test mode pause panel
    public GameObject testPlayCompletePanel;   // 测试模式完成面板 / Test mode complete panel

    [Header("测试数据 / Test Data")]
    public TextAsset testLevelAsset;  // 测试关卡数据文件 / Test level data file

    // 内部变量 / Internal Variables
    private List<WordData> allWordsForLevel = new List<WordData>();  // 当前关卡所有词语 / All words for current level
    private List<WordTile> selectedTiles = new List<WordTile>();     // 已选中的方块 / Selected tiles
    private int currentPage = 0;          // 当前页码 / Current page index
    private int totalPages;               // 总页数 / Total pages
    private bool isTransitioning = false; // 是否正在转场 / Is transitioning
    private bool isChecking = false;      // 是否正在检查匹配 / Is checking match
    private int currentScore = 0;         // 当前分数 / Current score
    private float elapsedTime = 0f;       // 已用时间 / Elapsed time
    private bool isLevelComplete = false; // 关卡是否完成 / Is level complete
    private bool isPaused = false;        // 是否暂停 / Is paused
    
    // UI画布组缓存 / UI CanvasGroup cache
    private CanvasGroup pausePanelCG;
    private CanvasGroup levelCompletePanelCG;
    private CanvasGroup gameCompletePanelCG;
    private CanvasGroup testPlayPausePanelCG;
    private CanvasGroup testPlayCompletePanelCG;
    #endregion

    /// <summary>
    /// 初始化CanvasGroup组件 / Initialize CanvasGroup components
    /// 在Start之前获取或添加必要的CanvasGroup组件 / Get or add necessary CanvasGroup components before Start
    /// </summary>
    void Awake()
    {
        if (pausePanel != null) { pausePanelCG = pausePanel.GetComponent<CanvasGroup>() ?? pausePanel.AddComponent<CanvasGroup>(); }
        if (levelCompletePanel != null) { levelCompletePanelCG = levelCompletePanel.GetComponent<CanvasGroup>() ?? levelCompletePanel.AddComponent<CanvasGroup>(); }
        if (gameCompletePanel != null) { gameCompletePanelCG = gameCompletePanel.GetComponent<CanvasGroup>() ?? gameCompletePanel.AddComponent<CanvasGroup>(); }
        if (testPlayPausePanel != null) { testPlayPausePanelCG = testPlayPausePanel.GetComponent<CanvasGroup>() ?? testPlayPausePanel.AddComponent<CanvasGroup>(); }
        if (testPlayCompletePanel != null) { testPlayCompletePanelCG = testPlayCompletePanel.GetComponent<CanvasGroup>() ?? testPlayCompletePanel.AddComponent<CanvasGroup>(); }
    }

    /// <summary>
    /// 游戏初始化 / Game Initialization
    /// 重置时间缩放、隐藏UI面板、加载关卡数据 / Reset time scale, hide UI panels, load level data
    /// </summary>
    void Start()
    {
        Time.timeScale = 1f;  // 确保时间正常流动 / Ensure time flows normally
        
        // 隐藏所有UI面板 / Hide all UI panels
        if (levelCompletePanel != null) { levelCompletePanel.SetActive(false); if (levelCompletePanelCG != null) levelCompletePanelCG.alpha = 0f; }
        if (gameCompletePanel != null) { gameCompletePanel.SetActive(false); if (gameCompletePanelCG != null) gameCompletePanelCG.alpha = 0f; }
        if (pausePanel != null) pausePanel.SetActive(false);
        if (testPlayPausePanel != null) testPlayPausePanel.SetActive(false);
        if (testPlayCompletePanel != null) testPlayCompletePanel.SetActive(false);

        // 加载关卡数据 / Load level data
        LevelData dataToLoad = LevelManager.selectedLevelData;
        if (dataToLoad == null)
        {
            // 测试模式 / Test mode
            Debug.LogWarning("未找到 LevelManager.selectedLevelData，已进入【测试模式】/ No LevelManager.selectedLevelData found, entering test mode");
            if (testLevelAsset != null)
            {
                levelTitleText.text = "测试关卡 / Test Level";
                LoadLevelDataFromAsset(testLevelAsset);
            }
            else
            {
                Debug.LogError("测试模式启动失败: 未指定测试关卡! / Test mode failed: No test level assigned!");
                return;
            }
        }
        else
        {
            // 正常模式：从LevelManager加载 / Normal mode: Load from LevelManager
            levelTitleText.text = dataToLoad.chapter + " - 第 " + dataToLoad.level + " 关";
            if (LevelManager.isTestPlayMode)
            {
                levelTitleText.text += " (测试 / Test)";
            }
            LoadLevelDataFromFirebase(dataToLoad);
        }
        UpdateScoreDisplay();
        if (allWordsForLevel.Count > 0)
        {
            totalPages = Mathf.CeilToInt((float)allWordsForLevel.Count / wordsPerPage);
            currentPage = 0;
            LoadPage(currentPage);
        }
    }

    // (Update, V-TestPlay ����)
    void Update()
    {
        if (isPaused || isLevelComplete || isChecking || isTransitioning) return;
        if (selectedTiles.Count >= 3) { isChecking = true; CheckForMatch(); }
        elapsedTime += Time.deltaTime;
        int minutes = (int)(elapsedTime / 60);
        int seconds = (int)(elapsedTime % 60);
        timerText.text = "ʱ��: " + string.Format("{0:00}:{1:00}", minutes, seconds);
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    // (LoadLevelDataFromAsset, V-TestPlay ����)
    void LoadLevelDataFromAsset(TextAsset textAsset)
    {
        allWordsForLevel.Clear();
        string[] lines = textAsset.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] values = line.Split(',');
            if (values.Length >= 4)
            {
                WordData word = new WordData
                {
                    groupId = int.Parse(values[0]),
                    hanzi = values[1],
                    pinyin = values[2],
                    english = values[3]
                };
                allWordsForLevel.Add(word);
            }
        }
    }

    // (LoadLevelDataFromFirebase, V-TestPlay ����)
    void LoadLevelDataFromFirebase(LevelData data)
    {
        allWordsForLevel.Clear();
        if (data.content_mode_1 == null || data.content_mode_1.Count == 0)
        {
            Debug.LogError($"���󣺹ؿ� {data.id} û�� Mode 1 (content_mode_1) ���ݣ�");
            return;
        }
        foreach (Mode1Content firebaseWord in data.content_mode_1)
        {
            WordData word = new WordData
            {
                groupId = (int)firebaseWord.groupId,
                hanzi = firebaseWord.hanzi,
                pinyin = firebaseWord.pinyin,
                english = firebaseWord.english
            };
            allWordsForLevel.Add(word);
        }
    }

    // ������������ �滻��ɵ� HandleLevelComplete() ���� ������������
    void HandleLevelComplete()
    {
        isLevelComplete = true;
        int minutes = (int)(elapsedTime / 60);
        int seconds = (int)(elapsedTime % 60);
        string finalTimeStr = string.Format("��ʱ: {0:00}:{1:00}", minutes, seconds);
        string finalScoreStr = "����: " + currentScore;

        // ������ ���޸� ����������� ����ģʽ �� ����Ա��¼
        if (LevelManager.isTestPlayMode || LevelManager.IsAdmin)
        {
            Debug.Log("����/����Աͨ������ʾ���������塣");
            if (testPlayCompletePanel != null)
            {
                testPlayCompletePanel.SetActive(true);
                StartCoroutine(FadeCanvasGroup(testPlayCompletePanelCG, 0f, 1f, 0.5f));
            }
        }
        else
        {
            if (LevelManager.instance != null && LevelManager.instance.IsLastLevel())
            {
                if (finalTimeText_GameComplete != null) finalTimeText_GameComplete.text = finalTimeStr;
                if (finalScoreText_GameComplete != null) finalScoreText_GameComplete.text = finalScoreStr;
                if (gameCompletePanel != null) { gameCompletePanel.SetActive(true); StartCoroutine(FadeCanvasGroup(gameCompletePanelCG, 0f, 1f, 0.5f)); }
            }
            else
            {
                if (finalTimeText_LevelComplete != null) finalTimeText_LevelComplete.text = finalTimeStr;
                if (finalScoreText_LevelComplete != null) finalScoreText_LevelComplete.text = finalScoreStr;
                if (levelCompletePanel != null) { levelCompletePanel.SetActive(true); StartCoroutine(FadeCanvasGroup(levelCompletePanelCG, 0f, 1f, 0.5f)); }
            }
        }
    }
    // ������������ �滻���� ������������

    // ������������ �滻��ɵ� TogglePause() ���� ������������
    public void TogglePause()
    {
        isPaused = !isPaused;

        // ������ ���޸� ����������� ����ģʽ �� ����Ա��¼
        if (LevelManager.isTestPlayMode || LevelManager.IsAdmin)
        {
            if (isPaused)
            {
                testPlayPausePanel.SetActive(true);
                if (testPlayPausePanelCG != null) testPlayPausePanelCG.interactable = true;
                StartCoroutine(FadeCanvasGroup(testPlayPausePanelCG, 0f, 1f, 0.2f, () => { Time.timeScale = 0f; }));
            }
            else
            {
                Time.timeScale = 1f;
                if (testPlayPausePanelCG != null) testPlayPausePanelCG.interactable = false;
                StartCoroutine(FadeCanvasGroup(testPlayPausePanelCG, 1f, 0f, 0.2f, () => { if (testPlayPausePanel != null) testPlayPausePanel.SetActive(false); }));
            }
        }
        else
        {
            if (isPaused)
            {
                pausePanel.SetActive(true);
                if (pausePanelCG != null) pausePanelCG.interactable = true;
                StartCoroutine(FadeCanvasGroup(pausePanelCG, 0f, 1f, 0.2f, () => { Time.timeScale = 0f; }));
            }
            else
            {
                Time.timeScale = 1f;
                if (pausePanelCG != null) pausePanelCG.interactable = false;
                StartCoroutine(FadeCanvasGroup(pausePanelCG, 1f, 0f, 0.2f, () => { if (pausePanel != null) pausePanel.SetActive(false); }));
            }
        }
    }
    // ������������ �滻���� ������������


    #region �������к��� (ԭ�ⲻ��)

    // (���水ť��Ӧ����, V-TestPlay.3 �޸�)
    #region ���水ť��Ӧ����
    public void OnClick_TestPlay_Continue()
    {
        TogglePause();
    }
    public void OnClick_TestPlay_Restart()
    {
        Time.timeScale = 1f;
        if (LevelManager.instance != null)
        {
            LevelManager.instance.ReloadCurrentLevel();
        }
    }
    public void OnClick_TestPlay_ReturnToEditor(bool didWin)
    {
        Time.timeScale = 1f;
        if (LevelManager.instance != null)
        {
            LevelManager.instance.ReturnToEditor(didWin);
        }
    }

    // --- ������V-TestPlay.3 �����޸ġ����� ---
    public void OnClick_TestPlay_ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        if (LevelManager.instance != null)
        {
            // (��) LevelManager.instance.LoadMainMenu();
            // (��) ���á�����ɹ�����ķ������˵�����
            LevelManager.instance.LoadMainMenuAfterTestWin();
        }
    }
    // --- ������V-TestPlay.3 ���������� ---

    #endregion

    /// <summary>
    /// 加载指定页码的内容 / Load Content for Specified Page
    /// </summary>
    /// <param name="pageIndex">页码索引 / Page index</param>
    void LoadPage(int pageIndex)
    {
        isTransitioning = true;
        int startIndex = pageIndex * wordsPerPage;
        List<WordData> wordsForThisPage = allWordsForLevel.Skip(startIndex).Take(wordsPerPage).ToList();
        StartCoroutine(SetupPageCoroutine(wordsForThisPage));
    }

    private IEnumerator SetupPageCoroutine(List<WordData> wordsForThisPage)
    {
        foreach (Transform child in gridParent) { Destroy(child.gameObject); }
        yield return null;

        List<TileInfo> pageTilesInfo = new List<TileInfo>();
        foreach (var word in wordsForThisPage)
        {
            pageTilesInfo.Add(new TileInfo { groupId = word.groupId, type = TileType.Hanzi, text = word.hanzi });
            pageTilesInfo.Add(new TileInfo { groupId = word.groupId, type = TileType.Pinyin, text = word.pinyin });
            pageTilesInfo.Add(new TileInfo { groupId = word.groupId, type = TileType.English, text = word.english });
        }

        pageTilesInfo = pageTilesInfo.OrderBy(x => Random.value).ToList();
        List<UIElementAnimator> animators = new List<UIElementAnimator>();
        int totalCells = totalRows * totalColumns;
        for (int i = 0; i < totalCells; i++)
        {
            GameObject tileGO = Instantiate(wordTilePrefab, gridParent);
            WordTile wordTile = tileGO.GetComponent<WordTile>();
            tileGO.GetComponent<CanvasGroup>().alpha = 0;
            tileGO.transform.localScale = Vector3.zero;
            if (i < pageTilesInfo.Count)
            {
                wordTile.Setup(pageTilesInfo[i], this, true);
                animators.Add(tileGO.GetComponent<UIElementAnimator>());
            }
            else
            {
                wordTile.Setup(null, this, false);
            }
        }
        yield return null;
        StartCoroutine(AnimateVisualsCoroutine(animators));
        StartCoroutine(PlayAudioCoroutine(animators.Count));
        yield return new WaitForSeconds(audioStaggerDelay * animators.Count * 0.5f);
        isTransitioning = false;
    }

    void CheckForPageClear() { StartCoroutine(CheckPageClearAfterFrame()); }

    private IEnumerator CheckPageClearAfterFrame()
    {
        yield return null;
        int activeTileCount = 0;
        foreach (Transform child in gridParent) { if (child.gameObject.activeSelf && child.GetComponent<WordTile>().info != null) { activeTileCount++; } }
        if (activeTileCount == 0 && !isLevelComplete)
        {
            currentPage++;
            if (currentPage < totalPages) { Debug.Log($"ҳ�� {currentPage - 1} ���! ������һҳ: {currentPage}"); LoadPage(currentPage); }
            else { Debug.Log("����ҳ�������ɣ��ؿ�ʤ����"); HandleLevelComplete(); }
        }
    }

    private IEnumerator MatchSequenceCoroutine(List<WordTile> matchedTiles, bool isMatch)
    {
        if (isMatch)
        {
            currentScore += 10;
            UpdateScoreDisplay();
            foreach (var tile in matchedTiles) { if (tile != null) { StartCoroutine(tile.FadeOut(fadeDuration)); } }
            yield return StartCoroutine(AnimateCompaction(matchedTiles));
            foreach (var tile in matchedTiles) { if (tile != null) Destroy(tile.gameObject); }
            CheckForPageClear();
        }
        else { foreach (var tile in matchedTiles) { if (tile != null) { StartCoroutine(tile.FlashError()); } } }
        isChecking = false;
    }

    private IEnumerator AnimateVisualsCoroutine(List<UIElementAnimator> animators)
    {
        for (int i = 0; i < animators.Count; i++) { if (animators[i] != null) StartCoroutine(animators[i].AnimateInCoroutine()); yield return new WaitForSeconds(visualStaggerDelay); }
    }

    private IEnumerator PlayAudioCoroutine(int count)
    {
        for (int i = 0; i < count; i++) { if (UISoundManager.instance != null) UISoundManager.instance.PlayMatch3TileAppearSound(); yield return new WaitForSeconds(audioStaggerDelay); }
    }

    public void OnTileSelected(WordTile tile)
    {
        if (isChecking || isTransitioning) return;
        if (!selectedTiles.Contains(tile)) { selectedTiles.Add(tile); }
    }

    public void OnTileDeselected(WordTile tile)
    {
        if (isChecking || isTransitioning) return;
        selectedTiles.Remove(tile);
    }

    void CheckForMatch()
    {
        List<WordTile> tilesToCheck = selectedTiles.Take(3).ToList();
        selectedTiles.RemoveRange(0, 3);
        int firstGroupId = tilesToCheck[0].info.groupId;
        bool isGroupIdMatch = tilesToCheck.All(tile => tile.info.groupId == firstGroupId);
        bool isTypeMatch = tilesToCheck.Select(tile => tile.info.type).Distinct().Count() == 3;
        StartCoroutine(MatchSequenceCoroutine(tilesToCheck, isGroupIdMatch && isTypeMatch));
    }

    private IEnumerator AnimateCompaction(List<WordTile> removedTiles)
    {
        List<WordTile> allTiles = new List<WordTile>();
        foreach (Transform child in gridParent) { allTiles.Add(child.GetComponent<WordTile>()); }
        List<Transform> emptySlots = new List<Transform>();
        foreach (var tile in removedTiles) { emptySlots.Add(tile.transform); }
        allTiles.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        emptySlots.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        GridLayoutGroup gridLayout = gridParent.GetComponent<GridLayoutGroup>();
        gridLayout.enabled = false;
        float totalAnimationTime = 0;
        for (int i = 0; i < allTiles.Count; i++)
        {
            WordTile tile = allTiles[i];
            if (tile == null || !tile.gameObject.activeSelf || removedTiles.Contains(tile)) continue;
            int emptySlotsBefore = emptySlots.Count(slot => slot.GetSiblingIndex() < tile.transform.GetSiblingIndex());
            if (emptySlotsBefore > 0)
            {
                Transform targetSlot = emptySlots[0];
                StartCoroutine(DelayedMove(tile, targetSlot.position, visualStaggerDelay * i));
                emptySlots.RemoveAt(0);
                emptySlots.Add(tile.transform);
                emptySlots.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
                totalAnimationTime = visualStaggerDelay * i;
            }
        }
        yield return new WaitForSeconds(totalAnimationTime + moveDuration);
        gridLayout.enabled = true;
    }

    private IEnumerator DelayedMove(WordTile tile, Vector3 targetPosition, float delay)
    {
        if (delay > 0) { yield return new WaitForSeconds(delay); }
        StartCoroutine(tile.MoveTo(targetPosition, moveDuration));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration, System.Action onComplete = null)
    {
        if (cg == null) { onComplete?.Invoke(); yield break; }
        float elapsedTime = 0f;
        cg.blocksRaycasts = false;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            yield return null;
        }
        cg.alpha = endAlpha;
        if (endAlpha > 0) { cg.interactable = true; cg.blocksRaycasts = true; }
        else { cg.interactable = false; }
        onComplete?.Invoke();
    }

    /// <summary>更新分数显示 / Update Score Display</summary>
    void UpdateScoreDisplay() { scoreText.text = "分数 / Score: " + currentScore; }

    /// <summary>重新开始当前关卡 / Restart Current Level</summary>
    public void OnClick_RestartCurrentLevel() { Time.timeScale = 1f; if (LevelManager.instance != null) { LevelManager.instance.ReloadCurrentLevel(); } else { SceneManager.LoadScene(SceneManager.GetActiveScene().name); } }
    
    /// <summary>进入下一关 / Next Level</summary>
    public void OnClick_NextLevel() { Time.timeScale = 1f; if (LevelManager.instance != null) { LevelManager.instance.LoadNextLevel(); } else { Debug.LogWarning("测试模式，无法加载下一关。/ Test mode, cannot load next level."); } }
    
    /// <summary>从第一关重新开始 / Restart from First Level</summary>
    public void OnClick_RestartGame() { Time.timeScale = 1f; if (LevelManager.instance != null) { LevelManager.instance.RestartGame(); } else { Debug.LogWarning("测试模式，无法从第一关重新开始。/ Test mode, cannot restart from first level."); } }
    
    /// <summary>返回主菜单 / Return to Main Menu</summary>
    public void OnClick_MainMenu() { Time.timeScale = 1f; if (LevelManager.instance != null) { LevelManager.instance.LoadMainMenu(); } else { SceneManager.LoadScene("MainMenu"); } }

    #endregion
}