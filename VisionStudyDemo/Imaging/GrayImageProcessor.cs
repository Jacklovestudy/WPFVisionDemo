namespace VisionStudyDemo.Imaging
{
    /// <summary>
    /// 灰度图学习算法：x 表示行，y 表示列。
    /// 输入 gray[x, y]；输出 pixels[x * width + y]，不修改原图。
    /// </summary>
    public static class GrayImageProcessor
    {


        /// <summary>从原图裁剪指定区域，返回新的二维灰度数组。</summary>
        public static byte[,] Crop(
            byte[,] gray,
            int startRow,
            int startCol,
            int roiWidth,
            int roiHeight)
        {
            int imageHeight = gray.GetLength(0);
            int imageWidth = gray.GetLength(1);

            // ROI 必须有大小，而且完整位于原图内部
            if (startRow < 0 || startCol < 0 ||
                roiWidth <= 0 || roiHeight <= 0 ||
                roiWidth > imageWidth || roiHeight > imageHeight ||
                startRow > imageHeight - roiHeight ||
                startCol > imageWidth - roiWidth)
            {
                throw new ArgumentException("ROI 超出原图范围或宽高无效。");
            }

            byte[,] cropped = new byte[roiHeight, roiWidth];

            // x、y 是小图中的行、列索引，从 0 开始
            for (int x = 0; x < roiHeight; x++)
            {
                for (int y = 0; y < roiWidth; y++)
                {
                    cropped[x, y] = gray[startRow + x, startCol + y];
                }
            }

            return cropped;
        }




        /// <summary>二维数组按行展开为一维数组，灰度不变。</summary>
        public static byte[] Flatten(byte[,] gray)
        {
            int height = gray.GetLength(0);
            int width = gray.GetLength(1);
            byte[] pixels = new byte[gray.Length];
            for (int x = 0; x < height; x++)
                for (int y = 0; y < width; y++)
                    pixels[x * width + y] = gray[x, y];
            return pixels;
        }

        /// <summary>亮度调整：正数变亮，负数变暗，截断到 0～255。</summary>
        public static byte[] AdjustBrightness(byte[,] gray, int offset)
        {
            int height = gray.GetLength(0);
            int width = gray.GetLength(1);
            byte[] pixels = new byte[gray.Length];
            // 更大的偏移也只会使全部像素饱和；限制偏移避免整数溢出。
            offset = Math.Clamp(offset, -255, 255);
            for (int x = 0; x < height; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    int value = gray[x, y] + offset;
                    pixels[x * width + y] = (byte)Math.Clamp(value, 0, 255);
                }
            }
            return pixels;
        }

        /// <summary>灰度反转：输出 = 255 - 原灰度。</summary>
        public static byte[] Invert(byte[,] gray)
        {
            int height = gray.GetLength(0);
            int width = gray.GetLength(1);
            byte[] pixels = new byte[gray.Length];
            for (int x = 0; x < height; x++)
                for (int y = 0; y < width; y++)
                    pixels[x * width + y] = (byte)(255 - gray[x, y]);
            return pixels;
        }

        /// <summary>二值化：严格小于阈值输出 255，其余（包括等于）输出 0。</summary>
        public static byte[] Threshold(byte[,] gray, int threshold)
        {
            int height = gray.GetLength(0);
            int width = gray.GetLength(1);
            byte[] pixels = new byte[gray.Length];
            for (int x = 0; x < height; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    if (gray[x, y] < threshold)
                        pixels[x * width + y] = 255;
                    else
                        pixels[x * width + y] = 0;
                }
            }
            return pixels;
        }

        /// <summary>统计二值图中白色像素数，不是物体数量。</summary>
        public static int CountForeground(byte[] pixels)
        {
            int count = 0;
            foreach (byte pixel in pixels)
                if (pixel == 255)
                    count++;
            return count;
        }

        /// <summary>
        /// 四邻域连通域统计：输入二值化后的一维数组，返回每块的面积。
        /// 面积单位是像素；结果按逐行发现的顺序排列。
        /// </summary>
        public static List<int> GetFourConnectedAreas(byte[] pixels, int width, int height)
        {
            // 保留只需要面积时的调用方式，搜索逻辑统一在下面的方法中。
            return GetFourConnectedRegions(pixels, width, height)
                .Select(region => region.Area).ToList();
        }

        /// <summary>返回完整连通域：每块既有像素位置，也能获取面积。</summary>
        public static List<ConnectedRegion> GetFourConnectedRegions(byte[] pixels, int width, int height)
        {
            if (width <= 0 || height <= 0 || (long)width * height != pixels.Length)
                throw new ArgumentException("像素数组长度必须等于正数宽、高的乘积。");

            bool[,] visited = new bool[height, width];
            List<ConnectedRegion> regions = new();

            // 外层遍历只负责寻找新的白色起点。
            for (int x = 0; x < height; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    if (pixels[x * width + y] != 255 || visited[x, y])
                        continue;

                    regions.Add(SearchFourConnectedRegion(pixels, width, height, x, y, visited));
                }
            }
            return regions;
        }

        /// <summary>
        /// 按面积筛选：保留面积大于等于 minArea 的连通域。
        /// 返回新的二值图，小块变黑，传入的 pixels 不变。
        /// </summary>
        public static byte[] FilterByArea(byte[] pixels, int width, int height, int minArea)
        {
            if (minArea < 1)
                throw new ArgumentOutOfRangeException(nameof(minArea), "最小面积必须大于等于 1。");

            List<ConnectedRegion> regions = GetFourConnectedRegions(pixels, width, height);
            byte[] result = new byte[pixels.Length]; // 默认全部为黑色 0

            foreach (ConnectedRegion region in regions)
            {
                if (region.Area < minArea)
                    continue;

                // 将合格区域的每个位置涂成白色，其余位置保持黑色。
                foreach (var point in region.Pixels)
                {
                    int x = point.Row;
                    int y = point.Col;
                    result[x * width + y] = 255;
                }
            }
            return result;
        }

        /// <summary>队列搜索一整块。visited 在全图搜索期间共用。</summary>
        private static ConnectedRegion SearchFourConnectedRegion(
            byte[] pixels, int width, int height, int startX, int startY, bool[,] visited)
        {
            // 上、下、左、右：行偏移和列偏移。
            (int Row, int Col)[] directions = { (-1, 0), (1, 0), (0, -1), (0, 1) };
            Queue<(int Row, int Col)> queue = new();
            visited[startX, startY] = true;
            queue.Enqueue((startX, startY));
            ConnectedRegion region = new();

            // 队列保存已发现、还没检查四周的像素。
            while (queue.Count > 0)
            {
                var point = queue.Dequeue();
                // 以前只做 area++，现在还保存这个像素的位置。
                region.Pixels.Add(point);

                foreach (var direction in directions)
                {
                    int nextX = point.Row + direction.Row;
                    int nextY = point.Col + direction.Col;

                    // 先检查边界，才能访问数组。
                    if (nextX < 0 || nextX >= height || nextY < 0 || nextY >= width)
                        continue;

                    if (pixels[nextX * width + nextY] != 255 || visited[nextX, nextY])
                        continue;

                    // 入队时就标记，避免被不同邻居重复加入。
                    visited[nextX, nextY] = true;
                    queue.Enqueue((nextX, nextY));
                }
            }
            return region;
        }
    }
}
