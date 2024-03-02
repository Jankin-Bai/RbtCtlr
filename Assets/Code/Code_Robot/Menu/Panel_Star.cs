using System;
using UnityEngine;
using UnityEngine.UI;

namespace TouchRobot.UI
{
    public class Panel_Star : MonoBehaviour
    {
        private Text TxtStarCount;

        private void Awake()
        {
            TxtStarCount = transform.Find("TxtStarCount").GetComponent<Text>();
            UpdateUI();
        }

        public void UpdateUI(int starCount = 0)
        {
            if(starCount < 0) starCount = 0;
            TxtStarCount.text = starCount.ToString();
        }
    }
}