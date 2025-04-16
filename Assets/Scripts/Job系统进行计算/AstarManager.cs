using System.Collections.Generic;
using JKFrame;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace Job系统进行计算
{
    [BurstCompile]
    public class AstarManager : Singleton<AstarManager>
    {
        /// <summary>
        /// 节点的边长
        /// </summary>
        float lengthOfSide = 1;

        /// <summary>
        /// 地图宽度
        /// </summary>
        int mapWidth;

        /// <summary>
        /// 地图高度
        /// </summary>
        int mapHeight;

        /// <summary>
        /// 地图节点数组
        /// </summary>
        //public AStarNode[,] nodeArray;
        public NativeArray<AStarNode> nodeArrayNative;

        /// <summary>
        /// 开放列表
        /// </summary>
        NativeList<int> openList = new NativeList<int>(Allocator.Persistent);

        /// <summary>
        /// 关闭列表
        /// </summary>
        NativeList<int> closeList = new NativeList<int>(Allocator.Persistent);

        /// <summary>
        /// 对查找过的格子进行变色
        /// 直观查看寻路消耗
        /// </summary>
        NativeList<int> visited = new NativeList<int>(10000, Allocator.Persistent);
        /// <summary>
        /// 最终路径的列表
        /// </summary>
        NativeList<int> pathList = new NativeList<int>(10000, Allocator.Persistent);

        [BurstCompile]
        /// <summary>
        /// 初始化地图信息
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        public void Initialize(int width, int height)
        {
            mapWidth = width;
            mapHeight = height;
            AStarNode.mapWidth = width;
            AStarNode.mapHeight = height;

            //根据宽高创建地图节点二维数组
            nodeArrayNative = new NativeArray<AStarNode>(width * height, Allocator.Persistent);
            //nodeArray = new AStarNode[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    AStarNode node = new AStarNode(x, y,
                        Random.Range(0, 100) < 50 ? AStarNodeType.Stop : AStarNodeType.Walk);
                    nodeArrayNative[x + y * width] = node;
                }
            }
        }

        public (NativeList<int>, NativeList<int>)? FindPathAsync(Vector3 startPos, Vector3 endPos)
        {
            // int count = 0;
            //首先要判断 传入的坐标是否合法
            //1.要在地图范围你
            //算出该坐标在哪一个格子节点中
            int startX = (int)(startPos.x / lengthOfSide);
            int startY = (int)(startPos.y / lengthOfSide);
            //判断是否超出二维数组索引
            int endX = (int)(endPos.x / lengthOfSide);
            int endY = (int)(endPos.y / lengthOfSide);
            if (startX < 0 || startY < 0
                           || startX >= mapWidth || startY >= mapHeight
                           || endX < 0 || endY < 0
                           || endX >= mapWidth || endY >= mapHeight)
            {
                Debug.Log("传入的坐标不合法,超出地图范围");
                return null;
            }

            //如果没有超出获取对应的节点
            //如果不合法 直接返回null 意味着不能寻路
            //如果合法 应该先找到起点和终点坐标对应的格子节点
            int startNodeIndex = startX + startY * mapWidth;
            int endNodeIndex = endX + endY * mapWidth;
            //2.不要是阻挡地图
            //判断起点和终点是否是阻挡地图
            if (nodeArrayNative[startNodeIndex].type == AStarNodeType.Stop || nodeArrayNative[endNodeIndex].type == AStarNodeType.Stop)
            {
                Debug.Log("传入的坐标不合法,起点或者终点是阻挡地图");
                return null;
            }


            //3.开始寻路
            //清空开启列表和关闭列表
            openList.Clear();
            closeList.Clear();
            pathList.Clear();
            visited.Clear();

            SetFatherIndex(startNodeIndex, -1);

            AStarNode startNode = nodeArrayNative[startNodeIndex];
            startNode.g = 0;
            startNode.h = 0;
            nodeArrayNative[startNodeIndex] = startNode;

            //把开始点放入关闭列表
            closeList.Add(startNodeIndex);

            //创建作业寻路
            AstarJob astarJob = new AstarJob
            {
                nodeArray = nodeArrayNative,
                startNodeIndex = startNodeIndex,
                endNodeIndex = endNodeIndex,
                mapWidth = mapWidth,
                mapHeight = mapHeight,
                openList = openList,
                closeList = closeList,
                pathList = pathList,
                visited = visited
            };
            JobHandle jobHandle = astarJob.Schedule();
            jobHandle.Complete();

            return (pathList, visited);
        }

        /// <summary>
        /// 设置父节点索引
        /// </summary>
        /// <param name="index">当前节点索引</param>
        /// <param name="fatherIndex">父节点索引</param>
        void SetFatherIndex(int index, int fatherIndex)
        {
            var node = nodeArrayNative[index];
            node.fatherIndex = fatherIndex;
            nodeArrayNative[index] = node;
        }

        public void Destroy()
        {
            nodeArrayNative.Dispose();
            openList.Dispose();
            closeList.Dispose();
            visited.Dispose();
            pathList.Dispose();
        }

        [BurstCompile]
        /// <summary>
        /// 寻路Job
        /// </summary>
        struct AstarJob : IJob
        {
            public NativeArray<AStarNode> nodeArray;
            public int startNodeIndex;
            public int endNodeIndex;
            public int mapWidth;
            public int mapHeight;

            public NativeList<int> openList;
            public NativeList<int> closeList;
            public NativeList<int> pathList;
            public NativeList<int> visited;

            [BurstCompile]
            public void Execute()
            {
                AStarNode endNode = nodeArray[endNodeIndex];
                do
                {
                    //从起点开始 找周围的点 并放入开启列表中
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            // 排除自身
                            if (x + y * mapWidth == startNodeIndex) continue;
                            AStarNode startNode = nodeArray[startNodeIndex];
                            int checkX = startNode.x + x;
                            int checkY = startNode.y + y;

                            if (x == 0 || y == 0)
                            {
                                FindNearlyNodeToOpenList(checkX, checkY, 1, startNodeIndex, endNode);
                            }
                            else
                            {
                                FindNearlyNodeToOpenList(checkX, checkY, 1.4f, startNodeIndex, endNode);
                            }

                        }
                    }
                    //判断这些点 是否是边界 是否是阻挡 是否在开启或者关闭列表 如果都不是 才放入开启列表

                    //选出开启列表中 寻路消耗最小的点
                    openList.Sort(new DescendingIntComparer
                    {
                        nodeArray = nodeArray
                    });

                    if (openList.Length == 0)
                    {
                        break;
                    }
                    //把寻路消耗最小的点放入关闭列表中 
                    closeList.Add(openList[openList.Length - 1]);

                    //找得这个点 又变成新的起点 进行下一次寻路计算了
                    startNodeIndex = openList[openList.Length - 1];
                    //然后再从开启列表中移除
                    openList.RemoveAt(openList.Length - 1);


                } while (openList.Length > 0 && startNodeIndex != endNodeIndex);

                //如果这个点已经是终点了 那么得到最终结果返回出去
                //如果这个点 不是终点 那么继续寻路

                if (startNodeIndex == endNodeIndex)
                {
                    //找完了
                    int index = endNodeIndex;
                    pathList.Add(index);
                    while (nodeArray[index].fatherIndex != -1)
                    {
                        index = nodeArray[index].fatherIndex;
                        pathList.Add(index);
                    }

                    //反转一下顺序
                    //pathList.Reverse();
                    //安全List没有反转API ，需要手动 或者不反转直接倒序使用优化性能
                }

                //如果开启列表为空 说明没有找到路径
                //if (openList.Length == 0)
                //{
                //Debug.Log("没有找到路径");
                //}
            }

            [BurstCompile]
            /// <summary>
            /// 把周围的点放入开启列表中
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            void FindNearlyNodeToOpenList(int x, int y, float g, int fatherIndex, AStarNode end)
            {

                //边界判断
                if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
                {
                    return;
                }
                int index = x + y * mapWidth;

                if (nodeArray[index].type == AStarNodeType.Stop)
                {
                    return;
                }

                // ❌ 如果在关闭列表中，直接忽略
                if (closeList.Contains(index)) return;


                //计算f寻路消耗 
                AStarNode node = nodeArray[index];
                AStarNode father = nodeArray[fatherIndex];
                node.fatherIndex = fatherIndex;
                node.g = father.g + g;
                node.h = Mathf.Abs(node.x - end.x) + Mathf.Abs(node.y - end.y);
                nodeArray[index] = node;

                //通过合法判断，放入开启列表
                if (closeList.Contains(index) || openList.Contains(index))
                {
                    return;
                }

                visited.Add(index);
                openList.Add(index);
            }

            [BurstCompile]
            /// <summary>
            /// 降序排序比较器
            /// </summary>
            public struct DescendingIntComparer : IComparer<int>
            {
                [ReadOnly]
                public NativeArray<AStarNode> nodeArray;

                public int Compare(int a, int b)
                {
                    return nodeArray[b].F.CompareTo(nodeArray[a].F);
                    //return b.CompareTo(a); // 倒过来，变成降序
                }
            }
        }

    }
}

