using UnityEngine;
using System;
using System.Threading;
using System.IO.Ports;
using TouchRobot.UI;
using JetBrains.Annotations;
using Codice.Utils;

namespace TouchRobot.Logic
{
    class RobotController // 不是静态方法,不可以调用UnityEngine,需要RobotInterface协助实现带UI的初始化
    {
        /*
         * 1. 由用户选取RobotInterface(及相关组件)打印的串口号后,实例化一个RobotController,实例化对象加入(List<RobotController>) RobotInterface.online_robot
         * 2. RobotController以串口名称为参数,开启串口->打开接收线程.串口打开时port_ready置为true,当线程接收到一个报文时,robot_ready置为true,随后system_ready置为true(system_ready => port_ready && robot_ready)
         * 3. 由用户通过GameInerface启动游戏,该类将遍历RobotInterface.online_robot,若全部处于system_ready状态下,则配对每一个RobotController与GameController 
         * ****代表外部执行的方法
         */
        /// 
        /// <实验性> 命名规则:变量全部是"l_l"格式,方法名则是"CaseCase"驼峰规则
        /// 通过外部调用的方法命名为"InterfaceXxxxXxxx",对委托的操作命名为"InternalXxxxXxxx"
        /// <未解决> 命名中Start和init同时存在,但是在程序设计时,二者功能并没有区别.同样的不规范出现在Close和Shut的使用中,但是不关乎固定搭配
        /// 

        // 如果换了个别硬件我们应该如何处理呢?虽然可以仿照键盘的模式,令新的硬件使用新的class,并且继承这个类.但是如果通讯方式与控制芯片没有变化的情况下,只需要对个别硬件修改参数即可(因为传递的数据为整数型,与准确值仅为整数倍区别)
        // 因为截至2023/4/20时,设备间区别仅为导轨长度,传感器选型与变送器,故而直接使用父类的RobotController   
        // 目前的数据流: ESP---COM--->RobotController->my_parameters---换算至国际单位--->GameController---换算至游戏单位--->GameCharacterAttribute
        // 目前的数据流: ESP<---COM---RobotController<-my_parameters<---换算至电机单位---GameController<---换算至游戏单位---GameCharacterAttribute
        protected internal control_module my_module;
        protected internal RobotInterface.status_robot_connect my_status;        // RobotController状态,会改变刷新后的按钮的形态和颜色
        internal RobotController() { }                                 // ****构造器,同时实例化机器人参数
        internal RobotController(string str, control_module this_module = control_module.robot_mode_0)
        {
            baud_rate = 921600;
            my_module = this_module;
            my_status = RobotInterface.status_robot_connect.wait;
            my_parameter = new RobotParameters(DataConverter.NewConverter(my_module));
            MessageManager.to_string(MessageManager.info_type.notice, "Attempting to set up port with name \"" + str + "\"...");
            interface_command = InternalEmpty;
            temp_port_name = str;
        }                       // ****构造器,同时实例化机器人参数,并通过名称配置串口.第二个参数是型号,影响换算单位
        internal virtual void InterfaceCallInitRobot()
        {
            if (InitUSB(temp_port_name))
            {
                try
                {
                    thread_on = true;
                    thread_read.Start();
                    thread_write.Start();
                    MessageManager.to_string(MessageManager.info_type.notice, "\"" + port_name + "\" starts the receiving thread.");
                }
                catch (Exception ex)
                {
                    OnPortAbnormalDisconnect(ex, RobotInterface.status_robot_connect.thread_exception);
                }
            }
            else
            {
                OnPortAbnormalDisconnect(new Exception("port input illegal"), RobotInterface.status_robot_connect.port_exception);
            }
        }              // ***命名还是不太规范// 这里是UIManager通过RobotInterface试图开启机器人连接.执行前先将this加入RobotInterface.list_online_robot,而后检查this.my_port能否开启,能则开启线程和接受
        protected internal GameController my_game_controller;                    // 点击游戏开始后会被赋值(需要施工GameController和GameInterface)
        protected RobotParameters my_parameter;                         // 机器人参数(为什么不用结构体?我还没想明白)
        internal string temp_port_name;                                // 端口名称可能是失效的,故用此临时变量
        internal string port_name => my_port == null ? temp_port_name : my_port.PortName;// 用于打印日志中绑定的串口
        internal bool system_ready => Ready();                          // ****系统可用
        internal virtual bool Ready()
        {
            return port_ready && robot_ready;
        }                                // 知识有限,暂时通过这个方式实现system_ready在衍生类的重写
        private bool port_ready => my_port == null ? false : my_port.IsOpen;    // 串口连通.键盘类不需要
        private bool robot_ready;                                       // 当收到数据且数据报文被发送出去后,置此值为true;正常/意外断链后置false
        protected void SetReady(bool condition) { robot_ready = condition; }
        private byte[] received_data = new byte[64];                    // 接收的数据，必须为字节
        protected SerialPort my_port;                                     // 用于通讯的串口,外部可通过方法/构造器修改
        protected Thread thread_read;                                     // 串口读写的线程
        protected Thread thread_write;                                    // 串口读写的线程
        protected bool thread_on;                                         // <实验性> 由于Editor里经常卡死,怀疑是线程中while(true)不能正常关闭?对C#的Thread理解有待提高
        private protected long tick_write;     // 用于写入线程的计时
        private long tick_read;      // 用于读取线程的计时
        protected short rate_write;   // 计算速率
        protected short rate_read;    // 读速率
        private int read_count;      // 次数统计
        private protected int write_count;

        protected internal delegate void my_delegate();                            // 参数为空的委托
        my_delegate interface_command;                                   // 这是一个委托,链接开始时它什么也不做;游戏开始时,它负责单位换算;应用退出时,它负责中断线程和关闭串口
        protected my_delegate async_write;                                         // 这是一个委托,读到数据后会让串口再另一个线程执行数据回写
        internal virtual void InterfaceCallGameStart()
        {
            //interface_command = 
        }               // ****<通过Interface唤醒进行单位换算,该过程通过该类委托实现>
        internal virtual void InterfaceCallGameStop()
        {
            interface_command = InternalEmpty;
            my_parameter = new RobotParameters(my_module);
        }                 // ****<通过Interface关闭单位换算,该过程通过该类委托实现>
        internal virtual void InterfaceCallShutRobot(bool direct)
        {
            if (direct)
                CloseThread();
            else
                OnPortNormalDisconnect();
        }    // ****<通过Interface唤醒关闭线程和串口,若true通过外部关闭线程,否则是用户主动的关闭线程.仅影响log> 实际执行的是OnRobotNormalDisconnect()或者CloseThread()
        internal virtual void InterfaceCallShutRobot()
        {
            InterfaceCallShutRobot(true);
        }               // 无参数的关闭机器人,主要是为了键盘继承方法名才写的
        private protected void InternalCallWrite()
        {
            async_write += InternalWrite;
        }                             // <通过委托在Write线程中用串口写入,实现串口接收一个立即返回.利用双线程避免单通道堵塞>
        private protected virtual void InternalWrite()
        {
            InternalGameDataConversion();
            try
            {
                // RS485写回数据至ESP
                my_parameter.GameFeedback(my_game_controller);
                my_parameter.PackControlMessage();
                my_port.Write(my_parameter.udp_send_msg, 0, 7);
                robot_ready = true;
                if (++write_count % 1000 == 0)
                {
                    long tp = DataRecorder.current_time;
                    rate_write = (short)(10000000000 / (tp - tick_write));
                    tick_write = DataRecorder.current_time;
                    write_count = 0;
                }
            }
            catch (System.IO.IOException te)
            {
                OnPortAbnormalDisconnect(te, RobotInterface.status_robot_connect.write_time_out);
            }
            async_write -= InternalWrite;
        }
        protected virtual void InternalEmpty() { }                        // <实现什么都不做.因为委托为空的时候会报错,故留这么一个空>
        protected void InternalGameDataConversion()
        {
            my_game_controller.GameDataTransmission();
        }                    // <实现单位换算:gamecontroller->gamedata.在每次getRobotData后委托中执行>-><2023/05/06: 单位换算的实际作用已经名不副实,其实际作用是读取游戏对象的目标量.由于这一功能变化,仅在每次写入前调用.>
        // 更新于2023/05/17,实现双向单位换算,1-Robot生成低维数据(单位inc)->GameC高维数据(标准单位)->游戏特征类(游戏单位);2-游戏特征类高维数据(游戏单位)->GameC高维数据(标准单位)->Robot低维数据(单位inc)
        // 标准的robotcontroller里,这个方法在getRobotData之后调用;键盘侠在每一个固定更新帧后执行
        protected void MyRemoveGameController() {
            GameInterface.RemoveGameController(my_game_controller);
        }                       // 当断开连接时清楚GameController并且更新GameInterface的表单
        protected void OnPortNormalDisconnect()
        {
            MyRemoveGameController();
            RobotInterface.RemoveController(this);
            RobotInterface.ExceptionHandle(RobotInterface.status_robot_connect.normal_abort, this);
            MessageManager.to_string(MessageManager.info_type.correct, "\"" + port_name + "\" is closed.");
        }                         // <实现主动关掉了串口,机器人也将断链>
        protected void OnPortAbnormalDisconnect(Exception ex = null, RobotInterface.status_robot_connect status = RobotInterface.status_robot_connect.wait)
        {
            MyRemoveGameController();
            RobotInterface.ExceptionHandle(status, this);
            RobotInterface.RemoveController(this);
            MessageManager.to_string(MessageManager.info_type.attention, "\"" + port_name + "\" is closed abnormally with following info: " + ex);
        }
        // 意外关掉了串口,机器人也将断链,同时输出错误信息ex
        private void CloseThread()
        {
            thread_on = false;
            robot_ready = false;
            if (thread_read != null)
            {
                thread_read.Interrupt();
                thread_read.Abort();
            }
            if (thread_write != null)
            {
                thread_write.Interrupt();
                thread_write.Abort();
            }
            if (port_ready)
            {
                my_parameter = new RobotParameters(my_module);
                my_parameter.PackControlMessage();
                try
                {
                    my_port.Write(my_parameter.udp_send_msg, 0, 6);
                    my_port.Close();
                }
                catch (Exception) { }
            }
            thread_on = false;
        }                                    // 关闭线程和串口的操作
        protected int baud_rate;
        private bool InitUSB(string str)
        {
            if (GameInterface.game_is_on)
            {
                MessageManager.to_string(MessageManager.info_type.unknown, "你是故意找茬是不是啊?说了不要在游戏进行的时候连接你非要连接,麻了");
                return false;
            }
            bool cond = true;
            try
            {
                my_parameter = new RobotParameters(my_module);
                my_port = new SerialPort();
                my_port.PortName = str;
                my_port.ReadTimeout = 100;
                my_port.WriteTimeout = 100;
                my_port.ReadBufferSize = 2;
                my_port.WriteBufferSize = 2;
                my_port.BaudRate = baud_rate;
                my_port.Parity = Parity.None;
                my_port.StopBits = StopBits.One;
                my_port.DataBits = 8;
                int time_out = 0;
                while (!port_ready && ++time_out < 1000)
                {
                    my_port.Open();
                }
                if (port_ready)
                {   //Touch
                    thread_read = new Thread(PortRead);
                    thread_write = new Thread(PortWrite);
                    //Force
                    //thread_read = new Thread(PortFRead);
                }
                else
                {
                    cond = false;
                }
            }
            catch (Exception)
            {
                cond = false;
            }
            return cond;
        }                              // ****参数:串口名,创建串口对象并开启收发线程.若成功则返回true.由RobotInterface调用this.InterfaceCallInitRobot().如果游戏正在进行,那么一定不能连接.
        private void StartInitCommand()
        {
            my_parameter.udp_send_msg[1] = (byte)(0b11101010);
        }                               // 将udp_msg[1]置为11101010,使其包含初始化指令.(以前需要按了才能初始化,现在觉得多此一举:现在如果收到了正确的数据则发送该指令)
        virtual protected void PortRead()
        {
            Debug.Log("PortRead: init start ...");
            RobotInterface.ExceptionHandle(RobotInterface.status_robot_connect.wait, this);
            // 当串口不匹配时(数据乱码/接受超时)时,将增加如下变量,超过阈值时将断开连接
            int matching_count = 0;
            // 临时数组用来搬运有用的数组
            byte[] new_byte = new byte[0];
            // 避免数据丢失,使用buffer来合成接收到的数据包
            byte[] synthetic_byte = new byte[0];
            // 旧版本中上传帧存在帧尾,此时desired_length=13,禁用帧尾后置值为12
            // check_index在帧长度为12时无用,它指代帧尾的index
            // ESP代码里第13位帧尾为0xCC
            int desired_length = 13;
            int check_index = 12;
            int data_length = check_index;
            // 避免重复声明,在此处创建这个临时变量
            byte[] tmp = new byte[0];
            // 尝试连接时,若数据帧错误则match为false
            bool match = false;
            bool remove_ui_handle = false;
            tick_read = DataRecorder.current_time;
            int len = 0;
            Debug.Log("PortRead: init ok!");
            while (thread_on)
            {
                // 什么都不做,单位换算,或者远程中断.因为中断的优先级高,故放在while首部
                interface_command();
                // 缓冲区数据长度需要不小于desired length
                try
                {
                    len = my_port.BytesToRead;
                    if (len >= desired_length)
                        len = desired_length;
                    else
                    {
                        if (matching_count++ > 1280)
                        {
                            if (!remove_ui_handle)
                            {
                                RobotInterface.ExceptionHandle(RobotInterface.status_robot_connect.read_time_out, this);
                            }
                            OnPortAbnormalDisconnect(new ApplicationException("Receive thread time out"), RobotInterface.status_robot_connect.read_time_out);
                            break;
                        }
                        Thread.Sleep(1);
                        continue;
                    }
                    received_data = new byte[len];
                }
                catch (System.IO.IOException ex)
                {
                    Debug.LogError("PortRead: " + ex);
                    OnPortAbnormalDisconnect(ex, RobotInterface.status_robot_connect.port_exception);
                    break;
                }
                my_port.Read(received_data, 0, len);
                try
                {
                    Debug.Log("try ??");
                }
                catch (Exception ex)
                {
                    Debug.Log("必然进来：" + ex);
                    OnPortAbnormalDisconnect(ex, RobotInterface.status_robot_connect.port_exception);
                    break;
                }
                tmp = (byte[])synthetic_byte.Clone();
                // 延长数组长度并复制数据
                synthetic_byte = new byte[tmp.Length + len];
                tmp.CopyTo(synthetic_byte, 0);
                received_data.CopyTo(synthetic_byte, tmp.Length);
                // 确保数据长度正确
                if (synthetic_byte.Length >= desired_length)
                {
                    // 寻找帧头帧尾
                    for (int i = 0; i < synthetic_byte.Length - desired_length; i++)
                    {
                        if (synthetic_byte[i] == 0xef)
                        {
                            if (synthetic_byte[i + check_index] != 0xcc) 
                            {
                                // 检查帧尾
                                continue;
                            }
                            if ((synthetic_byte[i + 1] & 0b10101010) == 0b10101010)
                            {
                                if (!remove_ui_handle)
                                {
                                    RobotInterface.ExceptionHandle(RobotInterface.status_robot_connect.ok, this);
                                    my_game_controller = GameInterface.GenerateGameController(this);
                                    remove_ui_handle = true;
                                    StartInitCommand();
                                }
                                match = true;
                                matching_count = 0;
                                new_byte = new byte[data_length];
                                // 截取单帧数据用于getData
                                for (int j = 0; j < data_length; j++)
                                {
                                    new_byte[j] = synthetic_byte[j + i];
                                }
                                Debug.Log("PortRead task->GetRobotData ->");
                                my_parameter.GetRobotData(new_byte);
                                my_parameter.GameFeedforward(my_game_controller);
                                // 调用串口写入方法
                                InternalCallWrite();
                                byte[] tmp1 = (byte[])synthetic_byte.Clone();
                                int tg = i + desired_length;
                                // 将未纳入上述步骤的数据添加回缓冲区
                                synthetic_byte = new byte[tmp1.Length - tg];
                                for (int k = 0; k < synthetic_byte.Length; k++)
                                {
                                    synthetic_byte[k] = tmp1[tg + k];
                                }
                                if (++read_count % 1000 == 0)
                                {
                                    long tp = DataRecorder.current_time;
                                    rate_read = (short)(10000000000 / (tp - tick_read));
                                    tick_read = DataRecorder.current_time;
                                    read_count = 0;
                                }
                                break;
                            }
                        }
                    }
                }
                // 避免缓冲区溢出
                if (synthetic_byte.Length > 64)
                {
                    match = false;
                    synthetic_byte = new byte[0];
                }
                // 如果不匹配次数过多则中断
                if (!remove_ui_handle)
                {
                    if (!match)
                    {
                        if (matching_count++ > 8192)
                        {

                            string tp = "";
                            foreach (byte bt in synthetic_byte)
                            {
                                tp += bt.ToString("x2") + " ";
                            }
                            Debug.Log(tp);
                            OnPortAbnormalDisconnect(new ApplicationException("Data mismatch"), RobotInterface.status_robot_connect.data_mismatch);
                            break;
                        }
                        Thread.Sleep(0);
                    }
                }
                // 重置匹配检测
                match = false;
            }
        }                                       // 端口连接后多线程操作
        protected virtual void PortWrite()
        {
            // 初始化委托
            async_write = InternalEmpty;
            tick_write = DataRecorder.current_time;
            while (thread_on)
            {
                // 当Read完成时,执行发送
                async_write();
                Thread.SpinWait(10);
            }
        }                                     // 端口连接后,在读取输入缓冲区后通过委托调用串口写入
        internal virtual string DataDisplay()
        {
            if (system_ready)
                return port_name + ": " + (my_parameter.handle_sw ? "■" : "▢") + "\n" + "Read@" + rate_read + "; Write@" + rate_write + "\nPosition@" + my_parameter.position_inc + "\nVelocity@" + my_parameter.speed_inc + "\nForce@" + my_parameter.inter_force.ToString("f1");
            else
                return "Port has not been connected.";
        }                                // 打印关键文本
        internal string LogRobotStatus(int index)
        {
            return index.ToString() + " is trying to record file...";
        }                                 // 以RobotController为参数,实现传递参数的多线程运行.这样可以为多个设备开展实时数据采集
    }

    internal class RobotParameters
    {
        const int max_speed_inc = 512000;
        const int max_position_inc = 51200;
        protected internal DataConverter.converting_ratio my_ratio;    // 以此来决定换算关系,或设备类型
        protected private standardlized_data ModifyDataToRobot(standardlized_data dsd)
        {
            return my_ratio.ModifyDataToRobot(dsd);
        }
        protected private downsampled_data ModifyDataToRobot(downsampled_data dsd) {
            return my_ratio.ModifyDataToRobot(dsd);
        }
        private standardlized_data ModifyDataToGame(downsampled_data dsd) {
            return my_ratio.ModifyDataToGame(dsd);
        }
        internal RobotParameters() { }
        internal RobotParameters(DataConverter.converting_ratio ratio)
        {
            my_ratio = ratio;
            udp_send_msg = new byte[] { 0xCE, 0b11101010, 0, 0, 0, 0 };
            handle_sw_trigger = false;
            desired_data.control_mode = control_mode.current;
        }                      // 复写默认构造器
        internal RobotParameters(control_module module)
        {
            my_ratio = DataConverter.NewConverter(module);
            udp_send_msg = new byte[] { 0xCE, 0b11101010, 0, 0, 0, 0 };
            handle_sw_trigger = false;
            desired_data.control_mode = control_mode.current;
        }
        private float speed_keyboard;
        private float position_keyboard;
        internal void SpeedIncKeyboard(Vector2 val, bool button, GameController gc)
        {
            speed_keyboard = val.x/* + UnityEngine.Random.Range(-KeyboardListener.keyboard_speed_noise, KeyboardListener.keyboard_speed_noise)*/;
            speed_inc = (int)speed_keyboard;
            position_keyboard += speed_inc;
            position_inc = (int)(position_keyboard)/* + UnityEngine.Random.Range(-KeyboardListener.keyboard_position_noise, KeyboardListener.keyboard_position_noise))*/;
            handle_sw = button;
            GameFeedforward(gc);
            GameFeedback(gc);
        } // 通过键盘输入改变inc
        virtual internal void GameFeedback(GameController gc = null) {
            if (gc == null)
            {
                desired_data.control_mode = control_mode.current;
                desired_current = 0;
                return;
            }
            else 
            {
                desired_data = ModifyDataToRobot(gc.output_data);
                desired_current = (short)desired_data.force;
                desired_position_inc = (int)desired_data.position;
                desired_speed_inc = (int)desired_data.velocity;
            }
        }    // 读取游戏控制量,并换算成特定模组的标准值.游戏输出量满足标准单位,其说明详见GameController.键盘似乎不能接受feedback呢,虽然可以模拟一下
        virtual internal void GameFeedforward(GameController gc = null) {
            if (gc == null)
            {
                return;
            }
            else {
                gc.RobotDataRead(ModifyDataToGame(received_data));
            }
        } // 将电机/传感器数据转换为标准单位,并赋值给GameController.两处引用分别是机器人和键盘

        // 更新之后试图使用结构体替代原先的复杂方案
        // 但是吧,机器人这一端用的是整数型16/32,我们的数据结构体是浮点型
        protected downsampled_data received_data;
        internal int speed_inc { get { return (int)received_data.velocity; }  set { velocity_buffer = (value - velocity_buffer > max_speed_inc ? velocity_buffer + max_speed_inc : (value - velocity_buffer < -max_speed_inc ? velocity_buffer - max_speed_inc : value)); received_data.velocity = velocity_buffer; } }
        private int velocity_buffer;
        internal int position_inc { get { return (int)received_data.position; } set { position_buffer = (value - position_buffer > max_position_inc? position_buffer + max_position_inc : (value - position_buffer < -max_position_inc ? position_buffer - max_position_inc : value)); received_data.position = position_buffer; } }
        private int position_buffer;            // 避免异常速度输入
        internal int current_inc;
        internal float inter_force { get { return received_data.force; } set { received_data.force = value; } }
        private downsampled_data desired_data;
        internal int desired_speed_inc { get; private set; }
        internal int desired_position_inc { get; private set; }
        internal short desired_current { get; private set; }
        // 0: position control; 1: speed control; 2: current control, 这是PC中的控制定义
        // 当你想修改控制模式时,一定要修改这个uint类型,它会被cast到RobotController.control_mode,并在PortFunction()中用于
        virtual internal void PackControlMessage()
        {
            // 在robotcontroller中,0-position, 1-speed, 2-current
            // 00-speed, 1-current, 11-position
            udp_send_msg[0] = 0xce;
            udp_send_msg[1] &= (0b11101010);
            switch (desired_data.control_mode)
            {
                case control_mode.position:
                    udp_send_msg[1] |= (byte)(0b00010100);
                    PackDesiredPosition();
                    break;
                case control_mode.speed:
                    udp_send_msg[1] |= (byte)(0b00000000);
                    PackDesiredSpeed();
                    break;
                case control_mode.current:
                    udp_send_msg[1] |= (byte)(0b00010000);
                    PackDesiredCurrent();
                    break;
                default:
                    udp_send_msg[1] |= (byte)(0b00000100);
                    PackDesiredCurrent();
                    Debug.Log("哦我的上帝,这是不合适的");
                    break;
            }
        }
        protected private bool handle_sw_last_state;                  // 按钮需要滤波来避免误识别
        protected private bool handle_sw_trigger = false;             // 已经忘了工作原理的临时变量
        internal bool handle_sw
        {
            get
            {
                received_data.pressed = handle_sw_last_state;
                return received_data.pressed;
            }
            set
            {
                if (value != handle_sw_last_state)
                {
                    if (value)
                    {
                        if (!handle_sw_trigger)
                            handle_sw_trigger = true;
                    }
                }
                handle_sw_last_state = value;
            }
        }
        internal byte[] udp_send_msg = { 0xCE, 0b11101010, 0, 0, 0, 0 };        // 这是默认的发送指令,意为未初始化且传递值为0
        protected virtual void PackDesiredCurrent()
        {
            udp_send_msg[2] = (byte)(desired_current & 0xFF);
            udp_send_msg[3] = (byte)(desired_current >> 8 & 0xFF);
            udp_send_msg[4] = (byte)(desired_current & 0xFF);
            udp_send_msg[5] = (byte)(desired_current >> 8 & 0xFF);
        }               // 电流控制下配置输出电流,1单位=待定
        protected virtual void PackDesiredPosition()
        {
            udp_send_msg[2] = (byte)(desired_position_inc & 0xFF);
            udp_send_msg[3] = (byte)(desired_position_inc >> 8 & 0xFF);
            udp_send_msg[4] = (byte)(desired_position_inc >> 16 & 0xFF);
            udp_send_msg[5] = (byte)(desired_position_inc >> 24 & 0xFF);
        }              // 位置控制下配置输出位置
        protected virtual void PackDesiredSpeed()
        {
            Debug.Log("GetRobotData ->override");
            udp_send_msg[2] = (byte)(desired_speed_inc & 0xFF);
            udp_send_msg[3] = (byte)(desired_speed_inc >> 8 & 0xFF);
            udp_send_msg[4] = (byte)(desired_speed_inc >> 16 & 0xFF);
            udp_send_msg[5] = (byte)(desired_speed_inc >> 24 & 0xFF);
        }                 // 位置控制下配置输出位置,8192inc=1mm/s
        virtual internal bool GetRobotData(byte[] udpRecArray)
        {
            position_inc = char2Int(udpRecArray, 2);
            speed_inc = char2Int(udpRecArray, 6);
            inter_force = char2Short(udpRecArray, 10) * .1f;
            handle_sw = Convert.ToBoolean(udpRecArray[1] & 0b01000000);
            return true;
        }  // 处理udp_msg并分配给相关变量
        private static int char2Int(byte[] tmp, int k)
        {
            byte[] t = new byte[] { tmp[k], tmp[1 + k], tmp[2 + k], tmp[3 + k] };
            return BitConverter.ToInt32(t, 0);
        }          // 把数据流转化为INT32
        private static int char2Short(byte[] tmp, int k)
        {
            byte[] t = new byte[] { tmp[k], tmp[1 + k] };
            return BitConverter.ToInt16(t, 0);
        }        // 把数据流转化为INT16
    }
}