// MainMenuManager.cs (�Ѹ���)
using System.Collections;
using UnityEngine;

// ���¡�Ϊƽ̨�������뵼��
#if UNITY_EDITOR
using UnityEditor;
#endif

// --- �������´��� ��0�������볡������������ ---
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    #region 序列化字段 / Serialized Fields
    [Header("UI 面板 / UI Panels")]
    public GameObject mainPanel;           // 主面板 / Main panel
    public GameObject modeSelectPanel;     // 模式选择面板 / Mode selection panel
    public GameObject levelSelectPanel;    // 关卡选择面板 / Level selection panel
    public GameObject settingsPanel;       // 设置面板 / Settings panel
    public GameObject chapterSelectPanel;  // 章节选择面板 / Chapter selection panel

    [Header("退出确认 / Exit Confirmation")]
    public GameObject goodbyePanel;        // 退出确认面板（需在Unity中创建）/ Exit confirmation panel (create in Unity)

    [Header("编辑器 / Editor")]
    public string levelEditorSceneName = "LevelEditorScene"; // 关卡编辑器场景名 / Level editor scene name

    [Header("管理器引用 / Manager References")]
    public LevelSelectManager levelSelectManager;        // 关卡选择管理器 / Level select manager
    public ChapterSelectManager chapterSelectManager;    // 章节选择管理器 / Chapter select manager

    [Header("动画配置 / Animation Configuration")]
    public float panelFadeDuration = 0.3f;  // 面板淡入淡出时长 / Panel fade duration
    #endregion

    #region 私有变量 / Private Variables
    // CanvasGroup缓存：用于控制UI面板的透明度和交互 / CanvasGroup cache: Controls UI panel transparency and interaction
    private CanvasGroup mainPanelCG;
    private CanvasGroup modeSelectPanelCG;
    private CanvasGroup levelSelectPanelCG;
    private CanvasGroup chapterSelectPanelCG;
    private CanvasGroup settingsPanelCG;
    private CanvasGroup goodbyePanelCG;
    #endregion

    /// <summary>
    /// 初始化CanvasGroup组件 / Initialize CanvasGroup Components
    /// 在游戏开始时获取或自动添加所有面板的CanvasGroup组件 / Get or auto-add CanvasGroup for all panels at game start
    /// </summary>
    void Awake()
    {
        // 获取或添加CanvasGroup组件（用于控制面板淡入淡出）/ Get or add CanvasGroup components (for fade control)
        mainPanelCG = mainPanel.GetComponent<CanvasGroup>() ?? mainPanel.AddComponent<CanvasGroup>();
        modeSelectPanelCG = modeSelectPanel.GetComponent<CanvasGroup>() ?? modeSelectPanel.AddComponent<CanvasGroup>();
        levelSelectPanelCG = levelSelectPanel.GetComponent<CanvasGroup>() ?? levelSelectPanel.AddComponent<CanvasGroup>();
        chapterSelectPanelCG = chapterSelectPanel.GetComponent<CanvasGroup>() ?? chapterSelectPanel.AddComponent<CanvasGroup>();
        settingsPanelCG = settingsPanel.GetComponent<CanvasGroup>() ?? settingsPanel.AddComponent<CanvasGroup>();

        // 退出确认面板（可选）/ Exit confirmation panel (optional)
        if (goodbyePanel != null)
        {
            goodbyePanelCG = goodbyePanel.GetComponent<CanvasGroup>() ?? goodbyePanel.AddComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// 初始化面板状态 / Initialize Panel States
    /// 确保只显示主面板，其他面板都隐藏且透明度为0 / Ensure only main panel is visible, others hidden with 0 alpha
    /// </summary>
    void Start()
    {
        // 显示主面板 / Show main panel
        mainPanel.SetActive(true);
        mainPanelCG.alpha = 1;
        mainPanelCG.interactable = true;

        // 隐藏所有其他面板 / Hide all other panels
        modeSelectPanel.SetActive(false);
        modeSelectPanelCG.alpha = 0;

        levelSelectPanel.SetActive(false);
        levelSelectPanelCG.alpha = 0;

        if (chapterSelectPanel != null)
        {
            chapterSelectPanel.SetActive(false);
            chapterSelectPanelCG.alpha = 0;
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            settingsPanelCG.alpha = 0;
        }

        if (goodbyePanel != null)
        {
            goodbyePanel.SetActive(false);
            goodbyePanelCG.alpha = 0;
        }
    }

    #region 页面切换核心逻辑 / Page Switching Core Logic
    public void ShowModeSelectPanel()
    {
        StartCoroutine(TransitionTo(mainPanelCG, modeSelectPanelCG));
    }
    // (���Ĵ���ԭ�ⲻ��)
    public void StartMode(int mode)
    {
        // 【游客限制】：游客只能玩单词消消乐（WordMatch3）
        if (TcbManager.UserLevel == -1 && (GameMode)mode == GameMode.WordLinkUp)
        {
            Debug.Log("[MainMenuManager] Guest tried to enter Word Link Up; showing upgrade prompt.");
            ShowGuestUpgradePrompt();
            return;
        }

        if (chapterSelectManager != null)
        {
            StartCoroutine(FadeOutAndShowChapterSelect(modeSelectPanelCG, (GameMode)mode));
        }
        else
        {
            Debug.LogError("ChapterSelectManager reference is not assigned.");
        }
    }

    // 显示游客转正提示
    private void ShowGuestUpgradePrompt()
    {
        if (TcbManager.instance != null && TcbManager.instance.loginCanvasGroup != null)
        {
            if (TcbManager.instance.statusText != null)
            {
                TcbManager.instance.statusText.text = "Guests: Word Match 3 only (first 5 levels). Upgrade for more.";
            }
            
            TcbManager.instance.loginCanvasGroup.alpha = 1;
            TcbManager.instance.loginCanvasGroup.interactable = true;
            TcbManager.instance.loginCanvasGroup.blocksRaycasts = true;
            TcbManager.instance.loginCanvasGroup.gameObject.SetActive(true);
        }
    }

    // (���Ĵ���ԭ�ⲻ��)
    public void ShowSettingsPanel()
    {
        StartCoroutine(TransitionTo(mainPanelCG, settingsPanelCG));
    }

    // (���Ĵ���ԭ�ⲻ��)
    public void HideSettingsPanel()
    {
        StartCoroutine(TransitionTo(settingsPanelCG, mainPanelCG));
    }

    // --- �������´��� ��2�������ӡ�����༭�����Ĵ������������� ---
    /// <summary>
    /// ��������ؿ��༭������ťʱ����
    /// </summary>
    public void OnClick_ShowLevelEditor()
    {
        Debug.Log($"Loading level editor scene: {levelEditorSceneName}");

        if (LevelManager.instance != null)
        {
            LevelManager.instance.LoadScene(levelEditorSceneName);
        }
        else
        {
            Debug.LogError("LevelManager instance not found; loading via SceneManager.");
            SceneManager.LoadScene(levelEditorSceneName);
        }
    }

    #endregion

    #region ���ذ�ť�߼�
    // (���Ĵ���ԭ�ⲻ��)
    public void ShowMainPanel() { StartCoroutine(TransitionTo(modeSelectPanelCG, mainPanelCG)); }
    public void ShowModeSelectPanelFromChapterSelect() { StartCoroutine(TransitionTo(chapterSelectPanelCG, modeSelectPanelCG)); }
    public void ShowChapterSelectFromLevelSelect() { StartCoroutine(TransitionTo(levelSelectPanelCG, chapterSelectPanelCG)); }

    #endregion

    #region ����Э��
    // (���Ĵ���ԭ�ⲻ��)
    // ͨ�õ�����л���������
    private IEnumerator TransitionTo(CanvasGroup panelToHide, CanvasGroup panelToShow)
    {
        // 1. ������ǰ���
        yield return StartCoroutine(FadeCanvasGroup(panelToHide, 1f, 0f, panelFadeDuration));
        panelToHide.interactable = false;
        panelToHide.blocksRaycasts = false;
        if (panelToHide.gameObject != null) panelToHide.gameObject.SetActive(false);

        // 2. ���������
        if (panelToShow.gameObject != null) panelToShow.gameObject.SetActive(true);
        yield return StartCoroutine(FadeCanvasGroup(panelToShow, 0f, 1f, panelFadeDuration));
        panelToShow.interactable = true;
        panelToShow.blocksRaycasts = true;
    }
    // (���Ĵ���ԭ�ⲻ��)
    private IEnumerator FadeOutAndShowChapterSelect(CanvasGroup panelToHide, GameMode mode)
    {
        yield return StartCoroutine(FadeCanvasGroup(panelToHide, 1f, 0f, panelFadeDuration));
        panelToHide.interactable = false;
        panelToHide.blocksRaycasts = false;
        if (panelToHide.gameObject != null) panelToHide.gameObject.SetActive(false);

        chapterSelectManager.Show(mode);
    }
    // (���Ĵ���ԭ�ⲻ��)
    // ���ǡ������桱�� FadeCanvasGroup Э��
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
        }
    }

    #endregion

    #region Game Exit

    /// <summary>
    /// �����µġ��˳���Ϸ��������������������ƽ̨
    /// </summary>
    public void ExitGame()
    {
        Debug.Log("Exit Game button clicked.");

        // 1. Editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;

        // 2. WebGL (browser)
#elif UNITY_WEBGL
        StartCoroutine(ShowGoodbyePanel());

        // 3. Standalone/mobile
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Show goodbye panel for WebGL builds and hide other panels first.
    /// </summary>
    private IEnumerator ShowGoodbyePanel()
    {
        if (mainPanelCG.alpha == 1)
        {
            yield return StartCoroutine(TransitionTo(mainPanelCG, goodbyePanelCG));
        }
        else if (settingsPanelCG.alpha == 1)
        {
            yield return StartCoroutine(TransitionTo(settingsPanelCG, goodbyePanelCG));
        }
        else
        {
            mainPanel.SetActive(false);
            settingsPanel.SetActive(false);
            modeSelectPanel.SetActive(false);
            levelSelectPanel.SetActive(false);
            chapterSelectPanel.SetActive(false);

            goodbyePanel.SetActive(true);
            goodbyePanelCG.alpha = 1;
            goodbyePanelCG.interactable = true;
        }
    }

    #endregion
}