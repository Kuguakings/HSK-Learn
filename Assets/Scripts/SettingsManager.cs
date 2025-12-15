// SettingsManager.cs (【【【修复 WebGL 启动崩溃版】】】)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class SettingsManager : MonoBehaviour
{
    [Header("UI 控件引用")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    private Resolution[] supportedResolutions;
    private List<Resolution> filteredResolutions;

    void Start()
    {
        // 【【【【【【【【【【 BUG 修复 】】】】】】】】】】
        // WebGL 不支持 Screen.resolutions 或 Screen.SetResolution。
        // 必须禁用此代码块，否则会导致启动时“沉默崩溃”。
#if UNITY_WEBGL
        // 1. 在 WebGL 中，禁用下拉框并【立即停止】
        if (resolutionDropdown != null)
        {
            resolutionDropdown.gameObject.SetActive(false);
        }
        
        // (在 WebGL 中，全屏切换由浏览器处理，
        //  我们可以选择也禁用这个 Toggle)
        // if (fullscreenToggle != null)
        // {
        //    fullscreenToggle.gameObject.SetActive(false);
        // }
        
#else

        // 2. 【【【修复】】】: 只有在 非WebGL 平台才运行这段代码
        supportedResolutions = Screen.resolutions;

        // 过滤掉刷新率不同的重复分辨率，并确保分辨率至少为 1024x768
        filteredResolutions = new List<Resolution>();
        HashSet<string> resolutionStrings = new HashSet<string>();

        for (int i = supportedResolutions.Length - 1; i >= 0; i--)
        {
            Resolution res = supportedResolutions[i];
            if (res.width < 1024 || res.height < 768) continue;

            string resString = res.width + " x " + res.height;
            if (!resolutionStrings.Contains(resString))
            {
                filteredResolutions.Add(res);
                resolutionStrings.Add(resString);
            }
        }

        // 填充下拉菜单
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var res in filteredResolutions)
        {
            options.Add(res.width + " x " + res.height);
        }
        resolutionDropdown.AddOptions(options);

        // 加载并应用设置
        LoadAndApplySettings();

#endif
        // 【【【【【【【【【【 修复结束 】】】】】】】】】】
    }

    public void ApplySettings()
    {
        // 【【【【【【【【【【 BUG 修复 】】】】】】】】】】
        // 同样，在 WebGL 平台禁用此函数
#if !UNITY_WEBGL

        // 获取选择的分辨率
        Resolution selectedResolution = filteredResolutions[resolutionDropdown.value];
        bool isFullscreen = fullscreenToggle.isOn;

        // 应用设置
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, isFullscreen);
        Debug.Log($"应用分辨率: {selectedResolution.width}x{selectedResolution.height}, 全屏: {isFullscreen}");

        // 保存设置
        SaveSettings(resolutionDropdown.value, isFullscreen);

#endif
    }

    private void SaveSettings(int resolutionIndex, bool isFullscreen)
    {
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
        PlayerPrefs.SetInt("IsFullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("设置已保存！");
    }

    private void LoadAndApplySettings()
    {
        // 【【【【【【【【【【 BUG 修复 】】】】】】】】】】
        // 这段代码也必须在 WebGL 平台被禁用
#if !UNITY_WEBGL

        // 加载分辨率，如果不存在，则默认为最高分辨率
        int resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", filteredResolutions.Count - 1);

        // 确保索引在有效范围内
        if (resolutionIndex >= filteredResolutions.Count)
        {
            resolutionIndex = filteredResolutions.Count - 1;
        }

        // 加载全屏设置，如果不存在，则默认为是
        bool isFullscreen = PlayerPrefs.GetInt("IsFullscreen", 1) == 1;

        // 更新UI显示
        resolutionDropdown.value = resolutionIndex;
        resolutionDropdown.RefreshShownValue();
        fullscreenToggle.isOn = isFullscreen;

#endif
    }
}