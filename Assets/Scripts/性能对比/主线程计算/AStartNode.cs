namespace 主线程计算
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

    public class AStarNode
    {
        //格子的坐标
        public int x;
        public int y;

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
        public AStarNode father;

        /// <summary>
        /// 类型
        /// </summary>
        public AStarNodeType type;


        public AStarNode(int x, int y, AStarNodeType type)
        {
            this.x = x;
            this.y = y;
            this.type = type;
        }
    }
}