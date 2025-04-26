using JKFrame;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace 小地图_Job_Acync
{

    public class MinMapController : Singleton<MinMapController>
    {
        RawImage minimapUI;
        int mapWidth;
        int mapHeight;


        public RectTransform minimapImage; // RawImage 的 RectTransform
        public Transform player; // 玩家Transform
        public float textureSize = 1000f; // 原始小地图纹理像素宽度


        RectTransform playerMap;




        public int[,] mapData;
        public void Initialize(NativeArray<AStarNode> nodeArrayNative, int width, int height)
        {
            minimapUI = GameObject.Find("MinMap").GetComponent<RawImage>();
            minimapImage = GameObject.Find("MinMap").GetComponent<RectTransform>();
            playerMap = minimapImage.Find("Player").GetComponent<RectTransform>();

            mapWidth = width;
            mapHeight = height;
            mapData = new int[width, height];
            foreach (var node in AStarManager.Instance.nodeArrayNative)
            {
                if (node.type == AStarNodeType.Walk)
                {
                    mapData[node.x, node.y] = 0;
                }
                else
                {
                    mapData[node.x, node.y] = 1;
                }
            }


            Texture2D minimapTexture = GenerateMinimapTexture(mapData);
            //赋值
            minimapUI.texture = minimapTexture;



            Texture2D GenerateMinimapTexture(int[,] data)
            {
                int cellSize = 10; // 每个格子占10x10像素

                Texture2D tex = new Texture2D(width * cellSize, height * cellSize);
                tex.filterMode = FilterMode.Point; // 保持像素风格清晰

                // 填充颜色
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color color = data[x, y] == 1 ? Color.red : Color.white;
                        FillBlock(tex, x * cellSize, y * cellSize, cellSize, color);
                    }
                }

                tex.Apply(); // 应用修改
                return tex;
            }
            void FillBlock(Texture2D tex, int startX, int startY, int size, Color color)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        tex.SetPixel(startX + x, startY + y, color);
                    }
                }
            }
        }



        public void LateUpdate()
        {
            player = PlayerController.Instance.transform;

            // 1. 计算 UV 尺寸
            float uvSize = 0.2f; //viewPixelSize / (float)texturePixelSize;  // 200/1000 = 0.2

            // 2. 计算玩家在世界地图中的归一化坐标（0~1）
            float u = player.position.x / 100;
            float v = player.position.z / 100;  // 假设 Z 轴为纵向

            // 3. 计算 UV 偏移 （让玩家始终出现在可视窗口中心）
            float offsetU = u - uvSize * 0.5f;
            float offsetV = v - uvSize * 0.5f;

            // 4. Clamp 保证不超出纹理边界
            offsetU = Mathf.Clamp01(offsetU);
            offsetV = Mathf.Clamp01(offsetV);

            // 5. 应用到 RawImage.uvRect
            minimapUI.uvRect = new Rect(offsetU, offsetV, uvSize, uvSize);

            // 6. 旋转玩家图标
            float playerYaw = player.eulerAngles.y;
            playerMap.localEulerAngles = new Vector3(0, 0, -playerYaw + 90);
        }
    }
}
