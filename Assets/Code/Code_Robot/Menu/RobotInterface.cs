using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using TouchRobot.UI;
namespace TouchRobot
{
    public enum control_mode { current, position, speed }          // ****控制模式(旧版本为0,1,2;容易记混,干脆改成枚举类)
    public enum control_module { keyboard, robot_mode_0, robot_2d_mode_0 }
}

namespace TouchRobot.Logic
{
    // 所有Interface类都继承自MonoBehaviour,并在场景首次加载后初始化表单.如果场景切换,则需保留这些内存数据
    // 待做:任意机器人离线后的异常处理
    sealed class RobotInterface:MonoBehaviour // 继承MonoBehaviour
    {
        internal static KeyboardController rc_keyboard_wsad, rc_keyboard_arrow;               // 预置2个键盘控制方式
        public enum status_robot_connect { wait, ok, read_time_out, write_time_out, data_mismatch, normal_abort, port_exception, thread_exception }
        public static string time_to_string => System.DateTime.Now.ToString();               // 用于打印日志中的时间
        public static int count_online_robot => list_online_robot == null ? 0 : list_online_robot.Count;      // list的长度
        internal static List<RobotController> list_online_robot { get; private set; }         // 已连接的RobotController
        internal static List<RobotController> list_garbage_robot { get; private set; }        // 准备断开的RobotController
        internal static RobotController GetRobotControllerByPortName(string str, out bool has)
        {
            if (count_online_robot == 0)
            {
                has = false;
                return null;
            }
            foreach (RobotController rc in list_online_robot)
            {
                if (str.Equals(rc.port_name))
                {
                    has = true;
                    return rc;
                }
            }
            has = false;
            return null;

        }// 通过端口名称寻找控制机器人.出于对程序结构合理性的考量,该方法主要由UI控件通过其参数port_name调用
        internal static List<string> list_string_online_robot_port_name
        {
            get
            {
                if (list_online_robot == null)
                {
                    return null;
                }
                else
                {
                    List<string> str = new List<string>();
                    foreach (RobotController rc in list_online_robot)
                    {
                        str.Add(rc.port_name);
                    }
                    return str;
                }
            }
        }                   // UIManager通过此方法的端口名称检查已连接的设备
        internal static void NewRobotController(string str) {
            if (list_online_robot == null)
                list_online_robot = new List<RobotController>();
            foreach (RobotController _rc in list_online_robot) {
                if (_rc.port_name.Equals(str)) {
                    return;
                }
            }
            switch (RobotInterfaceUIManager.connecting_module)
            {
                case control_module.robot_2d_mode_0:
                    RobotController2D rc = new RobotController2D(str, control_module.robot_2d_mode_0);
                    list_online_robot.Add(rc);
                    rc.InterfaceCallInitRobot();
                    break;
                default:
                    RobotController rc2 = new RobotController(str, control_module.robot_mode_0);
                    list_online_robot.Add(rc2);
                    rc2.InterfaceCallInitRobot();
                    break;
            }
        }                              // 生成一个新的RobotController并将它加入list.在点击port关联的按钮后执行
        internal static void StartKeyboardController(bool wsad) {
            if (wsad)
                rc_keyboard_wsad.InterfaceCallInitRobot();
            else
                rc_keyboard_arrow.InterfaceCallInitRobot();
        }                          // 开始keyboard监听.这里偷了懒,因为在一开始就实例化了KeyboardController,而不是像上一个方法在按按钮后才实例化.
        internal static void RemoveController(RobotController rc)
        {
            list_online_robot.Remove(rc);
            list_garbage_robot.Add(rc);
        }                         // 非结束应用条件下移除RobotController,通过RobotInterface在FixedUpdate中实现,具体为关闭端口.        
        internal static void RemoveController(string str) {
            bool check = false;
            RobotController rc = GetRobotControllerByPortName(str, out check);
            if (!check)
                return;
            rc.InterfaceCallShutRobot(false);
        }                                // 非结束应用条件下手动移除RobotController,这样会调用OnPortNormalAbort().
        internal static void CloseKeyboardController(bool wsad) {
            if (wsad)
                rc_keyboard_wsad.InterfaceCallShutRobot();
            else
                rc_keyboard_arrow.InterfaceCallShutRobot();
        }                          // 终止keyboard监听.因为键盘控制不同于串口,且在代码设计初期并没有妥善考虑,虽然其继承自RobotController,为了赶进度所以仅用system_ready的T/F值作为开关依据
        internal static void ExceptionHandle(status_robot_connect word, RobotController rc) {
            rc.my_status = word;    // 如果手贱,在连接期间刷新了UI界面,需要对刷新的UI界面同步RC侦测状态(ok or wait?),需要传递该状态字
            RobotInterfaceUIManager.OperateOnConnect(word, rc.port_name);
        }// 为了代码结构合理,RobotController通过这个静态方法去访问UIManager,查询
        internal static void OnDisconnection() { 
            
        }   // 如果一个机器人断线了,需要对关联的gameObject负责,因为它会失去引用导致更多错误

        private void FixedUpdate()
        {
            //if (Input.GetKeyDown(KeyCode.Space)) {
            //    print(count_online_robot);
            //}
            if (list_garbage_robot.Count > 0)
            {
                try
                {
                    RobotController rc = list_garbage_robot[0];
                    list_garbage_robot.Remove(list_garbage_robot[0]);
                    rc.InterfaceCallShutRobot();
                    MessageManager.to_string(MessageManager.info_type.correct, "Remove desired robot controller successfully.");
                }
                catch (System.Exception ex)
                {
                    MessageManager.to_string(MessageManager.info_type.fail, "Removing desired robot controller fails, the description is \"" + ex + "\".");
                }
            }
        }               // 在固定时间间隔后关闭线程.如果通过线程内自动中断好像也行
        void Start()
        {
            //HapticWaveformer_DualPeakExponential hw = new HapticWaveformer_DualPeakExponential();
            //print(hw.Calculate(10086));
            if (list_garbage_robot == null)
            {
                list_garbage_robot = new List<RobotController>();
                list_online_robot = new List<RobotController>();
            }
            if (rc_keyboard_arrow == null)
            {
                rc_keyboard_wsad = new KeyboardController(true);
                rc_keyboard_arrow = new KeyboardController(false);
            }
            list_online_robot.Add(rc_keyboard_wsad);
            list_online_robot.Add(rc_keyboard_arrow);
            MessageManager.to_string(MessageManager.info_type.notice, "Software is opened (this message is recorded from \"RobotInterface.cs\").");
        }
        private void OnApplicationQuit()
        {
            RobotController[] array_robot = list_online_robot.ToArray();
            foreach (RobotController rc in array_robot) {
                try
                {
                    rc.InterfaceCallShutRobot(true);
                }
                catch (System.Exception) {
                    continue;
                }
            }
        }
    }
}