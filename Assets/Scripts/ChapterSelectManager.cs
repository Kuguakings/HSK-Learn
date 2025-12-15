/// <summary>
/// 章节选择管理器 / Chapter Select Manager
/// 根据游戏模式显示可用章节，数据来自 TcbManager / Display available chapters based on game mode, data from TcbManager
/// </summary>
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // 用于 .Distinct() 和 .Where() 方法 / For .Distinct() and .Where() methods

public class ChapterSelectManager : MonoBehaviour
{
    [Header("UI组件引用 / UI Component References")]
    public TextMeshProUGUI titleText;           // 标题文本 / Title text
    public GameObject chapterButtonPrefab;      // 章节按钮预制体 / Chapter button prefab
    public Transform buttonContainer;           // 按钮容器 / Button container
    public LevelSelectManager levelSelectManager; // 关卡选择管理器引用 / Level select manager reference

    private GameMode currentMode;   // 当前游戏模式 / Current game mode
    private CanvasGroup canvasGroup; // 画布组组件 / Canvas group component

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
            titleText.text = "���������� - ѡ���½�";
        }
        else if (mode == GameMode.WordLinkUp)
        {
            titleText.text = "���������� - ѡ���½�";
        }

        StartCoroutine(FadeCanvasGroup(0f, 1f, 0.3f));

        // �������ش��޸ġ����� TcbManager �����½ڡ�����
        PopulateChapterButtons();
    }

    void PopulateChapterButtons()
    {
        // �����ɰ�ť
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // ������ �ع� ������
        if (TcbManager.AllLevels == null || TcbManager.AllLevels.levels == null)
        {
            Debug.LogError("TcbManager.AllLevels Ϊ�գ��޷������½ڣ�");
            return;
        }

        // �޸ĺ� (������ Where !string.IsNullOrEmpty)
        List<string> chapterNames = TcbManager.AllLevels.levels
            .Where(level => level.mode == (int)currentMode)
            .Select(level => level.chapter)
            .Where(name => !string.IsNullOrEmpty(name)) // <--- �����������˵������֣��������鰴ť��
            .Distinct()
            .OrderBy(name => name)
            .ToList();
        // ������ �ع����� ������

        // �����½��б���������ť
        foreach (var chapterName in chapterNames)
        {
            GameObject buttonGO = Instantiate(chapterButtonPrefab, buttonContainer);

            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = chapterName;
            }

            Button button = buttonGO.GetComponent<Button>();
            string capturedChapterName = chapterName; // �������
            button.onClick.AddListener(() => {
                OnChapterButtonClick(capturedChapterName);
            });
        }
    }

    // (�˺���ԭ�ⲻ��)
    void OnChapterButtonClick(string chapterName)
    {
        Debug.Log("ѡ����ģʽ " + currentMode + " ���½�: " + chapterName);
        LevelManager.selectedChapterName = chapterName;
        StartCoroutine(FadeOutAndShowLevelSelect());
    }

    // (�˺���ԭ�ⲻ��)
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
            Debug.LogError("LevelSelectManager ����δ���ã�");
        }
    }

    // (�˺���ԭ�ⲻ��)
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