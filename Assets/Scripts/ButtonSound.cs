using UnityEngine;
using UnityEngine.UI;

// 这行代码可以确保这个脚本必须附加在一个有Button组件的对象上
[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
    private Button button;

    void Start()
    {
        // 获取这个对象上的Button组件
        button = GetComponent<Button>();

        // 【核心】以代码的方式，为这个按钮的点击事件添加一个监听器
        // 当按钮被点击时，它会自动调用 UISoundManager 的播放方法
        button.onClick.AddListener(() => {
            if (UISoundManager.instance != null)
            {
                UISoundManager.instance.PlayButtonClickSound();
            }
        });
    }
}