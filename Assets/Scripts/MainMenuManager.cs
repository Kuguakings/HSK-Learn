// MainMenuManager.cs (已更新)
using System.Collections;
using UnityEngine;

// 【新】为平台依赖编译导入
#if UNITY_EDITOR
using UnityEditor;
#endif

// --- 【【【新代码 第0步：导入场景管理】】】 ---
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI 面板")]
    public GameObject mainPanel;
    public GameObject modeSelectPanel;
    public GameObject levelSelectPanel;
    public GameObject settingsPanel;
    public GameObject chapterSelectPanel;

    // --- 【【【已修改 第1步：添加“再见”面板的引用】】】 ---
    [Header("退出界面")]
    public GameObject goodbyePanel; // <--- 我们需要您在 Unity 里创建一个这个面板

    // --- 【【【新代码 第1.5步：添加“编辑器”场景的引用】】】 ---
    [Header("编辑器")]
    public string levelEditorSceneName = "LevelEditorScene"; // 你可以改成你实际的场景名

    [Header("管理器引用")]
    public LevelSelectManager levelSelectManager;
    public ChapterSelectManager chapterSelectManager;

    [Header("动画参数")]
    public float panelFadeDuration = 0.3f;

    // CanvasGroup组件的引用，用于控制UI的透明度和交互
    private CanvasGroup mainPanelCG;
    private CanvasGroup modeSelectPanelCG;
    private CanvasGroup levelSelectPanelCG;
    private CanvasGroup chapterSelectPanelCG;
    private CanvasGroup settingsPanelCG; // 【新增】设置面板的CanvasGroup引用

    // --- 【【【已修改 第2步：添加“再见”面板的CanvasGroup】】】 ---
    private CanvasGroup goodbyePanelCG; // <--- 新增

    void Awake()
    {
        // 在游戏开始时，获取或自动添加所有面板的CanvasGroup组件
        mainPanelCG = mainPanel.GetComponent<CanvasGroup>() ?? mainPanel.AddComponent<CanvasGroup>();
        modeSelectPanelCG = modeSelectPanel.GetComponent<CanvasGroup>() ?? modeSelectPanel.AddComponent<CanvasGroup>();
        levelSelectPanelCG = levelSelectPanel.GetComponent<CanvasGroup>() ?? levelSelectPanel.AddComponent<CanvasGroup>();
        chapterSelectPanelCG = chapterSelectPanel.GetComponent<CanvasGroup>() ?? chapterSelectPanel.AddComponent<CanvasGroup>();
        settingsPanelCG = settingsPanel.GetComponent<CanvasGroup>() ?? settingsPanel.AddComponent<CanvasGroup>(); // 【新增】

        // --- 【【【已修改 第3步：初始化“再见”面板】】】 ---
        // (这是“添加”的，没有改变您已有的 Awake 代码)
        if (goodbyePanel != null)
        {
            goodbyePanelCG = goodbyePanel.GetComponent<CanvasGroup>() ?? goodbyePanel.AddComponent<CanvasGroup>();
        }
    }

    void Start()
    {
        // 初始化，确保只显示主面板，其他所有面板都隐藏并设置为透明
        mainPanel.SetActive(true);
        mainPanelCG.alpha = 1;
        mainPanelCG.interactable = true;

        modeSelectPanel.SetActive(false);
        modeSelectPanelCG.alpha = 0;

        levelSelectPanel.SetActive(false);
        levelSelectPanelCG.alpha = 0;

        if (chapterSelectPanel != null)
        {
            chapterSelectPanel.SetActive(false);
            chapterSelectPanelCG.alpha = 0;
        }

        // 【新增】确保设置面板在开始时也完全隐藏
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            settingsPanelCG.alpha = 0;
        }

        // --- 【【【已修改 第4步：在开始时也隐藏“再见”面板】】】 ---
        // (这是“添加”的，没有改变您已有的 Start 代码)
        if (goodbyePanel != null)
        {
            goodbyePanel.SetActive(false);
            goodbyePanelCG.alpha = 0;
        }
    }

    #region 页面切换核心逻辑
    // (您的代码原封不动)
    public void ShowModeSelectPanel()
    {
        StartCoroutine(TransitionTo(mainPanelCG, modeSelectPanelCG));
    }
    // (您的代码原封不动)
    public void StartMode(int mode)
    {
        if (chapterSelectManager != null)
        {
            StartCoroutine(FadeOutAndShowChapterSelect(modeSelectPanelCG, (GameMode)mode));
        }
        else
        {
            Debug.LogError("ChapterSelectManager 引用未设置！");
        }
    }

    // (您的代码原封不动)
    public void ShowSettingsPanel()
    {
        StartCoroutine(TransitionTo(mainPanelCG, settingsPanelCG));
    }

    // (您的代码原封不动)
    public void HideSettingsPanel()
    {
        StartCoroutine(TransitionTo(settingsPanelCG, mainPanelCG));
    }

    // --- 【【【新代码 第2步：添加“点击编辑器”的处理函数】】】 ---
    /// <summary>
    /// 当点击“关卡编辑器”按钮时调用
    /// </summary>
    public void OnClick_ShowLevelEditor()
    {
        Debug.Log($"准备进入关卡编辑器场景: {levelEditorSceneName}");

        if (LevelManager.instance != null)
        {
            // 使用 LevelManager 的单例来加载新场景（这样就会有淡入淡出效果）
            LevelManager.instance.LoadScene(levelEditorSceneName);
        }
        else
        {
            // 作为保险，如果 LevelManager 丢失，就直接加载
            Debug.LogError("LevelManager 实例未找到！将使用 SceneManager 直接加载。");
            SceneManager.LoadScene(levelEditorSceneName);
        }
    }

    #endregion

    #region 返回按钮逻辑
    // (您的代码原封不动)
    public void ShowMainPanel() { StartCoroutine(TransitionTo(modeSelectPanelCG, mainPanelCG)); }
    public void ShowModeSelectPanelFromChapterSelect() { StartCoroutine(TransitionTo(chapterSelectPanelCG, modeSelectPanelCG)); }
    public void ShowChapterSelectFromLevelSelect() { StartCoroutine(TransitionTo(levelSelectPanelCG, chapterSelectPanelCG)); }

    #endregion

    #region 动画协程
    // (您的代码原封不动)
    // 通用的面板切换动画函数
    private IEnumerator TransitionTo(CanvasGroup panelToHide, CanvasGroup panelToShow)
    {
        // 1. 淡出当前面板
        yield return StartCoroutine(FadeCanvasGroup(panelToHide, 1f, 0f, panelFadeDuration));
        panelToHide.interactable = false;
        panelToHide.blocksRaycasts = false;
        if (panelToHide.gameObject != null) panelToHide.gameObject.SetActive(false);

        // 2. 淡入新面板
        if (panelToShow.gameObject != null) panelToShow.gameObject.SetActive(true);
        yield return StartCoroutine(FadeCanvasGroup(panelToShow, 0f, 1f, panelFadeDuration));
        panelToShow.interactable = true;
        panelToShow.blocksRaycasts = true;
    }
    // (您的代码原封不动)
    private IEnumerator FadeOutAndShowChapterSelect(CanvasGroup panelToHide, GameMode mode)
    {
        yield return StartCoroutine(FadeCanvasGroup(panelToHide, 1f, 0f, panelFadeDuration));
        panelToHide.interactable = false;
        panelToHide.blocksRaycasts = false;
        if (panelToHide.gameObject != null) panelToHide.gameObject.SetActive(false);

        chapterSelectManager.Show(mode);
    }
    // (您的代码原封不动)
    // 这是“完美版”的 FadeCanvasGroup 协程
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;

        // 动画开始时，立即阻止交互，防止在动画过程中点击
        cg.interactable = false;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            yield return null;
        }

        // 确保动画结束时在最终状态
        cg.alpha = endAlpha;

        if (endAlpha > 0) // 如果是淡入 (透明度大于0)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true; // 允许射线检测 (允许点击)
        }
        else // 如果是淡出 (透明度为0)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false; // 阻止射线检测 (不允许点击)
        }
    }

    #endregion

    // --- 【【【已修改 第5步：替换您的“退出”函数】】】 ---
    #region 游戏退出

    /// <summary>
    /// 这是新的“退出游戏”函数，它能智能区分平台
    /// </summary>
    public void ExitGame()
    {
        Debug.Log("“退出游戏”按钮被点击！");

        // 1. 如果在 Unity 编辑器中
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;

        // 2. 如果在 WebGL (网页) 平台中
#elif UNITY_WEBGL
        // 我们调用“显示再见面板”的协程，而不是 Application.Quit()！
        // 【重要】我们必须弄清楚当前是哪个面板开着，然后从它过渡
        StartCoroutine(ShowGoodbyePanel());

        // 3. 在所有“其他”平台 (比如 PC、Mac、手机 App)
#else
        // 安全地退出
        Application.Quit();
#endif
    }

    /// <summary>
    /// 这是一个新函数，用于在 WebGL 平台上显示“再见”面板
    /// 它会自动检测当前是主菜单还是设置菜单，并从那里开始淡出
    /// 【注意】它会使用您已有的 TransitionTo 协程！
    /// </summary>
    private IEnumerator ShowGoodbyePanel()
    {
        // 检查当前哪个面板是激活的 (透明度为1)
        if (mainPanelCG.alpha == 1)
        {
            // 从 主菜单 -> 淡入到 -> 再见面板
            yield return StartCoroutine(TransitionTo(mainPanelCG, goodbyePanelCG));
        }
        else if (settingsPanelCG.alpha == 1)
        {
            // 从 设置菜单 -> 淡入到 -> 再见面板
            yield return StartCoroutine(TransitionTo(settingsPanelCG, goodbyePanelCG));
        }
        else
        {
            // 作为“保险”，如果其他面板都关了，就直接强行显示“再见”面板
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