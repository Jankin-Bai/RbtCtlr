using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TouchRobot.Logic;
using UnityEngine.UI;
namespace TouchRobot.UI
{
    sealed class KeyboardUISubElement : RobotInterfaceUISubElement
    {
        [SerializeField] private bool is_wsad;
        internal override void ButtonColorWait()
        {
            SetButtonFunction(true);
            button_com.image.color = RobotInterfaceUIManager.color_button_wait;
        }       // 对父类的重写
        internal override void ButtonColorOn()
        {
            SetButtonFunction(false);
            button_com.image.color = RobotInterfaceUIManager.color_button_on;
        }
        internal override void ButtonColorOff()
        {
            SetButtonFunction(true);
            button_com.image.color = RobotInterfaceUIManager.color_button_off;
        }
        internal override void ButtonColorDefault()
        {
            SetButtonFunction(true);
            button_com.image.color = default_color;
        }
        internal override void OpenByPortName()
        {
            if (GameInterface.game_is_on)
            {
                MessageManager.to_string(MessageManager.info_type.unknown, "你是故意找茬是不是啊?说了游戏开始后不要再接设备了你还闹");
                return;
            }
            RobotInterface.StartKeyboardController(is_wsad);
            ButtonColorOn();
        }         // 为了减少bug量,两种键盘控制方式采用了不同于串口设备的方式,但是复用了对按钮颜色的修改
        internal override void ShutByPortName()
        {
            RobotInterface.CloseKeyboardController(is_wsad);
            ButtonColorDefault();
        }
        void Start()
        {
            default_color = new Color(1, 1, 1, 1);
            if (button_com == null)
                button_com = GetComponent<Button>();
            string_port_name = is_wsad? KeyboardController.name_forced_wsad : KeyboardController.name_forced_arrow;
            SetButtonFunction(true);
        }
        private void Update()
        {
            if (mouse_over)
            {
                MouseReadData();
            }
        }
    }
}