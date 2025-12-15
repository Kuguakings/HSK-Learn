using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

public enum GameMode { WordMatch3, WordLinkUp }

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("场景名称配置")]
    public string wordMatch3SceneName = "Match3_Scene";
    public string wordLinkUpSceneName = "LinkUp_Scene";
    public string levelEditorSceneName = "LevelEditorScene";
    public string mainMenuSceneName = "MainMenu";

    [Header("过渡动画")]
    public Image fadeImage;
    public float fadeDuration = 0.7f;

    public static GameMode selectedGameMode;
    public static string selectedChapterName;
    public static LevelData selectedLevelData;

    #region 试玩模式标记
    [Tooltip("告诉游戏场景，我们是“试玩”，不是“闯关”")]
    public static bool isTestPlayMode = false;
    [Tooltip("告诉编辑器，我们“试玩成功”了 (用于显示发布按钮)")]
    public static bool justCompletedTestPlay = false;
    [Tooltip("告诉编辑器，我们“刚从试玩返回” (用于自动导航)")]
    public static bool justReturnedFromTest = false;

    [Tooltip("告诉游戏场景，我们是“管理员”")]
    public static bool IsAdmin = false;

    #endregion

    #region 单例 和 场景加载
    private void Awake()
    {
        // 【核心修复】强制解锁时间，防止从暂停状态返回导致黑屏卡死
        Time.timeScale = 1f;

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // 【WEBGL 兼容：忽略分辨率设置】
#if !UNITY_WEBGL
            if (PlayerPrefs.HasKey("ResolutionIndex"))
            {
                var resolutions = Screen.resolutions;
                var filteredResolutions = new List<Resolution>();
                var resolutionStrings = new HashSet<string>();
                for (int i = resolutions.Length - 1; i >= 0; i--)
                {
                    var res = resolutions[i];
                    if (res.width < 1024 || res.height < 768) continue;
                    string resString = res.width + " x " + res.height;
                    if (!resolutionStrings.Contains(resString)) { filteredResolutions.Add(res); resolutionStrings.Add(resString); }
                }

                int resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex");
                if (resolutionIndex >= filteredResolutions.Count)
                {
                    resolutionIndex = filteredResolutions.Count - 1;
                }
                bool isFullscreen = PlayerPrefs.GetInt("IsFullscreen") == 1;
                Resolution savedRes = filteredResolutions[resolutionIndex];
                Screen.SetResolution(savedRes.width, savedRes.height, isFullscreen);
            }
#endif

            // 初始化遮罩：刚启动时设为透明，且不挡射线
            if (fadeImage != null)
            {
                fadeImage.gameObject.SetActive(false); // 默认隐藏
                fadeImage.color = new Color(0, 0, 0, 0);
                fadeImage.raycastTarget = false;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ManuallyTriggerFadeOut()
    {
        Debug.Log("[LevelManager] 正在被手动触发 FadeOut()...");
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 1);
            fadeImage.raycastTarget = true;
        }
        StartCoroutine(FadeOut());
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 【核心修复】再次强制解锁时间
        Time.timeScale = 1f;

        // 每次场景加载完毕，执行淡出动画
        StartCoroutine(FadeOut());
    }
    #endregion

    public void LoadLevel(LevelData dataToLoad)
    {
        selectedLevelData = dataToLoad;
        selectedChapterName = dataToLoad.chapter;

        if (selectedGameMode == GameMode.WordMatch3)
        {
            StartCoroutine(LoadSceneWithFade(wordMatch3SceneName));
        }
        else if (selectedGameMode == GameMode.WordLinkUp)
        {
            StartCoroutine(LoadSceneWithFade(wordLinkUpSceneName));
        }
    }

    #region 关卡流程控制

    public void LoadMainMenu()
    {
        isTestPlayMode = false;
        justCompletedTestPlay = false;
        justReturnedFromTest = false;
        StartCoroutine(LoadSceneWithFade(mainMenuSceneName));
    }

    public void LoadMainMenuAfterTestWin()
    {
        isTestPlayMode = false;
        justCompletedTestPlay = true;
        justReturnedFromTest = false;

        if (selectedLevelData != null && TcbManager.AllLevels != null)
        {
            var target = TcbManager.AllLevels.levels.Find(l => l.id == selectedLevelData.id);
            if (target != null) target.editorStatus = "Tested";
        }

        StartCoroutine(LoadSceneWithFade(mainMenuSceneName));
    }

    public void ReloadCurrentLevel()
    {
        if (selectedLevelData != null) { LoadLevel(selectedLevelData); }
        else { Debug.LogError("没有可重新加载的关卡数据！返回主菜单。"); LoadMainMenu(); }
    }

    public void LoadNextLevel()
    {
        if (TcbManager.AllLevels == null) { Debug.LogError("TcbManager 为空，无法找到下一关！"); LoadMainMenu(); return; }

        var levelsInThisChapter = TcbManager.AllLevels.levels
            .Where(l => l.mode == (long)selectedGameMode && l.chapter == selectedChapterName)
            .OrderBy(l => l.level)
            .ToList();

        int currentIndex = levelsInThisChapter.FindIndex(l => l.id == selectedLevelData.id);
        if (currentIndex != -1 && currentIndex + 1 < levelsInThisChapter.Count)
        {
            LevelData nextLevelData = levelsInThisChapter[currentIndex + 1];
            LoadLevel(nextLevelData);
        }
        else
        {
            LoadMainMenu();
        }
    }

    public void RestartGame()
    {
        LoadMainMenu();
    }
    #endregion

    #region 试玩专用函数
    public void ReturnToEditor(bool didWin)
    {
        isTestPlayMode = false;
        justCompletedTestPlay = didWin;
        justReturnedFromTest = true;

        if (didWin && selectedLevelData != null)
        {
            selectedLevelData.editorStatus = "Tested";
            UpdateMasterLevelStatus(selectedLevelData.id, "Tested");
        }
        LoadScene(levelEditorSceneName);
    }

    private void UpdateMasterLevelStatus(string levelId, string newStatus)
    {
        if (TcbManager.AllLevels != null && TcbManager.AllLevels.levels != null)
        {
            LevelData dataInMasterList = TcbManager.AllLevels.levels.Find(l => l.id == levelId);
            if (dataInMasterList != null)
            {
                dataInMasterList.editorStatus = newStatus;
            }
        }
    }
    #endregion

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneWithFade(sceneName));
    }

    #region 过渡动画 (FadeIn, FadeOut)

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        yield return StartCoroutine(FadeIn());
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeIn()
    {
        if (fadeImage == null) { yield break; }

        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true; // 阻挡点击

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, 1); // 确保全黑
    }

    private IEnumerator FadeOut()
    {
        if (fadeImage == null) { yield break; }

        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;
        fadeImage.color = new Color(0, 0, 0, 1); // 强制从全黑开始，防止闪烁

        // 等待一帧，确保场景其他物体初始化完成
        yield return null;

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsedTime / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, 0);
        fadeImage.raycastTarget = false; // 【关键】取消阻挡

        // 【核心修复】确保动画结束后一定要隐藏，否则可能会有残留黑屏
        fadeImage.gameObject.SetActive(false);
    }
    #endregion

    public bool IsLastLevel()
    {
        if (TcbManager.AllLevels == null || selectedLevelData == null) return true;
        var levelsInThisChapter = TcbManager.AllLevels.levels
            .Where(l => l.mode == (long)selectedGameMode && l.chapter == selectedChapterName)
            .OrderBy(l => l.level)
            .ToList();
        int currentIndex = levelsInThisChapter.FindIndex(l => l.id == selectedLevelData.id);
        return (currentIndex == -1 || currentIndex >= levelsInThisChapter.Count - 1);
    }
}