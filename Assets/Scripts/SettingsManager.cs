// SettingsManager.cs (�������޸� WebGL ���������桿����)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class SettingsManager : MonoBehaviour
{
    [Header("UI �ؼ�����")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    private Resolution[] supportedResolutions;
    private List<Resolution> filteredResolutions;

    void Start()
    {
        // �������������������� BUG �޸� ��������������������
        // WebGL ��֧�� Screen.resolutions �� Screen.SetResolution��
        // ������ô˴���飬����ᵼ������ʱ����Ĭ��������
#if UNITY_WEBGL
        // 1. �� WebGL �У����������򲢡�����ֹͣ��
        if (resolutionDropdown != null)
        {
            resolutionDropdown.gameObject.SetActive(false);
        }
        
        // (�� WebGL �У�ȫ���л��������������
        //  ���ǿ���ѡ��Ҳ������� Toggle)
        // if (fullscreenToggle != null)
        // {
        //    fullscreenToggle.gameObject.SetActive(false);
        // }
        
#else

        // 2. �������޸�������: ֻ���� ��WebGL ƽ̨��������δ���
        supportedResolutions = Screen.resolutions;

        // ���˵�ˢ���ʲ�ͬ���ظ��ֱ��ʣ���ȷ���ֱ�������Ϊ 1024x768
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

        // ��������˵�
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var res in filteredResolutions)
        {
            options.Add(res.width + " x " + res.height);
        }
        resolutionDropdown.AddOptions(options);

        // ���ز�Ӧ������
        LoadAndApplySettings();

#endif
        // �������������������� �޸����� ��������������������
    }

    public void ApplySettings()
    {
        // �������������������� BUG �޸� ��������������������
        // ͬ������ WebGL ƽ̨���ô˺���
#if !UNITY_WEBGL

        Resolution selectedResolution = filteredResolutions[resolutionDropdown.value];
        bool isFullscreen = fullscreenToggle.isOn;

        Screen.SetResolution(selectedResolution.width, selectedResolution.height, isFullscreen);
        Debug.Log($"Applied resolution: {selectedResolution.width}x{selectedResolution.height}, fullscreen: {isFullscreen}");

        SaveSettings(resolutionDropdown.value, isFullscreen);

#endif
    }

    private void SaveSettings(int resolutionIndex, bool isFullscreen)
    {
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
        PlayerPrefs.SetInt("IsFullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("Settings saved.");
    }

    private void LoadAndApplySettings()
    {
        // �������������������� BUG �޸� ��������������������
        // ��δ���Ҳ������ WebGL ƽ̨������
#if !UNITY_WEBGL

        // ���طֱ��ʣ���������ڣ���Ĭ��Ϊ��߷ֱ���
        int resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", filteredResolutions.Count - 1);

        // ȷ����������Ч��Χ��
        if (resolutionIndex >= filteredResolutions.Count)
        {
            resolutionIndex = filteredResolutions.Count - 1;
        }

        // ����ȫ�����ã���������ڣ���Ĭ��Ϊ��
        bool isFullscreen = PlayerPrefs.GetInt("IsFullscreen", 1) == 1;

        // ����UI��ʾ
        resolutionDropdown.value = resolutionIndex;
        resolutionDropdown.RefreshShownValue();
        fullscreenToggle.isOn = isFullscreen;

#endif
    }
}