using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CSharpParallel
{
    /// <summary>
    /// Bài toán 2: Counting Sort.
    /// 3 phiên bản: Sequential, Thread/Task, Parallel.For
    /// </summary>
    static class CountingSort
    {
        // --- 2a. Sequential ---
        public static int[] Sequential(int[] data, out double elapsedMs)
        {
            var sw = Stopwatch.StartNew();

            // Bước 1: Đếm tần suất
            int[] count = new int[Config.MAX_VALUE];
            for (int i = 0; i < data.Length; i++)
                count[data[i]]++;

            // Bước 2: Prefix sum
            for (int i = 1; i < Config.MAX_VALUE; i++)
                count[i] += count[i - 1];

            // Bước 3: Đặt phần tử vào vị trí (stable, duyệt ngược)
            int[] output = new int[data.Length];
            for (int i = data.Length - 1; i >= 0; i--)
            {
                output[count[data[i]] - 1] = data[i];
                count[data[i]]--;
            }

            sw.Stop();
            elapsedMs = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine($"    [Sequential]   Time: {elapsedMs:F4} ms");
            return output;
        }

        // --- 2b. Thread/Task ---
        public static int[] ThreadTask(int[] data, out double elapsedMs)
        {
            int numTasks = Environment.ProcessorCount;
            int chunkSize = data.Length / numTasks;

            var sw = Stopwatch.StartNew();

            // Bước 1: Song song đếm tần suất (mỗi task một histogram cục bộ)
            int[][] localCounts = new int[numTasks][];
            Task[] tasks = new Task[numTasks];

            for (int t = 0; t < numTasks; t++)
            {
                int taskId = t;
                int start = taskId * chunkSize;
                int end = (taskId == numTasks - 1) ? data.Length : start + chunkSize;

                tasks[taskId] = Task.Run(() =>
                {
                    int[] local = new int[Config.MAX_VALUE];
                    for (int i = start; i < end; i++)
                        local[data[i]]++;
                    localCounts[taskId] = local;
                });
            }
            Task.WaitAll(tasks);

            // Merge histogram
            int[] count = new int[Config.MAX_VALUE];
            for (int t = 0; t < numTasks; t++)
                for (int v = 0; v < Config.MAX_VALUE; v++)
                    count[v] += localCounts[t][v];

            // Bước 2: Prefix sum (tuần tự - nhỏ, O(K))
            for (int i = 1; i < Config.MAX_VALUE; i++)
                count[i] += count[i - 1];

            // Bước 3: Placement (tuần tự để đảm bảo stable)
            int[] output = new int[data.Length];
            for (int i = data.Length - 1; i >= 0; i--)
            {
                output[count[data[i]] - 1] = data[i];
                count[data[i]]--;
            }

            sw.Stop();
            elapsedMs = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine($"    [Thread/Task]  Time: {elapsedMs:F4} ms  (Tasks: {numTasks}, Chunk: {chunkSize:N0} items/task)");
            return output;
        }

        // --- 2c. Parallel.For ---
        public static int[] ParallelFor(int[] data, out double elapsedMs)
        {
            var sw = Stopwatch.StartNew();

            // Bước 1: Song song đếm tần suất
            int[] count = new int[Config.MAX_VALUE];
            object lockObj = new object();

            Parallel.For(0, data.Length,
                () => new int[Config.MAX_VALUE],
                (i, state, localCount) =>
                {
                    localCount[data[i]]++;
                    return localCount;
                },
                (localCount) =>
                {
                    lock (lockObj)
                    {
                        for (int v = 0; v < Config.MAX_VALUE; v++)
                            count[v] += localCount[v];
                    }
                });

            // Bước 2: Prefix sum (tuần tự)
            for (int i = 1; i < Config.MAX_VALUE; i++)
                count[i] += count[i - 1];

            // Bước 3: Placement (tuần tự)
            int[] output = new int[data.Length];
            for (int i = data.Length - 1; i >= 0; i--)
            {
                output[count[data[i]] - 1] = data[i];
                count[data[i]]--;
            }

            sw.Stop();
            elapsedMs = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine($"    [Parallel.For] Time: {elapsedMs:F4} ms");
            return output;
        }
    }
}
