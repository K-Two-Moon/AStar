using System.Collections.Generic;
using System.Threading.Tasks;
using JKFrame;
using UnityEngine;

namespace 后台线程计算
{
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
        public AStarNode[,] nodeArray;

        /// <summary>
        /// 开放列表
        /// </summary>
        List<AStarNode> openList = new List<AStarNode>();

        /// <summary>
        /// 关闭列表
        /// </summary>
        List<AStarNode> closeList = new List<AStarNode>();

        /// <summary>
        /// 对查找过的格子进行变色
        /// 直观查看寻路消耗
        /// </summary>
        List<AStarNode> visited = new List<AStarNode>(10000);
        /// <summary>
        /// 最终路径的列表
        /// </summary>
        List<AStarNode> pathList = new List<AStarNode>(10000);

        /// <summary>
        /// 初始化地图信息
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        public void Initialize(int width, int height)
        {
            mapWidth = width;
            mapHeight = height;
            //根据宽高创建地图节点二维数组
            nodeArray = new AStarNode[width, height];

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    AStarNode node = new AStarNode(i, j,
                        Random.Range(0, 100) < 40 ? AStarNodeType.Stop : AStarNodeType.Walk);
                    nodeArray[i, j] = node;
                }
            }
        }

        async public void FindPathAsync(Vector3 startPos, Vector3 endPos, Transform[,] array)
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
                return;
            }

            //如果没有超出获取对应的节点
            //如果不合法 直接返回null 意味着不能寻路
            //如果合法 应该先找到起点和终点坐标对应的格子节点
            AStarNode startNode = nodeArray[startX, startY];
            AStarNode endNode = nodeArray[endX, endY];
            //2.不要是阻挡地图
            //判断起点和终点是否是阻挡地图
            if (startNode.type == AStarNodeType.Stop || endNode.type == AStarNodeType.Stop)
            {
                Debug.Log("传入的坐标不合法,起点或者终点是阻挡地图");
                return;
            }


            //3.开始寻路
            //清空开启列表和关闭列表
            openList.Clear();
            closeList.Clear();
            pathList.Clear();
            visited.Clear();

            //把开始点放入关闭列表
            startNode.father = null;
            startNode.g = 0;
            startNode.h = 0;
            closeList.Add(startNode);




            await Task.Run(() =>
            {
                do
                {
                    //从起点开始 找周围的点 并放入开启列表中
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            // 排除自身
                            if (x == 0 && y == 0)
                                continue;

                            int checkX = startNode.x + x;
                            int checkY = startNode.y + y;


                            FindNearlyNodeToOpenList(checkX, checkY, 1, startNode, endNode);
                        }
                    }
                    //判断这些点 是否是边界 是否是阻挡 是否在开启或者关闭列表 如果都不是 才放入开启列表

                    //选出开启列表中 寻路消耗最小的点
                    openList.Sort((a, b) => b.F.CompareTo(a.F));

                    if (openList.Count == 0)
                    {
                        break;
                    }
                    //把寻路消耗最小的点放入关闭列表中 
                    closeList.Add(openList[openList.Count - 1]);

                    //找得这个点 又变成新的起点 进行下一次寻路计算了
                    startNode = openList[openList.Count - 1];
                    //然后再从开启列表中移除
                    openList.RemoveAt(openList.Count - 1);


                } while (openList.Count > 0 && startNode != endNode);

                //如果这个点已经是终点了 那么得到最终结果返回出去
                //如果这个点 不是终点 那么继续寻路

                if (startNode == endNode)
                {
                    //找完了
                    pathList.Add(endNode);
                    while (endNode.father != null)
                    {
                        pathList.Add(endNode.father);
                        endNode = endNode.father;
                    }

                    //反转一下顺序
                    pathList.Reverse();
                }

                //如果开启列表为空 说明没有找到路径
                if (openList.Count == 0)
                {
                    Debug.Log("没有找到路径");

                }
            });


            //把最查找过的格子进行变色
            foreach (var node in visited)
            {
                int x = node.x;
                int y = node.y;

                array[x, y].GetComponent<Renderer>().material.color = Color.black;
            }

            //把最终路径进行变色
            foreach (var node in pathList)
            {
                int x = node.x;
                int y = node.y;

                array[x, y].GetComponent<Renderer>().material.color = Color.yellow;
            }

        }



        /// <summary>
        /// 把周围的点放入开启列表中
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        void FindNearlyNodeToOpenList(int x, int y, float g, AStarNode father, AStarNode end)
        {
            //边界判断
            if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
            {
                return;
            }

            AStarNode node = nodeArray[x, y];
            if (node == null || node.type == AStarNodeType.Stop)
            {
                return;
            }

            // ❌ 如果在关闭列表中，直接忽略
            if (closeList.Contains(node)) return;


            //计算f寻路消耗 
            //f = g + h
            //记录父节点
            node.father = father;
            //计算g
            //计算g 我离起点的距离 就是我父亲离起点的距离 + 我离我父亲的距离
            node.g = father.g + g;
            //计算h
            node.h = Mathf.Abs(node.x - end.x) + Mathf.Abs(node.y - end.y);

            //通过合法判断，放入开启列表
            if (closeList.Contains(node) || openList.Contains(node))
            {
                return;
            }

            visited.Add(node);
            openList.Add(node);
        }
    }
}