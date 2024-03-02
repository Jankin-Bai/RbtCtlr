using System;
using UnityEngine;

namespace TouchRobot.UI
{
    public class UIMgr : MonoBehaviour
    {
        public Panel_Star Panel_Star;
        public  Panel_GameOver Panel_GameOver;

        private void Awake()
        {
            Panel_Star = this.transform.Find("Panel_Star").GetComponent<Panel_Star>();
            Panel_GameOver = this.transform.Find("Panel_GameOver").GetComponent<Panel_GameOver>();
        }
    }
}