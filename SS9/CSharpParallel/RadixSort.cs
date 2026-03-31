using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CSharpParallel
{
    /// <summary>
    /// Bài toán 3: Radix Sort (LSD, base 256).
    /// 3 phiên bản: Sequential, Thread/Task, Parallel.For
    /// </summary>
    static class RadixSort
    {
        // --- 3a. Sequential ---
        public static int[] Sequential(int[] data, out double elapsedMs)
        {
            var sw = Stopwatch.StartNew();

            int[] input = (int[])data.Clone();
            int[] output = new int[data.Length];
            int n = data.Length;

            // 4 passes cho 32-bit integer (base 256)
            for (int pass = 0; pass < 4; pass++)
            {
                int shift = pass * 8;
                int[] count = new int[256];

                // Đếm tần suất digit hiện tại
                for (int i = 0; i < n; i++)
                    count[(input[i] >> shift) & 0xFF]++;

                // Prefix sum
                for (int i = 1; i < 256; i++)
                    count[i] += count[i - 1];

                // Placement (stable, duyệt ngược)
                for (int i = n - 1; i >= 0; i--)
                {
                    int digit = (input[i] >> shift) & 0xFF;
                    output[count[digit] - 1] = input[i];
                    count[digit]--;
                }

                // Swap input/output cho pass tiếp theo
                var temp = input;
                input = output;
                output = temp;
            }

            sw.Stop();
            elapsedMs = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine($"    [Sequential]   Time: {elapsedMs:F4} ms  (4 passes, base 256)");
            return input; // Sau 4 pass (chẵn), kết quả nằm trong input
        }

        // --- 3b. Thread/Task ---
        public static int[] ThreadTask(int[] data, out double elapsedMs)
        {
            int numTasks = Environment.ProcessorCount;
            int chunkSize = data.Length / numTasks;

            var sw = Stopwatch.StartNew();

            int[] input = (int[])data.Clone();
            int[] output = new int[data.Length];
            int n = data.Length;

            for (int pass = 0; pass < 4; pass++)
            {
                int shift = pass * 8;

                // Bước 1: Song song đếm tần suất
                int[][] localCounts = new int[numTasks][];
                Task[] tasks = new Task[numTasks];

                for (int t = 0; t < numTasks; t++)
                {
                    int taskId = t;
                    int start = taskId * chunkSize;
                    int end = (taskId == numTasks - 1) ? n : start + chunkSize;

                    tasks[taskId] = Task.Run(() =>
                    {
                        int[] local = new int[256];
                        for (int i = start; i < end; i++)
                            local[(input[i] >> shift) & 0xFF]++;
                        localCounts[taskId] = local;
                    });
                }
                Task.WaitAll(tasks);

                // Merge + Prefix sum (tuần tự)
                int[] count = new int[256];
                for (int t = 0; t < numTasks; t++)
                    for (int d = 0; d < 256; d++)
                        count[d] += localCounts[t][d];

                for (int i = 1; i < 256; i++)
                    count[i] += count[i - 1];

                // Placement (tuần tự - đảm bảo stable)
                for (int i = n - 1; i >= 0; i--)
                {
                    int digit = (input[i] >> shift) & 0xFF;
                    output[count[digit] - 1] = input[i];
                    count[digit]--;
                }

                var temp = input;
                input = output;
                output = temp;
            }

            sw.Stop();
            elapsedMs = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine($"    [Thread/Task]  Time: {elapsedMs:F4} ms  (Tasks: {numTasks}, Chunk: {chunkSize:N0}, 4 passes)");
            return input;
        }

        // --- 3c. Parallel.For ---
        public static int[] ParallelFor(int[] data, out double elapsedMs)
        {
            var sw = Stopwatch.StartNew();

            int[] input = (int[])data.Clone();
            int[] output = new int[data.Length];
            int n = data.Length;

            for (int pass = 0; pass < 4; pass++)
            {
                int shift = pass * 8;

                // Bước 1: Song song đếm tần suất
                int[] count = new int[256];
                object lockObj = new object();

                Parallel.For(0, n,
                    () => new int[256],
                    (i, state, localCount) =>
                    {
                        localCount[(input[i] >> shift) & 0xFF]++;
                        return localCount;
                    },
                    (localCount) =>
                    {
                        lock (lockObj)
                        {
                            for (int d = 0; d < 256; d++)
                                count[d] += localCount[d];
                        }
                    });

                // Prefix sum (tuần tự)
                for (int i = 1; i < 256; i++)
                    count[i] += count[i - 1];

                // Placement (tuần tự)
                for (int i = n - 1; i >= 0; i--)
                {
                    int digit = (input[i] >> shift) & 0xFF;
                    output[count[digit] - 1] = input[i];
                    count[digit]--;
                }

                var temp = input;
                input = output;
                output = temp;
            }

            sw.Stop();
            elapsedMs = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine($"    [Parallel.For] Time: {elapsedMs:F4} ms  (4 passes, base 256)");
            return input;
        }
    }
}
