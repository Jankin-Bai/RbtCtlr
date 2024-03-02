using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using UnityEngine.UI;
using TouchRobot.Logic; 
namespace TouchRobot.UI
{
    sealed class RobotInterfaceUIManager : MonoBehaviour
    {
        [SerializeField] GameObject transform_button_target;        // 将生成的按钮置于这个游戏对象下,便于transform的管理
        [SerializeField] Image image_ui_panel;                      // (暂时好像没啥用)以前的习惯是用固定屏幕锚点布置UI,但是这次使用的是强制像素大小的UI,所以不需要了
        [SerializeField] Button button_fresh;                       // (好像也没啥用)  就当告诉你有这么一个UI负责打印port_names
        [SerializeField] Button button_hide_com;                    // (好像还是没用)  就当告诉你有这么一个按钮负责折叠和展开
        [SerializeField] Button button_focus;                       // (完全没有用)    就当告诉你曾经计划用这个按钮实现聚焦(屏蔽没有连接的端口对应的UI)
        [SerializeField] Text text_data_reader;                     // 这个有用,将鼠标浮到图标上,在这个UI的位置显示文本
        [SerializeField] Dropdown dropdown_module;
        internal static control_module connecting_module = control_module.robot_2d_mode_0;
        private static string my_static_text_reader = "Read data by placing pointer over button.";
        // 文本内容是一个静态基本变量,可以在其他线程中修改
        private static Coroutine my_reader;                         // 协程的声明,避免文本刷新过快影响性能
        private IEnumerator ReadOnMouseOver()
        {
            while (true)
            {
                my_static_text_reader = "Read data by placing pointer over button.";
                yield return new WaitForSeconds(0.15f);
                text_data_reader.text = my_static_text_reader;
            }
        }                   // 协程的操作,每间隔固定时间刷新文本(目前是0.15sec)
        internal static void ReadText(string str = "")
        {
            if (str.Equals(""))
            {
                my_static_text_reader = "Read data by placing pointer over button.";
            }
            else
            {
                my_static_text_reader = str;
            }
        }             // 外部操作,在UISubElement中执行,使上述UI展示数据或者连接状态
        /// 开关(SubElement)颜色,队列,预制体
        #region
        internal static Color color_button_on;       // 点亮时开关的颜色
        internal static Color color_button_wait;     // 等待时开关的颜色
        internal static Color color_button_off;      // 关闭时开关的颜色
        public Color _color_button_on;               // 弄一堆静态变量方便调用
        public Color _color_button_wait;
        public Color _color_button_off;
        static List<RobotInterfaceUISubElement> list_ui_subelement;
        // UI控件的队列,仅用于打印后操作
        [SerializeField] GameObject ui_attached_object;
        // UI元素的父节点选择比较离奇,使用setParent(this.transform)的结果是将画布设为parent,故在此处选取一个GameObject,用它的transform作为父对象
        #endregion

        internal static RobotInterfaceUISubElement FindRobotInterfaceByPort(string port_name, out bool match)
        {
            match = false;
            foreach (RobotInterfaceUISubElement rise in list_ui_subelement)
            {
                if (rise.string_port_name.Equals(port_name))
                {
                    match = true;
                    return rise;
                }
            }
            return null;
        }// 与下面一段代码有关,其作用是根据串口名寻找按钮
        internal static void OperateOnConnect(RobotInterface.status_robot_connect word, string tg_port_name)
        {
            bool status = false;
            RobotInterfaceUISubElement rise = FindRobotInterfaceByPort(tg_port_name, out status);
            if (!status)
                return;
            rise.RobotControl(word);
        }   // 由RobotInterface调用,用以操作开关形态以及开关事件:是打开端口还是关闭?
        private static void OperateOnConnect(string tg_port_name, RobotInterfaceUISubElement rise)
        {
            bool status = false;
            RobotController rc = RobotInterface.GetRobotControllerByPortName(tg_port_name, out status);
            if (!status)
            {
                rise.RobotControl(RobotInterface.status_robot_connect.normal_abort);
                return;
            }
            rise.RobotControl(rc.my_status);
        }          // 如果有人手贱在连接时点了刷新,那么在重新生成UI后有必要检查连接进度,如该按钮对应的按钮是否处于wait状态
        static string[] array_port_name;                                                                        // 储存端口名称的数组,这关系到后续按钮生成
        private bool is_hidden;                                                                                 // true则UI已被隐藏


        /// UI操作和方法
        internal static void RemoveAllUI()
        {
            if (list_ui_subelement == null)
            {
                list_ui_subelement = new List<RobotInterfaceUISubElement>();
                return;
            }
            if (list_ui_subelement.Count == 0)
                return;
            RobotInterfaceUISubElement[] array_rise = list_ui_subelement.ToArray();
            list_ui_subelement = new List<RobotInterfaceUISubElement>();
            foreach (RobotInterfaceUISubElement rise in array_rise)
            {
                Destroy(rise.gameObject);
                Destroy(rise);
            }
        }                             // 清除屏幕上的全部UI以及他们在list中的记录.
        static void ListSerialPort()
        {
            array_port_name = SerialPort.GetPortNames();
        }                                    // 为array_port_name赋值,寻找所有的端口
        static void InitializeArray()
        {
            RemoveAllUI();
            ListSerialPort();
        }                                   // 初始化串口相关数组
        static void InitializeElement(Transform transform = null, GameObject goo = null)
        {
            bool empty = false;
            if (array_port_name.Length == 0)
            {
                array_port_name = new string[] { "e" };
                empty = true;
            }
            int index = 0;
            foreach (string str in array_port_name)
            {
                RobotInterfaceUISubElement rise = Instantiate(goo).GetComponent<RobotInterfaceUISubElement>();
                list_ui_subelement.Add(rise);
                rise.AlignButton(index++, str, transform);
                if (empty)
                    rise.ChangeIconWhenNoDevice();
                else
                    OperateOnConnect(str, rise);
                if (index >= max_horizontal_count)
                    break;
            }
        }
        public void SelectModule() 
        {
            switch (dropdown_module.value) 
            {
                case 0:
                    connecting_module = control_module.robot_2d_mode_0;
                    MessageManager.to_string(MessageManager.info_type.notice, "准备链接2D设备");
                    break;
                case 1:
                    connecting_module = control_module.robot_mode_0;
                    MessageManager.to_string(MessageManager.info_type.notice, "准备链接1D设备");
                    break;
                default:
                    MessageManager.to_string(MessageManager.info_type.notice, "沿用上一个配置的设备");
                    break;
            }
        }
        public void FreshUI()
        {
            InitializeArray();
            InitializeElement(transform_button_target.transform, ui_attached_object);
            MessageManager.to_string(MessageManager.info_type.correct, "Ports are interpreted from the device manager.");
        }                                           // 根据设备管理器显示的端口&已连接端口生成UI控件.该方法通过按按钮后实现
        public void FocusUI()
        {

        }                                          // 先咕了,试验阶段意义不大:主要功能是移除未连接/不能连接的图标
        public void HideUI()
        {
            if (!is_hidden)
            {
                is_hidden = true;
                GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 512);
            }
            else
            {
                is_hidden = false;
                GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            }
        }                                           // 隐藏UI,暂时放弃平滑过渡动画
        /// UI布局
        ///  ________________________________________________________ ... ___   512x512px 
        /// |             ^                                                  |
        /// |             vb                                                 |
        /// |         ____v____            _________                         |
        /// |        |         |          |         | ^                      |
        /// |<- hb ->|  COM-X  |<-  hi  ->|  COM-Y  | h  ...count-2          |
        /// |        |_________|          |_________| v                      |
        /// |         <-  w  ->               ^                              |
        /// |                                 hi                             |
        /// |                                 v                              |
        /// |        ===============================================         |
        /// |        |Data display area                                      |
        /// |        ===============================================         |
        /// |                                                                |
        /// |                                                                |
        /// |                                                 _____________  |
        /// |                                                 \ o o o o o /  |
        /// |                                                  \_o_o_o_o_/   |
        /// |________________________________________________________ ... ___|
        /// 
        internal static float horizontal_bound = 0.05f;      // hb
        internal static float vertical_bound = 0.05f;        // vb
        internal static float horizontal_interval = 0.015f;  // hi
        internal static float button_height = 0.025f;        // h;暂时令h=w
        internal static float vertical_interval = 0.015f;    // vi
        internal static int max_horizontal_count = 4;        // count
        internal static float button_width => (1 - 2 * horizontal_bound - horizontal_interval * (max_horizontal_count - 1)) / max_horizontal_count;  // w
        void Start()
        {
            my_reader = StartCoroutine(ReadOnMouseOver());
            // 为了静态变量,所以在这里赋值引用
            color_button_off = _color_button_off;
            color_button_on = _color_button_on;
            color_button_wait = _color_button_wait;
            FreshUI();
        }                                      // 主要功能:静态变量赋值,开启协程
        private void OnApplicationQuit()
        {
            StopAllCoroutines();
        }
    }
}