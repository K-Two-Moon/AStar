using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JKFrame;
using UnityEngine;

namespace 小地图_Job_Acync
{
    public class PlayerController : Singleton<PlayerController>
    {
        // === 配置项 ===
        private float moveSpeed = 5f;           // 单位：米/秒
        private float arriveThreshold = 0.1f;   // 到达坐标的判定距离


        private List<Vector2> pathPoints;

        internal GameObject gameObject;
        internal Transform transform;
        internal void Initialize(AStarNode startNode)
        {
            gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            transform = gameObject.transform;
            gameObject.GetComponent<Renderer>().material.color = Color.yellow;

            transform.position = new Vector3(startNode.x, 1, startNode.y);
        }

        internal void Update()
        {
            if (pathPoints == null) return;
            if (pathPoints?.Count == 0) return;

            Vector3 targetPos = new Vector3(pathPoints[pathPoints.Count - 1].x, transform.position.y, pathPoints[pathPoints.Count - 1].y);

            //旋转朝向目标点
            Vector3 dir = targetPos - transform.position;
            Quaternion lookRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 5f * Time.deltaTime);

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPos) < arriveThreshold)
            {
                pathPoints.RemoveAt(pathPoints.Count - 1); // 路径点已经到达，删除
            }


        }

        internal void LateUpdate(float cameraHeight)
        {
            Camera.main.transform.position = transform.position + Vector3.up * cameraHeight;
            Camera.main.transform.LookAt(transform);

        }


        CancellationTokenSource moveCTS;
        internal void Move(List<Vector2> pathList)
        {
            // 取消上一次任务
            moveCTS?.Cancel();
            moveCTS = new CancellationTokenSource();

            this.pathPoints = pathList; // 寻路完成就会告诉我路径  我想让游戏对象按路径移动
            if (pathPoints.Count == 0) return;
            pathPoints.RemoveAt(pathList.Count - 1); // 删除最后一个点，因为它是目标点，已经到达了
        }
    }

}
