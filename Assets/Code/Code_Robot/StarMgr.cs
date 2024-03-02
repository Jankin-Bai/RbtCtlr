using UnityEngine;

namespace GameDemo
{
    public class StarMgr : MonoBehaviour
    {
        /// <summary>
        /// 生成星星
        /// </summary>
        public void GenerateStar()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(true);
            }
        }

        public int GetStarCount()
        {
            return transform.childCount;
        }
    }
}