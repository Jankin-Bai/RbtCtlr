using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TouchRobot.Logic;
using TouchRobot.UI;
namespace TouchRobot
{
    public delegate void button_delegate();
}
namespace TouchRobot.ToGame
{
    // 重新规范了命名空间,TouchRobot有3个子命名空间,分别为UI,Logic和ToGame.游戏内容访问时仅导入TouchRobot.ToGame即可,不会干扰UI等项目.
    // UI主要负责界面以及数据记录,Logic负责数据关联项,数据处理等
    public class GameUI : MonoBehaviour
    {
        const string txt_const_start_local_game = "Local";
        const string txt_const_close_local_game = "Abandon";
        [SerializeField] Button btn_start_local_game;
        Text txt_start_local_game;
        // 使用如下代码复用按钮的功能
        internal static button_delegate delegate_start_game;
        internal static button_delegate delegate_close_game;
        public static List<ToGame.GameCharacterAttribute> list_local_player => GameLogic.list_local_player;   // 游戏角色特征
        // 使用如下代码可以为新的游戏内容复用已有的开始按钮
        public static void AddStartFunction(button_delegate ua)
        {
            delegate_start_game = ua;
        }
        public static void AddCloseFunction(button_delegate ua)
        {
            delegate_close_game = ua;
        }
        public static void AssignAvatar(GameObject[] go, Vector3 position = new Vector3())
        {
            if (go == null) 
                return;
            if (go.Length == 0)
                return;
            GameLogic.AssignAvatar(go, position);
        }
        void ButtonStart() {
            if (GameInterface.count_players == 0) {
                MessageManager.to_string(MessageManager.info_type.fail, "No device is available for starting local game.");
                return;
            }
            txt_start_local_game.text = txt_const_close_local_game;
            GameLogic.StartLocalGame();
            btn_start_local_game.onClick.RemoveAllListeners();
            btn_start_local_game.onClick.AddListener(ButtonClose);
        }
        public void ButtonClose() {
            txt_start_local_game.text = txt_const_start_local_game;
            GameLogic.CloseLocalGame();
            btn_start_local_game.onClick.RemoveAllListeners();
            btn_start_local_game.onClick.AddListener(ButtonStart);
        }

        // Start is called before the first frame update
        void Start()
        {
            if (txt_start_local_game == null) {
                txt_start_local_game = btn_start_local_game.GetComponentInChildren<Text>();
            }
            if(delegate_start_game == null)
                AddStartFunction(VOID);
            if(delegate_close_game == null)
                AddCloseFunction(VOID);
            ButtonClose();
        }
        void VOID() { }
        // Update is called once per frame
        void Update()
        {

        }
        public void PressQuitGame()
        {
            Application.Quit();
        }
    }
}