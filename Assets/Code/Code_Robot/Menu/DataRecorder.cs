#define stop_watch
#define display_message
#if display_message
#define display_fail
#define display_unknown
#define display_attention
#define display_correct
#define display_notice
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Threading;
using System.IO;
using TouchRobot.Logic;
using UnityEngine.UI;
namespace TouchRobot.UI
{
    sealed class DataRecorder : MonoBehaviour
    {
        /// <版本迭代>
        /// 由于DateTime.Now.Ticks的精度太拉胯了,只有1ms.但是咱记录数据不能比他还慢,所以这个版本使用了System.Diagnostics.Stopwatch;在打印情况下其精度可以稳定在200us级
        /// 基于这一先进技术,可以以此为基础同步PC端时间,即通过DataRecorder实时更新时间,同步到RobotController等中用于工作频率的监控
        /// 计算机保护分为两部分,其一是最大录制时长不应超过2Hrs,其次是计算机无人监管(无键鼠输入)不应超过10分钟.二者均可触发记录结束
        [SerializeField] Button btn_recorder;       // 记录的按钮,提示是否正在记录
        static Button _btn_recorder;                // 记录的按钮,提示是否正在记录
        [SerializeField] Color unpressed_color;     // 按钮弹起时的颜色
        [SerializeField] Color pressed_color;       // 按钮按下时的颜色        
        static Color _unpressed_color;              // 按钮弹起时的颜色
        static Color _pressed_color;                // 按钮按下时的颜色
        private static string file_path;            // 写入文件的保存路径
        private static string data_path;            // 只能从主函数调用路径(Application.datapath)
        private static string file_name;            // 写入文件的文件名
        private static StringBuilder text_writer;   // 这是从旧的代码抄过来的,用这个类实现写入文本
        private static Thread thread_data_recording;// 在线程里操作写入
        private static Thread thread_public_timer;  // 开始计时器
        private static bool is_recording;           // 开启录制=按下了录制按钮.但是如果游戏没有开始,那么也不会有数据写入.
        private static bool is_timing;              // 开启计时器.
        private delegate void my_delegate();
        private static my_delegate re_record_event; // 如果关闭了一轮游戏,需要重新生成文本文件,避免复写.同时,在连接的设备发生断线时,也会同时生成新的文本文件.
                                                    // 由于断线这些事件是在GameInterface和GameLogic中监听的,所以依旧使用静态方法和静态类.
        private static bool saving_guard = false;   // 避免写入文件是一个异步过程,导致奇怪的问题
        private const double max_length = 600;      // 避免一不小心忘记关程序然后硬盘爆炸,最大录制时间是600秒,在鼠标/键盘输入时重置倒计时.
        private const double _max_length = 7200;    // 避免一不小心忘记关程序然后硬盘爆炸,最大录制时间是7200秒,仅在重新开始记录时重置倒计时.
        private static double current_frame = 0;    // 避免一不小心忘记关程序然后硬盘爆炸
        private static double _current_frame = 0;   // 避免一不小心忘记关程序然后硬盘爆炸
        private static long record_starting_time; // 数据记录的开始时间,在重新开始记录数据时会置零
        private static long record_last_time;     // 数据记录的开始时间,在重新开始记录数据时会置零
        internal static long current_time;        // 当前的时间
        private static long get_time => System.Diagnostics.Stopwatch.GetTimestamp();
        private static double get_elapsed_time(long compared_time, long base_time ) 
        {
            return (compared_time - base_time) / (1.0 * System.Diagnostics.Stopwatch.Frequency);
        }
        internal static void GameInterfaceCallReRecord() {
            re_record_event = InternalReCreateText;
        } // 发生设备中断,游戏退出等情形时,保存旧的并重新生成文件.添加委托
        private static void InternalReCreateText() {
            InitiateThreadDataRecording(false);
            re_record_event = InternalEmpty;
        }       // 发生设备中断,游戏退出等情形时,保存旧的并重新生成文件.委托的实现
        private static void InternalEmpty() { }              // 空的委托
        public static void InitiateThreadDataRecording(bool thread_operation = true)
        {
            current_frame = 0;
            _current_frame = 0;
            if (is_recording)
            {
                if (thread_operation)
                {
                    StopRecording();
                    return;
                }
                else
                {
                    WriteIn();
                    AppendHeadLine();
                }
            }
            else 
            {
                if (GameInterface.game_is_on) 
                {
                    GameInterfaceCallReRecord();
                }
            }
            int count = 0;
            while (saving_guard) 
            {
                Thread.SpinWait(1);
                if (count++ > 10000) {
                    MessageManager.to_string(MessageManager.info_type.fail, "File saving is interrupted.");
                    if (is_recording) 
                    {
                        _PressDataRecording();
                    }
                    return;
                }
            }
            file_name = "rec_" + System.DateTime.Now.ToString("MM_dd_HH_mm_ss") + ".csv";
            file_path = data_path + "/../" + file_name;
            record_starting_time = get_time;
            record_last_time = record_starting_time;
            if (thread_operation)
            {
                thread_data_recording = new Thread(new ThreadStart(DataRecording));
                thread_data_recording.Start();
                is_recording = true;
                MessageManager.to_string(MessageManager.info_type.correct, "Data recording is initiated.");
                return;
            }
        }       // 通过UIManager调用开启文本写入
        static void AppendHeadLine() {
            string head_line = "ms,";
            text_writer.AppendLine(head_line + GameInterface.head_line);
            //record_starting_time = current_time;
            //record_last_time = record_starting_time;
        }     // 仅在执行委托时发生,用于打印第一行
        static void DataRecording()
        {
            is_recording = true;                // 开始了吗
            record_last_time = current_time;    // 功能内计时器
            double delta_value = get_elapsed_time(current_time, record_last_time);
            while (is_recording) 
            {
                if (saving_guard) {
                    continue;
                }
                if (GameInterface.game_is_on)
                {
                    re_record_event();
                    delta_value = get_elapsed_time(current_time, record_last_time);
                    if (delta_value > 0.0005)
                    {
                        current_frame+=delta_value;
                        _current_frame+=delta_value;
                        string str_time = (get_elapsed_time(current_time, record_starting_time) * 1000f).ToString("f1") + "," + GameInterface.MakeUpSentences();
                        text_writer.AppendLine(str_time);
                        record_last_time = current_time;
                    }
                }
                Thread.SpinWait(100);
            }
        }       // 文本写入的具体实现
        public void PressDataRecording() 
        {
            InitiateThreadDataRecording();
            if (is_recording)
            {
                btn_recorder.image.color = pressed_color;
            }
            else {
                btn_recorder.image.color = unpressed_color;
            }
        } // 控制按下后徽标变色
        static void _PressDataRecording() 
        {
            InitiateThreadDataRecording();
            if (is_recording)
            {
                _btn_recorder.image.color = _pressed_color;
            }
            else {
                _btn_recorder.image.color = _unpressed_color;
            }
        }// 静态方法,以便于在写线程中直接操作图标
        static void WriteIn() {
            saving_guard = true;
            if(text_writer.ToString().Length > 15)
                File.WriteAllText(file_path, text_writer.ToString());
            text_writer.Clear();
            Thread.Sleep(1);
            saving_guard = false;
        }            // 将stringBuilder中的内容写入本地文件,否则它将躺在内存或临时文件中.情形1:设备变化时保存旧的并开启新的;情形2:正常的结束记录.
        static void StopRecording(MessageManager.info_type type = MessageManager.info_type.correct)
        {
            is_recording = false;
            MessageManager.to_string(type, "Data recording is interrupted.");
            if (thread_data_recording != null)
            {
                Thread.Sleep(1);
                thread_data_recording.Interrupt();
                Thread.Sleep(1);
                WriteIn();
                Thread.Sleep(50);
                thread_data_recording.Abort();
            }
            current_frame = 0;
            _current_frame = 0;
        }       // 停止记录并中断线程
        static void GetTime(object message) {
            is_timing = true;
            MessageManager.to_string((MessageManager.info_type)message, "Timer is initiated.");
            while (is_timing) 
            {
                current_time = get_time;
                Thread.SpinWait(1000);
            }
        }
        static void StopTiming(MessageManager.info_type type = MessageManager.info_type.notice)
        {
            is_timing = false;
            if (thread_public_timer != null)
            {
                thread_public_timer.Interrupt();
                thread_public_timer.Abort();
            }
            MessageManager.to_string(type, "Timer is interrupted.");
        }
        private void Start()
        {
            if (thread_data_recording == null)
            {
                MessageManager.info_type type = MessageManager.info_type.notice;
                thread_public_timer = new Thread(new ParameterizedThreadStart(GetTime));
                thread_public_timer.Start(type);
            }
            if (text_writer == null)
            {
                text_writer = new StringBuilder();
                data_path = Application.dataPath;
            }
            _btn_recorder = btn_recorder;
            _pressed_color = pressed_color;
            _unpressed_color = unpressed_color;
        }
        private void Update()
        {
            if (current_frame > max_length || _current_frame > _max_length) {
                if (is_recording)
                {
                    PressDataRecording();
                    MessageManager.to_string(MessageManager.info_type.attention, "Data recording time out, please restart or contact with the manager to increase recorded clip length.");
                }
            }
            if (Input.anyKey) 
            {
                current_frame = 0;
            }
        }
        private void OnApplicationQuit()
        {
            StopRecording(MessageManager.info_type.notice);
            StopTiming();
        }
    }
    public class MessageManager
    {
        static string time_to_string => RobotInterface.time_to_string;
        public enum info_type { 
            attention,          // 用户间接操作致使潜在不利影响提示;或系统不利事件起止
            notice,             // 用户直接递交指令,事件处理中提示;或系统常规事件起止
            fail,               // 用户操作失败的结果类提示
            correct,            // 用户操作成功的结果类提示
            unknown             // 原因未知的结果类提示,也可能是脏话
        }
        static string to_icon(info_type type)
        {
            switch (type)
            {
                case info_type.fail:
                    return "<x> ";
                case info_type.notice:
                    return "<i> ";
                case info_type.attention:
                    return "<!> ";
                case info_type.correct:
                    return "<o> ";
                default:
                    return "<?> ";
            }
        }
        public static void to_string(info_type type, string msg = "")
        {
            // Debug.Log与print方法应该用于早期debug,为了避免事件打印影响调试,姑且加入一个预处理器指令避免信息淹没,同时便于管理
#if !display_fail
            if (type == info_type.fail)
                return;
#endif
#if !display_attention
            if (type == info_type.attention)
                return;
#endif
#if !display_unknown
            if (type == info_type.unknown)
                return;
#endif
#if !display_correct
            if (type == info_type.correct)
                return;
#endif
#if !display_notice
            if (type == info_type.notice)
                return;
#endif
#if display_message
            Debug.Log(to_icon(type) + time_to_string + ": " + msg);
#else
            return;
#endif
        }
    }
}