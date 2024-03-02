using UnityEngine;

namespace TouchRobot.UI
{
    public class BoxMgr : MonoBehaviour
    {
        private Box[] _Boxs;

        private void Awake()
        {
            _Boxs = new Box[transform.childCount];
            _Boxs = transform.GetComponentsInChildren<Box>();
        }

        public void ResetBox()
        {
            if (_Boxs != null && _Boxs.Length > 0)
            {
                foreach (var box in _Boxs)
                {
                    box.ResetBox();
                }
            }
        }
    }
}