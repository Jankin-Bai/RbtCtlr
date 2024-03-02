using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TouchRobot.ToGame;
using TouchRobot.UI;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;

namespace GameDemo.EndEffector
{
    public enum EDirection
    {
        None = 0,
        Left = 1,
        Right = 2,
        Up = 3,
        Down = 4,
    }

    delegate void game_delegate(Vector3 val, TouchRobot.control_mode cm);            // 关联TouchRobot的委托,用以执行力反馈的计算与传递
    class MyCharacter : MonoBehaviour
    {
        [SerializeField] public Vector3 target_current;
        private game_delegate calculator;                   // 用于执行计算的委托
        private GameCharacterAttribute my_attribute;        // 关联TouchRobot相关代码
        private Vector3 robot_position => my_attribute == null ? Vector3.zero : new Vector3( -my_attribute.rendered_data.position.x,0, -my_attribute.rendered_data.position.y);
        private Vector3 robot_velocity => my_attribute == null ? Vector3.zero : my_attribute.rendered_data.velocity;
        private Vector3 robot_force => my_attribute == null ? Vector3.zero : my_attribute.rendered_data.force;
        private bool robot_button => my_attribute == null ? false : my_attribute.rendered_data.pressed;
      
        //刚体
        private Rigidbody rb;
        private SphereCollider cc;
        
        [Header("移动速度")]public float Speed = 550f;
        [Header("推箱子速度")]public float SokobanSpeed = 10f;
        
        
        /// <summary>
        /// 星星数量
        /// </summary>
        public int StarCount = 0;

        
        public EDirection currentDir = EDirection.None;

        private UIMgr UIMgr;
        private StarMgr Star;
        
        void Start()
        {
            my_attribute = GetComponent<GameCharacterAttribute>();
            SetCalculation(true);
            rb = this.GetComponent<Rigidbody>();
            cc = this.GetComponent<SphereCollider>();
            
            UIMgr = GameObject.Find("Canvas_Assessment").GetComponent<UIMgr>();
            Star = GameObject.Find("Star").GetComponent<StarMgr>();
            UIMgr.Panel_Star.UpdateUI(StarCount);
            _CanMove = true;

        }

        private void OnEnable()
        {
            StarCount = 0;
            GameObject.Find("Canvas_Assessment").GetComponent<UIMgr>().Panel_Star.UpdateUI(StarCount);
            GameObject.Find("Star").GetComponent<StarMgr>().GenerateStar();

            transform.position = new Vector3(0, 0.49f, 0);
            _CanMove = true;
        }

        // 如果没有力输出,或者需要忽略力输出时,请使用SetCalculation(false);
        // 无需理会Empty和Calculate
        internal void SetCalculation(bool state = true) 
        {
            if (state)
                calculator = Calculate;
            else
            {
                Calculate(Vector3.zero);
                calculator = Empty;
            }
        }
        private void Empty(Vector3 val, TouchRobot.control_mode cm)
        {
            my_attribute.CalculateDesires(cm, Vector3.zero);
        }
        private void Calculate(Vector3 val, TouchRobot.control_mode cm = TouchRobot.control_mode.current) 
        {
            my_attribute.CalculateDesires(TouchRobot.control_mode.current, val);
        }
        
        private bool _CanMove = true;

        private void FixedUpdate()
        {
            currentDir = GetDir(robot_position - transform.position);
            
            if (_CanMove)
            {
                transform.position = robot_position + new Vector3(0, 0.49f, 0);
            }
           
            calculator(target_current, TouchRobot.control_mode.current);
            
            
            //判断游戏结束
            if (StarCount >= Star.GetStarCount() && !UIMgr.Panel_GameOver.gameObject.activeInHierarchy)
            {
                UIMgr.Panel_GameOver.gameObject.SetActive(true);
                //结束游戏
                Debug.Log("游戏结束");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Star"))
            {
                StarCount++;
                UIMgr.Panel_Star.UpdateUI(StarCount);
                other.gameObject.SetActive(false);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("FixedBox"))
            {
                var dir = GetDir(other.transform.position - transform.position);

                _CanMove = currentDir != dir;
            }
        
            if (other.CompareTag("Box"))
            {
                Rigidbody boxRigidbody = other.gameObject.GetComponent<Rigidbody>();
                Vector3 pushDirection = (other.transform.position-transform.position).normalized; // 可以根据需要修改推动方向
                boxRigidbody.MovePosition(boxRigidbody.position + pushDirection * SokobanSpeed * Time.deltaTime);
            }
        }

        private EDirection GetDir(Vector3 normalized)
        {
            //获取方向
            if (normalized.x > 0)
            {
                return EDirection.Right;
            }
            else if (normalized.x < 0)
            {
                return EDirection.Left;
            }
            else if (normalized.y > 0)
            {
                return EDirection.Up;
            }
            else if (normalized.y < 0)
            {
                return EDirection.Down;
            }
            else
            {
                return EDirection.None;
            }
        }
    }
}