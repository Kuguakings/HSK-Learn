using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;
using System.Linq;

// =========================================================
// 1. 数据结构定义 (完整保留)
// =========================================================

[Serializable]
public class AdminData
{
    public string _id;
    public string name;
    public int level; // 管理员级别：1=普通管理员, 999=超级管理员
    public int userLevel = 1; // 用户等级：-1=游客, 0=学员, 1+=管理员（管理员应>=1）
}

[Serializable]
public class UserProfileData
{
    public string nickname;
    public string role;
    public int userLevel = 0; // -1=游客, 0=学员, 1+=管理员
}

[Serializable]
public class DbResponseWrapper<T>
{
    public List<T> data;
    public string requestId;
}

[Serializable]
public class SingleDocResponse<T>
{
    public T data;
}

[Serializable]
public class Mode1Content
{
    public int groupId;
    public string hanzi;
    public string pinyin;
    public string english;
}

[Serializable]
public class Mode2Content
{
    public int sentenceId;
    public int wordOrder;
    public string wordText;
    public string fullSentence;
}

[Serializable]
public class LevelData
{
    public string id;
    public string chapter;
    public int mode;
    public int level;
    public List<Mode1Content> content_mode_1 = new List<Mode1Content>();
    public List<Mode2Content> content_mode_2 = new List<Mode2Content>();
    public string editorStatus = "Working";
}

[Serializable]
public class LevelDataCollection
{
    public List<LevelData> levels = new List<LevelData>();
}

// =========================================================
// 2. TcbManager 主类
// =========================================================

public class TcbManager : MonoBehaviour
{
    public static TcbManager instance;

    // 全局静态状态
    public static bool isLoggedIn = false;
    public static bool IsAdmin = false;
    public static int AdminLevel = 0;
    public static int UserLevel = 0; // -1=游客, 0=学员, 1+=管理员
    public static string CurrentUid = "";
    public static string CurrentNickname = "";

    public static LevelDataCollection AllLevels;

    // 持久化 Key
    private const string PREF_AUTO_LOGIN_UID = "AutoLogin_UID";
    private const string PREF_AUTO_LOGIN_NICKNAME = "AutoLogin_Nickname";
    private const string PREF_IS_ADMIN = "AutoLogin_IsAdmin";
    private const string PREF_USER_LEVEL = "AutoLogin_UserLevel";

    #region JS 桥梁
    [DllImport("__Internal")] private static extern void JsRegisterUser(string e, string p, string o, string s, string r);
    [DllImport("__Internal")] private static extern void JsLoginUser(string e, string p, string o, string s, string r);
    [DllImport("__Internal")] private static extern void JsLogoutUser();
    [DllImport("__Internal")] private static extern void JsCheckAdminStatus(string u, string o, string s, string r);
    [DllImport("__Internal")] private static extern void JsGetLevels(string o, string s, string r);
    [DllImport("__Internal")] private static extern void JsUploadNewLevel(string d, string j, string o, string s, string r);
    [DllImport("__Internal")] private static extern void JsGetUserProfile(string u, string o, string s, string r);
    [DllImport("__Internal")] private static extern void JsCreateUserProfile(string u, string n, string o, string s, string r);
    [DllImport("__Internal")] private static extern void JsUpdateUsername(string u, string n, string o, string s, string r);
    [DllImport("__Internal")] private static extern void JsDbGetCollection(string coll, string reqId, string o, string s, string e);
    [DllImport("__Internal")] private static extern void JsDbSetDocument(string coll, string docId, string json, string reqId, string o, string s, string e);
    [DllImport("__Internal")] private static extern void JsDbAddDocument(string coll, string json, string reqId, string o, string s, string e);
    [DllImport("__Internal")] private static extern void JsDbDeleteDocument(string coll, string docId, string reqId, string o, string s, string e);
    [DllImport("__Internal")] private static extern void JsDbGetDocument(string coll, string docId, string reqId, string o, string s, string e);
    [DllImport("__Internal")] private static extern void JsCreateGuestAccount(string o, string s, string r);
    [DllImport("__Internal")] private static extern void JsUpgradeGuestAccount(string oldUid, string newUid, string newPassword, string o, string s, string r);
    [DllImport("__Internal")] private static extern void JsReloadPage();
    #endregion

    [Header("UI 元素")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Button registerButton;
    public Button loginButton;
    public Button guestUpgradeButton; // 游客转正按钮
    public Button logoutButton; // 退出登录按钮（非游客用户）
    public Button backButton; // 返回主菜单按钮
    public TextMeshProUGUI statusText;

    public CanvasGroup loginCanvasGroup;
    public CanvasGroup mainMenuObjectGroup;
    public Button levelEditorButton;

    public float panelFadeDuration = 0.3f;

    private Dictionary<string, Action<string>> dbSuccessCallbacks = new Dictionary<string, Action<string>>();
    private Dictionary<string, Action<string>> dbErrorCallbacks = new Dictionary<string, Action<string>>();

    // =========================================================
    // 3. 生命周期与初始化
    // =========================================================

    void Awake()
    {
        // 【关键修复】强制时间正常流逝，防止从暂停状态返回时卡死
        Time.timeScale = 1f;

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
#if UNITY_EDITOR
        Debug.LogWarning("--- UNITY EDITOR 模式 ---");
        IsAdmin = true; 
        AdminLevel = 999; 
        CurrentUid = "test_editor_user_001"; 
        CurrentNickname = "Editor Admin";
        if (LevelManager.instance != null) LevelManager.IsAdmin = true;
        if (levelEditorButton != null) levelEditorButton.gameObject.SetActive(true);
        
        SetCanvasGroupState(loginCanvasGroup, false);
        SetCanvasGroupState(mainMenuObjectGroup, true);
        
        if (AllLevels == null) AllLevels = new LevelDataCollection();
        isLoggedIn = true;
#else
        // 【核心修复】真机/WebGL 模式下，手动触发一次场景加载逻辑
        // 这解决了“刚进游戏时 OnSceneLoaded 可能因为注册晚了没触发”的问题
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            Debug.Log("[TcbManager] 手动触发主菜单初始化...");
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
#endif
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 每次切场景都确保时间正常
        Time.timeScale = 1f;

        if (scene.name == "MainMenu")
        {
            Debug.Log($"[TcbManager] 进入主菜单。当前登录状态: {isLoggedIn}");

            // 1. 绑定 UI
            BindUIComponentsSafe();

            // 2. 【核心修复】检查自动登录
            // 如果还没登录，就尝试读缓存
            if (!isLoggedIn)
            {
                CheckAutoLogin();
            }

            // 3. 刷新 UI
            UpdateUIState();
        }
    }

    private void UpdateUIState()
    {
        // 双重保险：如果引用丢了，再找一次
        if (loginCanvasGroup == null || mainMenuObjectGroup == null) BindUIComponentsSafe();

        if (isLoggedIn)
        {
            Debug.Log("[TcbManager] 已登录，隐藏登录框，显示主菜单。");
            // 已登录：关登录页，开主页
            SetCanvasGroupState(loginCanvasGroup, false);
            SetCanvasGroupState(mainMenuObjectGroup, true);

            var profile = FindObjectOfType<UserProfileManager>();
            if (profile != null) profile.UpdateUI();

            if (levelEditorButton) levelEditorButton.gameObject.SetActive(IsAdmin);

            // 如果数据没加载，静默加载
            if (AllLevels == null || AllLevels.levels.Count == 0)
            {
                LoadLevelsSilent();
            }
        }
        else
        {
            Debug.Log("[TcbManager] Not logged in; showing login panel.");
            // 未登录：开登录页，关主页
            SetCanvasGroupState(loginCanvasGroup, true);
            SetCanvasGroupState(mainMenuObjectGroup, false);

            if (statusText) statusText.text = "Please enter username and password.";
        }
    }

    private void CheckAutoLogin()
    {
        string savedUid = PlayerPrefs.GetString(PREF_AUTO_LOGIN_UID, "");
        if (!string.IsNullOrEmpty(savedUid))
        {
            Debug.Log($"[TcbManager] Auto-login detected for UID: {savedUid}");
            CurrentUid = savedUid;
            CurrentNickname = PlayerPrefs.GetString(PREF_AUTO_LOGIN_NICKNAME, "Student");
            IsAdmin = PlayerPrefs.GetInt(PREF_IS_ADMIN, 0) == 1;
            UserLevel = PlayerPrefs.GetInt(PREF_USER_LEVEL, 0);

            if (LevelManager.instance != null) LevelManager.IsAdmin = IsAdmin;

            // 标记为已登录
            isLoggedIn = true;

            // 触发静默校验（不挡UI）
            SilentReauth();
        }
        else
        {
            // 【新增：游客机制】首次访问，自动创建游客账号
            Debug.Log("[TcbManager] First visit; creating guest account...");
            CreateGuestAccount();
        }
    }

    private void SilentReauth()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        JsCheckAdminStatus(CurrentUid, gameObject.name, "OnAdminCheckResult_Silent", "OnAuthError_Silent");
        JsGetUserProfile(CurrentUid, gameObject.name, "OnGetUserProfileSuccess", "OnAuthError_Silent");
#endif
    }

    // 静默回调
    public void OnAdminCheckResult_Silent(string jsonOrEmpty) { OnAdminCheckResult(jsonOrEmpty); }
    public void OnAuthError_Silent(string err) 
    { 
        Debug.LogWarning("Silent refresh failed: " + err);
        
        if (!string.IsNullOrEmpty(CurrentUid) && CurrentUid.StartsWith("Guest") && 
            (err.Contains("不存在") || err.Contains("NotExist") || err.Contains("not exist")))
        {
            Debug.LogWarning($"[TcbManager] Guest account {CurrentUid} failed auth (maybe removed); clearing cache and reloading.");
            ClearCacheAndReload();
        }
    }

    // 清除缓存并刷新页面
    private void ClearCacheAndReload()
    {
        PlayerPrefs.DeleteKey(PREF_AUTO_LOGIN_UID);
        PlayerPrefs.DeleteKey(PREF_AUTO_LOGIN_NICKNAME);
        PlayerPrefs.DeleteKey(PREF_IS_ADMIN);
        PlayerPrefs.DeleteKey(PREF_USER_LEVEL);
        PlayerPrefs.Save();

#if UNITY_WEBGL && !UNITY_EDITOR
        JsReloadPage();
#else
        Debug.Log("[TcbManager] Editor mode: not reloading page.");
#endif
    }

    // =========================================================
    // 游客账号管理
    // =========================================================

    private void CreateGuestAccount()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (statusText) statusText.text = "Creating guest account...";
        JsCreateGuestAccount(gameObject.name, "OnGuestAccountCreated", "OnGuestAccountError");
#else
        // 编辑器模式下直接创建本地游客
        OnGuestAccountCreated("GuestEditor_" + System.Guid.NewGuid().ToString().Substring(0, 8));
#endif
    }

    public void OnGuestAccountCreated(string guestUid)
    {
        Debug.Log($"[TcbManager] 游客账号创建成功: {guestUid}");
        
        CurrentUid = guestUid;
        CurrentNickname = guestUid; // 直接使用 Guest1, Guest2... 作为昵称
        UserLevel = -1; // 游客等级
        IsAdmin = false;
        AdminLevel = 0;
        
        // 保存到本地，下次自动登录
        PlayerPrefs.SetString(PREF_AUTO_LOGIN_UID, guestUid);
        PlayerPrefs.SetString(PREF_AUTO_LOGIN_NICKNAME, CurrentNickname);
        PlayerPrefs.SetInt(PREF_IS_ADMIN, 0);
        PlayerPrefs.SetInt(PREF_USER_LEVEL, -1);
        PlayerPrefs.Save();
        
        // 标记为已登录
        isLoggedIn = true;
        
        // 更新 UI
        UpdateUIState();
        
        if (statusText) statusText.text = "";
    }

    public void OnGuestAccountError(string error)
    {
        Debug.LogError($"[TcbManager] 游客账号创建失败: {error}");
        if (statusText) statusText.text = "Failed to create guest account. Please refresh.";
    }

    // 游客转正
    public void UpgradeGuestAccount(string newUsername, string newPassword)
    {
        if (UserLevel != -1)
        {
            Debug.LogWarning("[TcbManager] 当前不是游客账号，无需转正");
            if (statusText) statusText.text = "You are not a guest user.";
            return;
        }

        if (string.IsNullOrEmpty(newUsername) || string.IsNullOrEmpty(newPassword))
        {
            if (statusText) statusText.text = "Username and password cannot be empty.";
            return;
        }

        if (statusText) statusText.text = "Upgrading account...";

#if UNITY_WEBGL && !UNITY_EDITOR
        JsUpgradeGuestAccount(CurrentUid, newUsername, newPassword, gameObject.name, "OnUpgradeSuccess", "OnUpgradeError");
#else
        OnUpgradeSuccess(newUsername);
#endif
    }

    public void OnUpgradeSuccess(string newUid)
    {
        Debug.Log($"[TcbManager] 转正成功！新账号: {newUid}");
        
        string oldUid = CurrentUid;
        
        // 更新当前用户信息
        CurrentUid = newUid;
        CurrentNickname = newUid;
        UserLevel = 0; // 学员等级
        IsAdmin = false;
        AdminLevel = 0;
        
        // 保存到本地
        PlayerPrefs.SetString(PREF_AUTO_LOGIN_UID, newUid);
        PlayerPrefs.SetString(PREF_AUTO_LOGIN_NICKNAME, CurrentNickname);
        PlayerPrefs.SetInt(PREF_USER_LEVEL, 0);
        PlayerPrefs.SetInt(PREF_IS_ADMIN, 0);
        PlayerPrefs.Save();
        
        if (statusText) statusText.text = "Upgrade successful! Welcome!";
        
        // 更新UI
        var profile = FindObjectOfType<UserProfileManager>();
        if (profile) profile.UpdateUI();
        
        // 隐藏登录框，显示主菜单
        UpdateUIState();
    }

    public void OnUpgradeError(string error)
    {
        Debug.LogError($"[TcbManager] 转正失败: {error}");
        if (statusText) statusText.text = "Upgrade failed: " + error;
    }

    private void SetCanvasGroupState(CanvasGroup cg, bool visible)
    {
        if (cg == null) return;
        cg.alpha = visible ? 1 : 0;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
        // 只有需要显示时才 SetActive(true)，隐藏时 SetActive(false)
        cg.gameObject.SetActive(visible);
    }

    private void BindUIComponentsSafe()
    {
        // 如果 loginButton 已经绑定且非空，说明之前已经成功过了，直接跳过，防止 Log 刷屏
        if (loginButton != null && emailInput != null && passwordInput != null)
        {
            return;
        }

        Debug.Log("[TcbManager] 正在安全绑定 UI 组件...");
        try
        {
            // 1. 查找 LoginPanel
            if (loginCanvasGroup == null)
            {
                var allCGs = Resources.FindObjectsOfTypeAll<CanvasGroup>();
                foreach (var cg in allCGs)
                {
                    if (cg.gameObject.scene.IsValid() && cg.gameObject.name == "LoginPanel")
                    {
                        loginCanvasGroup = cg;
                        break;
                    }
                }
            }

            // 2. 绑定 LoginPanel 内的元素
            if (loginCanvasGroup != null)
            {
                GameObject panelObj = loginCanvasGroup.gameObject;
                var allInputs = panelObj.GetComponentsInChildren<TMP_InputField>(true);

                // 优先按名字找
                emailInput = allInputs.FirstOrDefault(x => x.name == "Input_Account");
                passwordInput = allInputs.FirstOrDefault(x => x.name == "Input_Password");

                // 暴力兜底：按顺序找
                if (emailInput == null && allInputs.Length > 0)
                {
                    emailInput = allInputs[0];
                    Debug.LogWarning($"未找到 Input_Account，自动使用第一个输入框: {emailInput.name}");
                }
                if (passwordInput == null && allInputs.Length > 1)
                {
                    passwordInput = allInputs[1];
                    Debug.LogWarning($"未找到 Input_Password，自动使用第二个输入框: {passwordInput.name}");
                }

                var allBtns = panelObj.GetComponentsInChildren<Button>(true);
                loginButton = allBtns.FirstOrDefault(x => x.name == "Btn_Login");
                registerButton = allBtns.FirstOrDefault(x => x.name == "Btn_Register");
                guestUpgradeButton = allBtns.FirstOrDefault(x => x.name == "Btn_GuestUpgrade");
                logoutButton = allBtns.FirstOrDefault(x => x.name == "Btn_Logout");
                backButton = allBtns.FirstOrDefault(x => x.name == "Btn_Back");
                
                var allTexts = panelObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                statusText = allTexts.FirstOrDefault(x => x.name == "Text_Status");

                if (loginButton != null)
                {
                    loginButton.onClick.RemoveAllListeners();
                    loginButton.onClick.AddListener(LoginUser);
                }
                if (registerButton != null)
                {
                    registerButton.onClick.RemoveAllListeners();
                    registerButton.onClick.AddListener(RegisterUser);
                }
                if (guestUpgradeButton != null)
                {
                    guestUpgradeButton.onClick.RemoveAllListeners();
                    guestUpgradeButton.onClick.AddListener(OnGuestUpgradeButtonClicked);
                }
                if (logoutButton != null)
                {
                    logoutButton.onClick.RemoveAllListeners();
                    logoutButton.onClick.AddListener(LogoutUser);
                    // 游客不显示退出登录按钮
                    logoutButton.gameObject.SetActive(UserLevel != -1);
                }
                if (backButton != null)
                {
                    backButton.onClick.RemoveAllListeners();
                    backButton.onClick.AddListener(BackToMainMenu);
                }
            }
            else
            {
                Debug.LogError("【严重】找不到 LoginPanel！");
            }

            // 3. 查找 MainPanel
            if (mainMenuObjectGroup == null)
            {
                var allCGs = Resources.FindObjectsOfTypeAll<CanvasGroup>();
                foreach (var cg in allCGs)
                {
                    if (cg.gameObject.scene.IsValid() && cg.gameObject.name == "MainPanel")
                    {
                        mainMenuObjectGroup = cg;
                        break;
                    }
                }
            }

            // 4. 查找 LevelEditorButton
            if (levelEditorButton == null)
            {
                var allBtns = Resources.FindObjectsOfTypeAll<Button>();
                foreach (var btn in allBtns)
                {
                    if (btn.gameObject.scene.IsValid() && btn.gameObject.name == "LevelEditor_Button")
                    {
                        levelEditorButton = btn;
                        break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[TcbManager] UI 绑定异常: " + e.Message);
        }
    }

    public void RegisterUser() { CallJsAuth(true); }
    public void LoginUser()
    {
        Debug.Log("点击了登录按钮...");
        CallJsAuth(false);
    }

    // 游客转正按钮点击
    public void OnGuestUpgradeButtonClicked()
    {
        Debug.Log("[TcbManager] 点击了游客转正按钮");
        if (emailInput == null || passwordInput == null) return;
        
        string newUsername = emailInput.text;
        string newPassword = passwordInput.text;
        
        UpgradeGuestAccount(newUsername, newPassword);
    }

    // 退出登录
    public void LogoutUser()
    {
        Debug.Log("[TcbManager] 退出登录");
        
        // 清除所有本地缓存
        PlayerPrefs.DeleteKey(PREF_AUTO_LOGIN_UID);
        PlayerPrefs.DeleteKey(PREF_AUTO_LOGIN_NICKNAME);
        PlayerPrefs.DeleteKey(PREF_IS_ADMIN);
        PlayerPrefs.DeleteKey(PREF_USER_LEVEL);
        PlayerPrefs.Save();
        
        // 清除当前状态
        isLoggedIn = false;
        CurrentUid = "";
        CurrentNickname = "";
        UserLevel = 0;
        IsAdmin = false;
        AdminLevel = 0;

#if UNITY_WEBGL && !UNITY_EDITOR
        // 调用腾讯云登出
        JsLogoutUser();
        // 刷新页面（强制重新进入，创建新游客账号）
        JsReloadPage();
#else
        Debug.Log("[TcbManager] 编辑器模式下不刷新页面");
#endif
    }

    // 返回主菜单
    public void BackToMainMenu()
    {
        Debug.Log("[TcbManager] 返回主菜单");
        
        // 隐藏登录面板
        SetCanvasGroupState(loginCanvasGroup, false);
        
        // 显示主菜单
        SetCanvasGroupState(mainMenuObjectGroup, true);
        
        // 清空输入框和状态文本
        if (emailInput != null) emailInput.text = "";
        if (passwordInput != null) passwordInput.text = "";
        if (statusText != null) statusText.text = "";
    }

    private void CallJsAuth(bool isRegister)
    {
        if (emailInput == null || passwordInput == null) BindUIComponentsSafe();
        if (emailInput == null || passwordInput == null)
        {
            if (statusText) statusText.text = "UI Error: Input fields not found.";
            return;
        }

        string email = emailInput.text;
        string password = passwordInput.text;

        if (statusText) statusText.text = "Connecting...";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            if (statusText) statusText.text = "Username and password cannot be empty.";
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        if(isRegister) JsRegisterUser(email, password, gameObject.name, "OnLoginOrRegisterSuccess", "OnAuthError");
        else JsLoginUser(email, password, gameObject.name, "OnLoginOrRegisterSuccess", "OnAuthError");
#endif
    }

    public void OnLoginOrRegisterSuccess(string uid)
    {
        CurrentUid = uid;
        if (statusText != null) statusText.text = "Login successful, loading...";

        // 【核心修复】保存自动登录信息
        PlayerPrefs.SetString(PREF_AUTO_LOGIN_UID, uid);
        PlayerPrefs.Save();

#if UNITY_WEBGL && !UNITY_EDITOR
        JsCheckAdminStatus(uid, gameObject.name, "OnAdminCheckResult", "OnAuthError");
        JsGetUserProfile(uid, gameObject.name, "OnGetUserProfileSuccess", "OnAuthError");
#endif
    }

    public void OnGetUserProfileSuccess(string json)
    {
        if (!string.IsNullOrEmpty(json))
        {
            var data = JsonUtility.FromJson<UserProfileData>(json);
            CurrentNickname = data.nickname;
            UserLevel = data.userLevel;
            
            PlayerPrefs.SetString(PREF_AUTO_LOGIN_NICKNAME, CurrentNickname);
            PlayerPrefs.SetInt(PREF_USER_LEVEL, UserLevel);
            PlayerPrefs.Save();

            var p = FindObjectOfType<UserProfileManager>();
            if (p) p.UpdateUI();
        }
        else
        {
            // 【新增】如果是游客账号且不存在，清除缓存并刷新页面
            if (!string.IsNullOrEmpty(CurrentUid) && CurrentUid.StartsWith("Guest"))
            {
                Debug.LogWarning($"[TcbManager] 游客账号 {CurrentUid} 在数据库中不存在（可能已过期），清除缓存并刷新页面");
                ClearCacheAndReload();
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            string defaultName = "Student_" + CurrentUid.Substring(0, 4);
            JsCreateUserProfile(CurrentUid, defaultName, gameObject.name, "OnCreateProfileSuccess", "OnAuthError");
#endif
        }
    }

    public void OnCreateProfileSuccess(string msg)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        JsGetUserProfile(CurrentUid, gameObject.name, "OnGetUserProfileSuccess", "OnAuthError");
#endif
    }

    public void RequestUpdateUsername(string newName)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        JsUpdateUsername(CurrentUid, newName, gameObject.name, "OnUpdateNameSuccess", "OnAuthError");
#endif
    }
    public void OnUpdateNameSuccess(string msg) { RequestUpdateUsername(CurrentUid); }

    public void OnAdminCheckResult(string jsonOrEmpty)
    {
        if (!string.IsNullOrEmpty(jsonOrEmpty))
        {
            IsAdmin = true;
            if (LevelManager.instance != null) LevelManager.IsAdmin = true;
            try { 
                var d = JsonUtility.FromJson<AdminData>(jsonOrEmpty); 
                AdminLevel = d.level;
                UserLevel = d.userLevel; // 读取用户等级
            } catch { 
                AdminLevel = 1;
                UserLevel = 1; // 管理员默认等级为1
            }
            if (levelEditorButton) levelEditorButton.gameObject.SetActive(true);
        }
        else
        {
            IsAdmin = false;
            AdminLevel = 0;
            // 如果之前是管理员，现在撤销权限，降为学员等级（游客-1保持不变）
            if (UserLevel > 0) UserLevel = 0;
            if (LevelManager.instance != null) LevelManager.IsAdmin = false;
            if (levelEditorButton) levelEditorButton.gameObject.SetActive(false);
        }

        PlayerPrefs.SetInt(PREF_IS_ADMIN, IsAdmin ? 1 : 0);
        PlayerPrefs.SetInt(PREF_USER_LEVEL, UserLevel);
        PlayerPrefs.Save();

        var p = FindObjectOfType<UserProfileManager>();
        if (p) p.UpdateUI();

        LoadLevelsSilent();
    }

    private void LoadLevelsSilent()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        JsGetLevels(gameObject.name, "OnGetLevelsSuccess", "OnAuthError");
#endif
    }

    public void OnGetLevelsSuccess(string jsonString)
    {
        try
        {
            AllLevels = JsonUtility.FromJson<LevelDataCollection>(jsonString);
            if (AllLevels == null) AllLevels = new LevelDataCollection();
        }
        catch (Exception e)
        {
            OnAuthError("Data Error: " + e.Message);
            return;
        }

        isLoggedIn = true;

        // 数据加载完毕，如果是当前在登录界面，则执行切换
        if (loginCanvasGroup != null && loginCanvasGroup.gameObject.activeSelf)
        {
            StartCoroutine(TransitionTo(loginCanvasGroup, mainMenuObjectGroup));
        }
    }

    public void OnAuthError(string error)
    {
        if (statusText != null) statusText.text = "Error: " + error;
        Debug.LogError("[TcbManager] Auth Error: " + error);
    }

    public void UploadNewLevel(string docId, LevelData data)
    {
        if (!IsAdmin) return;
#if UNITY_WEBGL && !UNITY_EDITOR
        string jsonData = JsonUtility.ToJson(data);
        JsUploadNewLevel(docId, jsonData, gameObject.name, "OnUploadSuccess", "OnAuthError");
#endif
    }

    public void OnUploadSuccess(string message)
    {
        var editor = FindObjectOfType<LevelEditorManager>();
        if (editor != null) editor.OnUploadSuccessCallback(message);
    }

    // =========================================================
    // 6. 通用数据库操作
    // =========================================================
    public void GetCollectionData<T>(string c, Action<List<T>> s, Action<string> e = null)
    {
        string rId = Guid.NewGuid().ToString();
        RegisterCallbacks(rId, (j) => { try { string w = "{\"data\":" + j + "}"; var r = JsonUtility.FromJson<DbResponseWrapper<T>>(w); s?.Invoke(r.data); } catch (Exception ex) { e?.Invoke(ex.Message); } }, e);
#if UNITY_WEBGL && !UNITY_EDITOR
        JsDbGetCollection(c, rId, gameObject.name, "OnDbGenericSuccess", "OnDbGenericError");
#endif
    }

    public void SetDocument<T>(string c, string d, T data, Action s = null, Action<string> e = null)
    {
        string rId = Guid.NewGuid().ToString(); TrySetId(data, d);
        RegisterCallbacks(rId, (m) => s?.Invoke(), e);
#if UNITY_WEBGL && !UNITY_EDITOR
        JsDbSetDocument(c, d, JsonUtility.ToJson(data), rId, gameObject.name, "OnDbGenericSuccess", "OnDbGenericError");
#endif
    }

    public void AddDocument<T>(string c, T data, Action s = null, Action<string> e = null)
    {
        string rId = Guid.NewGuid().ToString(); string d = TryGetId(data); if (string.IsNullOrEmpty(d)) d = Guid.NewGuid().ToString(); TrySetId(data, d);
        RegisterCallbacks(rId, (m) => s?.Invoke(), e);
#if UNITY_WEBGL && !UNITY_EDITOR
        JsDbAddDocument(c, JsonUtility.ToJson(data), rId, gameObject.name, "OnDbGenericSuccess", "OnDbGenericError");
#endif
    }

    public void DeleteDocument(string c, string d, Action s = null, Action<string> e = null)
    {
        string rId = Guid.NewGuid().ToString();
        RegisterCallbacks(rId, (m) => s?.Invoke(), e);
#if UNITY_WEBGL && !UNITY_EDITOR
        JsDbDeleteDocument(c, d, rId, gameObject.name, "OnDbGenericSuccess", "OnDbGenericError");
#endif
    }

    public void GetDocument<T>(string c, string d, Action<T> s, Action<string> e = null)
    {
        string rId = Guid.NewGuid().ToString();
        RegisterCallbacks(rId, (j) => { try { s?.Invoke(JsonUtility.FromJson<T>(j)); } catch { s?.Invoke(default(T)); } }, e);
#if UNITY_WEBGL && !UNITY_EDITOR
        JsDbGetDocument(c, d, rId, gameObject.name, "OnDbGenericSuccess", "OnDbGenericError");
#endif
    }

    private void TrySetId<T>(T data, string id) { try { var f = typeof(T).GetField("_id") ?? typeof(T).GetField("id"); if (f != null) f.SetValue(data, id); } catch { } }
    private string TryGetId<T>(T data) { try { var f = typeof(T).GetField("_id") ?? typeof(T).GetField("id"); if (f != null) return f.GetValue(data) as string; } catch { } return null; }

    public void OnDbGenericSuccess(string p)
    {
        int i = p.IndexOf('|'); if (i < 0) return;
        string id = p.Substring(0, i); string data = p.Substring(i + 1);
        if (dbSuccessCallbacks.ContainsKey(id)) { dbSuccessCallbacks[id]?.Invoke(data); CleanupCallbacks(id); }
    }

    public void OnDbGenericError(string p)
    {
        int i = p.IndexOf('|'); if (i < 0) return;
        string id = p.Substring(0, i); string err = p.Substring(i + 1);
        if (dbErrorCallbacks.ContainsKey(id)) { dbErrorCallbacks[id]?.Invoke(err); CleanupCallbacks(id); }
    }

    private void RegisterCallbacks(string r, Action<string> s, Action<string> e) { dbSuccessCallbacks[r] = s; dbErrorCallbacks[r] = e; }
    private void CleanupCallbacks(string r) { dbSuccessCallbacks.Remove(r); dbErrorCallbacks.Remove(r); }

    private IEnumerator TransitionTo(CanvasGroup from, CanvasGroup to)
    {
        yield return StartCoroutine(FadeCanvasGroup(from, 1, 0, panelFadeDuration));
        yield return StartCoroutine(FadeCanvasGroup(to, 0, 1, panelFadeDuration));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float dur)
    {
        if (cg == null) yield break;
        if (start > 0 || end > 0) cg.gameObject.SetActive(true);
        cg.interactable = false;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, end, t / dur);
            yield return null;
        }
        cg.alpha = end;
        bool isVisible = (end > 0.01f);
        cg.interactable = isVisible;
        cg.blocksRaycasts = isVisible;
        if (!isVisible) cg.gameObject.SetActive(false);
    }
}