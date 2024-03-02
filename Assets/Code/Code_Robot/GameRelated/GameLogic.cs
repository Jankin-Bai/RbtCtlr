using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TouchRobot.ToGame;
using System.Threading;
using System.Threading.Tasks;
using TouchRobot.UI;

namespace TouchRobot.Logic
{
    // 2023/4/19 log: 目前还是单机模式啊,虽然继承自NetworkBehaviour,但是实际的流程还是先按单机逻辑来写的.
    //                缺项:游戏流程中网络对象的销毁,转移.
    // 2023/4/20 log: 有一说一,这个类做的工作都是与游戏内容本身无关的辅助功能,建议把实际游戏逻辑另外新建一个类
    sealed class GameLogic : MonoBehaviour
    {
        [SerializeField] GameObject[] _player_avatar;                                               // 在编辑器中选定角色的形状
        [SerializeField] Vector3 position_instantiate;
        static Vector3 position_instantiate_;
        static GameObject[] player_avatar;                                                          // 玩家的形状
        internal static List<GameCharacterAttribute> list_local_player;                                      // 游戏角色特征
        internal static int count_local_player => list_local_player == null ? 0 : list_local_player.Count;   // 游戏角色特征
        internal static void AssignAvatar(GameObject[] go, Vector3 position) 
        {
            player_avatar = go;
            position_instantiate_ = position;
        }
        internal static void RemoveAttribute(GameCharacterAttribute gca)
        {
            if (list_local_player != null)
            {
                list_local_player.Remove(gca);
            }
        }
        internal static void StartLocalGame()
        {
            try
            {
                if (GeneratePrefabs())
                {
                    GameInterface.StartTransmission();
                    DataRecorder.GameInterfaceCallReRecord();
                    MessageManager.to_string(MessageManager.info_type.correct, "Local game is initiated.");
                    GameUI.delegate_start_game();
                }
                else
                {
                    RemoveAllLocalPlayers();
                }
            }
            catch (System.Exception ex)
            {
                MessageManager.to_string(MessageManager.info_type.unknown, ex.ToString());
                GameInterface.CloseTransmission();
                RemoveAllLocalPlayers();
            }
        }
        // 游戏的开始的相关程序是在GameLogic中执行的,部分游戏配置参数在GameInterface中管理.
        // GeneratePrefabs:开始游戏意味着先生成游戏对象,随后赋予GameCharacterAttribute;GameInterface.StartGames:开始数据流.
        // 如果第一步出现错误,那么清除所有已经生成的本地游戏对象
        internal static void CloseLocalGame()
        {
            GameInterface.CloseTransmission();
            RemoveAllLocalPlayers();
            GameUI.delegate_close_game();
        }
        // 正常的游戏关闭.仅清除GameController对Attribute的引用,并清除场景中的游戏对象.
        // 联机的情况等重新研究了Mirror再说
        // 这俩时静态方法,但是落实到UI操作,需要别的方法
        internal static bool GeneratePrefabs(int[] order = null)
        {
            if (order == null)
                order = new int[1];
            if (player_avatar == null)
            {
                MessageManager.to_string(MessageManager.info_type.unknown, "The reference of target avatar has not been assigned.");
                return false;
            }
            else
            {
                bool fine = false;
                foreach (GameObject go in player_avatar)
                {
                    if (go != null)
                    {
                        fine = true;
                        break;
                    }
                }
                if (!fine)
                {
                    MessageManager.to_string(MessageManager.info_type.unknown, "The reference of target avatar has not been assigned.");
                    return false;
                }
            }
            for (int i = 0; i < GameInterface.count_players; i++)
            {
                GameCharacterAttribute gca = GeneratePrefab(Vector3.left * i + position_instantiate_, Quaternion.identity, order[i % order.Length], "local_player_" + i.ToString());
                GameInterface.list_online_game[i].GameAssignCharacterAttribute(gca);
                list_local_player.Add(gca);
            }
            return true;
        }                // 照着GameInterface里的list<GameController>去生成指定数量的gameCharacterAttribute对象,生成对象的形状是player_avatar按照order逐个生成
        static GameCharacterAttribute GeneratePrefab(Vector3 _position = new Vector3(), Quaternion _rotation = new Quaternion(), int object_id = 0, string name = "")
        {
            GameObject go = Instantiate(player_avatar[object_id], _position, new Quaternion());
            go.name = name;
            return go.AddComponent<GameCharacterAttribute>();
        }
        // 生成单个游戏对象并赋予GameCharacterAttribute属性.对对应的GameController赋值在GeneratePrefabs(order)中实现
        static void RemoveAllLocalPlayers()
        {
            if (list_local_player == null)
            {
                list_local_player = new List<GameCharacterAttribute>();
                return;
            }
            foreach (GameCharacterAttribute gca in list_local_player)
            {
                gca.GameControllerStripeAttribute();
            }
            list_local_player = new List<GameCharacterAttribute>();
            GameInterface.SetGame(false);
        }                                 // 功能上,清空Attribute的表单,随后销毁所有游戏对象,并消除他们对应的GameController对他们的引用
        private void Start()
        {
            OnValidate();
        }
        private void OnValidate()
        {
            // 后续将会把辅助由别的类实现,故当非空时不赋值
            if (player_avatar == null)
            {
                player_avatar = _player_avatar;
            }
            position_instantiate_ = position_instantiate;
        }                                            // 在编辑器里修改一次就生效,干的事情是把非静态字段赋值到静态量
    }
}