using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GameDemo.EndEffector
{
    public class CalExpectedValue : MonoBehaviour
    {
        /// <summary>
        /// myCharacte脚本
        /// </summary>
        MyCharacter m_myCharacter;

        /// <summary>
        /// 物体的二维屏幕坐标
        /// </summary>
        private Vector2 m_screenPos;

        /// <summary>
        /// 模式切换
        /// </summary>
        public CalExpectedValueMode expectedValueMode;

        /// <summary>
        /// linear模式下的b值, 默认 
        /// </summary>
        public int b = 24000;

        /// <summary>
        /// linear模式下的k值
        /// </summary>
        [Range(1, 1000)] public int k;



        /// <summary>
        /// 更大的椭圆轨道长半径占屏幕的比例
        /// </summary>
        public float ratio;

        /// <summary>
        /// 更小的椭圆轨道长半径占屏幕的比例
        /// </summary>
        public float ratioLarger;

        /// <summary>
        /// 更大的椭圆轨道长半径
        /// </summary>
        private float radiusAMajor;

        /// <summary>
        /// 更大的椭圆轨道短半径
        /// </summary>
        private float radiusAMinor;

        /// <summary>
        /// 更小的椭圆轨道长半径
        /// </summary>
        private float radiusBMajor;

        /// <summary>
        /// 更小的椭圆短半径
        /// </summary>
        private float radiusBMinor;

        /// <summary>
        /// 屏幕空间的中心坐标，椭圆轨道的中心
        /// </summary>
        private Vector2 m_center;

        /// <summary>
        /// x的取值范围
        /// </summary>
        private float numberDigits = 1;

        private Vector2 m_lastPos;
        private Vector2 m_curPos;

        [Header("箱子k值")] public int k_Box;
        [Header("低阻力区域固定值(路)")] public float AreaLowDragValue;
        [Header("高阻力区域固定值(路)")] public float AreaHighDragValue;
        [Header("撞墙固定值")] public float FixedBoxValue = 100;

        // Start is called before the first frame update
        void Start()
        {
            m_lastPos = m_curPos = GetComponent<Transform>().position;
            m_screenPos = Camera.main.WorldToScreenPoint(this.transform.position);
            // 获取mycharacter组件
            m_myCharacter = GetComponent<MyCharacter>();
            if (m_myCharacter == null)
            {
                Debug.LogError("需要在prefab下添加MyCharacter脚本");
            }

            // 获取屏幕宽度和高度的一半
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float centerX = screenWidth / 2f;
            float centerY = screenHeight / 2f;
            // 计算屏幕的中心
            m_center = new Vector2(centerX, centerY);
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            //椭圆--老是算不对
            //radiusAMajor = Screen.width * 0.5f * ratioLarger;
            //radiusAMinor = Screen.width * 0.5f * ratioLarger / Camera.main.aspect;
            //radiusBMajor = Screen.width * 0.5f * ratio;
            //radiusBMinor = Screen.width * 0.5f * ratio / Camera.main.aspect;
            //那我改成圆，直接用半径算
            radiusAMajor = Screen.width * 0.5f * ratioLarger;
            radiusAMinor = radiusAMajor;
            radiusBMajor = Screen.width * 0.5f * ratio;
            radiusBMinor = radiusBMajor;
            m_screenPos = Camera.main.WorldToScreenPoint(this.transform.position);


            int num = Mathf.Abs(k); // 获取 x 的绝对值
            int count = 1;
            numberDigits = 1;
            while (num >= 10)
            {
                num = num / 10; // 将数除以10
                count++;
            }

            for (int i = 0; i < count; i++)
            {
                numberDigits *= 10;
            }


            switch (expectedValueMode)
            {
                // 常数模式下，输出一个恒定值b
                case CalExpectedValueMode.constantNumber:
                    ChangeEVFormat(b);
                    break;
                // linear模式下，在轨道外输出kx+b，在轨道内输出b
                case CalExpectedValueMode.LinearNumber:
                    // float number = GetKXPUlSBNew();
                    // ChangeEVFormat(number);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 当模式切换到linear mode模式, 计算期望值
        /// </summary>
        /// <returns></returns>
        //private float GetKXPUlSB()
        //{

        //    // 计算直线方程
        //    Vector2 direction = (m_center - m_screenPos).normalized;
        //    float slope = direction.y / direction.x;
        //    float intercept = m_center.y - m_center.x * slope;

        //    EllipseIntersection(m_center, radiusAMajor, radiusAMinor, slope, intercept, out var intersection1, out var intersection2);
        //    EllipseIntersection(m_center, radiusBMajor, radiusBMinor, slope, intercept, out var intersection3, out var intersection4);

        //    Vector2 targetIntersectionWithA = (Vector2.Distance(m_screenPos, intersection1) < Vector2.Distance(m_screenPos, intersection2) ? intersection1 : intersection2);
        //    Vector2 targetIntersectionWithB = (Vector2.Distance(m_screenPos, intersection3) < Vector2.Distance(m_screenPos, intersection4) ? intersection3 : intersection4);

        //    // 处理垂直与x轴的情况，此时斜率是正无穷
        //    if (slope > int.MaxValue-1000)
        //    {
        //        targetIntersectionWithA = (Vector2.Distance(m_screenPos, new Vector2(m_center.x, m_center.y + radiusAMinor)) < Vector2.Distance(m_screenPos, new Vector2(m_center.x, m_center.y - radiusAMinor)) ? new Vector2(m_center.x, m_center.y + radiusAMinor) : new Vector2(m_center.x, m_center.y - radiusAMinor));
        //        targetIntersectionWithB = (Vector2.Distance(m_screenPos, new Vector2(m_center.x, m_center.y + radiusBMinor)) < Vector2.Distance(m_screenPos, new Vector2(m_center.x, m_center.y - radiusBMinor)) ? new Vector2(m_center.x, m_center.y + radiusBMinor) : new Vector2(m_center.x, m_center.y - radiusBMinor));
        //    }

        //    bool checkifPointInsideEllipseB = CheckIfPointInEllipse(m_center, radiusBMajor, radiusBMinor , m_screenPos);
        //    bool checkifPointInsideEllipseA = CheckIfPointInEllipse(m_center, radiusAMajor, radiusAMinor , m_screenPos);

        //    // 轨迹内： 返回常数值
        //    if (!checkifPointInsideEllipseB && checkifPointInsideEllipseA)
        //    {
        //        return b;
        //    }

        //    // 轨迹外:外圈
        //    if(!checkifPointInsideEllipseA)
        //    {
        //        float distanceObjToA = Vector2.Distance(m_screenPos, targetIntersectionWithA);
        //        return k * distanceObjToA / ( (float)numberDigits * 0.05f) + b;
        //        //return k * distanceObjToA  + b;
        //    }

        //    // 轨迹外：内圈
        //    if(checkifPointInsideEllipseB)
        //    {
        //        float distanceObjToB = Vector2.Distance(m_screenPos, targetIntersectionWithB);
        //        return k * distanceObjToB / ( (float)numberDigits * 0.05f) + b;
        //        //return k * distanceObjToB  + b;
        //        Debug.Log("dis" + k * distanceObjToB / ((float)numberDigits * 2.0f));
        //    }

        //    return b;
        //}

        private float GetKXPUlSB()
        {
            // 计算直线方程
            //Vector2 direction = (m_center - m_screenPos).normalized;
            float dis_real = Vector2.Distance(m_screenPos, m_center);
            int min_k = 1000;

            //float slope = direction.y / direction.x;
            //float intercept = m_center.y - m_center.x * slope;

            //EllipseIntersection(m_center, radiusAMajor, radiusAMinor, slope, intercept, out var intersection1, out var intersection2);
            //EllipseIntersection(m_center, radiusBMajor, radiusBMinor, slope, intercept, out var intersection3, out var intersection4);


            //Vector2 targetIntersectionWithA = (Vector2.Distance(m_screenPos, intersection1) < Vector2.Distance(m_screenPos, intersection2) ? intersection1 : intersection2);
            //Vector2 targetIntersectionWithB = (Vector2.Distance(m_screenPos, intersection3) < Vector2.Distance(m_screenPos, intersection4) ? intersection3 : intersection4);

            //// 处理垂直与x轴的情况，此时斜率是正无穷
            //if (slope > int.MaxValue - 1000)
            //{
            //    targetIntersectionWithA = (Vector2.Distance(m_screenPos, new Vector2(m_center.x, m_center.y + radiusAMinor)) < Vector2.Distance(m_screenPos, new Vector2(m_center.x, m_center.y - radiusAMinor)) ? new Vector2(m_center.x, m_center.y + radiusAMinor) : new Vector2(m_center.x, m_center.y - radiusAMinor));
            //    targetIntersectionWithB = (Vector2.Distance(m_screenPos, new Vector2(m_center.x, m_center.y + radiusBMinor)) < Vector2.Distance(m_screenPos, new Vector2(m_center.x, m_center.y - radiusBMinor)) ? new Vector2(m_center.x, m_center.y + radiusBMinor) : new Vector2(m_center.x, m_center.y - radiusBMinor));
            //}


            //bool checkifPointInsideEllipseB = CheckIfPointInEllipse(m_center, radiusBMajor, radiusBMinor, m_screenPos);
            //bool checkifPointInsideEllipseA = CheckIfPointInEllipse(m_center, radiusAMajor, radiusAMinor, m_screenPos);


            // 轨迹内： 返回常数值
            if (dis_real > radiusBMajor && dis_real < radiusAMajor)
            {
                return b;
            }

            // 轨迹外:外圈
            if (dis_real >= radiusAMajor)
            {
                //return k * (dis_real - radiusAMajor) + b;
                float dc_kxb = (k * (dis_real - radiusAMajor) + b) <= 24000 - min_k
                    ? (k * (dis_real - radiusAMajor) + b)
                    : 24000 - min_k;
                return dc_kxb + min_k;
                //return k * distanceObjToA  + b;
            }

            // 轨迹外：内圈
            if (dis_real <= radiusBMajor)
            {
                //return k * ( radiusBMajor - dis_real) + b;
                float dc_kxb = (k * (radiusBMajor - dis_real) + b) <= 24000 - min_k
                    ? (k * (radiusBMajor - dis_real) + b)
                    : 24000 - min_k;
                return dc_kxb + min_k;
                //return k * distanceObjToB  + b;
                //Debug.Log("dis" + k * distanceObjToB / ((float)numberDigits * 2.0f));
            }

            return b;
        }

        /// <summary>
        /// 将值输出到myCharacter中，只输出最高四位
        /// </summary>
        /// <param name="result"></param>
        private void ChangeEVFormat(float resFloat)
        {
            float result = Mathf.RoundToInt(resFloat);
            int length = result.ToString().Length;
            string zeroString = "";
            while (length < 6)
            {
                zeroString += "0";
                length++;
            }

            string resString = zeroString + result.ToString();
            int highestFour = int.Parse(resString.Substring(0, 5));
            Debug.Log("highestFour: " + highestFour);

            m_myCharacter.target_current.x = highestFour;
        }


        /// <summary>
        /// 计算椭圆和直线的交点
        /// </summary>
        /// <param name="m_center"></param>
        /// <param name="radiusX"></param>
        /// <param name="radiusY"></param>
        /// <param name="slope"></param>
        /// <param name="intercept"></param>
        /// <param name="intersection1"></param>
        /// <param name="intersection2"></param>
        private void EllipseIntersection(Vector2 m_center, float radiusX, float radiusY, float slope, float intercept,
            out Vector2 intersection1, out Vector2 intersection2)
        {
            // 计算二次方程参数
            float a = radiusY * radiusY + radiusX * radiusX * slope * slope;
            float b = 2f * m_center.x * radiusX * radiusX * slope - 2f * intercept * radiusX * radiusY * slope -
                      2f * m_center.y * radiusY * radiusY;
            float c = m_center.x * m_center.x * radiusX * radiusX + intercept * intercept * radiusY * radiusY +
                m_center.y * m_center.y * radiusY * radiusY - radiusX * radiusX * radiusY * radiusY;

            // 计算交点
            float delta = b * b - 4f * a * c;
            if (delta < 0f)
            {
                Debug.Log("No intersection point found.");
                intersection1 = intersection2 = Vector2.zero;
                return;
            }

            float sqrtDelta = Mathf.Sqrt(delta);
            float x1 = (-b + sqrtDelta) / (2f * a);
            float x2 = (-b - sqrtDelta) / (2f * a);

            intersection1 = new Vector2(x1, slope * x1 + intercept);
            intersection2 = new Vector2(x2, slope * x2 + intercept);
        }


        /// <summary>
        /// 判断点是否在椭圆内
        /// </summary>
        /// <param name="center"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        private bool CheckIfPointInEllipse(Vector2 center, float width, float height, Vector2 point)
        {
            float value = Mathf.Pow(point.x - center.x, 2) / (width * width) +
                          (Mathf.Pow(point.y - center.y, 2) / (height * height));
            return (value <= 1) ? true : false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 
        /// </summary>
        // private void OnDrawGizmos()
        // {
        //     Gizmos.color = Color.black;
        //     Gizmos.DrawSphere(m_screenPos, 10.0f);
        //
        //     Gizmos.color = Color.red;
        //     DrawEllipseGizmos(m_center, radiusAMajor, radiusAMinor);
        //
        //     Gizmos.color = Color.blue;
        //     DrawEllipseGizmos(m_center, radiusBMajor, radiusBMinor);
        //
        // }

        /// <summary>
        ///  在局部坐标系中绘制椭圆
        /// </summary>
        /// <param name="m_center"></param>
        /// <param name="radiusX"></param>
        /// <param name="radiusY"></param>
        private void DrawEllipseGizmos(Vector2 m_center, float radiusX, float radiusY)
        {
            const int SEGMENT_COUNT = 60;

            float angleIncrement = 2f * Mathf.PI / SEGMENT_COUNT;
            Vector3[] points = new Vector3[SEGMENT_COUNT];

            for (int i = 0; i < SEGMENT_COUNT; i++)
            {
                float angle = i * angleIncrement;
                float x = m_center.x + Mathf.Cos(angle) * radiusX;
                float y = m_center.y + Mathf.Sin(angle) * radiusY;
                points[i] = new Vector3(x, y, 0f);
            }

            for (int i = 0; i < SEGMENT_COUNT - 1; i++)
            {
                Gizmos.DrawLine(points[i], points[i + 1]);
            }

            Gizmos.DrawLine(points[SEGMENT_COUNT - 1], points[0]);
        }


#endif
        public enum CalExpectedValueMode
        {
            constantNumber = 0,
            LinearNumber = 1
        }


        /// <summary>
        ///  在局部坐标系中绘制椭圆
        /// </summary>
        /// <param name="m_center"></param>
        /// <param name="radiusX"></param>
        /// <param name="radiusY"></param>
        private void DrawEllipseHandles(Vector2 m_center, float radiusX, float radiusY)
        {
            const int SEGMENT_COUNT = 60;

            float angleIncrement = 2f * Mathf.PI / SEGMENT_COUNT;
            Vector3[] points = new Vector3[SEGMENT_COUNT];

            for (int i = 0; i < SEGMENT_COUNT; i++)
            {
                float angle = i * angleIncrement;
                float x = m_center.x + Mathf.Cos(angle) * radiusX;
                float y = m_center.y + Mathf.Sin(angle) * radiusY;
                points[i] = new Vector3(x, y, 0f);
            }

            for (int i = 0; i < SEGMENT_COUNT - 1; i++)
            {
                Handles.DrawLine(points[i], points[i + 1]);
            }

            Handles.DrawLine(points[SEGMENT_COUNT - 1], points[0]);
        }

        private void OnGUI()
        {
            // Handles.color = Color.red;
            // DrawEllipseHandles(m_center, radiusAMajor, radiusAMinor);
            //
            // Handles.color = Color.blue;
            // DrawEllipseHandles(m_center, radiusBMajor, radiusBMinor);
        }


        private Collider AreaLowDragCollider;
        private Collider AreaHighDragCollider;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("AreaLowDrag"))
            {
                AreaLowDragCollider = other;
            }
            
            if (other.CompareTag("AreaHighDrag"))
            {
                AreaHighDragCollider = other;
            }
        }


        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("AreaLowDrag") && AreaLowDragCollider == other)
            {
                AreaLowDragCollider = null;
            }
            
            if (other.CompareTag("AreaHighDrag") && AreaHighDragCollider == other)
            {
                AreaHighDragCollider = null;
            }
        }

        private void LogDrag()
        {
            if(AreaLowDragCollider == null || AreaHighDragCollider == null) return;
            
            var areaLowDragClosestPos = Vector3.Distance(AreaLowDragCollider.transform.position, new Vector3(transform.position.x, 0, transform.position.z));
            var areaHighDragClosestPos = Vector3.Distance(AreaHighDragCollider.transform.position, new Vector3(transform.position.x, 0, transform.position.z));
            
            var total = areaLowDragClosestPos + areaHighDragClosestPos;
            
            var areaLowDragProportion = areaLowDragClosestPos / total;
            var areaHighDragProportion = areaHighDragClosestPos / total;


            ChangeEVFormat((AreaLowDragValue * areaLowDragProportion) + (AreaHighDragValue * areaHighDragProportion));
            // Debug.Log($"重叠区域值：{(AreaLowDragValue * areaLowDragProportion) + (AreaHighDragValue * areaHighDragProportion)}");
        }
        
        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("AreaLowDrag"))
            {
                ChangeEVFormat(AreaLowDragValue);
                // Debug.Log($"低阻力区域：{AreaLowDragValue}");
            }
            else if (other.CompareTag("AreaHighDrag"))
            {
                ChangeEVFormat(AreaHighDragValue);
                // Debug.Log($"高阻力区域：{AreaHighDragValue}");
            }

            LogDrag();
        }

        private void OnCollisionStay(Collision other)
        {
            if (other.gameObject.CompareTag("Box"))
            {
                ChangeEVFormat(Mathf.Abs(k_Box * other.impulse.x));
                // Debug.Log($"箱子碰撞值：{Mathf.Abs(k_Box * other.impulse.x)}");
            }
            else if (other.gameObject.CompareTag("FixedBox"))
            {
                ChangeEVFormat(FixedBoxValue);
                // Debug.Log($"撞墙值：" + FixedBoxValue);
            }
        }
    }
}