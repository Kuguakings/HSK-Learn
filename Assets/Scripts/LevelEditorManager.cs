// LevelEditorManager.cs (V5 - 【【【已重构为 TcbManager】】】)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using System.Runtime.InteropServices;

public class LevelEditorManager : MonoBehaviour
{
    [Header("动画参数")]
    public float panelFadeDuration = 0.3f;

    #region 变量
    [Header("编辑器测试")]
    [Tooltip("【仅编辑器模式】: 用于在未从 MainMenu 启动时自动创建 LevelManager。")]
    public GameObject levelManagerPrefab;

    [Header("UI 控件")]
    public Button backToMenuButton;
    public TextMeshProUGUI statusText;

    [Header("模式选择 (主面板)")]
    public GameObject mainPanel;
    public TMP_Dropdown modeSelectDropdown;
    public GameObject mode1EditorPanel;
    public GameObject mode2EditorPanel;

    #region 模式 1 UI 引用
    [Header("模式 1：章节选择")]
    public GameObject chapterSelectPanel_M1;
    public Button hsk1Button_M1;
    public Button hsk2Button_M1;
    public Button hsk3Button_M1;
    public Button hsk4Button_M1;
    public Button chapterPanelBackButton_M1;

    [Header("模式 1：关卡选择")]
    public GameObject levelSelectPanel_M1;
    public Button levelPanelBackButton_M1;
    public Button addLevelButton_M1;
    public Transform levelListContainer_M1;
    public GameObject levelInEditorButtonPrefab_M1;

    [Header("模式 1：内容编辑器")]
    public GameObject contentEditorPanel_M1;
    public Button contentEditorBackButton_M1;
    public Button addRowButton_M1;
    public Transform contentListContainer_M1;
    public GameObject m1EditorCellPrefab;
    public Button saveButton_M1;
    public Button testPlayButton_M1;
    public Button publishButton_M1;
    public CanvasGroup publishButtonCG;
    public Button batchPasteButton_M1;

    [Header("模式 1：添加关卡弹窗")]
    public GameObject addLevelPopupPanel_M1;
    public TMP_InputField newLevelInputfield_M1;
    public Button addLevelConfirmButton_M1;
    public Button addLevelCancelButton_M1;

    [Header("模式 1：删除行弹窗")]
    public GameObject deleteRowPopupPanel;
    public Button deleteConfirmButton;
    public Button deleteCancelButton;
    private int m1_cellIndexToDelete = -1;

    [Header("模式 1：批量粘贴弹窗")]
    public GameObject batchPastePopupPanel;
    public TMP_InputField pasteInputField;
    public Transform pastePreviewContainer;
    public Button pasteConfirmImportButton;
    public Button pasteCancelButton;
    #endregion

    #region 模式 2 UI 引用
    [Header("模式 2：章节选择")]
    public GameObject chapterSelectPanel_M2;
    public Button hsk1Button_M2;
    public Button hsk2Button_M2;
    public Button hsk3Button_M2;
    public Button hsk4Button_M2;
    public Button chapterPanelBackButton_M2;

    [Header("模式 2：关卡选择")]
    public GameObject levelSelectPanel_M2;
    public Button levelPanelBackButton_M2;
    public Button addLevelButton_M2;
    public Transform levelListContainer_M2;

    [Header("模式 2：内容编辑器 (V6 布局)")]
    public GameObject contentEditorPanel_M2;
    public TMP_InputField detail_SentenceIdInput;
    public TMP_InputField detail_FullSentenceInput;
    public Button contentEditorBackButton_M2;
    public Button addSentenceButton_M2;
    public Button globalAutoSplitButton_M2;
    public Button batchPasteButton_M2;
    public Button addWordButton_M2;
    public Button detailSaveButton_M2;
    public Button resplitButton_M2;
    public Button mergeUpButton_M2;
    public Button mergeDownButton_M2;
    public Button deleteRowButton_M2;
    public Transform sentenceInputContainer_M2;
    public GameObject m2SentenceInputPrefab;
    public Transform wordListContainer_M2;
    public GameObject m2WordRowPrefab;

    [Header("模式 2：全局按钮 (M2)")]
    public Button saveButton_M2;
    public Button testPlayButton_M2;
    public Button publishButton_M2;
    public CanvasGroup publishButtonCG_M2;

    [Header("模式 2：批量粘贴弹窗")]
    public GameObject batchPastePopupPanel_M2;
    public TMP_InputField pasteInputField_M2;
    public Transform pastePreviewContainer_M2;
    public Button pasteConfirmImportButton_M2;
    public Button pasteCancelButton_M2;
    private List<Mode2Content> parsedPasteSentences;

    [Header("模式 2：添加关卡弹窗")]
    public GameObject addLevelPopupPanel_M2;
    public TMP_InputField newLevelInputfield_M2;
    public Button addLevelConfirmButton_M2;
    public Button addLevelCancelButton_M2;
    #endregion

    // (内部状态)
    private string currentEditingChapter_M1;
    private LevelData currentEditingLevel_M1;
    private List<Mode1Content> parsedPasteData;
    private string currentEditingChapter_M2;
    private LevelData currentEditingLevel_M2;
    private M2_SentenceInputRow currentSelectedSentenceRow;
    private M2_WordRow currentSelectedWordRow;

    // --- (CanvasGroup 引用) ---
    private CanvasGroup mainPanelCG;
    private CanvasGroup mode1EditorPanelCG;
    private CanvasGroup mode2EditorPanelCG;
    private CanvasGroup chapterSelectPanel_M1_CG;
    private CanvasGroup levelSelectPanel_M1_CG;
    private CanvasGroup contentEditorPanel_M1_CG;
    private CanvasGroup chapterSelectPanel_M2_CG;
    private CanvasGroup levelSelectPanel_M2_CG;
    private CanvasGroup contentEditorPanel_M2_CG;
    private CanvasGroup deleteRowPopupPanel_CG;
    private CanvasGroup addLevelPopupPanel_M1_CG;
    private CanvasGroup batchPastePopupPanel_CG;
    private CanvasGroup addLevelPopupPanel_M2_CG;
    private CanvasGroup batchPastePopupPanel_M2_CG;
    private List<CanvasGroup> allNavPanelCGs = new List<CanvasGroup>();

    // --- 【【【V5 修复：脏检查 标记】】】 ---
    private bool isDirty_M1 = false;
    private bool isDirty_M2 = false;

    private GameObject currentEditingCell_M1; // 用于记住我们正在编辑哪个单元格

    [DllImport("__Internal")]
    private static extern void JsShowNativePrompt(string existingText, string objectName, string callbackSuccess);

    #endregion

    void Start()
    {
        // 1. 【【【编辑器模式修复】】】
#if UNITY_EDITOR
        Debug.LogWarning("【编辑器测试模式】：自动将权限设为 Admin。");
        // 【【【 重构 】】】
        TcbManager.IsAdmin = true; 

        if (TcbManager.AllLevels == null)
        {
            Debug.LogWarning("【编辑器测试模式】：TcbManager.AllLevels 为空，已自动初始化。");
            TcbManager.AllLevels = new LevelDataCollection();
        }
        // 【【【 重构结束 】】】

        if (LevelManager.instance == null)
        {
            Debug.LogWarning("【编辑器测试模式】：LevelManager 实例未找到，正在从 Prefab 自动创建。");
            if (levelManagerPrefab != null)
            {
                GameObject lmGO = Instantiate(levelManagerPrefab);
                LevelManager newLM = lmGO.GetComponent<LevelManager>();
                if (newLM != null)
                {
                    newLM.ManuallyTriggerFadeOut();
                }
            }
            else
            {
                Debug.LogError("【编辑器测试模式】: 严重错误! LevelManager Prefab 未在 LevelEditorManager 中设置! 试玩功能将失败。");
            }
        }
#endif
        // 【【【编辑器修复结束】】】

        // 2. 【【【初始化 CanvasGroups】】】
        mainPanelCG = mainPanel.GetComponent<CanvasGroup>() ?? mainPanel.AddComponent<CanvasGroup>();
        mode1EditorPanelCG = mode1EditorPanel.GetComponent<CanvasGroup>() ?? mode1EditorPanel.AddComponent<CanvasGroup>();
        mode2EditorPanelCG = mode2EditorPanel.GetComponent<CanvasGroup>() ?? mode2EditorPanel.AddComponent<CanvasGroup>();
        chapterSelectPanel_M1_CG = chapterSelectPanel_M1.GetComponent<CanvasGroup>() ?? chapterSelectPanel_M1.AddComponent<CanvasGroup>();
        levelSelectPanel_M1_CG = levelSelectPanel_M1.GetComponent<CanvasGroup>() ?? levelSelectPanel_M1.AddComponent<CanvasGroup>();
        contentEditorPanel_M1_CG = contentEditorPanel_M1.GetComponent<CanvasGroup>() ?? contentEditorPanel_M1.AddComponent<CanvasGroup>();
        chapterSelectPanel_M2_CG = chapterSelectPanel_M2.GetComponent<CanvasGroup>() ?? chapterSelectPanel_M2.AddComponent<CanvasGroup>();
        levelSelectPanel_M2_CG = levelSelectPanel_M2.GetComponent<CanvasGroup>() ?? levelSelectPanel_M2.AddComponent<CanvasGroup>();
        contentEditorPanel_M2_CG = contentEditorPanel_M2.GetComponent<CanvasGroup>() ?? contentEditorPanel_M2.AddComponent<CanvasGroup>();

        allNavPanelCGs.Add(mainPanelCG);
        allNavPanelCGs.Add(mode1EditorPanelCG);
        allNavPanelCGs.Add(mode2EditorPanelCG);
        allNavPanelCGs.Add(chapterSelectPanel_M1_CG);
        allNavPanelCGs.Add(levelSelectPanel_M1_CG);
        allNavPanelCGs.Add(contentEditorPanel_M1_CG);
        allNavPanelCGs.Add(chapterSelectPanel_M2_CG);
        allNavPanelCGs.Add(levelSelectPanel_M2_CG);
        allNavPanelCGs.Add(contentEditorPanel_M2_CG);

        if (deleteRowPopupPanel != null)
        {
            deleteRowPopupPanel_CG = deleteRowPopupPanel.GetComponent<CanvasGroup>() ?? deleteRowPopupPanel.AddComponent<CanvasGroup>();
            deleteRowPopupPanel_CG.interactable = false;
            deleteRowPopupPanel_CG.blocksRaycasts = false;
        }
        if (addLevelPopupPanel_M1 != null) addLevelPopupPanel_M1_CG = addLevelPopupPanel_M1.GetComponent<CanvasGroup>() ?? addLevelPopupPanel_M1.AddComponent<CanvasGroup>();
        if (batchPastePopupPanel != null) batchPastePopupPanel_CG = batchPastePopupPanel.GetComponent<CanvasGroup>() ?? batchPastePopupPanel.AddComponent<CanvasGroup>();
        if (addLevelPopupPanel_M2 != null) addLevelPopupPanel_M2_CG = addLevelPopupPanel_M2.GetComponent<CanvasGroup>() ?? addLevelPopupPanel_M2.AddComponent<CanvasGroup>();
        if (batchPastePopupPanel_M2 != null) batchPastePopupPanel_M2_CG = batchPastePopupPanel_M2.GetComponent<CanvasGroup>() ?? batchPastePopupPanel_M2.AddComponent<CanvasGroup>();

        // 3. 【【【原始 Start 逻辑】】】
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(GoToMainMenu);

        // 【【【 重构 】】】
        if (!TcbManager.IsAdmin)
        {
            if (statusText != null) { statusText.text = "错误：您没有管理员权限，无法使用此编辑器！"; statusText.color = Color.red; }
            if (modeSelectDropdown != null) modeSelectDropdown.gameObject.SetActive(false);
            if (mode1EditorPanel != null) mode1EditorPanel.gameObject.SetActive(false);
            if (mode2EditorPanel != null) mode2EditorPanel.gameObject.SetActive(false);
            return;
        }
        else
        {
            if (statusText != null) { statusText.text = "管理员您好，请选择您要创建的关卡模式。"; statusText.color = Color.white; }
        }

        #region 按钮绑定 (模式 1)
        SetupModeDropdown();
        if (hsk1Button_M1 != null) hsk1Button_M1.onClick.AddListener(() => { M1_OnClick_SelectChapter("HSK1"); });
        if (hsk2Button_M1 != null) hsk2Button_M1.onClick.AddListener(() => { M1_OnClick_SelectChapter("HSK2"); });
        if (hsk3Button_M1 != null) hsk3Button_M1.onClick.AddListener(() => { M1_OnClick_SelectChapter("HSK3"); });
        if (hsk4Button_M1 != null) hsk4Button_M1.onClick.AddListener(() => { M1_OnClick_SelectChapter("HSK4"); });
        if (chapterPanelBackButton_M1 != null) chapterPanelBackButton_M1.onClick.AddListener(ResetToModeSelect);
        if (levelPanelBackButton_M1 != null) levelPanelBackButton_M1.onClick.AddListener(() => M1_OnClick_BackToChapterSelect(false));
        if (addLevelButton_M1 != null) addLevelButton_M1.onClick.AddListener(M1_OnClick_AddLevel);
        if (addLevelPopupPanel_M1 != null) addLevelPopupPanel_M1.gameObject.SetActive(false);
        if (addLevelConfirmButton_M1 != null) addLevelConfirmButton_M1.onClick.AddListener(M1_OnClick_AddLevel_Confirm);
        if (addLevelCancelButton_M1 != null) addLevelCancelButton_M1.onClick.AddListener(M1_OnClick_AddLevel_Cancel);
        if (contentEditorPanel_M1 != null) contentEditorPanel_M1.gameObject.SetActive(false);
        if (contentEditorBackButton_M1 != null) contentEditorBackButton_M1.onClick.AddListener(M1_OnClick_ContentEditor_Back);
        if (addRowButton_M1 != null) addRowButton_M1.onClick.AddListener(M1_OnClick_AddRow);
        if (saveButton_M1 != null) saveButton_M1.onClick.AddListener(M1_OnClick_SaveLevel);
        if (testPlayButton_M1 != null) testPlayButton_M1.onClick.AddListener(M1_OnClick_TestPlay);
        if (publishButton_M1 != null) publishButton_M1.onClick.AddListener(M1_OnClick_Publish);
        if (deleteRowPopupPanel != null) deleteRowPopupPanel.gameObject.SetActive(false);
        if (batchPastePopupPanel != null) batchPastePopupPanel.gameObject.SetActive(false);
        if (batchPasteButton_M1 != null) batchPasteButton_M1.onClick.AddListener(M1_OnClick_BatchPaste);
        if (pasteInputField != null) pasteInputField.onValueChanged.AddListener(M1_OnPasteInputChanged);
        if (pasteConfirmImportButton != null) pasteConfirmImportButton.onClick.AddListener(M1_OnClick_Paste_ConfirmImport);
        if (pasteCancelButton != null) pasteCancelButton.onClick.AddListener(M1_OnClick_Paste_Cancel);
        if (deleteConfirmButton != null) deleteConfirmButton.onClick.AddListener(M1_ConfirmDeleteRow);
        if (deleteCancelButton != null) deleteCancelButton.onClick.AddListener(M1_CancelDeleteRow);
        #endregion

        #region 按钮绑定 (模式 2)
        if (hsk1Button_M2 != null) hsk1Button_M2.onClick.AddListener(() => { M2_OnClick_SelectChapter("HSK1"); });
        if (hsk2Button_M2 != null) hsk2Button_M2.onClick.AddListener(() => { M2_OnClick_SelectChapter("HSK2"); });
        if (hsk3Button_M2 != null) hsk3Button_M2.onClick.AddListener(() => { M2_OnClick_SelectChapter("HSK3"); });
        if (hsk4Button_M2 != null) hsk4Button_M2.onClick.AddListener(() => { M2_OnClick_SelectChapter("HSK4"); });
        if (chapterPanelBackButton_M2 != null) chapterPanelBackButton_M2.onClick.AddListener(ResetToModeSelect);
        if (levelPanelBackButton_M2 != null) levelPanelBackButton_M2.onClick.AddListener(() => M2_OnClick_BackToChapterSelect(false));
        if (addLevelButton_M2 != null) addLevelButton_M2.onClick.AddListener(M2_OnClick_AddLevel);
        if (addLevelPopupPanel_M2 != null) addLevelPopupPanel_M2.gameObject.SetActive(false);
        if (addLevelConfirmButton_M2 != null) addLevelConfirmButton_M2.onClick.AddListener(M2_OnClick_AddLevel_Confirm);
        if (addLevelCancelButton_M2 != null) addLevelCancelButton_M2.onClick.AddListener(M2_OnClick_AddLevel_Cancel);
        if (contentEditorPanel_M2 != null) contentEditorPanel_M2.gameObject.SetActive(false);
        if (contentEditorBackButton_M2 != null) contentEditorBackButton_M2.onClick.AddListener(M2_OnClick_ContentEditor_Back);
        if (addSentenceButton_M2 != null) addSentenceButton_M2.onClick.AddListener(M2_OnClick_AddSentence);
        if (globalAutoSplitButton_M2 != null) globalAutoSplitButton_M2.onClick.AddListener(M2_OnClick_GlobalSplit);
        if (addWordButton_M2 != null) addWordButton_M2.onClick.AddListener(M2_OnClick_AddWord);
        if (detailSaveButton_M2 != null) detailSaveButton_M2.onClick.AddListener(M2_OnClick_DetailSave);
        if (resplitButton_M2 != null) resplitButton_M2.onClick.AddListener(M2_OnRequestAutoSplit_Current);
        if (deleteRowButton_M2 != null) deleteRowButton_M2.onClick.AddListener(M2_OnClick_DeleteWord);
        if (mergeUpButton_M2 != null) mergeUpButton_M2.onClick.AddListener(M2_OnClick_MergeUp);
        if (mergeDownButton_M2 != null) mergeDownButton_M2.onClick.AddListener(M2_OnClick_MergeDown);
        if (saveButton_M2 != null) saveButton_M2.onClick.AddListener(OnClick_Save_M2);
        if (testPlayButton_M2 != null) testPlayButton_M2.onClick.AddListener(OnClick_TestPlay_M2);
        if (publishButton_M2 != null) publishButton_M2.onClick.AddListener(OnClick_Publish_M2);
        if (batchPastePopupPanel_M2 != null) batchPastePopupPanel_M2.gameObject.SetActive(false);
        if (batchPasteButton_M2 != null) batchPasteButton_M2.onClick.AddListener(M2_OnClick_BatchPaste);
        if (pasteInputField_M2 != null) { pasteInputField_M2.onValueChanged.AddListener(M2_OnPasteInputChanged); }
        if (pasteConfirmImportButton_M2 != null) pasteConfirmImportButton_M2.onClick.AddListener(M2_OnClick_Paste_ConfirmImport);
        if (pasteCancelButton_M2 != null) pasteCancelButton_M2.onClick.AddListener(M2_OnClick_Paste_Cancel);
        M2_UpdateContextualButtons(null);
        #endregion

        // 4. 【【【V4 导航逻辑】】】
        if (LevelManager.justReturnedFromTest)
        {
            Debug.Log("检测到从“试玩”返回！");
            if (LevelManager.selectedGameMode == GameMode.WordMatch3)
            {
                ShowPanelInstant(contentEditorPanel_M1_CG);
                // 【【【 重构 】】】
                LevelData dataToOpen = TcbManager.AllLevels.levels.Find(l => l.id == LevelManager.selectedLevelData.id);
                if (dataToOpen != null) M1_OnClick_SelectLevel(dataToOpen, true);
                else M1_OnClick_SelectLevel(LevelManager.selectedLevelData, true);

                if (LevelManager.justCompletedTestPlay) { if (statusText != null) statusText.text = "试玩通过！现在可以“发布”了。"; }
                else { if (statusText != null) statusText.text = "已从试玩返回。"; }
            }
            else if (LevelManager.selectedGameMode == GameMode.WordLinkUp)
            {
                ShowPanelInstant(contentEditorPanel_M2_CG);
                // 【【【 重构 】】】
                LevelData dataToOpen = TcbManager.AllLevels.levels.Find(l => l.id == LevelManager.selectedLevelData.id);
                if (dataToOpen != null) M2_OnClick_SelectLevel(dataToOpen, true);
                else M2_OnClick_SelectLevel(LevelManager.selectedLevelData, true);

                if (LevelManager.justCompletedTestPlay) { if (statusText != null) statusText.text = "试玩通过！现在可以“发布”了。"; }
                else { if (statusText != null) statusText.text = "已从试玩返回。"; }
            }

            LevelManager.justReturnedFromTest = false;
            LevelManager.justCompletedTestPlay = false;
        }
        else
        {
            ShowPanelInstant(mainPanelCG);
            if (statusText != null) statusText.text = "管理员您好，请选择您要创建的关卡模式。";
            if (modeSelectDropdown != null) modeSelectDropdown.value = 0;
        }
    }

    #region 导航 (V5 - "返回试玩"Bug 修复)
    private void SetupModeDropdown()
    {
        if (modeSelectDropdown == null) return;
        modeSelectDropdown.ClearOptions();
        List<string> options = new List<string> { "请选择模式...", "模式 1: 单词消消乐", "模式 2: 词语连连看" };
        modeSelectDropdown.AddOptions(options);
        modeSelectDropdown.onValueChanged.AddListener(OnModeSelected);
        if (mode1EditorPanel != null) mode1EditorPanel.gameObject.SetActive(false);
        if (mode2EditorPanel != null) mode2EditorPanel.gameObject.SetActive(false);
    }

    private void OnModeSelected(int index)
    {
        if (index == 0)
        {
            ResetToModeSelect();
        }
        else if (index == 1)
        {
            // 【【【修改】】】: 把 "M1_OnClick_BackToChapterSelect(true)" 作为 onComplete 回调传递
            StartCoroutine(TransitionTo(mainPanelCG, mode1EditorPanelCG, () => {
                M1_OnClick_BackToChapterSelect(true);
            }));
        }
        else if (index == 2)
        {
            // 【【【修改】】】: 把 "M2_OnClick_BackToChapterSelect(true)" 作为 onComplete 回调传递
            StartCoroutine(TransitionTo(mainPanelCG, mode2EditorPanelCG, () => {
                M2_OnClick_BackToChapterSelect(true);
            }));
        }
    }

    public void ResetToModeSelect()
    {
        CanvasGroup panelToHide = null;
        if (mode1EditorPanelCG.alpha > 0) panelToHide = mode1EditorPanelCG;
        else if (mode2EditorPanelCG.alpha > 0) panelToHide = mode2EditorPanelCG;
        else if (mainPanelCG.alpha > 0) panelToHide = null;
        else panelToHide = allNavPanelCGs.FirstOrDefault(cg => cg.alpha > 0);

        if (panelToHide != null && panelToHide != mainPanelCG)
        {
            StartCoroutine(TransitionTo(panelToHide, mainPanelCG));
        }
        else
        {
            ShowPanelInstant(mainPanelCG);
        }

        if (modeSelectDropdown != null) modeSelectDropdown.value = 0;
        if (statusText != null) statusText.text = "管理员您好，请选择您要创建的关卡模式。";
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        if (LevelManager.instance != null) LevelManager.instance.LoadMainMenu();
        else SceneManager.LoadScene("MainMenu");
    }

    // 【【【V5 修复：试玩返回 Bug】】】
    private void ShowPanelInstant(CanvasGroup panelToShow)
    {
        // --- 【【【V5 修复】】】: 找出需要保持激活的父面板
        CanvasGroup parentPanelToKeep = null;
        if (panelToShow == contentEditorPanel_M1_CG || panelToShow == levelSelectPanel_M1_CG || panelToShow == chapterSelectPanel_M1_CG)
        {
            parentPanelToKeep = mode1EditorPanelCG;
        }
        else if (panelToShow == contentEditorPanel_M2_CG || panelToShow == levelSelectPanel_M2_CG || panelToShow == chapterSelectPanel_M2_CG)
        {
            parentPanelToKeep = mode2EditorPanelCG;
        }
        // --- 【【【V5 修复结束】】】

        foreach (var cg in allNavPanelCGs)
        {
            // 【【【V5 修复】】】: 
            // 1. 不隐藏自己 (panelToShow)
            // 2. 不隐藏需要保持激活的父面板 (parentPanelToKeep)
            if (cg != null && cg != panelToShow && cg != parentPanelToKeep)
            {
                cg.alpha = 0;
                cg.interactable = false;
                cg.blocksRaycasts = false;
                cg.gameObject.SetActive(false);
            }
        }

        // 【【【V5 修复】】】: 确保父面板也被激活
        if (parentPanelToKeep != null)
        {
            parentPanelToKeep.gameObject.SetActive(true);
            parentPanelToKeep.alpha = 1;
            parentPanelToKeep.interactable = true;
            parentPanelToKeep.blocksRaycasts = true;
        }

        if (panelToShow != null)
        {
            panelToShow.gameObject.SetActive(true);
            panelToShow.alpha = 1;
            panelToShow.interactable = true;
            panelToShow.blocksRaycasts = true;
        }
    }

    #endregion

    #region 模式 1：关卡列表
    private void M1_OnClick_SelectChapter(string chapterName)
    {
        currentEditingChapter_M1 = chapterName;
        StartCoroutine(TransitionTo(chapterSelectPanel_M1_CG, levelSelectPanel_M1_CG));
        if (statusText != null) statusText.text = $"当前编辑: 模式 1 / {chapterName} / (请选择关卡)";
        M1_PopulateLevelList(chapterName);
    }

    private void M1_OnClick_BackToChapterSelect(bool instant = false)
    {
        if (instant)
        {
            ShowPanelInstant(chapterSelectPanel_M1_CG);
        }
        else
        {
            StartCoroutine(TransitionTo(levelSelectPanel_M1_CG, chapterSelectPanel_M1_CG));
        }

        currentEditingChapter_M1 = null;
        if (statusText != null) statusText.text = "当前编辑: 模式 1 - 请选择章节";
        foreach (Transform child in levelListContainer_M1) { Destroy(child.gameObject); }
    }

    private void M1_PopulateLevelList(string chapterName)
    {
        // 【【【 重构 】】】
        if (TcbManager.AllLevels == null || TcbManager.AllLevels.levels == null) return;
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in levelListContainer_M1) { childrenToDestroy.Add(child.gameObject); }
        foreach (GameObject go in childrenToDestroy) { Destroy(go); }

        List<LevelData> levels = TcbManager.AllLevels.levels
            .Where(l => l.mode == (long)GameMode.WordMatch3 && l.chapter == chapterName)
            .OrderBy(l => l.level)
            .ToList();
        // 【【【 重构结束 】】】

        foreach (LevelData levelData in levels)
        {
            GameObject buttonGO = Instantiate(levelInEditorButtonPrefab_M1, levelListContainer_M1);
            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) { buttonText.text = $"关卡 {levelData.level}"; }
            Image buttonImage = buttonGO.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (levelData.editorStatus == "Published") { buttonImage.color = Color.green; }
                else if (levelData.editorStatus == "Tested") { buttonImage.color = Color.yellow; }
                else { buttonImage.color = Color.red; }
            }
            LevelData capturedData = levelData;
            Button button = buttonGO.GetComponent<Button>();
            button.onClick.AddListener(() => { M1_OnClick_SelectLevel(capturedData, false); });
        }
    }
    #endregion

    #region 模式 1：添加关卡 (V4 - Popups)
    private void M1_OnClick_AddLevel()
    {
        if (addLevelPopupPanel_M1_CG != null)
        {
            addLevelPopupPanel_M1_CG.gameObject.SetActive(true);
            addLevelPopupPanel_M1_CG.alpha = 1;
            addLevelPopupPanel_M1_CG.interactable = true;
            addLevelPopupPanel_M1_CG.blocksRaycasts = true;
        }
        if (newLevelInputfield_M1 != null) newLevelInputfield_M1.text = "";
        if (statusText != null) statusText.text = $"当前章节: {currentEditingChapter_M1}。请输入新关卡号。";
    }

    private void M1_OnClick_AddLevel_Cancel()
    {
        if (addLevelPopupPanel_M1_CG != null)
        {
            addLevelPopupPanel_M1_CG.alpha = 0;
            addLevelPopupPanel_M1_CG.interactable = false;
            addLevelPopupPanel_M1_CG.blocksRaycasts = false;
            addLevelPopupPanel_M1_CG.gameObject.SetActive(false);
        }
        if (statusText != null) statusText.text = $"当前编辑: 模式 1 / {currentEditingChapter_M1} / (请选择关卡)";
    }

    private void M1_OnClick_AddLevel_Confirm()
    {
        int newLevelNum = -1;
        if (!int.TryParse(newLevelInputfield_M1.text, out newLevelNum) || newLevelNum <= 0)
        {
            if (statusText != null) statusText.text = "错误：请输入一个有效的正整数！";
            return;
        }
        // 【【【 重构 】】】
        bool isDuplicate = TcbManager.AllLevels.levels.Any(l =>
            l.mode == (long)GameMode.WordMatch3 &&
            l.chapter == currentEditingChapter_M1 &&
            l.level == newLevelNum
        );
        if (isDuplicate)
        {
            if (statusText != null) statusText.text = $"错误：关卡 {newLevelNum} 已经存在！";
            return;
        }
        LevelData newLevelData = new LevelData
        {
            mode = (int)GameMode.WordMatch3,
            chapter = currentEditingChapter_M1,
            level = newLevelNum,
            id = $"m{(int)GameMode.WordMatch3}-{currentEditingChapter_M1}-l{newLevelNum}",
            content_mode_1 = new List<Mode1Content>(),
            content_mode_2 = new List<Mode2Content>(),
            editorStatus = "Working"
        };
        TcbManager.AllLevels.levels.Add(newLevelData);
        // 【【【 重构结束 】】】

        if (addLevelPopupPanel_M1_CG != null)
        {
            addLevelPopupPanel_M1_CG.alpha = 0;
            addLevelPopupPanel_M1_CG.interactable = false;
            addLevelPopupPanel_M1_CG.blocksRaycasts = false;
            addLevelPopupPanel_M1_CG.gameObject.SetActive(false);
        }

        M1_OnClick_SelectLevel(newLevelData, false);
    }
    #endregion

    #region 模式 1：内容编辑器
    private void M1_OnClick_SelectLevel(LevelData levelData, bool instant = false)
    {
        if (!instant)
        {
            StartCoroutine(TransitionTo(levelSelectPanel_M1_CG, contentEditorPanel_M1_CG));
        }

        // 【【【【【【【 新增这行代码！ 】】】】】】】
        // 无论从哪里进来的，强制把“当前章节”设置为这个关卡所属的章节
        // 这样点“返回”时，它就永远知道该回哪里了！
        currentEditingChapter_M1 = levelData.chapter;
        // 【【【【【【【 新增结束 】】】】】】】

        isDirty_M1 = false;
        currentEditingLevel_M1 = levelData;
        if (statusText != null) statusText.text = $"正在编辑: {levelData.chapter} - 关卡 {levelData.level}";
        M1_PopulateContentEditor(currentEditingLevel_M1);
        UpdateEditorButtonStates();
    }

    private void M1_OnClick_ContentEditor_Back()
    {
        StartCoroutine(TransitionTo(contentEditorPanel_M1_CG, levelSelectPanel_M1_CG));
        M1_PopulateLevelList(currentEditingChapter_M1);
        if (statusText != null) statusText.text = $"当前编辑: 模式 1 / {currentEditingChapter_M1} / (请选择关卡)";
        currentEditingLevel_M1 = null;
        isDirty_M1 = false; // 【【【V5 修复】】】
        UpdateEditorButtonStates();
    }

    private void M1_PopulateContentEditor(LevelData levelData)
    {
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in contentListContainer_M1) { childrenToDestroy.Add(child.gameObject); }
        foreach (GameObject go in childrenToDestroy) { Destroy(go); }

        if (levelData.content_mode_1 == null || levelData.content_mode_1.Count == 0) return;
        foreach (Mode1Content content in levelData.content_mode_1)
        {
            M1_AddCellRow(content.groupId.ToString(), content.hanzi, content.pinyin, content.english, contentListContainer_M1, false);
        }
    }

    private void M1_OnClick_AddRow()
    {
        int newGroupId = 0;
        int cellCount = contentListContainer_M1.childCount;
        if (cellCount > 0)
        {
            Transform lastGroupIdCell = contentListContainer_M1.GetChild(cellCount - 4);
            TMP_InputField lastGroupIdInput = lastGroupIdCell.GetComponent<TMP_InputField>();
            if (lastGroupIdInput != null && int.TryParse(lastGroupIdInput.text, out int lastGroupId))
            {
                newGroupId = lastGroupId + 1;
            }
            else
            {
                newGroupId = cellCount / 4;
            }
        }
        M1_AddCellRow(newGroupId.ToString(), "", "", "", contentListContainer_M1, true);
    }

    private void M1_AddCellRow(string id, string hanzi, string pinyin, string english, Transform container, bool isNewCollection)
    {
        GameObject cell1 = Instantiate(m1EditorCellPrefab, container);
        TMP_InputField input1 = cell1.GetComponent<TMP_InputField>();
        input1.text = id;
        input1.onValueChanged.AddListener(OnAnyCellChanged);
        M1_EditorCell cellScript1 = cell1.GetComponent<M1_EditorCell>() ?? cell1.AddComponent<M1_EditorCell>();
        cellScript1.Setup(this);

        GameObject cell2 = Instantiate(m1EditorCellPrefab, container);
        TMP_InputField input2 = cell2.GetComponent<TMP_InputField>();
        input2.text = hanzi;
        input2.onValueChanged.AddListener(OnAnyCellChanged);
        M1_EditorCell cellScript2 = cell2.GetComponent<M1_EditorCell>() ?? cell2.AddComponent<M1_EditorCell>();
        cellScript2.Setup(this);

        GameObject cell3 = Instantiate(m1EditorCellPrefab, container);
        TMP_InputField input3 = cell3.GetComponent<TMP_InputField>();
        input3.text = pinyin;
        input3.onValueChanged.AddListener(OnAnyCellChanged);
        M1_EditorCell cellScript3 = cell3.GetComponent<M1_EditorCell>() ?? cell3.AddComponent<M1_EditorCell>();
        cellScript3.Setup(this);

        GameObject cell4 = Instantiate(m1EditorCellPrefab, container);
        TMP_InputField input4 = cell4.GetComponent<TMP_InputField>();
        input4.text = english;
        input4.onValueChanged.AddListener(OnAnyCellChanged);
        M1_EditorCell cellScript4 = cell4.GetComponent<M1_EditorCell>() ?? cell4.AddComponent<M1_EditorCell>();
        cellScript4.Setup(this);

        if (isNewCollection) MarkLevelAsDirty();
    }

    private bool M1_SaveGridToCurrentLevelData()
    {
        List<Mode1Content> newContentList = new List<Mode1Content>();
        int cellCount = contentListContainer_M1.childCount;
        if (cellCount % 4 != 0)
        {
            if (statusText != null) statusText.text = "错误：表格行不完整！单元格总数必须是4的倍数。";
            return false;
        }
        for (int i = 0; i < cellCount; i += 4)
        {
            TMP_InputField cell_groupId = contentListContainer_M1.GetChild(i).GetComponent<TMP_InputField>();
            TMP_InputField cell_hanzi = contentListContainer_M1.GetChild(i + 1).GetComponent<TMP_InputField>();
            TMP_InputField cell_pinyin = contentListContainer_M1.GetChild(i + 2).GetComponent<TMP_InputField>();
            TMP_InputField cell_english = contentListContainer_M1.GetChild(i + 3).GetComponent<TMP_InputField>();
            if (cell_groupId == null || cell_hanzi == null || cell_pinyin == null || cell_english == null)
            {
                if (statusText != null) statusText.text = $"错误：第 {i / 4 + 1} 行的单元格组件丢失！";
                return false;
            }
            string hanzi = cell_hanzi.text;
            string pinyin = cell_pinyin.text;
            string english = cell_english.text;
            if (string.IsNullOrEmpty(hanzi) && string.IsNullOrEmpty(pinyin) && string.IsNullOrEmpty(english)) continue;
            newContentList.Add(new Mode1Content
            {
                groupId = int.TryParse(cell_groupId.text, out int gId) ? gId : 0,
                hanzi = hanzi,
                pinyin = pinyin,
                english = english
            });
        }
        currentEditingLevel_M1.content_mode_1 = newContentList;
        return true;
    }

    private void M1_OnClick_SaveLevel()
    {
        if (statusText != null) statusText.text = "正在保存 (M1)...";
        if (!M1_SaveGridToCurrentLevelData()) return;

        isDirty_M1 = false; // 【【【V5 修复】】】
        currentEditingLevel_M1.editorStatus = "Working"; // 保存后状态依然是 Working
        UpdateEditorButtonStates();

        // 【【【【【【 功能修复：调用 TcbManager 上传 】】】】】】
        if (TcbManager.instance != null)
        {
            TcbManager.instance.UploadNewLevel(currentEditingLevel_M1.id, currentEditingLevel_M1);
        }
    }

    private void M1_OnClick_TestPlay()
    {
        if (statusText != null) statusText.text = "正在保存并准备试玩 (M1)...";
        if (!M1_SaveGridToCurrentLevelData())
        {
            if (statusText != null) statusText.text = "试玩失败：表格数据不完整。";
            return;
        }

        // 【【【V5 修复：空内容检查】】】
        if (currentEditingLevel_M1.content_mode_1 == null || currentEditingLevel_M1.content_mode_1.Count == 0)
        {
            if (statusText != null) statusText.text = "试玩失败：关卡内容为空！请添加内容后再试玩。";
            return;
        }

        if (LevelManager.instance == null)
        {
            if (statusText != null) statusText.text = "试玩失败：LevelManager 实例未找到！";
            return;
        }
        LevelManager.isTestPlayMode = true;
        LevelManager.selectedGameMode = GameMode.WordMatch3;
        LevelManager.instance.LoadLevel(currentEditingLevel_M1);
    }

    public void OnUploadSuccessCallback(string message)
    {
        if (statusText != null) statusText.text = "保存成功！";
    }

    private void M1_OnClick_Publish()
    {
        if (statusText != null) statusText.text = "正在发布 (M1)...";
        if (!M1_SaveGridToCurrentLevelData())
        {
            if (statusText != null) statusText.text = "发布失败：表格数据不完整。";
            return;
        }
        currentEditingLevel_M1.editorStatus = "Published";

        // 【【【【【【 功能修复：调用 TcbManager 上传 】】】】】】
        if (TcbManager.instance != null)
        {
            TcbManager.instance.UploadNewLevel(currentEditingLevel_M1.id, currentEditingLevel_M1);
        }
        UpdateEditorButtonStates();
    }
    #endregion

    #region 模式 1：脏检查
    private void OnAnyCellChanged(string s)
    {
        if (currentEditingLevel_M1 != null) // 只要在编辑，就标记
        {
            MarkLevelAsDirty();
        }
    }

    // 【【【V5 修复：脏检查】】】
    public void MarkLevelAsDirty()
    {
        if (currentEditingLevel_M1 != null)
        {
            if (currentEditingLevel_M1.editorStatus != "Working") currentEditingLevel_M1.editorStatus = "Working";
            isDirty_M1 = true;
        }
        if (currentEditingLevel_M2 != null)
        {
            if (currentEditingLevel_M2.editorStatus != "Working") currentEditingLevel_M2.editorStatus = "Working";
            isDirty_M2 = true;
        }
        UpdateEditorButtonStates();
    }

    // 【【【V5 修复：脏检查】】】
    private void UpdateEditorButtonStates()
    {
        // 1. 更新 M1 按钮
        if (publishButton_M1 != null)
        {
            bool isM1Active = (currentEditingLevel_M1 != null);
            if (isM1Active)
            {
                bool canPublish = (currentEditingLevel_M1.editorStatus == "Tested");
                publishButton_M1.interactable = canPublish;
                if (publishButtonCG != null) publishButtonCG.alpha = canPublish ? 1.0f : 0.5f;

                // 【【【V5 修复】】】: 只有在“未修改”状态下才允许试玩
                if (testPlayButton_M1 != null) testPlayButton_M1.interactable = !isDirty_M1;
            }
        }

        // 2. 更新 M2 按钮
        if (publishButton_M2 != null)
        {
            bool isM2Active = (currentEditingLevel_M2 != null);
            if (isM2Active)
            {
                bool canPublish = (currentEditingLevel_M2.editorStatus == "Tested");
                publishButton_M2.interactable = canPublish;
                if (publishButtonCG_M2 != null) publishButtonCG_M2.alpha = canPublish ? 1.0f : 0.5f;

                // 【【【V5 修复】】】: 只有在“未修改”状态下才允许试玩
                if (testPlayButton_M2 != null) testPlayButton_M2.interactable = !isDirty_M2;
            }
        }
    }
    #endregion

    #region 模式 1：批量粘贴 (V4 - Popups)
    // 【【【【【【 2. 替换这个函数 】】】】】】
    private void M1_OnClick_BatchPaste()
    {
        // 1. (旧代码) 禁用旧的 Unity 弹窗
        /*
        if (batchPastePopupPanel_CG != null)
        {
            batchPastePopupPanel_CG.gameObject.SetActive(true);
            batchPastePopupPanel_CG.alpha = 1;
            batchPastePopupPanel_CG.interactable = true;
            batchPastePopupPanel_CG.blocksRaycasts = true;
        }
        if (pasteInputField != null) pasteInputField.text = "";
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in pastePreviewContainer) { childrenToDestroy.Add(child.gameObject); }
        foreach (GameObject go in childrenToDestroy) { Destroy(go); }
        parsedPasteData = null;
        */

        // 2. 【【【 新代码 】】】
        // 调用我们新的 JavaScript 原生输入框
        // 它会把结果发回到 "M1_ReceivePastedTextFromHtml" 函数
        Debug.Log("正在调用 JsShowHtmlTextInput (M1)...");
#if UNITY_WEBGL && !UNITY_EDITOR
    JsShowNativePrompt("", this.gameObject.name, "M1_ReceivePastedTextFromHtml");
#else
        Debug.LogWarning("【编辑器模式】：JsShowHtmlTextInput 无法在编辑器中运行。");
        statusText.text = "请在 WebGL 构建中测试此功能。";
#endif
    }
    // 【【【【【【 替换结束 】】】】】】

    private void M1_OnPasteInputChanged(string text)
    {
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in pastePreviewContainer) { childrenToDestroy.Add(child.gameObject); }
        foreach (GameObject go in childrenToDestroy) { Destroy(go); }

        parsedPasteData = M1_ParsePastedCSV(text);
        if (parsedPasteData == null || parsedPasteData.Count == 0) return;
        foreach (var content in parsedPasteData)
        {
            M1_AddCellRow_Preview(content.groupId.ToString(), content.hanzi, content.pinyin, content.english, pastePreviewContainer);
        }
    }

    private void M1_AddCellRow_Preview(string id, string hanzi, string pinyin, string english, Transform container)
    {
        GameObject cell1 = Instantiate(m1EditorCellPrefab, container);
        TMP_InputField input1 = cell1.GetComponent<TMP_InputField>();
        input1.text = id;
        input1.interactable = false;
        GameObject cell2 = Instantiate(m1EditorCellPrefab, container);
        TMP_InputField input2 = cell2.GetComponent<TMP_InputField>();
        input2.text = hanzi;
        input2.interactable = false;
        GameObject cell3 = Instantiate(m1EditorCellPrefab, container);
        TMP_InputField input3 = cell3.GetComponent<TMP_InputField>();
        input3.text = pinyin;
        input3.interactable = false;
        GameObject cell4 = Instantiate(m1EditorCellPrefab, container);
        TMP_InputField input4 = cell4.GetComponent<TMP_InputField>();
        input4.text = english;
        input4.interactable = false;
    }

    private List<Mode1Content> M1_ParsePastedCSV(string csvText)
    {
        List<Mode1Content> parsedList = new List<Mode1Content>();
        string[] lines = csvText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] values = line.Split(new[] { ',', '\t' }, StringSplitOptions.None);
            if (values.Length >= 4)
            {
                parsedList.Add(new Mode1Content
                {
                    groupId = int.TryParse(values[0].Trim(), out int gId) ? gId : 0,
                    hanzi = values[1].Trim(),
                    pinyin = values[2].Trim(),
                    english = values[3].Trim()
                });
            }
        }
        return parsedList;
    }

    private void M1_OnClick_Paste_ConfirmImport()
    {
        if (parsedPasteData == null || parsedPasteData.Count == 0) return;
        foreach (var content in parsedPasteData)
        {
            M1_AddCellRow(content.groupId.ToString(), content.hanzi, content.pinyin, content.english, contentListContainer_M1, false);
        }
        MarkLevelAsDirty();
        M1_OnClick_Paste_Cancel();
    }

    private void M1_OnClick_Paste_Cancel()
    {
        if (batchPastePopupPanel_CG != null)
        {
            batchPastePopupPanel_CG.alpha = 0;
            batchPastePopupPanel_CG.interactable = false;
            batchPastePopupPanel_CG.blocksRaycasts = false;
            batchPastePopupPanel_CG.gameObject.SetActive(false);
        }
        parsedPasteData = null;
        if (pasteInputField != null) pasteInputField.text = "";
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in pastePreviewContainer) { childrenToDestroy.Add(child.gameObject); }
        foreach (GameObject go in childrenToDestroy) { Destroy(go); }
    }
    #endregion

    #region 模式 1：删除行 (V4 - "阻挡"修复版)
    public void M1_OnRequestDeleteRow(GameObject cell, Vector3 clickPosition)
    {
        m1_cellIndexToDelete = cell.transform.GetSiblingIndex();
        if (deleteRowPopupPanel != null)
        {
            deleteRowPopupPanel.transform.SetAsLastSibling();
            deleteRowPopupPanel.SetActive(true);
            if (deleteRowPopupPanel_CG != null)
            {
                deleteRowPopupPanel_CG.interactable = true;
                deleteRowPopupPanel_CG.blocksRaycasts = true;
            }
        }
        if (contentEditorPanel_M1_CG != null)
        {
            contentEditorPanel_M1_CG.interactable = false;
            contentEditorPanel_M1_CG.blocksRaycasts = false;
        }
    }

    private void M1_CancelDeleteRow()
    {
        if (deleteRowPopupPanel != null)
        {
            deleteRowPopupPanel.SetActive(false);
            if (deleteRowPopupPanel_CG != null)
            {
                deleteRowPopupPanel_CG.interactable = false;
                deleteRowPopupPanel_CG.blocksRaycasts = false;
            }
        }
        m1_cellIndexToDelete = -1;
        if (contentEditorPanel_M1_CG != null)
        {
            contentEditorPanel_M1_CG.interactable = true;
            contentEditorPanel_M1_CG.blocksRaycasts = true;
        }
    }

    private void M1_ConfirmDeleteRow()
    {
        if (m1_cellIndexToDelete < 0 || contentListContainer_M1 == null)
        {
            M1_CancelDeleteRow();
            return;
        }
        int rowIndex = m1_cellIndexToDelete / 4;
        int firstCellInRowIndex = rowIndex * 4;
        if (firstCellInRowIndex + 3 >= contentListContainer_M1.childCount)
        {
            M1_CancelDeleteRow();
            return;
        }
        try
        {
            GameObject cell4 = contentListContainer_M1.GetChild(firstCellInRowIndex + 3).gameObject;
            GameObject cell3 = contentListContainer_M1.GetChild(firstCellInRowIndex + 2).gameObject;
            GameObject cell2 = contentListContainer_M1.GetChild(firstCellInRowIndex + 1).gameObject;
            GameObject cell1 = contentListContainer_M1.GetChild(firstCellInRowIndex + 0).gameObject;
            DestroyImmediate(cell4);
            DestroyImmediate(cell3);
            DestroyImmediate(cell2);
            DestroyImmediate(cell1);
            MarkLevelAsDirty();
        }
        catch (System.Exception e) { Debug.LogError($"删除行时发生错误: {e.Message}"); }
        M1_CancelDeleteRow();
    }
    #endregion

    #region 模式 2：关卡列表
    private void M2_OnClick_SelectChapter(string chapterName)
    {
        currentEditingChapter_M2 = chapterName;
        StartCoroutine(TransitionTo(chapterSelectPanel_M2_CG, levelSelectPanel_M2_CG));
        if (statusText != null) statusText.text = $"当前编辑: 模式 2 / {chapterName} / (请选择关卡)";
        M2_PopulateLevelList(chapterName);
    }

    private void M2_OnClick_BackToChapterSelect(bool instant = false)
    {
        if (instant)
        {
            ShowPanelInstant(chapterSelectPanel_M2_CG);
        }
        else
        {
            StartCoroutine(TransitionTo(levelSelectPanel_M2_CG, chapterSelectPanel_M2_CG));
        }
        currentEditingChapter_M2 = null;
        if (statusText != null) statusText.text = "当前编辑: 模式 2 - 请选择章节";
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in levelListContainer_M2) { childrenToDestroy.Add(child.gameObject); }
        foreach (GameObject go in childrenToDestroy) { Destroy(go); }
    }

    private void M2_PopulateLevelList(string chapterName)
    {
        // 【【【 重构 】】】
        if (TcbManager.AllLevels == null || TcbManager.AllLevels.levels == null) return;
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in levelListContainer_M2) { childrenToDestroy.Add(child.gameObject); }
        foreach (GameObject go in childrenToDestroy) { Destroy(go); }
        List<LevelData> levels = TcbManager.AllLevels.levels
            .Where(l => l.mode == (long)GameMode.WordLinkUp && l.chapter == chapterName)
            .OrderBy(l => l.level)
            .ToList();
        // 【【【 重构结束 】】】

        foreach (LevelData levelData in levels)
        {
            GameObject buttonGO = Instantiate(levelInEditorButtonPrefab_M1, levelListContainer_M2);
            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) { buttonText.text = $"关卡 {levelData.level}"; }
            Image buttonImage = buttonGO.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (levelData.editorStatus == "Published") { buttonImage.color = Color.green; }
                else if (levelData.editorStatus == "Tested") { buttonImage.color = Color.yellow; }
                else { buttonImage.color = Color.red; }
            }
            LevelData capturedData = levelData;
            Button button = buttonGO.GetComponent<Button>();
            button.onClick.AddListener(() => { M2_OnClick_SelectLevel(capturedData, false); });
        }
    }
    #endregion

    #region 模式 2：添加关卡 (V4 - Popups)
    private void M2_OnClick_AddLevel()
    {
        if (addLevelPopupPanel_M2_CG != null)
        {
            addLevelPopupPanel_M2_CG.gameObject.SetActive(true);
            addLevelPopupPanel_M2_CG.alpha = 1;
            addLevelPopupPanel_M2_CG.interactable = true;
            addLevelPopupPanel_M2_CG.blocksRaycasts = true;
        }
        if (newLevelInputfield_M2 != null) newLevelInputfield_M2.text = "";
        if (statusText != null) statusText.text = $"当前章节: {currentEditingChapter_M2}。请输入新关卡号。";
    }

    private void M2_OnClick_AddLevel_Cancel()
    {
        if (addLevelPopupPanel_M2_CG != null)
        {
            addLevelPopupPanel_M2_CG.alpha = 0;
            addLevelPopupPanel_M2_CG.interactable = false;
            addLevelPopupPanel_M2_CG.blocksRaycasts = false;
            addLevelPopupPanel_M2_CG.gameObject.SetActive(false);
        }
        if (statusText != null) statusText.text = $"当前编辑: 模式 2 / {currentEditingChapter_M2} / (请选择关卡)";
    }

    private void M2_OnClick_AddLevel_Confirm()
    {
        int newLevelNum = -1;
        if (!int.TryParse(newLevelInputfield_M2.text, out newLevelNum) || newLevelNum <= 0)
        {
            if (statusText != null) statusText.text = "错误：请输入一个有效的正整数！";
            return;
        }
        // 【【【 重构 】】】
        bool isDuplicate = TcbManager.AllLevels.levels.Any(l =>
            l.mode == (long)GameMode.WordLinkUp &&
            l.chapter == currentEditingChapter_M2 &&
            l.level == newLevelNum
        );
        if (isDuplicate)
        {
            if (statusText != null) statusText.text = $"错误：关卡 {newLevelNum} 已经存在！";
            return;
        }
        LevelData newLevelData = new LevelData
        {
            mode = (int)GameMode.WordLinkUp,
            chapter = currentEditingChapter_M2,
            level = newLevelNum,
            id = $"m{(int)GameMode.WordLinkUp}-{currentEditingChapter_M2}-l{newLevelNum}",
            content_mode_1 = new List<Mode1Content>(),
            content_mode_2 = new List<Mode2Content>(),
            editorStatus = "Working"
        };
        TcbManager.AllLevels.levels.Add(newLevelData);
        // 【【【 重构结束 】】】

        if (addLevelPopupPanel_M2_CG != null)
        {
            addLevelPopupPanel_M2_CG.alpha = 0;
            addLevelPopupPanel_M2_CG.interactable = false;
            addLevelPopupPanel_M2_CG.blocksRaycasts = false;
            addLevelPopupPanel_M2_CG.gameObject.SetActive(false);
        }
        M2_OnClick_SelectLevel(newLevelData, false);
    }
    #endregion

    #region 模式 2：内容编辑器
    private void M2_OnClick_SelectLevel(LevelData levelData, bool instant = false)
    {
        if (!instant)
        {
            StartCoroutine(TransitionTo(levelSelectPanel_M2_CG, contentEditorPanel_M2_CG));
        }

        currentEditingChapter_M2 = levelData.chapter;

        isDirty_M2 = false; // 【【【V5 修复】】】
        currentEditingLevel_M2 = levelData;
        if (statusText != null) statusText.text = $"正在编辑: {levelData.chapter} - 关卡 {levelData.level}";
        M2_PopulateSentenceEditor(currentEditingLevel_M2);
        if (sentenceInputContainer_M2.childCount > 0) { }
        else
        {
            M2_ClearWordList();
            M2_UpdateContextualButtons(null);
            M2_SetDetailPanelActive(false);
        }
        UpdateEditorButtonStates();
    }

    private void M2_OnClick_ContentEditor_Back()
    {
        M2_OnClick_GlobalSave();
        StartCoroutine(TransitionTo(contentEditorPanel_M2_CG, levelSelectPanel_M2_CG));
        M2_PopulateLevelList(currentEditingChapter_M2);
        if (statusText != null) statusText.text = $"当前编辑: 模式 2 / {currentEditingChapter_M2} / (请选择关卡)";
        currentEditingLevel_M2 = null;
        currentSelectedSentenceRow = null;
        currentSelectedWordRow = null;
        isDirty_M2 = false; // 【【【V5 修复】】】
        UpdateEditorButtonStates();
    }

    private void M2_PopulateSentenceEditor(LevelData levelData)
    {
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in sentenceInputContainer_M2) { childrenToDestroy.Add(child.gameObject); }
        foreach (GameObject go in childrenToDestroy) { Destroy(go); }
        if (levelData.content_mode_2 == null || levelData.content_mode_2.Count == 0) return;
        var sentenceGroups = levelData.content_mode_2
            .GroupBy(word => word.sentenceId)
            .OrderBy(group => group.Key);
        foreach (var group in sentenceGroups)
        {
            int sentenceId = group.Key;
            Mode2Content firstWord = group.First();
            M2_AddSentenceInputRow(sentenceId, firstWord);
        }
    }

    private void M2_OnClick_AddSentence()
    {
        int newId = 1;
        if (sentenceInputContainer_M2.childCount > 0)
        {
            var lastRow = sentenceInputContainer_M2.GetChild(sentenceInputContainer_M2.childCount - 1).GetComponent<M2_SentenceInputRow>();
            newId = lastRow.GetSentenceId() + 1;
        }
        M2_SentenceInputRow newRow = M2_AddSentenceInputRow(newId, null);
    }

    private M2_SentenceInputRow M2_AddSentenceInputRow(int id, Mode2Content data)
    {
        GameObject rowGO = Instantiate(m2SentenceInputPrefab, sentenceInputContainer_M2);
        M2_SentenceInputRow rowUI = rowGO.GetComponent<M2_SentenceInputRow>();
        if (rowUI != null) rowUI.Setup(this, id, data);
        return rowUI;
    }

    public void M2_OnSentenceToggleChanged(M2_SentenceInputRow toggledRow, bool isToggledOn)
    {
        if (isToggledOn)
        {
            currentSelectedSentenceRow = toggledRow;
            if (detail_SentenceIdInput != null) detail_SentenceIdInput.text = toggledRow.GetSentenceId().ToString();
            if (detail_FullSentenceInput != null) detail_FullSentenceInput.text = toggledRow.GetFullSentence();
            M2_SetDetailPanelActive(true);
            M2_PopulateWordList(toggledRow);
        }
        else
        {
            if (currentSelectedSentenceRow == toggledRow)
            {
                M2_OnClick_DetailSave();
                currentSelectedSentenceRow = null;
                M2_SetDetailPanelActive(false);
                M2_ClearWordList();
            }
        }
    }

    private void M2_PopulateWordList(M2_SentenceInputRow selectedRow)
    {
        M2_ClearWordList();
        int sentenceId = selectedRow.GetSentenceId();
        var words = currentEditingLevel_M2.content_mode_2
            .Where(w => w.sentenceId == sentenceId)
            .OrderBy(w => w.wordOrder)
            .ToList();
        if (words.Count == 0)
        {
            M2_OnRequestAutoSplit(selectedRow, false);
        }
        else
        {
            foreach (var wordData in words)
            {
                M2_AddWordRow(wordData.wordOrder, wordData.wordText);
            }
            M2_RenumberWordList();
        }
    }

    public void M2_OnRequestAutoSplit(M2_SentenceInputRow rowToSplit, bool autoSave = false)
    {
        if (rowToSplit == null) return;
        M2_ClearWordList();
        string fullSentence = rowToSplit.GetFullSentence();
        if (string.IsNullOrEmpty(fullSentence)) return;
        List<Mode2Content> newWords = new List<Mode2Content>();
        int order = 1;
        foreach (char c in fullSentence)
        {
            M2_AddWordRow(order, c.ToString());
            newWords.Add(new Mode2Content
            {
                sentenceId = rowToSplit.GetSentenceId(),
                wordOrder = order,
                wordText = c.ToString(),
                fullSentence = fullSentence
            });
            order++;
        }
        M2_RenumberWordList();
        if (autoSave) M2_SaveDetailDataToLevel(rowToSplit.GetSentenceId(), newWords);
    }

    private void M2_OnRequestAutoSplit_Current()
    {
        if (currentSelectedSentenceRow == null)
        {
            if (statusText != null) statusText.text = "错误：请先在“左侧”选中一个句子！";
            return;
        }
        M2_OnRequestAutoSplit(currentSelectedSentenceRow, false);
        MarkLevelAsDirty();
    }

    private void M2_OnClick_GlobalSplit()
    {
        if (statusText != null) statusText.text = "正在拆分所有句子...";
        foreach (Transform child in sentenceInputContainer_M2)
        {
            M2_SentenceInputRow row = child.GetComponent<M2_SentenceInputRow>();
            if (row != null) M2_OnRequestAutoSplit(row, true);
        }
        if (currentSelectedSentenceRow != null)
        {
            M2_PopulateWordList(currentSelectedSentenceRow);
        }
        if (statusText != null) statusText.text = "全部拆分完毕！";
        MarkLevelAsDirty();
    }

    private void M2_OnClick_DetailSave()
    {
        if (currentSelectedSentenceRow == null) return;
        int sentenceId = currentSelectedSentenceRow.GetSentenceId();
        System.Text.StringBuilder reconstructedSentence = new System.Text.StringBuilder();
        List<Mode2Content> newWords = new List<Mode2Content>();
        for (int i = 0; i < wordListContainer_M2.childCount; i++)
        {
            M2_WordRow wordRow = wordListContainer_M2.GetChild(i).GetComponent<M2_WordRow>();
            if (wordRow != null)
            {
                string wordText = wordRow.GetWord();
                reconstructedSentence.Append(wordText);
                newWords.Add(new Mode2Content
                {
                    sentenceId = sentenceId,
                    wordOrder = i + 1,
                    wordText = wordText,
                    fullSentence = ""
                });
            }
        }
        string finalSentence = reconstructedSentence.ToString();
        foreach (var word in newWords) word.fullSentence = finalSentence;
        M2_SaveDetailDataToLevel(sentenceId, newWords);
        currentSelectedSentenceRow.SetFullSentenceText(finalSentence);
        if (detail_FullSentenceInput != null) detail_FullSentenceInput.text = finalSentence;
        if (statusText != null) statusText.text = $"句子 {sentenceId} 保存成功！ (同步完成)";
        MarkLevelAsDirty();
    }

    private void M2_OnClick_GlobalSave()
    {
        M2_OnClick_DetailSave();
        if (currentEditingLevel_M2 == null) return;
        foreach (Transform child in sentenceInputContainer_M2)
        {
            M2_SentenceInputRow row = child.GetComponent<M2_SentenceInputRow>();
            if (row != null)
            {
                int sId = row.GetSentenceId();
                string sText = row.GetFullSentence();
                var wordsToUpdate = currentEditingLevel_M2.content_mode_2
                    .Where(w => w.sentenceId == sId);
                foreach (var word in wordsToUpdate) word.fullSentence = sText;
            }
        }
    }

    private void M2_SaveDetailDataToLevel(int sentenceId, List<Mode2Content> newWords)
    {
        if (currentEditingLevel_M2 == null) return;
        currentEditingLevel_M2.content_mode_2.RemoveAll(w => w.sentenceId == sentenceId);
        currentEditingLevel_M2.content_mode_2.AddRange(newWords);
        currentEditingLevel_M2.content_mode_2 = currentEditingLevel_M2.content_mode_2
            .OrderBy(w => w.sentenceId)
            .ThenBy(w => w.wordOrder)
            .ToList();
    }

    private void M2_OnClick_AddWord()
    {
        if (currentSelectedSentenceRow == null)
        {
            if (statusText != null) statusText.text = "错误：请先在“左侧”选中一个句子！";
            return;
        }
        int newOrder = wordListContainer_M2.childCount + 1;
        M2_AddWordRow(newOrder, "");
        M2_RenumberWordList();
        MarkLevelAsDirty();
    }

    private void M2_AddWordRow(int order, string word)
    {
        GameObject rowGO = Instantiate(m2WordRowPrefab, wordListContainer_M2);
        M2_WordRow rowUI = rowGO.GetComponent<M2_WordRow>();
        if (rowUI != null) rowUI.Setup(this, order, word);
    }

    private void M2_ClearWordList()
    {
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in wordListContainer_M2) { childrenToDestroy.Add(child.gameObject); }
        foreach (GameObject go in childrenToDestroy) { DestroyImmediate(go); }
        currentSelectedWordRow = null;
        M2_UpdateContextualButtons(null);
    }

    private void M2_SetDetailPanelActive(bool isActive)
    {
        if (addWordButton_M2 != null) addWordButton_M2.gameObject.SetActive(isActive);
        if (resplitButton_M2 != null) resplitButton_M2.gameObject.SetActive(isActive);
        if (detailSaveButton_M2 != null) detailSaveButton_M2.gameObject.SetActive(isActive);
        if (!isActive) M2_UpdateContextualButtons(null);
    }

    #region M2 核心编辑
    public void M2_OnRequestMoveWord(M2_WordRow rowToMove, int direction)
    {
        int currentIndex = rowToMove.transform.GetSiblingIndex();
        int newIndex = currentIndex + direction;
        if (newIndex < 0 || newIndex >= wordListContainer_M2.childCount) return;
        rowToMove.transform.SetSiblingIndex(newIndex);
        MarkLevelAsDirty();
        M2_RenumberWordList();
    }

    public void M2_OnSelectWordRowToggle(M2_WordRow toggledRow, bool isToggledOn)
    {
        if (isToggledOn)
        {
            foreach (Transform child in wordListContainer_M2)
            {
                M2_WordRow row = child.GetComponent<M2_WordRow>();
                if (row != null && row != toggledRow) row.SetSelected(false);
            }
            currentSelectedWordRow = toggledRow;
            toggledRow.SetSelected(true);
            M2_UpdateContextualButtons(toggledRow);
        }
        else
        {
            if (currentSelectedWordRow == toggledRow)
            {
                currentSelectedWordRow = null;
                toggledRow.SetSelected(false);
                M2_UpdateContextualButtons(null);
            }
        }
    }

    private void M2_OnClick_DeleteWord()
    {
        if (currentSelectedWordRow == null) return;
        DestroyImmediate(currentSelectedWordRow.gameObject);
        currentSelectedWordRow = null;
        MarkLevelAsDirty();
        M2_RenumberWordList();
        M2_UpdateContextualButtons(null);
    }

    private void M2_OnClick_MergeUp()
    {
        if (currentSelectedWordRow == null) return;
        int currentIndex = currentSelectedWordRow.transform.GetSiblingIndex();
        if (currentIndex == 0) return;
        M2_WordRow targetRow = wordListContainer_M2.GetChild(currentIndex - 1).GetComponent<M2_WordRow>();
        if (targetRow == null) return;
        string textToMerge = currentSelectedWordRow.GetWord();
        string targetText = targetRow.GetWord();
        targetRow.wordInput.text = targetText + textToMerge;
        DestroyImmediate(currentSelectedWordRow.gameObject);
        currentSelectedWordRow = null;
        MarkLevelAsDirty();
        M2_RenumberWordList();
        M2_UpdateContextualButtons(null);
    }

    private void M2_OnClick_MergeDown()
    {
        if (currentSelectedWordRow == null) return;
        int currentIndex = currentSelectedWordRow.transform.GetSiblingIndex();
        if (currentIndex >= wordListContainer_M2.childCount - 1) return;
        M2_WordRow targetRow = wordListContainer_M2.GetChild(currentIndex + 1).GetComponent<M2_WordRow>();
        if (targetRow == null) return;
        string textToMerge = targetRow.GetWord();
        string currentText = currentSelectedWordRow.GetWord();
        currentSelectedWordRow.wordInput.text = currentText + textToMerge;
        DestroyImmediate(targetRow.gameObject);
        MarkLevelAsDirty();
        M2_RenumberWordList();
        M2_UpdateContextualButtons(currentSelectedWordRow);
    }

    private void M2_RenumberWordList()
    {
        int total = wordListContainer_M2.childCount;
        for (int i = 0; i < total; i++)
        {
            M2_WordRow rowUI = wordListContainer_M2.GetChild(i).GetComponent<M2_WordRow>();
            if (rowUI != null)
            {
                bool isFirst = (i == 0);
                bool isLast = (i == total - 1);
                rowUI.UpdateVisuals(i + 1, isFirst, isLast);
            }
        }
    }

    private void M2_UpdateContextualButtons(M2_WordRow selectedRow)
    {
        bool isSelected = (selectedRow != null);
        if (deleteRowButton_M2 != null) deleteRowButton_M2.gameObject.SetActive(isSelected);
        int index = isSelected ? selectedRow.transform.GetSiblingIndex() : -1;
        bool canMergeUp = isSelected && (index > 0);
        bool canMergeDown = isSelected && (index < wordListContainer_M2.childCount - 1);
        if (mergeUpButton_M2 != null) mergeUpButton_M2.gameObject.SetActive(canMergeUp);
        if (mergeDownButton_M2 != null) mergeDownButton_M2.gameObject.SetActive(canMergeDown);
    }
    #endregion

    #region 模式 2：全局按钮
    private void OnClick_Save_M2()
    {
        if (statusText != null) statusText.text = "正在保存 (M2)...";
        M2_OnClick_GlobalSave();

        isDirty_M2 = false; // 【【【V5 修复】】】
        currentEditingLevel_M2.editorStatus = "Working";
        UpdateEditorButtonStates();

        // 【【【【【【 功能修复：调用 TcbManager 上传 】】】】】】
        if (TcbManager.instance != null)
        {
            TcbManager.instance.UploadNewLevel(currentEditingLevel_M2.id, currentEditingLevel_M2);
        }
    }

    private void OnClick_TestPlay_M2()
    {
        if (statusText != null) statusText.text = "正在保存并准备试玩 (M2)...";
        M2_OnClick_GlobalSave();

        // 【【【V5 修复：空内容检查】】】
        if (currentEditingLevel_M2.content_mode_2 == null || currentEditingLevel_M2.content_mode_2.Count == 0)
        {
            if (statusText != null) statusText.text = "试玩失败：关卡内容为空！请添加内容后再试玩。";
            return;
        }

        if (LevelManager.instance == null)
        {
            if (statusText != null) statusText.text = "试玩失败：LevelManager 实例未找到！";
            return;
        }
        LevelManager.isTestPlayMode = true;
        LevelManager.selectedGameMode = GameMode.WordLinkUp;
        LevelManager.instance.LoadLevel(currentEditingLevel_M2);
    }

    private void OnClick_Publish_M2()
    {
        if (statusText != null) statusText.text = "正在发布 (M2)...";
        M2_OnClick_GlobalSave();
        currentEditingLevel_M2.editorStatus = "Published";

        // 【【【【【【 功能修复：调用 TcbManager 上传 】】】】】】
        if (TcbManager.instance != null)
        {
            TcbManager.instance.UploadNewLevel(currentEditingLevel_M2.id, currentEditingLevel_M2);
        }
        UpdateEditorButtonStates();
    }
    #endregion

    #region 模式 2：批量粘贴功能 (V4 - Popups)
    private void M2_OnClick_BatchPaste()
    {
        Debug.Log("正在调用 JsShowNativePrompt (M2)...");
#if UNITY_WEBGL && !UNITY_EDITOR
        // 【修改点】改为调用原生 Native Prompt
        // 注意：这里只传 3 个参数 (内容, 对象名, 回调函数名)
        JsShowNativePrompt("", this.gameObject.name, "M2_ReceivePastedTextFromHtml");
#else
        Debug.LogWarning("【编辑器模式】：JsShowNativePrompt 无法在编辑器中运行。");
        if (statusText != null) statusText.text = "请在 WebGL 构建中测试此功能。";
#endif
    }

    private void M2_OnPasteInputChanged(string text)
    {
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in pastePreviewContainer_M2) { childrenToDestroy.Add(child.gameObject); }
        foreach (GameObject go in childrenToDestroy) { Destroy(go); }
        parsedPasteSentences = M2_ParsePastedCSV(text);
        if (parsedPasteSentences == null || parsedPasteSentences.Count == 0) return;
        foreach (var wordData in parsedPasteSentences)
        {
            M2_AddWordRow_Preview(wordData, pastePreviewContainer_M2);
        }
    }

    private void M2_AddWordRow_Preview(Mode2Content wordData, Transform container)
    {
        GameObject rowGO = Instantiate(m2WordRowPrefab, container);
        M2_WordRow rowUI = rowGO.GetComponent<M2_WordRow>();
        if (rowUI != null)
        {
            rowUI.Setup(null, wordData.wordOrder, wordData.wordText);
            if (rowUI.wordInput != null) rowUI.wordInput.interactable = false;
            if (rowUI.moveUpButton != null) rowUI.moveUpButton.interactable = false;
            if (rowUI.moveDownButton != null) rowUI.moveDownButton.interactable = false;
            if (rowUI.selectionToggle != null) rowUI.selectionToggle.gameObject.SetActive(false);
        }
    }

    private List<Mode2Content> M2_ParsePastedCSV(string csvText)
    {
        List<Mode2Content> parsedList = new List<Mode2Content>();
        string[] lines = csvText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("sentenceId,wordOrder,wordText,fullSentenceText")) continue;
            string[] values = line.Split(',');
            if (values.Length == 4)
            {
                if (!int.TryParse(values[0].Trim(), out int sId)) { continue; }
                if (!int.TryParse(values[1].Trim(), out int wOrder)) { continue; }
                string wordText = values[2].Trim();
                string fullSentence = values[3].Trim();
                parsedList.Add(new Mode2Content
                {
                    sentenceId = sId,
                    wordOrder = wOrder,
                    wordText = wordText,
                    fullSentence = fullSentence
                });
            }
        }
        return parsedList;
    }

    private void M2_OnClick_Paste_ConfirmImport()
    {
        if (currentEditingLevel_M2 == null)
        {
            if (statusText != null) statusText.text = "错误：请先选中一个关卡！";
            M2_OnClick_Paste_Cancel();
            return;
        }
        if (parsedPasteSentences == null || parsedPasteSentences.Count == 0)
        {
            M2_OnClick_Paste_Cancel();
            return;
        }
        int maxExistingId = 0;
        if (currentEditingLevel_M2.content_mode_2.Count > 0)
        {
            maxExistingId = currentEditingLevel_M2.content_mode_2.Max(w => w.sentenceId);
        }
        foreach (Transform child in sentenceInputContainer_M2)
        {
            M2_SentenceInputRow row = child.GetComponent<M2_SentenceInputRow>();
            if (row != null && row.GetSentenceId() > maxExistingId) maxExistingId = row.GetSentenceId();
        }
        var sentenceGroups = parsedPasteSentences.GroupBy(w => w.sentenceId).OrderBy(g => g.Key);
        int importedSentenceCount = 0;
        foreach (var group in sentenceGroups)
        {
            importedSentenceCount++;
            int originalSentenceId = group.Key;
            List<Mode2Content> wordsForThisSentence = group.OrderBy(w => w.wordOrder).ToList();
            Mode2Content firstWord = wordsForThisSentence.First();
            bool uiExists = sentenceInputContainer_M2.Cast<Transform>().Any(t => {
                var row = t.GetComponent<M2_SentenceInputRow>();
                return row != null && row.GetSentenceId() == originalSentenceId;
            });
            int finalSentenceId = originalSentenceId;
            if (uiExists || finalSentenceId <= maxExistingId) finalSentenceId = ++maxExistingId;
            else maxExistingId = finalSentenceId;
            firstWord.sentenceId = finalSentenceId;
            M2_SentenceInputRow newRow = M2_AddSentenceInputRow(finalSentenceId, firstWord);
            foreach (var word in wordsForThisSentence) word.sentenceId = finalSentenceId;
            M2_SaveDetailDataToLevel(finalSentenceId, wordsForThisSentence);
        }
        if (currentSelectedSentenceRow != null)
        {
            M2_OnSentenceToggleChanged(currentSelectedSentenceRow, false);
            M2_OnSentenceToggleChanged(currentSelectedSentenceRow, true);
        }
        else if (sentenceInputContainer_M2.childCount > 0)
        {
            M2_SentenceInputRow firstRow = sentenceInputContainer_M2.GetChild(0).GetComponent<M2_SentenceInputRow>();
            if (firstRow != null) M2_OnSentenceToggleChanged(firstRow, true);
        }
        MarkLevelAsDirty();
        M2_OnClick_Paste_Cancel();
        if (statusText != null) statusText.text = $"成功导入 {importedSentenceCount} 条句子 (共 {parsedPasteSentences.Count} 个词)！";
    }

    private void M2_OnClick_Paste_Cancel()
    {
        if (batchPastePopupPanel_M2_CG != null)
        {
            batchPastePopupPanel_M2_CG.alpha = 0;
            batchPastePopupPanel_M2_CG.interactable = false;
            batchPastePopupPanel_M2_CG.blocksRaycasts = false;
            batchPastePopupPanel_M2_CG.gameObject.SetActive(false);
        }
        parsedPasteSentences = null;
        if (pasteInputField_M2 != null) pasteInputField_M2.text = "";
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in pastePreviewContainer_M2) { childrenToDestroy.Add(child.gameObject); }
        foreach (GameObject go in childrenToDestroy) { Destroy(go); }
        if (currentEditingLevel_M2 != null && statusText != null)
        {
            statusText.text = $"正在编辑: {currentEditingLevel_M2.chapter} - 关卡 {currentEditingLevel_M2.level}";
        }
    }
    #endregion

    #endregion

    #region 动画协程 (V4 - 从 MainMenuManager 复制)
    private IEnumerator TransitionTo(CanvasGroup panelToHide, CanvasGroup panelToShow, System.Action onComplete = null)
    {
        yield return StartCoroutine(FadeCanvasGroup(panelToHide, 1f, 0f, panelFadeDuration));
        if (panelToShow.gameObject != null) panelToShow.gameObject.SetActive(true);
        yield return StartCoroutine(FadeCanvasGroup(panelToShow, 0f, 1f, panelFadeDuration));

        // 【【【新增这行】】】: 动画放完后，调用回调函数
        onComplete?.Invoke();
    }
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        cg.interactable = false;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            yield return null;
        }
        cg.alpha = endAlpha;
        if (endAlpha > 0)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
        else
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
            if (cg.gameObject != null) cg.gameObject.SetActive(false);
        }
    }
    #endregion

    /// <summary>
    /// 【新】JS 发生错误时的回调
    /// </summary>
    public void OnHtmlInputError(string error)
    {
        Debug.LogError($"[LevelEditorManager] OnHtmlInputError: {error}");
        if (statusText != null) statusText.text = "操作失败: " + error;
    }

    /// <summary>
    /// 【新】接收 M1 从 JS 浮层发回的粘贴数据
    /// </summary>
    public void M1_ReceivePastedTextFromHtml(string pastedText)
    {
        Debug.Log("[LevelEditorManager] M1 成功接收到 JS 文本！");
        if (statusText != null) statusText.text = "已接收到数据，正在解析...";

        // 1. (复用旧逻辑) 解析数据
        parsedPasteData = M1_ParsePastedCSV(pastedText);
        if (parsedPasteData == null || parsedPasteData.Count == 0)
        {
            if (statusText != null) statusText.text = "解析失败：未识别到有效数据。";
            return;
        }

        // 2. (复用旧逻辑) 确认导入
        M1_OnClick_Paste_ConfirmImport();
    }

    /// <summary>
    /// 【新】接收 M2 从 JS 浮层发回的粘贴数据
    /// </summary>
    public void M2_ReceivePastedTextFromHtml(string pastedText)
    {
        Debug.Log("[LevelEditorManager] M2 成功接收到 JS 文本！");
        if (statusText != null) statusText.text = "已接收到数据，正在解析...";

        // 1. (复用旧逻辑) 解析数据
        parsedPasteSentences = M2_ParsePastedCSV(pastedText);
        if (parsedPasteSentences == null || parsedPasteSentences.Count == 0)
        {
            if (statusText != null) statusText.text = "解析失败：未识别到有效数据。";
            return;
        }

        // 2. (复用旧逻辑) 确认导入
        M2_OnClick_Paste_ConfirmImport();
    }

    public void M1_OnRequestEditCell(GameObject cell, string currentText)
    {
        currentEditingCell_M1 = cell;
        Debug.Log($"[LevelEditorManager] 正在请求编辑单元格，当前文本: {currentText}");
#if UNITY_WEBGL && !UNITY_EDITOR
        // 【修改点】改为调用原生 Native Prompt
        // 注意：这里只传 3 个参数 (当前文本, 对象名, 回调函数名)
        JsShowNativePrompt(currentText, this.gameObject.name, "M1_ReceiveCellEditText");
#else
        Debug.LogWarning("【编辑器模式】：JsShowNativePrompt 无法在编辑器中运行。");
        if (statusText != null) statusText.text = "请在 WebGL 构建中测试此功能。";
#endif
    }

    /// <summary>
    /// 【新】接收 M1 从 JS 浮层发回的【单个单元格】编辑数据
    /// </summary>
    public void M1_ReceiveCellEditText(string newText)
    {
        Debug.Log($"[LevelEditorManager] 接收到 JS 发回的单元格文本: {newText}");
        if (currentEditingCell_M1 == null)
        {
            Debug.LogError("接收到单元格文本，但 currentEditingCell_M1 为 null！");
            OnHtmlInputError("接收单元格文本时出错：未找到目标单元格。");
            return;
        }

        // 1. 找到那个单元格的输入框
        TMP_InputField inputField = currentEditingCell_M1.GetComponent<TMP_InputField>();
        if (inputField != null)
        {
            // 2. 更新它的文本
            inputField.text = newText;

            // 3. 标记为“已修改”
            MarkLevelAsDirty();
            if (statusText != null) statusText.text = "单元格已更新。";
        }
        else
        {
            OnHtmlInputError("接收单元格文本时出错：未在目标上找到 TMP_InputField。");
        }

        // 4. 清理引用，为下次编辑做准备
        currentEditingCell_M1 = null;
    }

}