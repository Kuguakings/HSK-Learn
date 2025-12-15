// LinkupGameManager.cs (已实现“D计划”的最终版)
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LinkupGameManager : MonoBehaviour
{
    // ... (您所有的 Header 和变量，原封不动) ...
    #region 变量 (原封不动)
    [Header("核心组件")]
    public GameObject draggableWordPrefab;
    public GameObject sentenceSlotPrefab;
    public RectTransform wordSpawnArea;
    public RectTransform slotContainer;
    public Transform mainCanvasTransform;
    [Header("滑动视图组件")]
    public ScrollRect slotScrollRect;
    public RectTransform viewportRect;
    [Header("游戏UI")]
    public TextMeshProUGUI levelTitleText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public CanvasGroup blackFadePanelCG;
    [Header("UI 面板")]
    public GameObject pausePanel;
    public GameObject levelCompletePanel;
    public GameObject gameCompletePanel;
    [Header("胜利UI文本")]
    public TextMeshProUGUI finalTimeText_LevelComplete;
    public TextMeshProUGUI finalScoreText_LevelComplete;
    public TextMeshProUGUI finalTimeText_GameComplete;
    public TextMeshProUGUI finalScoreText_GameComplete;
    [Header("玩法参数")]
    public float ejectAnimationDuration = 0.5f;
    public GameObject sentencePromptPrefab;
    public float staggerDelay = 0.1f;
    public float transitionDuration = 1.2f;
    [Header("开发与测试")]
    public TextAsset testLevelAsset;

    [Header("【【【试玩模式 UI】】】")]
    public GameObject testPlayPausePanel;
    public GameObject testPlayCompletePanel;

    // 【【【已修复 V4.2】：字典的 Key 现在是 int】】】
    private Dictionary<int, List<Mode2Content>> allSentences = new Dictionary<int, List<Mode2Content>>();

    // 【【【已修复 V4.2】：currentSentenceId 也是 int】】】
    private int currentSentenceId = 1;
    private List<SentenceSlot> currentSlots = new List<SentenceSlot>();
    private List<DraggableWord> createdWords = new List<DraggableWord>();
    private bool isChecking = false;
    private int currentScore = 0;
    private float elapsedTime = 0f;
    private bool isPaused = false;
    private bool isLevelComplete = false;
    private CanvasGroup pausePanelCG;
    private CanvasGroup levelCompletePanelCG;
    private CanvasGroup gameCompletePanelCG;

    private CanvasGroup testPlayPausePanelCG;
    private CanvasGroup testPlayCompletePanelCG;

    private class WordOrderInfo : MonoBehaviour { public int order; }
    #endregion

    // (Awake 原封不动)
    void Awake()
    {
        if (pausePanel != null) pausePanelCG = pausePanel.GetComponent<CanvasGroup>() ?? pausePanel.AddComponent<CanvasGroup>();
        if (levelCompletePanel != null) { levelCompletePanelCG = levelCompletePanel.GetComponent<CanvasGroup>() ?? levelCompletePanel.AddComponent<CanvasGroup>(); }
        if (gameCompletePanel != null) { gameCompletePanelCG = gameCompletePanel.GetComponent<CanvasGroup>() ?? gameCompletePanel.AddComponent<CanvasGroup>(); }
        if (testPlayPausePanel != null) { testPlayPausePanelCG = testPlayPausePanel.GetComponent<CanvasGroup>() ?? testPlayPausePanel.AddComponent<CanvasGroup>(); }
        if (testPlayCompletePanel != null) { testPlayCompletePanelCG = testPlayCompletePanel.GetComponent<CanvasGroup>() ?? testPlayCompletePanel.AddComponent<CanvasGroup>(); }
    }

    // (Start 函数原封不动)
    void Start()
    {
        Time.timeScale = 1f;
        if (blackFadePanelCG != null) { blackFadePanelCG.alpha = 1f; }
        if (pausePanel != null) pausePanel.SetActive(false);
        if (levelCompletePanel != null) { levelCompletePanel.SetActive(false); if (levelCompletePanelCG != null) levelCompletePanelCG.alpha = 0f; }
        if (gameCompletePanel != null) { gameCompletePanel.SetActive(false); if (gameCompletePanelCG != null) gameCompletePanelCG.alpha = 0f; }
        if (testPlayPausePanel != null) testPlayPausePanel.SetActive(false);
        if (testPlayCompletePanel != null) { testPlayCompletePanel.SetActive(false); if (testPlayCompletePanelCG != null) testPlayCompletePanelCG.alpha = 0f; }

        LevelData dataToLoad = LevelManager.selectedLevelData;

        if (dataToLoad == null)
        {
            Debug.LogWarning("未找到 LevelManager.selectedLevelData，已进入【测试模式】。");
            if (testLevelAsset != null)
            {
                levelTitleText.text = "测试关卡";
                UpdateScoreDisplay();
                LoadDataFromAsset(testLevelAsset);
                StartLevel();
            }
            else { Debug.LogError("【测试模式】错误: 请设置测试关卡!"); }
        }
        else
        {
            levelTitleText.text = dataToLoad.chapter + " - 第 " + dataToLoad.level + " 关";
            if (LevelManager.isTestPlayMode)
            {
                levelTitleText.text += " (试玩)";
            }
            UpdateScoreDisplay();
            LoadDataFromFirebase(dataToLoad);
            StartLevel();
        }
    }

    // (Update 原封不动)
    void Update()
    {
        if (isPaused || isLevelComplete) return;
        elapsedTime += Time.deltaTime;
        int minutes = (int)(elapsedTime / 60);
        int seconds = (int)(elapsedTime % 60);
        if (timerText != null) timerText.text = string.Format("时间: {0:00}:{1:00}", minutes, seconds);
        if (Input.GetKeyDown(KeyCode.Escape)) { TogglePause(); }
    }

    // (StartLevel 原封不动)
    void StartLevel() { StartCoroutine(RoundCoroutine(1)); }

    // (LoadDataFromFirebase 原封不动)
    void LoadDataFromFirebase(LevelData data)
    {
        allSentences.Clear();
        if (data.content_mode_2 == null || data.content_mode_2.Count == 0)
        {
            Debug.LogError($"错误：关卡 {data.id} 没有 Mode 2 (content_mode_2) 数据！");
            return;
        }

        foreach (Mode2Content firebaseWord in data.content_mode_2)
        {
            int sId = firebaseWord.sentenceId;

            if (!allSentences.ContainsKey(sId))
            {
                allSentences[sId] = new List<Mode2Content>();
            }
            allSentences[sId].Add(firebaseWord);
        }

        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in slotContainer)
        {
            childrenToDestroy.Add(child.gameObject);
        }
        foreach (GameObject go in childrenToDestroy)
        {
            Destroy(go);
        }
    }

    // (LoadDataFromAsset 原封不动)
    void LoadDataFromAsset(TextAsset textAsset)
    {
        ParseCSV(textAsset.text);
    }

    // (ParseCSV 原封不动)
    void ParseCSV(string csvText)
    {
        allSentences.Clear();
        string[] lines = csvText.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');
            if (values.Length >= 4)
            {
                var wordData = new Mode2Content
                {
                    sentenceId = int.Parse(values[0]),
                    wordOrder = int.Parse(values[1]),
                    wordText = values[2].Trim(),
                    fullSentence = values[3].Trim()
                };

                if (!allSentences.ContainsKey(wordData.sentenceId))
                {
                    allSentences[wordData.sentenceId] = new List<Mode2Content>();
                }
                allSentences[wordData.sentenceId].Add(wordData);
            }
        }
    }

    // (SetupRoundElements 原封不动)
    void SetupRoundElements(int sentenceId)
    {
        var currentSentenceWords = allSentences[sentenceId];
        for (int i = 0; i < currentSentenceWords.Count; i++)
        {
            GameObject slotGO = Instantiate(sentenceSlotPrefab, slotContainer);
            slotGO.GetComponent<CanvasGroup>().alpha = 0;
            slotGO.transform.localScale = Vector3.zero;
            slotGO.GetComponent<SentenceSlot>().Setup(this, i + 1);
            currentSlots.Add(slotGO.GetComponent<SentenceSlot>());
        }
        List<Mode2Content> shuffledWords = currentSentenceWords.OrderBy(x => Random.value).ToList();
        foreach (var wordData in shuffledWords)
        {
            GameObject wordGO = Instantiate(draggableWordPrefab, wordSpawnArea);
            wordGO.GetComponent<CanvasGroup>().alpha = 0;
            wordGO.transform.localScale = Vector3.zero;
            var dw = wordGO.GetComponent<DraggableWord>();
            dw.wordText = wordData.wordText;
            dw.originalParent = wordSpawnArea;
            wordGO.GetComponentInChildren<TextMeshProUGUI>().text = wordData.wordText;
            wordGO.AddComponent<WordOrderInfo>().order = wordData.wordOrder;
            createdWords.Add(dw);
        }
    }

    // ... (所有其他函数，RoundCoroutine, UpdateSlotLayoutAfterFrame, ...等等... 原封不动) ...
    #region 其余所有函数 (原封不动)
    private IEnumerator RoundCoroutine(int sentenceId)
    {
        if (!allSentences.ContainsKey(sentenceId)) { StartCoroutine(LevelCompleteSequenceCoroutine()); yield break; }
        if (blackFadePanelCG != null && blackFadePanelCG.alpha > 0) { yield return StartCoroutine(Fade(blackFadePanelCG, 1f, 0f, transitionDuration)); }
        yield return new WaitForSeconds(transitionDuration / 4f);
        GameObject promptGO = Instantiate(sentencePromptPrefab, mainCanvasTransform);
        CanvasGroup promptCG_forShow = promptGO.GetComponent<CanvasGroup>();
        if (promptCG_forShow != null) { promptCG_forShow.alpha = 0f; StartCoroutine(Fade(promptCG_forShow, 0f, 1f, 0.3f)); }
        TextMeshProUGUI promptText = promptGO.GetComponentInChildren<TextMeshProUGUI>();
        string fullSentence = allSentences[sentenceId][0].fullSentence;
        promptText.text = "本次需要匹配的句子是：\n<b>" + fullSentence + "</b>";
        float timer = 0f;
        while (timer < 10f) { if (Input.GetMouseButtonDown(0)) { break; } timer += Time.deltaTime; yield return null; }
        CanvasGroup promptCG = promptGO.GetComponent<CanvasGroup>();
        if (promptCG != null) yield return StartCoroutine(Fade(promptCG, 1f, 0f, 0.3f));
        Destroy(promptGO);
        SetupRoundElements(sentenceId);
        yield return StartCoroutine(UpdateSlotLayoutAfterFrame());
        yield return StartCoroutine(PositionWordsAfterLayout());
        yield return StartCoroutine(AnimateElementsIn());
    }
    private IEnumerator UpdateSlotLayoutAfterFrame() { yield return new WaitForEndOfFrame(); ContentSizeFitter sizeFitter = slotContainer.GetComponent<ContentSizeFitter>(); if (sizeFitter == null || slotScrollRect == null) { Debug.LogError("滑动视图的关键组件引用缺失！"); yield break; } if (currentSlots.Count <= 3) { sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; slotScrollRect.enabled = false; yield return new WaitForEndOfFrame(); slotContainer.anchoredPosition = new Vector2(0, slotContainer.anchoredPosition.y); } else { sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; slotScrollRect.enabled = true; } }
    private IEnumerator PositionWordsAfterLayout() { yield return new WaitForEndOfFrame(); Rect spawnAreaRect = wordSpawnArea.rect; List<Vector2> occupiedPositions = new List<Vector2>(); foreach (var word in createdWords) { RectTransform wordRect = word.GetComponent<RectTransform>(); Vector2 randomPos; int attempts = 0; do { float randomX = Random.Range(spawnAreaRect.xMin + (wordRect.rect.width / 2), spawnAreaRect.xMax - (wordRect.rect.width / 2)); float randomY = Random.Range(spawnAreaRect.yMin + (wordRect.rect.height / 2), spawnAreaRect.yMax - (wordRect.rect.height / 2)); randomPos = new Vector2(randomX, randomY); attempts++; } while (occupiedPositions.Any(p => Vector2.Distance(p, randomPos) < wordRect.rect.width) && attempts < 200); wordRect.anchoredPosition = randomPos; occupiedPositions.Add(randomPos); } }
    private IEnumerator ValidateAnswerCoroutine() { yield return new WaitForSeconds(0.5f); bool isCorrect = true; foreach (var slot in currentSlots) { if (slot.currentWord == null) { isCorrect = false; break; } int wordOrder = slot.currentWord.GetComponent<WordOrderInfo>().order; if (wordOrder != slot.slotOrder) { isCorrect = false; break; } } if (isCorrect) { currentScore += 100; UpdateScoreDisplay(); currentSentenceId++; StartCoroutine(TransitionToNextRound()); } else { EjectAllWords(); } }
    private IEnumerator TransitionToNextRound() { currentSlots.ForEach(slot => { if (slot != null) Destroy(slot.gameObject); }); createdWords.ForEach(word => { if (word != null) Destroy(word.gameObject); }); currentSlots.Clear(); createdWords.Clear(); isChecking = false; yield return StartCoroutine(Fade(blackFadePanelCG, 0f, 1f, transitionDuration)); StartCoroutine(RoundCoroutine(currentSentenceId)); }

    // 【【【【【【 这里是修改点 1 】】】】】】
    private IEnumerator LevelCompleteSequenceCoroutine()
    {
        yield return StartCoroutine(Fade(blackFadePanelCG, 0f, 1f, transitionDuration));
        isLevelComplete = true;

        // 【【【 已修改 】】】：检查 试玩模式 或 管理员登录
        if (LevelManager.isTestPlayMode || LevelManager.IsAdmin)
        {
            Debug.Log("试玩/管理员通过！显示试玩过关面板。");
            if (testPlayCompletePanel != null)
            {
                testPlayCompletePanel.SetActive(true);
                StartCoroutine(Fade(testPlayCompletePanelCG, 0f, 1f, 0.5f));
            }
            yield break; // 关键：提前结束协程，不显示正常通关UI
        }

        int minutes = (int)(elapsedTime / 60);
        int seconds = (int)(elapsedTime % 60);
        string finalTimeStr = string.Format("用时: {0:00}:{1:00}", minutes, seconds);
        string finalScoreStr = "积分: " + currentScore;
        bool isLastLevel = (LevelManager.instance != null) ? LevelManager.instance.IsLastLevel() : true;

        if (isLastLevel)
        {
            if (finalTimeText_GameComplete != null) finalTimeText_GameComplete.text = finalTimeStr;
            if (finalScoreText_GameComplete != null) finalScoreText_GameComplete.text = finalScoreStr;
            if (gameCompletePanel != null) { gameCompletePanel.SetActive(true); StartCoroutine(Fade(gameCompletePanelCG, 0f, 1f, 0.5f)); }
        }
        else
        {
            if (finalTimeText_LevelComplete != null) finalTimeText_LevelComplete.text = finalTimeStr;
            if (finalScoreText_LevelComplete != null) finalScoreText_LevelComplete.text = finalScoreStr;
            if (levelCompletePanel != null) { levelCompletePanel.SetActive(true); StartCoroutine(Fade(levelCompletePanelCG, 0f, 1f, 0.5f)); }
        }
    }
    // 【【【【【【 修改结束 1 】】】】】】

    private IEnumerator Fade(CanvasGroup cg, float startAlpha, float endAlpha, float duration, System.Action onComplete = null) { if (cg == null) { onComplete?.Invoke(); yield break; } float elapsedTime = 0f; cg.blocksRaycasts = false; while (elapsedTime < duration) { elapsedTime += Time.unscaledDeltaTime; cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration); yield return null; } cg.alpha = endAlpha; if (endAlpha > 0) { cg.interactable = true; cg.blocksRaycasts = true; } else { cg.interactable = false; } onComplete?.Invoke(); }
    private IEnumerator AnimateElementsIn() { for (int i = 0; i < currentSlots.Count; i++) { StartCoroutine(currentSlots[i].GetComponent<UIElementAnimator>().AnimateInCoroutine()); if (UISoundManager.instance != null) UISoundManager.instance.PlaySlotAppearSound(); yield return new WaitForSeconds(staggerDelay); } for (int i = 0; i < createdWords.Count; i++) { StartCoroutine(createdWords[i].GetComponent<UIElementAnimator>().AnimateInCoroutine()); if (UISoundManager.instance != null) UISoundManager.instance.PlayWordAppearSound(); yield return new WaitForSeconds(staggerDelay); } }
    public void HandleWordPlacement(DraggableWord droppedWord, SentenceSlot targetSlot) { if (isChecking) return; if (targetSlot.currentWord != null && targetSlot.currentWord != droppedWord) { DraggableWord existingWord = targetSlot.currentWord; existingWord.transform.SetParent(wordSpawnArea); existingWord.AnimateMoveTo(wordSpawnArea.TransformPoint(GetRandomPositionInRect(wordSpawnArea.rect, existingWord.GetComponent<RectTransform>().rect)), ejectAnimationDuration); } var oldSlot = currentSlots.FirstOrDefault(s => s.currentWord == droppedWord); if (oldSlot != null) { oldSlot.Clear(); } targetSlot.PlaceWord(droppedWord); if (UISoundManager.instance != null) { UISoundManager.instance.PlayCardPlaceSound(); } CheckIfReadyForValidation(); }
    void CheckIfReadyForValidation() { if (currentSlots.All(slot => slot.currentWord != null) && !isChecking) { isChecking = true; StartCoroutine(ValidateAnswerCoroutine()); } }
    void EjectAllWords() { isChecking = false; List<Vector2> newEjectPositions = new List<Vector2>(); foreach (var slot in currentSlots) { if (slot.currentWord != null) { DraggableWord wordToEject = slot.currentWord; wordToEject.transform.SetParent(wordSpawnArea); RectTransform wordRect = wordToEject.GetComponent<RectTransform>(); Rect spawnAreaRect = wordSpawnArea.rect; Vector2 ejectPos; int attempts = 0; do { ejectPos = GetRandomPositionInRect(spawnAreaRect, wordRect.rect); attempts++; } while ((createdWords.Any(w => w != wordToEject && Vector2.Distance(w.GetComponent<RectTransform>().anchoredPosition, ejectPos) < wordRect.rect.width) || newEjectPositions.Any(p => Vector2.Distance(p, ejectPos) < wordRect.rect.width)) && attempts < 200); newEjectPositions.Add(ejectPos); wordToEject.AnimateMoveTo(wordSpawnArea.TransformPoint(ejectPos), ejectAnimationDuration); slot.Clear(); } } }
    private Vector2 GetRandomPositionInRect(Rect areaRect, Rect elementRect) { float randomX = Random.Range(areaRect.xMin + (elementRect.width / 2), areaRect.xMax - (elementRect.width / 2)); float randomY = Random.Range(areaRect.yMin + (elementRect.height / 2), areaRect.yMax - (elementRect.height / 2)); return new Vector2(randomX, randomY); }
    void UpdateScoreDisplay() { if (scoreText != null) scoreText.text = "积分: " + currentScore; }

    // 【【【【【【 这里是修改点 2 】】】】】】
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
                StartCoroutine(Fade(testPlayPausePanelCG, 0f, 1f, 0.2f, () => { Time.timeScale = 0f; }));
            }
            else
            {
                Time.timeScale = 1f;
                if (testPlayPausePanelCG != null) testPlayPausePanelCG.interactable = false;
                StartCoroutine(Fade(testPlayPausePanelCG, 1f, 0f, 0.2f, () => { if (testPlayPausePanel != null) testPlayPausePanel.SetActive(false); }));
            }
        }
        else // (旧) 正常模式逻辑
        {
            if (isPaused)
            {
                pausePanel.SetActive(true);
                StartCoroutine(Fade(pausePanelCG, 0f, 1f, 0.2f, () => { Time.timeScale = 0f; }));
            }
            else
            {
                Time.timeScale = 1f;
                StartCoroutine(Fade(pausePanelCG, 1f, 0f, 0.2f, () => { if (pausePanel != null) { pausePanel.SetActive(false); } }));
            }
        }
    }
    // 【【【【【【 修改结束 2 】】】】】】

    public void GoToMainMenu() { Time.timeScale = 1f; if (LevelManager.instance != null) LevelManager.instance.LoadMainMenu(); else SceneManager.LoadScene("MainMenu"); }
    public void LoadNextLevel() { Time.timeScale = 1f; if (LevelManager.instance != null) LevelManager.instance.LoadNextLevel(); }
    public void RestartCurrentLevel() { Time.timeScale = 1f; if (LevelManager.instance != null) LevelManager.instance.ReloadCurrentLevel(); else SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public void RestartFromFirstLevel() { Time.timeScale = 1f; if (LevelManager.instance != null) LevelManager.instance.RestartGame(); }
    #endregion
    #region 试玩按钮响应函数

    // (你的试玩按钮函数保持不变)
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
    public void OnClick_TestPlay_ReturnToEditor_Fail()
    {
        Time.timeScale = 1f;
        if (LevelManager.instance != null)
        {
            LevelManager.instance.ReturnToEditor(didWin: false);
        }
    }
    public void OnClick_TestPlay_ReturnToEditor_Win()
    {
        Time.timeScale = 1f;
        if (LevelManager.instance != null)
        {
            LevelManager.instance.ReturnToEditor(didWin: true);
        }
    }
    public void OnClick_TestPlay_ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        if (LevelManager.instance != null)
        {
            LevelManager.instance.LoadMainMenuAfterTestWin();
        }
    }
    #endregion
}