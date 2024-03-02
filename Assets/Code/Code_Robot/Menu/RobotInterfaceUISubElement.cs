using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TouchRobot.Logic;
namespace TouchRobot.UI
{
    internal class RobotInterfaceUISubElement : MonoBehaviour
    {
        [SerializeField] Sprite sprite_no_device; // 如果系统没检测到串口,那么更改为该图标
        internal Color default_color;             // 按钮默认颜色
        internal string string_port_name = "";    // 由RobotInterfaceUIManager赋值的串口名,随后将被赋值给Text
        private Text text_on_button;              // 游戏对象上附着的文本类型
        private string string_temp_text;          // UI文本只能在主线程操作,使用该中间变量暂时储存UI文本
        internal Button button_com;               // 该游戏对象的主体按钮,会在operation_wait时变成不可交互状态
        internal RobotController attached_robot;  // 本来想减少复杂度,不将对应的UI-端口在类中储存的,这种行为应该在Interface代码中实现.第一次写不太习惯,就有点乱,下次一定
        private bool find_robot;                  // 如果不知道attached_robot,那就去根据串口名寻找.但是找太多次浪费资源,所以用这个布尔值表示搜寻过一次了
        internal bool mouse_over;                 // 鼠标停留指示
        private delegate void my_delegate();      // ***一个无关紧要的委托,叫什么名字都行,但是不能有参数
        private my_delegate cross_thread_delegate;// 串口的错误出现在独立线程中,需要这段委托来令其实现于主线程中
        private void Empty()
        {
            if (mouse_over)
            {
                MouseReadData();
            }
        }                   // 看起来是空的,实际上会在主线程调用并检测鼠标是否悬浮
        private void InternalButtonColorOff()
        {
            button_com.interactable = true;
            button_com.image.color = RobotInterfaceUIManager.color_button_off;
            cross_thread_delegate -= InternalButtonColorOff;
        }  // RobotController的读写是在新线程中的,修改UI只能在主线程里,通过委托实现跨线程UI操作
        private void InternalButtonColorOn()
        {
            button_com.interactable = true;
            button_com.image.color = RobotInterfaceUIManager.color_button_on;
            cross_thread_delegate -= InternalButtonColorOn;
        }   // 同上
        private void InternalPersonalText(string str)
        {
            string_temp_text = str;
            cross_thread_delegate += PersonalText;
        }
        // 同上,暂时将按钮文本修改的目标值存入临时变量string_temp_text
        internal void AlignButton(int index = 0, string assigned_name = "", Transform parent = null)
        {
            if (!assigned_name.Equals(""))
            {
                string_port_name = assigned_name;
                string_temp_text = assigned_name;
                PersonalText();
            }
            button_com.transform.SetParent(parent);
            button_com.GetComponent<RectTransform>().anchorMin = new Vector2(RobotInterfaceUIManager.horizontal_bound + index * (RobotInterfaceUIManager.horizontal_interval + RobotInterfaceUIManager.button_width), 1 - RobotInterfaceUIManager.vertical_bound - RobotInterfaceUIManager.button_width);
            button_com.GetComponent<RectTransform>().anchorMax = new Vector2(RobotInterfaceUIManager.horizontal_bound + index * (RobotInterfaceUIManager.horizontal_interval + RobotInterfaceUIManager.button_width) + RobotInterfaceUIManager.button_width, 1 - RobotInterfaceUIManager.vertical_bound);
            button_com.GetComponent<RectTransform>().pivot = Vector2.one / 2;
            button_com.GetComponent<RectTransform>().anchoredPosition3D = Vector3.zero;
            button_com.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            button_com.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            button_com.GetComponent<RectTransform>().localScale = Vector2.one;
            SubStart();
        }
        // UIManager决定要生成哪些按钮后执行,根据序号排列,参数表见RobotInterfaceUIManager
        internal void RobotControl(RobotInterface.status_robot_connect status)
        {
            //Debug.Log(status);
            switch (status)
            {
                case RobotInterface.status_robot_connect.wait:
                    ButtonColorWait();
                    break;
                case RobotInterface.status_robot_connect.ok:
                    ButtonColorOn();
                    break;
                case RobotInterface.status_robot_connect.normal_abort:
                    ButtonColorDefault();
                    break;
                default:
                    ButtonColorOff(true, status.ToString());
                    break;
            }
            SubStart();
        }
        // 点击这个按钮,由RobotInterface新建一个RobotController.当RobotController特定事件发生时(比如断连),会从UIManager调用这个方法改变颜色和交互性
        private void PersonalText()
        {
            text_on_button.text = string_temp_text;
            cross_thread_delegate -= PersonalText;
        }            // 为了避免在线程里调用ui,在此处使用委托完成跨线程操作
        internal virtual void ButtonColorWait()
        {
            InternalPersonalText("Wait");
            button_com.interactable = false;
            button_com.image.color = RobotInterfaceUIManager.color_button_wait;
            SetButtonFunction(true);
        }       // 等待指令
        internal virtual void ButtonColorOn()
        {
            InternalPersonalText(string_port_name);
            cross_thread_delegate += InternalButtonColorOn;
            SetButtonFunction(false);
        }         // 打开指令
        internal void ButtonColorOff(bool user = false, string replaced_message = "")
        {
            InternalPersonalText(user ? replaced_message : string_port_name);
            cross_thread_delegate += InternalButtonColorOff;
            SetButtonFunction(true);
        }        // 关闭指令,同时输出log到ui
        internal virtual void ButtonColorOff()
        {
            ButtonColorOff(false);
        }        // 关闭指令,但是支持重载
        internal virtual void ButtonColorDefault()
        {
            InternalPersonalText(string_port_name);
            button_com.interactable = true;
            button_com.image.color = default_color;
            SetButtonFunction(true);
        }   // 正常关闭UI就显示默认颜色
        /// 按钮侦听方法
        /// >>  通过端口名称打开/关闭RobotController
        internal void SetButtonFunction(bool status)
        {
            button_com.onClick.RemoveAllListeners();
            if (status)
            {
                button_com.onClick.AddListener(OpenByPortName);
            }
            else
            {
                button_com.onClick.AddListener(ShutByPortName);
            }
        }     // 按钮可能变化为"连接","断开"等状态,通过该方法改变Listener行为
        internal virtual void OpenByPortName()
        {
            if (GameInterface.game_is_on)
            {
                MessageManager.to_string(MessageManager.info_type.unknown, "你是故意找茬是不是啊?说了游戏开始后不要再接设备了你还闹");
                return;
            }
            ButtonColorWait();
            RobotInterface.NewRobotController(string_port_name);
            SubStart();
        }           // 按钮侦听功能,实际行为为根据string_port_name打开串口
        internal virtual void ShutByPortName()
        {
            RobotInterface.RemoveController(string_port_name);
            SubStart();
        }           // 按钮侦听功能,实际行为为根据string_port_name关闭串口
        internal void ChangeIconWhenNoDevice()
        {
            button_com.image.sprite = sprite_no_device;
            InternalPersonalText("Empty");
        }          // 当系统中没有串口时,改变其Sprite让它与众不同
        public void MouseReadData(bool leave = false)
        {
            if (leave)
                RobotInterfaceUIManager.ReadText("");
            if (attached_robot == null)
            {
                if (find_robot)
                {
                    RobotInterfaceUIManager.ReadText("");
                }
                else
                {
                    attached_robot = RobotInterface.GetRobotControllerByPortName(string_port_name, out bool a);
                    find_robot = true;
                }
            }
            else
            {
                RobotInterfaceUIManager.ReadText(attached_robot.DataDisplay());
            }
        }   // 选择输出文本,并加入RobotInterfaceUIManager的临时字符串
        public void MouseEnter()
        {
            mouse_over = true;
        }                         // 由TriggerEvent触发,修改布尔值确定是否展示文本
        public void MouseLeave()
        {
            mouse_over = false;
        }                         // 同上
        void SubStart()
        {
            attached_robot = null;
            find_robot = false;
            if (text_on_button == null)
                text_on_button = GetComponentInChildren<Text>();
            if (button_com == null)
                button_com = GetComponent<Button>();
        }                                  // 初始化一些值,尤其是attached_robot,当RobotInterface操作断开或开启后,一些RobotController会被抹除,需要在因而无法得到数据
        private void Awake()
        {
            cross_thread_delegate = Empty;
            default_color = new Color(1, 1, 1, 1);
            SubStart();
        }
        private void Update()
        {
            cross_thread_delegate();
        }
    }
}