using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TouchRobot.Logic;
using TouchRobot.UI;
namespace TouchRobot
{
    namespace ToGame
    {
        public class GameCharacterAttribute : MonoBehaviour
        {
            public static bool game_is_on => GameInterface.game_is_on;
            // GameCharacterAttribu是MonoBehaviour,访问方式不建议通过new实现
            // 分情况说哈,如果是设备在游戏内/外突然离线,那么RobotController在disconnect前会用静态方法GameInterface.Remove,清除GameController时,该gc调用RobotControllerRemoveAttribute(),销毁游戏对象.
            // >> 表单更新在静态方法实现,对象销毁由该类调用
            // 如果是正常的游戏结束,那么只销毁GameController对Attribute的引用,不销毁GameController本身

            // 2023/4/20 log: 新的想法是,将该类放在TouchRobot的命名空间里.但是这样做的后果是GameController显得很多余.
            //                因此如果要大改的话,建议彻底废除这个类,然后访问方式如下:GC直接作为游戏对象的数据访问
            // RobotController:new--->GameController<---GameObject.GetComponent<GameController>()
            //                然而现在是:GC进行必要的单位换算,#RC不一定等于#GC.凑合用吧,旧代码是面向这样的使用场景:不是所有链接的机器人都会加入游戏,那么就违背了之前的假设--见GameInterface的log
            //                所以说，最好采用上述而非下面的数据流,但是做都做了,并且keyboard现有代码强制占据两个robotcontroller,工作量太大,就凑合用吧
            // RobotController:new--->GameController<---Bind(GameCharacterAttribute)<---GameObject.GetComponent<GameCharacterAttribute>()
            // 由于上述设计,所有代码均可以放进TouchRobot的命名空间,游戏内容相关的对象主程序仅需要using TouchRobot.ToGame即可

            // 2023/5/6   log: 重新思考了一下,GameController是必要的.但是GameController由RobotController生成,不能继承自MonoBehaviour.
            //                 如果采用4/20log中的下方案(即目前方案),将MonoBehaviour子对象(即CharacterAttribute)的生成从RobotController代码中抽离,模块化了RobotController仅提供机器人类服务操作的功能属性
            //                 此时与游戏对象的对接由GameController管理,游戏内容的交互由其生成的CharacterAttribute管理,这是合理的
            // 综上,截至本日,工作流如下:
            // RobotController:new--->GameController---->Instantiate<CharacterAttribute>()
            // 机器人沟通层代码已折叠
            #region
            delegate void my_delegate(bool state);
            my_delegate delete_relation;                        // 不要保存GameController的实例,但是保留其删除CharacterAttribute的委托
            internal void AddDeletionDelegate(GameController gc)
            {
                delete_relation = gc.InterfaceRemoveAttribute;
            } // 优雅一点的时候,退出一局时,从GameLogic访问所有gca,令gca从它们对应的gamecontroller眼里消失.虽然也可以直接调用gameinterface的遍历就是了
            internal void GameControllerStripeAttribute()
            {
                delete_relation(false);
                GamerQuit();
            }    // 执行委托,其目的是在游戏正常退出时销毁自身与游戏对象.不同于下面的代码,该步骤同时将对应的GameController的绑定引用清除,不销毁GameController本身.
            internal void RobotControllerRemoveAttribute()
            {
                GameLogic.RemoveAttribute(this);
                GamerQuit();
            }   // 当RobotController离线前,命令其GameController调用GameInterface方法对其list清除.由于该方法是在RobotController离线时执行的,GameController本身会被销毁.
            void GamerQuit()
            {
                Destroy(gameObject);
                Destroy(this);
                MessageManager.to_string(MessageManager.info_type.notice, "Game object " + name + " is removed.");
            }                               // 销毁自身的游戏对象与自身
            #endregion
            // 游戏内容关联代码
            public standardlized_data rendered_data { get; internal set; } // 游戏单位的量
            internal void DataRead(standardlized_data sd) {
                standardlized_data received_data = sd;
                // 如果存在标准单位和游戏单位的换算,那么请写在这里
                // 如果存在标准单位和游戏单位的换算,那么请写在这里
                // 如果存在标准单位和游戏单位的换算,那么请写在这里
                // 如果存在标准单位和游戏单位的换算,那么请写在这里
                // 如果存在标准单位和游戏单位的换算,那么请写在这里
                rendered_data = received_data;
            }   // 从GameController读取电机参数
            internal standardlized_data output_data 
            {
                get 
                {
                    standardlized_data tp_data = desired_data;
                    // 如果存在标准单位和游戏单位的换算,那么请写在这里
                    // 如果存在标准单位和游戏单位的换算,那么请写在这里
                    // 如果存在标准单位和游戏单位的换算,那么请写在这里
                    // 如果存在标准单位和游戏单位的换算,那么请写在这里
                    // 如果存在标准单位和游戏单位的换算,那么请写在这里
                    return tp_data;
                }
            }         // 单位标准化的控制量
            private standardlized_data desired_data;            // 游戏里计算得到的控制量
            public void CalculateDesires(control_mode cm, Vector3 val) 
            {
                desired_data.control_mode = cm;
                desired_data.Control(val);
                //print(val.x);
            }
            private void Start()
            {
                desired_data.control_mode = control_mode.current;
            }
        }
    }
}