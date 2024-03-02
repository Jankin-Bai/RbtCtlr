using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TouchRobot.ToGame;

namespace GameDemo
{
    public class GameManager : MonoBehaviour
    {
        // 游戏角色的形象,如果未配置,则启用默认形象.默认形象需要在Canvas_Assessment>>GameRelated下修改
        [SerializeField] GameObject[] avatar_list;
        // 角色初始化位置,如果未配置角色形象,则不生效.默认形象需要在Canvas_Assessment>>GameRelated下修改
        [SerializeField] Vector3 initialize_position;

        // 如果想访问游戏内任意玩家成员,请使用数组.如果偷懒,可以使用如下方法访问
        //List<GameCharacterAttribute> list_local_player => 
        private void Start()
        {
            OnValidate();
        }
        private void OnValidate()
        {
            AssignButtonFunction();
        }
        // 下面的代码将修改按下开始/结束游戏按钮的功能,建议不动
        void AssignButtonFunction()
        {
            TouchRobot.button_delegate ok;
            ok = CreateGameObject;
            GameUI.AddStartFunction(ok);
            ok = RemoveGameObject;
            GameUI.AddCloseFunction(ok);
            GameUI.AssignAvatar(avatar_list, initialize_position);
        }
        // 按下按钮时将生成游戏角色之外的物体,或加载场景,实现定义如下
        void CreateGameObject()
        {

        }
        // 按下按钮时将销毁游戏角色之外的物体,或卸载场景,实现定义如下
        void RemoveGameObject()
        {

        }
    }
}