using System.Diagnostics;
using JKFrame;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;

namespace 小地图_Job_Acync
{
    public class GameLoop : SingletonMono<GameLoop>
    {
        [Range(1, 100)]
        [Header("地图大小")]
        [SerializeField]
        int width, height;
        [Header("相机高度")]
        [Range(10, 100)]
        [SerializeField]
        float cameraHeight;
        Transform start, end;
        public Transform[,] array;
        Transform cubeParent;

        //作业句柄
        JobHandle? jobHandle;
        void Start()
        {
            cubeParent = new GameObject("CubeParent").transform;
            cubeParent.position = Vector3.zero;

            array = new Transform[width, height];
            AStarManager.Instance.Initialize(width, height);
            //初始化小地图
            MinMapController.Instance.Initialize(AStarManager.Instance.nodeArrayNative, width, height);
            //初始化地图显示
            foreach (var node in AStarManager.Instance.nodeArrayNative)
            {
                Transform cube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                cube.SetParent(cubeParent);
                cube.position = new Vector3(node.x, 0, node.y);
                array[node.x, node.y] = cube;
                if (node.type == AStarNodeType.Stop)
                {
                    cube.GetComponent<Renderer>().material.color = Color.red;
                    cube.localPosition = new Vector3(node.x, 1, node.y);
                }
            }
            //玩家初始化
            //随机一个节点传入,但必须是可行节点
            AStarNode startNode;
            do
            {
                int startIndex = Random.Range(0, AStarManager.Instance.nodeArrayNative.Length);
                startNode = AStarManager.Instance.nodeArrayNative[startIndex];
            } while (startNode.type == AStarNodeType.Stop);
            PlayerController.Instance.Initialize(startNode);
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (end == null)
                    {
                        start = array[(int)PlayerController.Instance.transform.position.x, (int)PlayerController.Instance.transform.position.z];

                        foreach (var node in AStarManager.Instance.nodeArrayNative)
                        {
                            if (node.type == AStarNodeType.Walk)
                            {
                                array[node.x, node.y].GetComponent<Renderer>().material.color = Color.white;
                                array[node.x, node.y].localPosition = new Vector3(node.x, 0, node.y);
                            }
                            else
                            {
                                array[node.x, node.y].GetComponent<Renderer>().material.color = Color.red;
                                array[node.x, node.y].localPosition = new Vector3(node.x, 1, node.y);
                            }
                        }

                        end = hit.transform;
                        end.GetComponent<Renderer>().material.color = Color.blue;
                        ExecutePathTaskAsync();
                    }
                }
            }


            PlayerController.Instance.Update();
        }

        void LateUpdate()
        {
            MinMapController.Instance.LateUpdate();
            PlayerController.Instance.LateUpdate(cameraHeight);
        }

        /// <summary>
        /// 传给玩家的路径
        /// </summary>
        List<Vector2> v2List = new List<Vector2>();

        //执行寻路任务
        async void ExecutePathTaskAsync()
        {
            //开始计时
            // Stopwatch stopwatch = new Stopwatch();
            // stopwatch.Start();
            //异步方法，使用工作线程
            jobHandle = AStarManager.Instance.FindPathAsync(start.position, end.position);

            if (jobHandle != null)
            {
                await UniTask.WaitUntil(() => jobHandle.Value.IsCompleted);
                //即使 handle.IsCompleted == true，你依然 必须手动调用 handle.Complete()，
                // 否则 Unity 不会清除它的内存安全锁（SafetyHandle），你访问 NativeList/NativeArray 会依旧抛错。
                jobHandle.Value.Complete();
                NativeList<int> pathList = AStarManager.Instance.pathList;
                NativeList<int> visited = AStarManager.Instance.visited;

                NativeArray<AStarNode> nodeArrayNative = AStarManager.Instance.nodeArrayNative;
                foreach (var node in visited)
                {
                    int x = nodeArrayNative[node].x;
                    int y = nodeArrayNative[node].y;
                    array[x, y].GetComponent<Renderer>().material.color = Color.black;
                }


                v2List.Clear();
                foreach (var node in pathList)
                {
                    int x = nodeArrayNative[node].x;
                    int y = nodeArrayNative[node].y;
                    array[x, y].GetComponent<Renderer>().material.color = Color.green;
                    v2List.Add(new Vector2(x, y));
                }

                PlayerController.Instance.Move(v2List);
            }
            //获取寻路结果


            end = null;
        }

        void OnDestroy()
        {
            AStarManager.Instance.Destroy();
        }
    }
}

