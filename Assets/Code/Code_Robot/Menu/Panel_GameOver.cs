using GameDemo;
using TouchRobot.Logic;
using TouchRobot.ToGame;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TouchRobot.UI
{
    public class Panel_GameOver : MonoBehaviour
    {
        private UIMgr UIMgr;
        private StarMgr StarMgr;
        private GameUI GameUI;
        private BoxMgr BoxMgr;

        private void Awake()
        {
            UIMgr = GameObject.Find("Canvas_Assessment").GetComponent<UIMgr>();
            StarMgr = GameObject.Find("Star").GetComponent<StarMgr>();
            GameUI = GameObject.Find("GameRelated").GetComponent<GameUI>();
            BoxMgr = GameObject.Find("Boxs").GetComponent<BoxMgr>();

            

            transform.Find("BtnReset").GetComponent<Button>().onClick.AddListener(() =>
            {
                BoxMgr.ResetBox();
                GameUI.ButtonClose();
                UIMgr.Panel_Star.UpdateUI();
                StarMgr.GenerateStar();
                gameObject.SetActive(false);
            });
            gameObject.SetActive(false);
        }
    }
}