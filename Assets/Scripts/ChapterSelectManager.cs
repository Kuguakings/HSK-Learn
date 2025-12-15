// ChapterSelectManager.cs (【【【已重构为 TcbManager】】】)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // 【新】为 .Distinct() 和 .Where() 导入

public class ChapterSelectManager : MonoBehaviour
{
    [Header("UI组件引用")]
    public TextMeshProUGUI titleText;
    public GameObject chapterButtonPrefab;
    public Transform buttonContainer;
    public LevelSelectManager levelSelectManager;

    private GameMode currentMode;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    public void Show(GameMode mode)
    {
        currentMode = mode;
        LevelManager.selectedGameMode = mode;
        gameObject.SetActive(true);

        if (mode == GameMode.WordMatch3)
        {
            titleText.text = "单词消消乐 - 选择章节";
        }
        else if (mode == GameMode.WordLinkUp)
        {
            titleText.text = "词语连连看 - 选择章节";
        }

        StartCoroutine(FadeCanvasGroup(0f, 1f, 0.3f));

        // 【【【重大修改】：从 TcbManager 加载章节】】】
        PopulateChapterButtons();
    }

    void PopulateChapterButtons()
    {
        // 清理旧按钮
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // 【【【 重构 】】】
        if (TcbManager.AllLevels == null || TcbManager.AllLevels.levels == null)
        {
            Debug.LogError("TcbManager.AllLevels 为空！无法加载章节！");
            return;
        }

        // 修改后 (增加了 Where !string.IsNullOrEmpty)
        List<string> chapterNames = TcbManager.AllLevels.levels
            .Where(level => level.mode == (int)currentMode)
            .Select(level => level.chapter)
            .Where(name => !string.IsNullOrEmpty(name)) // <--- 【新增】过滤掉空名字，消灭幽灵按钮！
            .Distinct()
            .OrderBy(name => name)
            .ToList();
        // 【【【 重构结束 】】】

        // 遍历章节列表，创建按钮
        foreach (var chapterName in chapterNames)
        {
            GameObject buttonGO = Instantiate(chapterButtonPrefab, buttonContainer);

            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = chapterName;
            }

            Button button = buttonGO.GetComponent<Button>();
            string capturedChapterName = chapterName; // 捕获变量
            button.onClick.AddListener(() => {
                OnChapterButtonClick(capturedChapterName);
            });
        }
    }

    // (此函数原封不动)
    void OnChapterButtonClick(string chapterName)
    {
        Debug.Log("选择了模式 " + currentMode + " 的章节: " + chapterName);
        LevelManager.selectedChapterName = chapterName;
        StartCoroutine(FadeOutAndShowLevelSelect());
    }

    // (此函数原封不动)
    private IEnumerator FadeOutAndShowLevelSelect()
    {
        yield return StartCoroutine(FadeCanvasGroup(1f, 0f, 0.3f));
        gameObject.SetActive(false);
        if (levelSelectManager != null)
        {
            levelSelectManager.Show(currentMode);
        }
        else
        {
            Debug.LogError("LevelSelectManager 引用未设置！");
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