using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TouchRobot.UI;
using System.Threading;
using System.IO.Ports;
using System;

namespace TouchRobot.Logic
{
    class RobotController2D : RobotController
    {
        private standardlized_data rendered_data;
        my_delegate interface_command;                                   // 这是一个委托,链接开始时它什么也不做;游戏开始时,它负责单位换算;应用退出时,它负责中断线程和关闭串口
        RobotParameter2D my_parameter;
        internal RobotController2D(string str, control_module this_module = control_module.robot_2d_mode_0)
        {
            my_module = this_module;
            my_status = RobotInterface.status_robot_connect.wait;
            my_parameter = new RobotParameter2D(DataConverter.NewConverter(my_module));
            MessageManager.to_string(MessageManager.info_type.notice, "Attempting to set up port with name \"" + str + "\" as 2d device...");
            interface_command = InternalEmpty;
            temp_port_name = str;
        }
        internal override void InterfaceCallGameStart()
        {
            base.InterfaceCallGameStart();
        }
        internal override void InterfaceCallGameStop()
        {
            base.InterfaceCallGameStop();   // 虽然压根没有重写,但是以此提示父类包含这个功能
        }
        internal override void InterfaceCallInitRobot()
        {
            baud_rate = 460800;
            base.InterfaceCallInitRobot();
        }
        internal override void InterfaceCallShutRobot()
        {
            base.InterfaceCallShutRobot();
        }
        internal override void InterfaceCallShutRobot(bool direct)
        {
            base.InterfaceCallShutRobot(direct);
        }
        private protected override void InternalWrite()
        {
            InternalGameDataConversion();
            try
            {
                // RS485写回数据至ESP
                my_parameter.GameFeedback(my_game_controller);
                my_parameter.PackControlMessage();
                my_port.Write(my_parameter.udp_send_msg, 0, 7);
                if (++write_count % 100 == 0)
                {
                    long tp = DataRecorder.current_time;
                    rate_write = (short)(1000000000 / (tp - tick_write));
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
        override protected void PortRead()
        {
            MessageManager.to_string(MessageManager.info_type.attention, "demo read");
            Debug.Log("PortRead thread init ...");
            RobotInterface.ExceptionHandle(RobotInterface.status_robot_connect.wait, this);
            byte[] received_data = new byte[0];
            bool operate_once = false;
            int mismatch_count = 0;
            long last_tick = DataRecorder.current_time;
            int read_count = 0;
            Debug.Log("PortRead thraed init ok");
            while (thread_on)
            {
                try
                {
                    received_data = new byte[my_port.BytesToRead];
                    my_port.Read(received_data, 0, received_data.Length);
                    if (received_data.Length == 0)
                    {
                        Debug.LogError("No data !!!!!!!");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("PortRead ex: " + ex);
                    OnPortAbnormalDisconnect(ex, RobotInterface.status_robot_connect.port_exception);
                }
                if (my_parameter.GetRobotData(received_data))
                {
                    Debug.Log("数据读取成功");
                    mismatch_count = 0;
                    if (!operate_once)
                    {
                        my_game_controller = GameInterface.GenerateGameController(this);
                        RobotInterface.ExceptionHandle(RobotInterface.status_robot_connect.ok, this);
                        operate_once = true;
                    }
                    my_parameter.GameFeedforward(my_game_controller);
                    InternalCallWrite();
                    SetReady(true);
                    if (read_count++ >= 100)
                    {
                        read_count = 0;
                        rate_read = (short)(1000000000 / (DataRecorder.current_time - last_tick));
                        last_tick = DataRecorder.current_time;
                    }
                }
                else
                {
                    if (mismatch_count++ > 16384)
                    {
                        OnPortAbnormalDisconnect(new System.Exception("Data mismatch"), RobotInterface.status_robot_connect.data_mismatch);
                    }
                }
                Thread.SpinWait(2000);
            }
        }
        protected override void PortWrite()
        {
            base.PortWrite();
        }
        internal override string DataDisplay()
        {
            if (system_ready)
                return port_name + ": " + (my_parameter.handle_sw ? "■" : "▢") + "\n" + "Read@" + rate_read + "; Write@" + rate_write + "\nPosition@\n" + my_parameter.TouchToStrings();
            else
                return "Port has not been connected.";
        }
    }
    internal class RobotParameter2D : RobotParameters
    {
        internal byte received_code;        // debug时可以打印保留位
        internal byte reserved_code;        // debug时可以打印保留位
        ScrollingDataReader scroll;
        new standardlized_data received_data;
        internal string TouchToString(int site = 0)
        {
            site = site > 5 ? 5 : site;
            touching_item ti = scroll.touching_items.ItemAt(site);
            return (ti.registered ? "◉" : "○") + ti.touch_index + ": " + ti.touch_position.x.ToString("d") + ", " + ti.touch_position.y.ToString("d") + "\n";
        }
        internal string TouchToStrings()
        {
            string temp = "";
            for (int i = 0; i < 6; i++)
            {
                temp += TouchToString(i);
            }
            return temp;
        }
        internal standardlized_data desired_data;
        internal struct touching_item
        {
            internal bool registered;
            internal int touch_index;
            internal Vector2Int touch_position;
            internal void AssignRegister(bool status) { registered = status; }
            internal void AssignIndex(int i) { touch_index = i; }
            internal void AssignPosition(Vector2Int i) { touch_position = i; }
        }
        new internal byte[] udp_send_msg { get { return uart_message; } }
        private byte[] uart_message;
        internal RobotParameter2D(DataConverter.converting_ratio ratio)
        {
            my_ratio = ratio;
            uart_message = new byte[] { 0xef, 0b10010000, 0b10101111, 0, 0, 0, 0 };
            handle_sw_trigger = false;
            desired_data.control_mode = control_mode.current;
            if (scroll == null)
                scroll = new ScrollingDataReader();
        }                      // 复写默认构造器
        internal RobotParameter2D(control_module module)
        {
            my_ratio = DataConverter.NewConverter(module);
            uart_message = new byte[] { 0x1f, 0x7f, 0b11101010, 0, 0, 0, 0 };
            handle_sw_trigger = false;
            desired_data.control_mode = control_mode.current;
            if (scroll == null)
                scroll = new ScrollingDataReader();
        }
        internal override void GameFeedback(GameController gc = null)
        {
            if (gc == null)
            {
                desired_data.control_mode = control_mode.current;
                desired_data.force = Vector3.zero;
                return;
            }
            else
            {
                desired_data = ModifyDataToRobot(gc.original_output_data);
            }
        }
        internal override void GameFeedforward(GameController gc = null)
        {
            if (gc == null)
            {
                return;
            }
            else
            {
                gc.RobotDataRead(my_ratio.ModifyDataToGame(received_data));
            }
        }
        internal override void PackControlMessage()
        {
            Debug.Log("GetRobotData ->override");
            uart_message[0] = 0xef;
            uart_message[1] &= (0b10010000);
            uart_message[2] = (byte)((0b10100000) | (0b00001111 & (++reserved_code)));
            short temp_value = (short)desired_data.force.x;
            uart_message[3] = (byte)temp_value;
            uart_message[4] = (byte)(temp_value >> 8);
            temp_value = (short)desired_data.force.y;
            uart_message[5] = (byte)temp_value;
            uart_message[6] = (byte)(temp_value >> 8);
        }
        override internal bool GetRobotData(byte[] recv_array)
        {
            bool result = scroll.Read(recv_array);
            handle_sw = (scroll.touching_items.touching_switches & 0b00000001) == 0b00000001;
            received_data.pressed = base.received_data.pressed;
            received_code = scroll.touching_items.debug_code;
            // 理论上将多点定位的结果用于单点控制需要很多代码,我们先做出demo,将读到的第一个点作为识别的位置
            // ps. 从之前的框架来看,GameController应该将来自RobotController的各种奇怪的格式转换为游戏内容接受的数据格式
            //     原计划将多点展示出来,请在继承和接口认真理解后在后续工作中完成
            if (result)
            {
                if (scroll.touching_items.ItemAt(0).registered)
                {
                    received_data.position = (Vector2IntToFloat(scroll.touching_items.ItemAt(0).touch_position));
                }
                else
                {
                    received_data.position = Vector3.zero;
                }
            }
            Debug.Log("RobotParameters.GetRobotData return  " + result);

            return result;
        }
        private Vector2 Vector2IntToFloat(Vector2Int v2int)
        {
            v2int = new Vector2Int(-v2int.x + 16384, -16384 + v2int.y);
            return new Vector2(v2int.x, v2int.y * .5625f);
        }
        private Vector2Int Vector2FloatToInt(Vector2Int v2float)
        {
            return new Vector2Int(v2float.x, v2float.y);
        }

    }

    class TouchingItems
    {
        internal ref RobotParameter2D.touching_item ItemAt(int i)
        {
            switch (i)
            {
                case 0: return ref touching_item_0;
                case 1: return ref touching_item_1;
                case 2: return ref touching_item_2;
                case 3: return ref touching_item_3;
                case 4: return ref touching_item_4;
                default: return ref touching_item_5;
            }
        }
        internal void ClearAt(int i)
        {
            ItemAt(i).AssignRegister(false);
        }
        internal void ClearFrom(int i)
        {
            for (int j = i; j < 6; j++)
            {
                ClearAt(j);
            }
        }
        internal byte debug_code;
        internal byte touching_switches;
        internal RobotParameter2D.touching_item touching_item_0;
        internal RobotParameter2D.touching_item touching_item_1;
        internal RobotParameter2D.touching_item touching_item_2;
        internal RobotParameter2D.touching_item touching_item_3;
        internal RobotParameter2D.touching_item touching_item_4;
        internal RobotParameter2D.touching_item touching_item_5;
    }

    class ScrollingDataReader
    {
        private static readonly byte[] head_check_code = { 0x1f, 0x7f, 0b10010000 };    // 数据帧头
                                                                                        //const byte touch_index_check_code = 0b10100000;                                 // ESP报文的3+5N位表示触摸的编号.高4位比特是1001
                                                                                        //const byte touch_index_check_code = 0b00110000;                                 // ESP报文的3+5N位表示触摸的编号.高4位比特是1001
        const byte touch_index_check_code = 0b10100001;
        const byte reserved_index_check_code = 0b10110000;                              // ESP报文的E-3位表示具有保留功能.高4位比特是1011
        const byte touch_number_check_code = 0b11000001;                                // ESP报文的E-3位表示具有保留功能.高4位比特是1011
        const byte tail_check_code = 0x0f;                                              // 数据帧尾
        const byte frame_mask = 0b11110000;                                             // 半字节校验时用于与运算
        const byte data_mask = 0b00001111;                                              // 半字节校验时用于与运算
        private byte check_index;                                                       // 帧头检测的位数
        private byte read_count;
        private byte touch_count;                                                       // 触摸个数统计(用于校验)
        private byte sum_check;                                                         // 数值校验
        internal TouchingItems touching_items;
        private TouchingItems protected_touching_items;
        internal ScrollingDataReader()
        {
            touching_items = new TouchingItems();
            protected_touching_items = new TouchingItems();
        }
        private void CountReset()
        {
            read_count = 0;
            check_index = 0;
            sum_check = 0;
            touch_count = 0;
        }
        private void DataMismatch(byte error_code = 0)
        {
            CountReset();
            //Debug.LogError("Data mismatch with code " + error_code);
        }

        internal struct int16_synthesis
        {
            internal byte low_case;
            internal byte up_case;
            internal ushort int16 { get { return (ushort)((up_case << 8) + low_case); } }
        }
        private int16_synthesis short_synthesis;
        private Vector2Int vector_synthesis;
        internal bool Read(byte[] udp_msg)
        {
            Debug.Log("ScrollingDataReader.Read start ... msg => " + BitConverter.ToString(udp_msg));
            bool result = false;
            for (byte i = 0; i < udp_msg.Length; i++)
            {
                Debug.Log("data type>>>>>>>>>>>>>>>>>>>>>>: [" + check_index + "]  udp_msg => " + Convert.ToString(udp_msg[i], 16));
                switch (check_index)
                {
                    case 6:
                        if (udp_msg[i] == tail_check_code)
                        {
                            protected_touching_items.ClearFrom(touch_count);
                            touching_items = protected_touching_items;
                            CountReset();
                            result = true;
                        }
                        else
                        {
                            DataMismatch(6);
                        }
                        break;
                    case 5:
                        //if (udp_msg[i] == sum_check)
                        //Todo fix 
                        if (sum_check == sum_check)
                        {
                            check_index++;
                        }
                        else
                        {
                            //Debug.Log(sum_check);
                            DataMismatch((byte)(udp_msg[i]));
                        }
                        break;
                    case 4:
                        if (udp_msg[i] == touch_number_check_code)
                        {
                            //Debug.Log("touch_count" + touch_count + "(udp_msg[i] & data_mask)=" + (udp_msg[i] & data_mask));
                            //if (touch_count == (udp_msg[i] & data_mask))

                            //{
                            check_index++;
                            sum_check += udp_msg[i];
                            //}
                            //else
                            //{
                            //    DataMismatch(5);
                            //}
                        }
                        else
                        {
                            DataMismatch(4);
                        }
                        break;
                    case 3:
                        Debug.Log("type_3: [" + read_count % 6 + "]  udp_msg => " + Convert.ToString(udp_msg[i], 16));
                        switch (read_count % 6)
                        {
                            case 0:
                                if (udp_msg[i] == touch_index_check_code)
                                {
                                    protected_touching_items.ItemAt(read_count / 6).AssignIndex(0x0f & udp_msg[i]);
                                }
                                else if ((udp_msg[i] & frame_mask) == reserved_index_check_code)
                                {
                                    protected_touching_items.debug_code = udp_msg[i];
                                    check_index++;
                                }
                                else
                                {
                                    DataMismatch(3);
                                    continue;
                                }
                                break;
                            case 1:
                                short_synthesis.low_case = udp_msg[i];
                                break;
                            case 2:
                                short_synthesis.up_case = udp_msg[i];
                                vector_synthesis.x = short_synthesis.int16;
                                break;
                            case 3:
                                short_synthesis.low_case = udp_msg[i];
                                break;
                            case 4:
                                short_synthesis.up_case = udp_msg[i];
                                vector_synthesis.y = short_synthesis.int16;
                                protected_touching_items.ItemAt(read_count / 6).AssignPosition(vector_synthesis);
                                protected_touching_items.ItemAt(read_count / 6).AssignRegister(true);
                                touch_count++;
                                break;
                            case 5:
                                Debug.Log("角度=> " + Convert.ToString(udp_msg[i], 16));
                                break;
                        }
                        read_count++;
                        sum_check += udp_msg[i];
                        break;
                    case 2:
                        if ((udp_msg[i] & frame_mask) == head_check_code[2])
                        {
                            check_index++;
                            protected_touching_items.touching_switches = (byte)(udp_msg[i] & 0b00001111);
                            sum_check += udp_msg[i];
                        }
                        else
                        {
                            DataMismatch(2);
                        }
                        break;
                    default:
                        if (head_check_code[check_index] == udp_msg[i])
                        {
                            check_index++;
                        }
                        else
                        {
                            DataMismatch(1);
                        }
                        break;
                }
            }
            Debug.Log("ScrollingDataReader.Read return  " + result);
            return result;
        }
    }
}