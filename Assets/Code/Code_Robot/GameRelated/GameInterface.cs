using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TouchRobot.UI;
namespace TouchRobot.Logic
{
    sealed class GameInterface : MonoBehaviour
    {
        // 旧的思路是:你可以直接连接所有的设备,在此之后选择哪些设备用于游戏.
        // 现在:用户带点脑子,你都不打算用某个设备游戏,那你肯定不会去连接它.
        // 上面这点很重要啊!!!!我们不需要额外增加选择功能,允许用户选择哪些连接的设备需要加入游戏,而是逼着他们自己在开始游戏前就把这些玩意弄清楚
        // 结果:RobotControrller出现后便调用GameInterface生成与之对应的GameController
        public static bool game_is_on { private set; get; }                                            // 如果游戏在进行中,那就别添加设备了,反正求你也没用,我直接不让你这么干
        public static void SetGame(bool state) {
            game_is_on = state;
        }
        static string time_to_string => System.DateTime.Now.ToString();           // 用于打印日志中的时间
        internal static List<GameController> list_online_game { get; private set; } // 当前启用的GameController
        internal static string head_line
        {
            get
            {
                string head_line = "";
                int count = 0;
                foreach (GameController gc in list_online_game)
                {
                    head_line += gc.append_head_line;
                    if (++count < count_players)
                    {
                        head_line += ",";
                    }
                }
                return head_line;
            }
        }
        internal static int count_players => list_online_game == null ? 0 : list_online_game.Count;
        static void ResetList()
        {
            list_online_game = new List<GameController>();
        }                                                // 退出游戏/程序开始时,重置这个列表.
        internal static GameController GenerateGameController(RobotController rc) {
            GameController new_gc = new GameController();
            new_gc.InterfaceAssignRobotController(rc);
            if (list_online_game == null)
                ResetList();
            list_online_game.Add(new_gc);
            DataRecorder.GameInterfaceCallReRecord();
            MessageManager.to_string(MessageManager.info_type.notice, "Corresponding game controller is created.");
            return new_gc;
        }                 // 每连接成功一个RobotController,则预生成一个GameController,并添加至表单
        internal static void RemoveGameController(GameController gc) {
            DataRecorder.GameInterfaceCallReRecord();
            if (gc != null)
            {
                gc.GameStopData();
                gc.InterfaceRemoveAttribute();
                if (list_online_game != null)
                    list_online_game.Remove(gc);
            }
            MessageManager.to_string(MessageManager.info_type.notice, "Corresponding game controller is destroyed.");
        }            // 当RobotController断开时,清除对应的GameController,同时关闭可能具有的CharacterAttribute
        public static void StartTransmission() 
        {
            foreach (GameController gc in list_online_game) 
            {
                gc.GameStartData();
            }
            SetGame(true);
        }                                        // 开始游戏,令存储的所有GameController开始单位换算
        public static void CloseTransmission()
        {
            foreach (GameController gc in list_online_game)
            {
                gc.GameStopData();
            }
        }                                         // 结束游戏时,关闭所有GameController的单位换算
        public static string MakeUpSentences() {
            string tmp = "";
            int count = 0;
            foreach (GameController gc in list_online_game) 
            {
                tmp += gc.to_string;
                if (++count < count_players)
                    tmp += ",";
            }
            return tmp;
        }
        private void Start()
        {
            if(list_online_game==null)
                ResetList();
        }
        private void Update()
        {
        }
    }
}