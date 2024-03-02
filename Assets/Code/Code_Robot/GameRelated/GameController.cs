using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TouchRobot.ToGame;
using TouchRobot.Extra;
namespace TouchRobot.Logic
{
    class GameController
    {
        // 通过析构函数测试,目前的代码可以自动清理孤立的gamecontroller
        protected string port_name;
        // 所有参数都是标准单位,最终传回
        private standardlized_data robot_data;
        private standardlized_data desired_data {
            set
            {
                _desired_data = value;
                //_desired_data.force = new Vector3(Modulate(value.force.x), 0, 0);
            }
            get
            {
                return _desired_data;
            }
        }
        private standardlized_data _desired_data;
        internal downsampled_data output_data
        {
            get
            {
                return DataConverter.DownSampling(desired_data);
            }
        }
        internal standardlized_data original_output_data
        {
            get
            {
                return desired_data;
            }
        }
        // 命名不规范,后人泪两行
        // 建议:命名第一个驼峰写其服务对象,如用于游戏,则Game...;如果仅限于当前类,则my_...,如果想让Interface代为执行,则Interface....,如果是事件,则以On开头
        delegate void my_delegate();                                    // 委托
        my_delegate my_invoke_game_start;                               // 通过储存方法在游戏开始时,要挟RobotController开始单位换算
        my_delegate my_invoke_game_close;                               // 通过储存方法在游戏开始时,要挟RobotController结束单位换算

        GameCharacterAttribute binded_character;
        internal void RobotDataRead(standardlized_data sd) {
            robot_data = sd;
        }   // RobotController将电机单位转化为标准单位.虽然推荐一步到位,但是这样真的很好啊
        internal void GameAssignCharacterAttribute(GameCharacterAttribute tg) {
            binded_character = tg;
            tg.AddDeletionDelegate(this);
        }
        // 在GameLogic中开始生成游戏prefabs时执行,根据已有GameController实例化相同数量的GameCharacterAttribute,并实现引用赋值.同时为GCA的委托赋值,便于之后正常退出游戏仍能使用该GameController
        internal void GameDataTransmission()
        {
            if (binded_character != null)
            {
                binded_character.DataRead(robot_data);
                desired_data = binded_character.output_data;
            }
        }                            // 机器人数据单位变换的具体操作,这是在RobotController的接收线程中通过委托调用的
        internal void GameStartData() {
            my_invoke_game_start();
        }                                   // 通过GameInterface启动单个GameController的数据单位变换
        internal void GameStopData() {
            my_invoke_game_close();
        }                                   // 通过GameInterface终止单个GameController的数据单位变换;具体行为是让RobotController在接收循环里调用相关委托
        internal void InterfaceAssignRobotController(RobotController rc) {
            rc.my_game_controller = this;
            port_name = rc.port_name;
            my_invoke_game_start = rc.InterfaceCallGameStart;
            my_invoke_game_close = rc.InterfaceCallGameStop;
        } // 配对机器人与设备.为了简化代码,不让GameController本身影响RobotController,这里用my_invoke_game_的两个委托分别唤起RobotController在线程中开始单位换算和置为空的两个行为.
        public void InterfaceRemoveAttribute(bool robot=true) {
            if (robot)
            {
                if (binded_character != null)
                {
                    binded_character.RobotControllerRemoveAttribute();
                    binded_character = null;
                }
            }
            else
            {
                if (binded_character != null)
                {
                    binded_character = null;
                }
            }
            desired_data = new standardlized_data { control_mode = control_mode.current };
        }       // true:当机器人被移除时,同时移除该实例.若游戏在进行中,同时移除CharacterAttribute,并从GameLogic列表中删除;false:仅移除引用
        public string append_head_line {
            get { return string.Format("{0}_button,{0}_position,{0}_velocity,{0}_force,{0}_control_mode,{0}_control_value", port_name); }
        }
        public string to_string 
        {
            get 
            { 
                return string.Format("{0},{1},{2},{3},{4}", 
                robot_data.pressed?"1":"0", 
                robot_data.position.x.ToString("f3"), 
                robot_data.velocity.x.ToString("f3"),
                robot_data.force.x.ToString("f3"),
                desired_data.control_mode==control_mode.current?("0,"+desired_data.force.x.ToString("f3")):(desired_data.control_mode == control_mode.position?"1," + desired_data.position.x.ToString("f3"):"2," + desired_data.velocity.x.ToString("f3"))); 
            }
        }
        // New feature @ May 15th, 2023
        // 增强触觉(输出波形调制器)
        internal GameController()
        {
            my_augmentor = new AugmentedHapticModule();
        }
        internal AugmentedHapticModule my_augmentor;
        // 在每次单位换算前调用,见RobotController.cs
        internal float Modulate(float val)
        {
            float value = val + my_augmentor.modulate(val);
            if (val > 0)
            {
                value = value > 0 ? val : 0;
            }
            else 
            {
                value = value < 0 ? val : 0;
            }
            return value;
        }
    }
    /* ____________________________________________________________________
     * **************如果想不明白要用结构体还是类,参考如下提示*****************
     * 结构体是值类型，类是引用类型
     * 结构体存在栈中，类存在堆中结构体成员不能使用protected访问修饰符，而类可以
     * 结构体成员变量申明不能指定初始值，而类可以
     * 结构体不能申明无参的构造函数，而类可以
     * 结构体申明有参构造函数后，无参构造不会被顶掉
     * 结构体不能申明析构函数，而类可以
     * 结构体不能被继承，而类可以
     * 结构体需要在构造函数中初始化所有成员变量，而类随意
     * 结构体不能被静态static修饰（不存在静态结构体），而类可以
     * ————————————————————————————————————————————————————————————————————
     */
    class GameControllerMultiPointer : GameController 
    {

    }

    // 你瞧哈,截至2023/4/20,设备还是单自由度的.所以和上面一样,位置,速度,力传感器示数都是一维的.所以下述方法都是操作数值型
    class DataConverter
    {
        public struct converting_ratio
        {
            public void cnm() {
                Debug.Log(position_r2c);            
            }
            internal float position_r2c { private get; set; } // 将驱动器位置转化为标准单位.当然也可以让ESP算好了包装成标准单位发回来
            internal float velocity_r2c { private get; set; } // 将驱动器速度转化为标准单位
            internal float force_r2c { private get; set; }    // 将力传感器示数转化为标准单位.由于使用整数传递,需要乘上系数实现统一
            private float position_c2r; // 后面三个从理论上讲是前者的倒数
            private float velocity_c2r;
            private float force_c2r;
            private downsampled_data feedback_data;
            internal void AutoSetInverse()
            {
                position_c2r = position_r2c == 0 ? 0 : 1 / position_r2c;
                velocity_c2r = velocity_r2c == 0 ? 0 : 1 / velocity_r2c;
                force_c2r = force_r2c == 0 ? 0 : 1 / force_r2c;
            }       // 单位换算是可逆的
            internal standardlized_data ModifyDataToGame(downsampled_data dsd)
            {
                standardlized_data rendered_data = UpSampling(dsd);
                rendered_data.position *= position_r2c;
                rendered_data.velocity *= velocity_r2c;
                rendered_data.force *= force_r2c;
                return rendered_data;
            }
            internal standardlized_data ModifyDataToGame(standardlized_data dsd)
            {
                dsd.position *= position_r2c;
                dsd.velocity *= velocity_r2c;
                dsd.force *= force_r2c;
                return dsd;
            }
            internal downsampled_data ModifyDataToRobot(downsampled_data tp_data)
            {
                tp_data.position *= position_c2r;
                tp_data.velocity *= velocity_c2r;
                tp_data.force *= force_c2r;
                return tp_data;
            }
            internal standardlized_data ModifyDataToRobot(standardlized_data tp_data)
            {
                tp_data.position *= position_c2r;
                tp_data.velocity *= velocity_c2r;
                tp_data.force *= force_c2r;
                return tp_data;
            }
        }
        internal static converting_ratio NewConverter(control_module module = control_module.keyboard)
        {
            converting_ratio tmp_converting_ratio = new converting_ratio { };
            switch (module)
            {
                case control_module.robot_2d_mode_0:
                    tmp_converting_ratio.position_r2c = 1 / 2000f;
                    tmp_converting_ratio.velocity_r2c = 1 / 2000f;
                    tmp_converting_ratio.force_r2c = 1;
                    tmp_converting_ratio.AutoSetInverse();
                    return tmp_converting_ratio;
                case control_module.robot_mode_0:
                    tmp_converting_ratio.position_r2c = 1 / 16384f;
                    tmp_converting_ratio.velocity_r2c = 1 / 10000f;
                    tmp_converting_ratio.force_r2c = 1;
                    tmp_converting_ratio.AutoSetInverse();
                    return tmp_converting_ratio;
                default:
                    tmp_converting_ratio.position_r2c = .0001f;
                    tmp_converting_ratio.velocity_r2c = .0001f;
                    tmp_converting_ratio.force_r2c = .0001f;
                    tmp_converting_ratio.AutoSetInverse();
                    return tmp_converting_ratio;
            }
        }   // 生成新的单位换算机器
        internal static downsampled_data DownSampling(standardlized_data data)
        {
            return new downsampled_data { position = data.position.x, velocity = data.velocity.x, force = data.force.x, pressed = data.pressed, control_mode = data.control_mode };
        }
        internal static standardlized_data UpSampling(downsampled_data data)
        {
            return new standardlized_data { position = new Vector3(data.position, 0, 0), velocity = new Vector3(data.velocity, 0, 0), force = new Vector3(data.force, 0, 0), pressed = data.pressed };
        }
    }
    public struct standardlized_data
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 force;
        public control_mode control_mode;
        public bool pressed;
        public void Control(Vector3 input) 
        {
            switch (control_mode) 
            {
                case control_mode.current:
                    force = input;
                    //Debug.Log(input.x);
                    break;
                case control_mode.position:
                    position = input;
                    break;
                default:
                    velocity = input;
                    break;
            }
        }
    }   // 机器人的标准数据包.这应该是
    internal struct downsampled_data
    {
        public float position;
        public float velocity;
        public float force;
        public control_mode control_mode;
        public bool pressed;
    }   // 除非用上了三维机器人,否则全部都是降采样数据
        // 如果你真用上了三维机器人,那太好了
}