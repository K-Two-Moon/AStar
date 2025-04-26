using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using JKFrame;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace 后台线程计算
{
    public class GameLoop : SingletonMono<GameLoop>
    {
        [Range(1, 100)]
        [Header("地图大小")]
        [SerializeField]
        int width, height;

        Transform start, end;
        public Transform[,] array;
        Transform cubeParent;

        Task<(List<AStarNode>, List<AStarNode>)?> pathTask;
        void Start()
        {
            cubeParent = new GameObject("CubeParent").transform;
            cubeParent.position = Vector3.zero;

            array = new Transform[width, height];
            AStarManager.Instance.Initialize(width, height);
            foreach (var node in AStarManager.Instance.nodeArray)
            {
                Transform cube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                cube.SetParent(cubeParent);
                cube.position = new Vector3(node.x, node.y, 0);
                array[node.x, node.y] = cube;
                if (node.type == AStarNodeType.Stop)
                {
                    cube.GetComponent<Renderer>().material.color = Color.red;
                }
            }
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (start == null && end == null)
                    {
                        foreach (var node in AStarManager.Instance.nodeArray)
                        {
                            if (node.type == AStarNodeType.Walk)
                            {
                                array[node.x, node.y].GetComponent<Renderer>().material.color = Color.white;
                            }
                            else
                            {
                                array[node.x, node.y].GetComponent<Renderer>().material.color = Color.red;
                            }
                        }

                        start = hit.transform;
                        start.GetComponent<Renderer>().material.color = Color.yellow;
                    }
                    else
                    {
                        end = hit.transform;
                        if (start == end)
                        {
                            end = null;
                            Debug.Log("起点和终点不能相同");
                            return;
                        }

                        end.GetComponent<Renderer>().material.color = Color.blue;
                        ExecutePathTaskAsync();


                    }
                }
            }
        }

        //执行寻路任务
        async void ExecutePathTaskAsync()
        {
            //开始计时
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            //异步方法，使用工作线程
            pathTask = AStarManager.Instance.FindPathAsync(start.position, end.position);
            //获取寻路结果
            await pathTask;
            if (pathTask.Result.HasValue)
            {
                List<AStarNode> pathList = pathTask.Result.Value.Item1;
                List<AStarNode> visited = pathTask.Result.Value.Item2;

                foreach (var node in visited)
                {
                    array[node.x, node.y].GetComponent<Renderer>().material.color = Color.black;
                }

                foreach (var node in pathList)
                {
                    array[node.x, node.y].GetComponent<Renderer>().material.color = Color.green;
                }
                //计算时间
                stopwatch.Stop();
                Debug.Log($"同步寻路耗时: {stopwatch.ElapsedMilliseconds} ms");
                Debug.Log($"遍历节点数: {pathTask.Result.Value.Item2.Count}");
                Debug.Log($"性能指数: {stopwatch.ElapsedMilliseconds * 10000 / pathTask.Result.Value.Item2.Count} ,值越小性能越高");
            }

            start = null;
            end = null;
        }
    }
}