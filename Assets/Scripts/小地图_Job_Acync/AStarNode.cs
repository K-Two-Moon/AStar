using System;
using UnityEngine;

namespace 小地图_Job_Acync
{
    public enum AStarNodeType : byte
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
    /// <summary>
    /// 结构体最好要小于16字节
    /// </summary>
    public struct AStarNode
    {
        /// <summary>
        /// 地图大小
        /// </summary>
        public static int mapWidth = -1;
        public static int mapHeight = -1;


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
        public short fatherIndex; // 2字节

        //格子的坐标
        public byte x { get; }
        public byte y { get; }
        /// <summary>
        /// 类型
        /// </summary>
        public AStarNodeType type;


        public AStarNode(byte x, byte y, AStarNodeType type)
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
