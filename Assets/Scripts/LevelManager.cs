/// <summary>
/// 关卡管理器 / Level Manager
/// 管理场景切换、关卡进度、淡入淡出效果 / Manages scene transitions, level progress, and fade effects
/// 使用单例模式，场景切换时不销毁 / Uses singleton pattern, persists across scene changes
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

/// <summary>游戏模式枚举 / Game Mode Enumeration</summary>
public enum GameMode { WordMatch3, WordLinkUp }

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;  // 单例实例 / Singleton instance

    [Header("场景名称配置 / Scene Name Configuration")]
    public string wordMatch3SceneName = "Match3_Scene";         // 消消乐场景 / Match-3 scene
    public string wordLinkUpSceneName = "LinkUp_Scene";         // 连连看场景 / Link-up scene
    public string levelEditorSceneName = "LevelEditorScene";    // 关卡编辑器场景 / Level editor scene
    public string mainMenuSceneName = "MainMenu";               // 主菜单场景 / Main menu scene

    [Header("淡入淡出配置 / Fade Configuration")]
    public Image fadeImage;           // 黑色遮罩图片 / Black fade image
    public float fadeDuration = 0.7f; // 淡入淡出时长 / Fade duration

    // 静态变量：存储关卡选择信息 / Static variables: Store level selection info
    // 注意：使用静态变量方便跨场景传递数据，但需要小心管理状态
    // Note: Static variables make cross-scene data passing easy, but state management requires care
    public static GameMode selectedGameMode;       // 选中的游戏模式 / Selected game mode
    public static string selectedChapterName;      // 选中的章节名 / Selected chapter name
    public static LevelData selectedLevelData;     // 选中的关卡数据 / Selected level data

    #region 测试模式标志 / Test Mode Flags
    // 这些静态布尔值用于在关卡编辑器和测试之间传递状态
    // These static booleans pass state between level editor and testing
    
    [Tooltip("当前游戏是否处于【测试】模式（不是【正常】）/ Is currently in test mode (not normal mode)")]
    public static bool isTestPlayMode = false;
    
    [Tooltip("关卡编辑器是否【刚完成测试】(用于显示返回按钮) / Did just complete test play (for showing return button)")]
    public static bool justCompletedTestPlay = false;
    
    [Tooltip("关卡编辑器是否【刚从测试返回】(用于自动保存) / Did just return from test (for auto-save)")]
    public static bool justReturnedFromTest = false;

    [Tooltip("当前游戏是否处于【管理员】模式 / Is currently in admin mode")]
    public static bool IsAdmin = false;

    #endregion

    #region ���� �� ��������
    private void Awake()
    {
        // �������޸���ǿ�ƽ���ʱ�䣬��ֹ����ͣ״̬���ص��º�������
        Time.timeScale = 1f;

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // ��WEBGL ���ݣ����Էֱ������á�
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

            // ��ʼ�����֣�������ʱ��Ϊ͸�����Ҳ�������
            if (fadeImage != null)
            {
                fadeImage.gameObject.SetActive(false); // Ĭ������
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
        Debug.Log("[LevelManager] ���ڱ��ֶ����� FadeOut()...");
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
        // �������޸����ٴ�ǿ�ƽ���ʱ��
        Time.timeScale = 1f;

        // ÿ�γ���������ϣ�ִ�е�������
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

    #region �ؿ����̿���

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
        else { Debug.LogError("û�п����¼��صĹؿ����ݣ��������˵���"); LoadMainMenu(); }
    }

    public void LoadNextLevel()
    {
        if (TcbManager.AllLevels == null) { Debug.LogError("TcbManager Ϊ�գ��޷��ҵ���һ�أ�"); LoadMainMenu(); return; }

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

    #region ����ר�ú���
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

    #region ���ɶ��� (FadeIn, FadeOut)

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        yield return StartCoroutine(FadeIn());
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeIn()
    {
        if (fadeImage == null) { yield break; }

        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true; // �赲���

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, 1); // ȷ��ȫ��
    }

    private IEnumerator FadeOut()
    {
        if (fadeImage == null) { yield break; }

        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;
        fadeImage.color = new Color(0, 0, 0, 1); // ǿ�ƴ�ȫ�ڿ�ʼ����ֹ��˸

        // �ȴ�һ֡��ȷ���������������ʼ�����
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
        fadeImage.raycastTarget = false; // ���ؼ���ȡ���赲

        // �������޸���ȷ������������һ��Ҫ���أ�������ܻ��в�������
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