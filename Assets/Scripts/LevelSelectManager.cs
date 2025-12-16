// LevelSelectManager.cs (���������ع�Ϊ TcbManager������)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // ���¡�Ϊ .Where() �� .OrderBy() ����

public class LevelSelectManager : MonoBehaviour
{
    [Header("UI�������")]
    public TextMeshProUGUI titleText;

    [Header("Ԥ���������")]
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

        // ���¡���������Ҳ��ʾ�½���
        if (mode == GameMode.WordMatch3)
        {
            titleText.text = $"���������� - {LevelManager.selectedChapterName}";
        }
        else if (mode == GameMode.WordLinkUp)
        {
            titleText.text = $"���������� - {LevelManager.selectedChapterName}";
        }

        StartCoroutine(FadeCanvasGroup(0f, 1f, 0.3f));

        // �������ش��޸ġ����� TcbManager ���عؿ�������
        PopulateLevelButtons();
    }

    void PopulateLevelButtons()
    {
        // �����ɰ�ť
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // �������ش��޸ġ����� TcbManager ��ȡ���ݡ�����
        // ������ �ع� ������
        if (TcbManager.AllLevels == null || TcbManager.AllLevels.levels == null)
        {
            Debug.LogError("TcbManager.AllLevels Ϊ�գ��޷����عؿ���");
            return;
        }

        // 1. ���ݡ�ģʽ���͡���ѡ�½ڡ�ɸѡ
        // 2. ���ݡ��ؿ��š� (level) ����
        List<LevelData> levelsForThisChapter = TcbManager.AllLevels.levels
            .Where(l => l.mode == (long)currentMode && l.chapter == LevelManager.selectedChapterName)
            .OrderBy(l => l.level)
            .ToList();
        // ������ �ع����� ������

        if (levelsForThisChapter.Count == 0)
        {
            Debug.LogWarning($"�� TCB ���Ҳ��� {LevelManager.selectedChapterName} (ģʽ: {currentMode}) ���κιؿ���");
            return;
        }

        // �����ؿ��б���������ť
        foreach (var levelData in levelsForThisChapter)
        {
            GameObject buttonGO = Instantiate(levelButtonPrefab, buttonContainer);

            // ���ð�ť�ϵ��ı�Ϊ�ؿ���
            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = levelData.level.ToString();
            }

            // ���ӵ���¼�
            Button button = buttonGO.GetComponent<Button>();

            // ���¡����������� levelData ����
            LevelData capturedLevelData = levelData;

            button.onClick.AddListener(() => {
                // 【游客限制】：游客只能玩前5关
                if (TcbManager.UserLevel == -1 && capturedLevelData.level > 5)
                {
                    ShowGuestUpgradePrompt();
                }
                else
                {
                    OnLevelButtonClick(capturedLevelData);
                }
            });
        }
    }

    // 显示游客转正提示
    private void ShowGuestUpgradePrompt()
    {
        Debug.Log("[LevelSelectManager] Guest tried to access content beyond level 5.");
        
        // 使用Unity的原生对话框或自定义弹窗
        // 这里先用简单的方式：显示登录面板
        if (TcbManager.instance != null && TcbManager.instance.loginCanvasGroup != null)
        {
            if (TcbManager.instance.statusText != null)
            {
                TcbManager.instance.statusText.text = "Guests can only play first 5 levels. Please upgrade.";
            }
            
            TcbManager.instance.loginCanvasGroup.alpha = 1;
            TcbManager.instance.loginCanvasGroup.interactable = true;
            TcbManager.instance.loginCanvasGroup.blocksRaycasts = true;
            TcbManager.instance.loginCanvasGroup.gameObject.SetActive(true);
        }
    }

    // �������ش��޸ġ��������� int index ��Ϊ LevelData������
    void OnLevelButtonClick(LevelData dataToLoad)
    {
        Debug.Log($"ѡ���˹ؿ�: {dataToLoad.id} (�½�: {dataToLoad.chapter}, ģʽ: {dataToLoad.mode})");

        LevelManager.selectedGameMode = currentMode;

        if (LevelManager.instance != null)
        {
            // ���¡����ǰѡ��������Ĺؿ����ݴ��ݸ� LevelManager
            LevelManager.instance.LoadLevel(dataToLoad);
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