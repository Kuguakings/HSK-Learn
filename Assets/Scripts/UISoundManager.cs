using UnityEngine;

public class UISoundManager : MonoBehaviour
{
    // 这是一个场景内的单例，切换场景后会被销毁
    public static UISoundManager instance;

    private AudioSource audioSource;

    public AudioClip buttonClickSound;
    public AudioClip cardPlaceSound;
    public AudioClip slotAppearSound;
    public AudioClip wordAppearSound;
    public AudioClip match3TileAppearSound;

    private void Awake()
    {
        // 如果当前场景还没有实例，就把自己设为实例
        if (instance == null)
        {
            instance = this;
        }
        // 如果已经有实例了（极少见的情况），就把多余的自己销毁
        else if (instance != this)
        {
            Destroy(gameObject);
            return; // 提前返回，不执行后续代码
        }

        // 获取或添加 AudioSource 组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // 播放音效的方法保持不变
    public void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void PlayButtonClickSound() { PlaySound(buttonClickSound); }
    public void PlayCardPlaceSound() { PlaySound(cardPlaceSound); }
    public void PlaySlotAppearSound() { PlaySound(slotAppearSound); }
    public void PlayWordAppearSound() { PlaySound(wordAppearSound); }
    public void PlayMatch3TileAppearSound() { PlaySound(match3TileAppearSound); }
}