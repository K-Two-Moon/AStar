using System.Collections.Generic;
using System.Diagnostics;
using JKFrame;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace 主线程计算
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

        List<AStarNode> pathList;

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (start == null)
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
                        start.GetComponent<Renderer>().material.color = Color.green;

                        if (pathList != null)
                        {
                            foreach (var node in pathList)
                            {
                                Transform cube = array[node.x, node.y];
                                cube.GetComponent<Renderer>().material.color = Color.white;
                            }
                        }
                    }
                    else
                    {
                        end = hit.transform;
                        if (start == end)
                        {
                            end = null;
                            JKLog.Warning("起点和终点不能相同");
                            return;
                        }

                        end.GetComponent<Renderer>().material.color = Color.blue;

                        //计算寻路消耗时间
                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();
                        pathList = AStarManager.Instance.FindPath(start.position, end.position);
                        stopwatch.Stop();
                        Debug.Log($"同步寻路耗时: {stopwatch.ElapsedMilliseconds} ms");
                        Debug.Log($"遍历节点数: {AStarManager.Instance.traversedNodeCount}");
                        Debug.Log($"性能指数: {stopwatch.ElapsedMilliseconds * 10000 / AStarManager.Instance.traversedNodeCount} ,值越小性能越高");

                        if (pathList == null)
                        {
                            Debug.Log("路径不存在");
                            start = null;
                            return;
                        }

                        foreach (var node in pathList)
                        {
                            Transform cube = array[node.x, node.y];
                            cube.GetComponent<Renderer>().material.color = Color.yellow;
                        }

                        start = null;
                    }
                }
            }
        }
    }
}