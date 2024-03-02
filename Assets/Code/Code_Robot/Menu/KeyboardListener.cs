using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TouchRobot.Logic
{
    sealed class KeyboardListener : MonoBehaviour
    {
        public static float keyboard_speed_coefficient = 0.98f;
        public static float keyboard_speed_noise = 2;
        public static float keyboard_position_noise = 5;
        KeyboardListener keyboard_listener;
        internal static int frame_count;        // 通过数帧数并测量时间间隔,估算响应频率
        internal static long last_tick;         // 用于上一条的上次测量时间
        internal static Vector2 input_wsad;     // 通过WSAD的输入量
        internal static Vector2 input_arrow;    // 通过小键盘的输入量
        internal static bool input_space;       // 空格按钮值
        internal static bool input_enter;       // 回车按钮值
        private void Start()
        {
            if (keyboard_listener == null)
            {
                keyboard_listener = this;
            }
            else 
            {
                Destroy(gameObject);
                Destroy(this);
                return;
            }
            KeyboardController.public_parameter_arrow = new RobotParameters(control_module.keyboard);
            KeyboardController.public_parameter_wsad = new RobotParameters(control_module.keyboard);
            last_tick = UI.DataRecorder.current_time;
        }                 // 初始化静态RobotParameters,便于代码实现,控制游戏控制器
        private void FixedUpdate()
        {
            if (Input.GetKey(KeyCode.W))
            {
                input_wsad += new Vector2(0, KeyboardController.axial_gain.y);
            }
            if (Input.GetKey(KeyCode.S))
            {
                input_wsad -= new Vector2(0, KeyboardController.axial_gain.y);
            }
            if (Input.GetKey(KeyCode.D))
            {
                input_wsad += new Vector2(KeyboardController.axial_gain.x, 0);
            }
            if (Input.GetKey(KeyCode.A))
            {
                input_wsad -= new Vector2(KeyboardController.axial_gain.x, 0);
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                input_arrow += new Vector2(0, KeyboardController.axial_gain.y);
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                input_arrow -= new Vector2(0, KeyboardController.axial_gain.y);
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                input_arrow += new Vector2(KeyboardController.axial_gain.x, 0);
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                input_arrow -= new Vector2(KeyboardController.axial_gain.x, 0);
            }
            if (Input.GetKey(KeyCode.Space))
            {
                input_space = true;
            }
            else {
                input_space = false;
            }
            if (Input.GetKey(KeyCode.KeypadEnter) || Input.GetKey(KeyCode.Return))
            {
                input_enter = true;
            }
            else {
                input_enter = false;
            }
            if (++frame_count%1000 == 0) 
            {
                long tmp = UI.DataRecorder.current_time;
                KeyboardController.frame_record = (short)(10000000000 / (tmp - last_tick));
                last_tick = tmp;
                frame_count = 0;
            }
            KeyboardController.data_conversion_task();
        }           // 在每帧中调用,改变两种输入的量
    }
    sealed class KeyboardController : RobotController
    {
        /// 
        /// 实验性代码>> 
        /// 这里想用继承,KeyboardController和RobotController共用与GameController相关的代码
        /// 可以在设备不够的时候快速部署键盘侠参与调试
        /// 但是键盘输入侦听只能在主线程里?所以用一个静态变量的访问
        ///
        internal static short frame_record;                     // 数据传输速率同帧率
        internal static RobotParameters public_parameter_wsad;  // 如果启用了,那么就从这里取值
        internal static RobotParameters public_parameter_arrow;
        internal static Vector2 axial_gain = new Vector2(1f, 1f);        // 每侦测到一次键盘输入的增益
        internal const string name_forced_wsad = "keyboard_wsad";        // 总得有个名字,不然遍历的时候让编译器摸不着头脑
        internal const string name_forced_arrow = "keyboard_arrow";      // 常量,两个固定的名字
        private new bool system_ready;                          // 用了新名字,用于临时存储.但是外部函数是以RobotController类去访问变量,故必须重载system_ready的lambda函数"Ready()",使其赋值满足键盘使用场景
        private bool is_wsad;                                   // 定义这是键盘的哪种控制方式
        internal static my_delegate data_conversion_task;       // 这是一个静态委托,它被放在KeyboardListener的FixedUpdate里执行积分和单位换算,参数传递的功能
        internal override bool Ready()
        {
            return system_ready;
        }                       // 功能上相当于重写lambda函数
        internal KeyboardController(bool wsad)
        {
            my_module = control_module.keyboard;
            is_wsad = wsad;
            temp_port_name = wsad ? name_forced_wsad : name_forced_arrow;
            my_parameter = wsad ? public_parameter_wsad : public_parameter_arrow;
            system_ready = false;
            // 由于两个键盘是在Start中实例化的,所以这里对委托赋值没有危险
            data_conversion_task += InternalEmpty;
        }               // 构造器,通过布尔型为其name赋值.因为没有端口,故访问时返回temp_port_name,即wsad或arrow
        protected override void InternalEmpty()
        {
            rate_read = frame_record;
            rate_write = frame_record;
        }               // 响应速度由KeyboardListener在FixedUpdate里得到,并赋值给KeyboardController.public_parameter_xxx,在这里赋值给父类对应地址
        private void InternalWsadReady() {
            if (!system_ready)
                return;
            public_parameter_wsad.SpeedIncKeyboard(KeyboardListener.input_wsad, KeyboardListener.input_space, my_game_controller);
            KeyboardListener.input_wsad *= KeyboardListener.keyboard_speed_coefficient;
        }                    // 如果开启机器人(按下UI按钮),则开始侦听WSAD输入
        private void InternalArrowReady() {
            if (!system_ready)
                return;
            public_parameter_arrow.SpeedIncKeyboard(KeyboardListener.input_arrow, KeyboardListener.input_enter, my_game_controller);
            KeyboardListener.input_arrow *= KeyboardListener.keyboard_speed_coefficient;
        }                   // 如果开启机器人,则开始侦听方向键的输入
        internal override void InterfaceCallInitRobot()
        {
            system_ready = true;
            my_game_controller = GameInterface.GenerateGameController(this);
            if (is_wsad)
                data_conversion_task += InternalWsadReady;
            else
                data_conversion_task += InternalArrowReady;
        }      // windows的键盘输入已经被封装好了,没必要指定什么端口号了.复用initRobot的方法名.
        internal override void InterfaceCallShutRobot() {
            system_ready = false;
            MyRemoveGameController();
            if (is_wsad)
                data_conversion_task -= InternalWsadReady;
            else
                data_conversion_task -= InternalArrowReady;
        }     // 复用shutRobot的方法名,实际只操作system_ready这个变量 
        internal override void InterfaceCallGameStart()
        {
            data_conversion_task += InternalGameDataConversion;
        }      // 启动游戏之后才会发布委托进行单位换算
        internal override void InterfaceCallGameStop()
        {
            data_conversion_task -= InternalGameDataConversion;
        }       // 游戏结束后删除委托,节省资源
    }
}