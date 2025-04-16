using UnityEngine;

namespace Job系统进行计算_同步计算为方便查看性能
{
    public enum AStarNodeType
    {
        /// <summary>
        /// 可以走的格子节点
        /// </summary>
        Walk,

        /// <summary>
        /// 不能走的格子节点
        /// </summary>
        Stop
    }
    public struct AStarNode
    {
        /// <summary>
        /// 地图大小
        /// </summary>
        public static int mapWidth = -1;
        public static int mapHeight = -1;

        //格子的坐标
        public int x { get; }
        public int y { get; }
        public int Index => x + y * mapWidth;

        /// <summary>
        /// 寻路消耗
        /// </summary>
        public float F => g + h;

        /// <summary>
        /// 离起点的距离
        /// </summary>
        public float g;

        /// <summary>
        /// 离终点的距离
        /// </summary>
        public float h;

        /// <summary>
        /// 父对象
        /// </summary>
        public int fatherIndex;

        /// <summary>
        /// 类型
        /// </summary>
        public AStarNodeType type;


        public AStarNode(int x, int y, AStarNodeType type)
        {
            this.x = x;
            this.y = y;
            this.type = type;
            this.h = 0;
            this.g = 0;
            fatherIndex = -1;
        }
    }
}
