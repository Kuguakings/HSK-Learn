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
        if (chapterSelectManager != null)
        {
            StartCoroutine(FadeOutAndShowChapterSelect(modeSelectPanelCG, (GameMode)mode));
        }
        else
        {
            Debug.LogError("ChapterSelectManager ����δ���ã�");
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
        Debug.Log($"׼������ؿ��༭������: {levelEditorSceneName}");

        if (LevelManager.instance != null)
        {
            // ʹ�� LevelManager �ĵ����������³����������ͻ��е��뵭��Ч����
            LevelManager.instance.LoadScene(levelEditorSceneName);
        }
        else
        {
            // ��Ϊ���գ���� LevelManager ��ʧ����ֱ�Ӽ���
            Debug.LogError("LevelManager ʵ��δ�ҵ�����ʹ�� SceneManager ֱ�Ӽ��ء�");
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

        // ������ʼʱ��������ֹ��������ֹ�ڶ��������е��
        cg.interactable = false;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            yield return null;
        }

        // ȷ����������ʱ������״̬
        cg.alpha = endAlpha;

        if (endAlpha > 0) // ����ǵ��� (͸���ȴ���0)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true; // �������߼�� (�������)
        }
        else // ����ǵ��� (͸����Ϊ0)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false; // ��ֹ���߼�� (���������)
        }
    }

    #endregion

    // --- ���������޸� ��5�����滻���ġ��˳������������� ---
    #region ��Ϸ�˳�

    /// <summary>
    /// �����µġ��˳���Ϸ��������������������ƽ̨
    /// </summary>
    public void ExitGame()
    {
        Debug.Log("���˳���Ϸ����ť�������");

        // 1. ����� Unity �༭����
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;

        // 2. ����� WebGL (��ҳ) ƽ̨��
#elif UNITY_WEBGL
        // ���ǵ��á���ʾ�ټ���塱��Э�̣������� Application.Quit()��
        // ����Ҫ�����Ǳ���Ū�����ǰ���ĸ���忪�ţ�Ȼ���������
        StartCoroutine(ShowGoodbyePanel());

        // 3. �����С�������ƽ̨ (���� PC��Mac���ֻ� App)
#else
        // ��ȫ���˳�
        Application.Quit();
#endif
    }

    /// <summary>
    /// ����һ���º����������� WebGL ƽ̨����ʾ���ټ������
    /// �����Զ���⵱ǰ�����˵��������ò˵����������￪ʼ����
    /// ��ע�⡿����ʹ�������е� TransitionTo Э�̣�
    /// </summary>
    private IEnumerator ShowGoodbyePanel()
    {
        // ��鵱ǰ�ĸ�����Ǽ���� (͸����Ϊ1)
        if (mainPanelCG.alpha == 1)
        {
            // �� ���˵� -> ���뵽 -> �ټ����
            yield return StartCoroutine(TransitionTo(mainPanelCG, goodbyePanelCG));
        }
        else if (settingsPanelCG.alpha == 1)
        {
            // �� ���ò˵� -> ���뵽 -> �ټ����
            yield return StartCoroutine(TransitionTo(settingsPanelCG, goodbyePanelCG));
        }
        else
        {
            // ��Ϊ�����ա������������嶼���ˣ���ֱ��ǿ����ʾ���ټ������
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