// GameManager.cs (V-TestPlay.3 - 修复“返回主菜单”逻辑)
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    // (内部数据结构, V-TestPlay.2 不变)
    #region 内部数据结构 (V-TestPlay.2 不变)
    // (删除了内部的 TileType 和 TileInfo)
    public class WordData
    {
        public int groupId;
        public string hanzi;
        public string pinyin;
        public string english;
    }
    #endregion

    // (变量, V-TestPlay 不变)
    #region 变量 (V-TestPlay 不变)
    [Header("布局设置")]
    public int totalRows = 5;
    public int totalColumns = 5;
    [Header("游戏核心元素")]
    public GameObject wordTilePrefab;
    public Transform gridParent;
    public Canvas rootCanvas;
    [Header("分页加载")]
    public int wordsPerPage = 5;
    [Header("动画参数")]
    public float fadeDuration = 0.3f;
    public float moveDuration = 0.4f;
    public float visualStaggerDelay = 0.03f;
    public float audioStaggerDelay = 0.1f;
    [Header("游戏UI")]
    public TextMeshProUGUI levelTitleText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public GameObject pausePanel;
    [Header("胜利UI")]
    public GameObject levelCompletePanel;
    public GameObject gameCompletePanel;
    public TextMeshProUGUI finalTimeText_LevelComplete;
    public TextMeshProUGUI finalScoreText_LevelComplete;
    public TextMeshProUGUI finalTimeText_GameComplete;
    public TextMeshProUGUI finalScoreText_GameComplete;

    [Header("【【【试玩模式 UI】】】")]
    public GameObject testPlayPausePanel;
    public GameObject testPlayCompletePanel;

    [Header("开发与测试")]
    public TextAsset testLevelAsset;

    // (内部变量, V-TestPlay 不变)
    private List<WordData> allWordsForLevel = new List<WordData>();
    private List<WordTile> selectedTiles = new List<WordTile>();
    private int currentPage = 0;
    private int totalPages;
    private bool isTransitioning = false;
    private bool isChecking = false;
    private int currentScore = 0;
    private float elapsedTime = 0f;
    private bool isLevelComplete = false;
    private bool isPaused = false;
    private CanvasGroup pausePanelCG;
    private CanvasGroup levelCompletePanelCG;
    private CanvasGroup gameCompletePanelCG;
    private CanvasGroup testPlayPausePanelCG;
    private CanvasGroup testPlayCompletePanelCG;
    #endregion

    // (Awake, V-TestPlay 不变)
    void Awake()
    {
        if (pausePanel != null) { pausePanelCG = pausePanel.GetComponent<CanvasGroup>() ?? pausePanel.AddComponent<CanvasGroup>(); }
        if (levelCompletePanel != null) { levelCompletePanelCG = levelCompletePanel.GetComponent<CanvasGroup>() ?? levelCompletePanel.AddComponent<CanvasGroup>(); }
        if (gameCompletePanel != null) { gameCompletePanelCG = gameCompletePanel.GetComponent<CanvasGroup>() ?? gameCompletePanel.AddComponent<CanvasGroup>(); }
        if (testPlayPausePanel != null) { testPlayPausePanelCG = testPlayPausePanel.GetComponent<CanvasGroup>() ?? testPlayPausePanel.AddComponent<CanvasGroup>(); }
        if (testPlayCompletePanel != null) { testPlayCompletePanelCG = testPlayCompletePanel.GetComponent<CanvasGroup>() ?? testPlayCompletePanel.AddComponent<CanvasGroup>(); }
    }

    // (Start, V-TestPlay 不变)
    void Start()
    {
        Time.timeScale = 1f;
        if (levelCompletePanel != null) { levelCompletePanel.SetActive(false); if (levelCompletePanelCG != null) levelCompletePanelCG.alpha = 0f; }
        if (gameCompletePanel != null) { gameCompletePanel.SetActive(false); if (gameCompletePanelCG != null) gameCompletePanelCG.alpha = 0f; }
        if (pausePanel != null) pausePanel.SetActive(false);
        if (testPlayPausePanel != null) testPlayPausePanel.SetActive(false);
        if (testPlayCompletePanel != null) testPlayCompletePanel.SetActive(false);

        LevelData dataToLoad = LevelManager.selectedLevelData;
        if (dataToLoad == null)
        {
            Debug.LogWarning("未找到 LevelManager.selectedLevelData，已进入【测试模式】。");
            if (testLevelAsset != null)
            {
                levelTitleText.text = "测试关卡";
                LoadLevelDataFromAsset(testLevelAsset);
            }
            else
            {
                Debug.LogError("【测试模式】错误: 请设置测试关卡!");
                return;
            }
        }
        else
        {
            levelTitleText.text = dataToLoad.chapter + " - 第 " + dataToLoad.level + " 关";
            if (LevelManager.isTestPlayMode)
            {
                levelTitleText.text += " (试玩)";
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

    // (Update, V-TestPlay 不变)
    void Update()
    {
        if (isPaused || isLevelComplete || isChecking || isTransitioning) return;
        if (selectedTiles.Count >= 3) { isChecking = true; CheckForMatch(); }
        elapsedTime += Time.deltaTime;
        int minutes = (int)(elapsedTime / 60);
        int seconds = (int)(elapsedTime % 60);
        timerText.text = "时间: " + string.Format("{0:00}:{1:00}", minutes, seconds);
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    // (LoadLevelDataFromAsset, V-TestPlay 不变)
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

    // (LoadLevelDataFromFirebase, V-TestPlay 不变)
    void LoadLevelDataFromFirebase(LevelData data)
    {
        allWordsForLevel.Clear();
        if (data.content_mode_1 == null || data.content_mode_1.Count == 0)
        {
            Debug.LogError($"错误：关卡 {data.id} 没有 Mode 1 (content_mode_1) 数据！");
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

    // 【【【【【【 替换你旧的 HandleLevelComplete() 函数 】】】】】】
    void HandleLevelComplete()
    {
        isLevelComplete = true;
        int minutes = (int)(elapsedTime / 60);
        int seconds = (int)(elapsedTime % 60);
        string finalTimeStr = string.Format("用时: {0:00}:{1:00}", minutes, seconds);
        string finalScoreStr = "积分: " + currentScore;

        // 【【【 已修改 】】】：检查 试玩模式 或 管理员登录
        if (LevelManager.isTestPlayMode || LevelManager.IsAdmin)
        {
            Debug.Log("试玩/管理员通过！显示试玩过关面板。");
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
    // 【【【【【【 替换结束 】】】】】】

    // 【【【【【【 替换你旧的 TogglePause() 函数 】】】】】】
    public void TogglePause()
    {
        isPaused = !isPaused;

        // 【【【 已修改 】】】：检查 试玩模式 或 管理员登录
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
    // 【【【【【【 替换结束 】】】】】】


    #region 其余所有函数 (原封不动)

    // (试玩按钮响应函数, V-TestPlay.3 修改)
    #region 试玩按钮响应函数
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

    // --- 【【【V-TestPlay.3 核心修改】】】 ---
    public void OnClick_TestPlay_ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        if (LevelManager.instance != null)
        {
            // (旧) LevelManager.instance.LoadMainMenu();
            // (新) 调用“试玩成功”版的返回主菜单函数
            LevelManager.instance.LoadMainMenuAfterTestWin();
        }
    }
    // --- 【【【V-TestPlay.3 结束】】】 ---

    #endregion

    // (LoadPage, SetupPageCoroutine, ... , V-TestPlay.2 不变)
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
            if (currentPage < totalPages) { Debug.Log($"页面 {currentPage - 1} 完成! 加载下一页: {currentPage}"); LoadPage(currentPage); }
            else { Debug.Log("所有页面均已完成，关卡胜利！"); HandleLevelComplete(); }
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

    void UpdateScoreDisplay() { scoreText.text = "积分: " + currentScore; }

    public void OnClick_RestartCurrentLevel() { Time.timeScale = 1f; if (LevelManager.instance != null) { LevelManager.instance.ReloadCurrentLevel(); } else { SceneManager.LoadScene(SceneManager.GetActiveScene().name); } }
    public void OnClick_NextLevel() { Time.timeScale = 1f; if (LevelManager.instance != null) { LevelManager.instance.LoadNextLevel(); } else { Debug.LogWarning("测试模式下无法进入下一关。"); } }
    public void OnClick_RestartGame() { Time.timeScale = 1f; if (LevelManager.instance != null) { LevelManager.instance.RestartGame(); } else { Debug.LogWarning("测试模式下无法从第一关重新开始。"); } }
    public void OnClick_MainMenu() { Time.timeScale = 1f; if (LevelManager.instance != null) { LevelManager.instance.LoadMainMenu(); } else { SceneManager.LoadScene("MainMenu"); } }

    #endregion
}