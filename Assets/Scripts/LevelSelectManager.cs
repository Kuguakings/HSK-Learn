// LevelSelectManager.cs (【【【已重构为 TcbManager】】】)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // 【新】为 .Where() 和 .OrderBy() 导入

public class LevelSelectManager : MonoBehaviour
{
    [Header("UI组件引用")]
    public TextMeshProUGUI titleText;

    [Header("预制体和容器")]
    public GameObject levelButtonPrefab;
    public Transform buttonContainer;

    private GameMode currentMode;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    public void Show(GameMode mode)
    {
        currentMode = mode;
        gameObject.SetActive(true);

        // 【新】标题现在也显示章节名
        if (mode == GameMode.WordMatch3)
        {
            titleText.text = $"单词消消乐 - {LevelManager.selectedChapterName}";
        }
        else if (mode == GameMode.WordLinkUp)
        {
            titleText.text = $"词语连连看 - {LevelManager.selectedChapterName}";
        }

        StartCoroutine(FadeCanvasGroup(0f, 1f, 0.3f));

        // 【【【重大修改】：从 TcbManager 加载关卡】】】
        PopulateLevelButtons();
    }

    void PopulateLevelButtons()
    {
        // 清理旧按钮
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // 【【【重大修改】：从 TcbManager 读取数据】】】
        // 【【【 重构 】】】
        if (TcbManager.AllLevels == null || TcbManager.AllLevels.levels == null)
        {
            Debug.LogError("TcbManager.AllLevels 为空！无法加载关卡！");
            return;
        }

        // 1. 根据“模式”和“已选章节”筛选
        // 2. 根据“关卡号” (level) 排序
        List<LevelData> levelsForThisChapter = TcbManager.AllLevels.levels
            .Where(l => l.mode == (long)currentMode && l.chapter == LevelManager.selectedChapterName)
            .OrderBy(l => l.level)
            .ToList();
        // 【【【 重构结束 】】】

        if (levelsForThisChapter.Count == 0)
        {
            Debug.LogWarning($"在 TCB 中找不到 {LevelManager.selectedChapterName} (模式: {currentMode}) 的任何关卡。");
            return;
        }

        // 遍历关卡列表，创建按钮
        foreach (var levelData in levelsForThisChapter)
        {
            GameObject buttonGO = Instantiate(levelButtonPrefab, buttonContainer);

            // 设置按钮上的文本为关卡号
            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = levelData.level.ToString();
            }

            // 添加点击事件
            Button button = buttonGO.GetComponent<Button>();

            // 【新】捕获完整的 levelData 对象
            LevelData capturedLevelData = levelData;

            button.onClick.AddListener(() => {
                OnLevelButtonClick(capturedLevelData);
            });
        }
    }

    // 【【【重大修改】：参数从 int index 变为 LevelData】】】
    void OnLevelButtonClick(LevelData dataToLoad)
    {
        Debug.Log($"选择了关卡: {dataToLoad.id} (章节: {dataToLoad.chapter}, 模式: {dataToLoad.mode})");

        LevelManager.selectedGameMode = currentMode;

        if (LevelManager.instance != null)
        {
            // 【新】我们把“完整”的关卡数据传递给 LevelManager
            LevelManager.instance.LoadLevel(dataToLoad);
        }
    }

    // (此函数原封不动)
    private IEnumerator FadeCanvasGroup(float startAlpha, float endAlpha, float duration)
    {
        canvasGroup.interactable = false;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            yield return null;
        }
        canvasGroup.alpha = endAlpha;
        if (endAlpha > 0) { canvasGroup.interactable = true; canvasGroup.blocksRaycasts = true; }
        else { canvasGroup.interactable = false; canvasGroup.blocksRaycasts = false; }
    }
}