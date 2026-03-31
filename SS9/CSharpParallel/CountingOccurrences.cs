using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CSharpParallel
{
    /// <summary>
    /// Bài toán 1: Đếm số lần xuất hiện của mỗi số.
    /// 3 phiên bản: Sequential, Thread/Task, Parallel.For
    /// </summary>
    static class CountingOccurrences
    {
        // --- 1a. Sequential ---
        public static int[] Sequential(int[] data, out double elapsedMs)
        {
            var sw = Stopwatch.StartNew();
            int[] hist = new int[Config.MAX_VALUE];
            for (int i = 0; i < data.Length; i++)
                hist[data[i]]++;
            sw.Stop();
            elapsedMs = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine($"    [Sequential]   Time: {elapsedMs:F4} ms");
            return hist;
        }

        // --- 1b. Thread/Task ---
        public static int[] ThreadTask(int[] data, out double elapsedMs)
        {
            int numTasks = Environment.ProcessorCount;
            int chunkSize = data.Length / numTasks;

            var sw = Stopwatch.StartNew();

            // Mỗi task xây dựng histogram cục bộ
            int[][] localHists = new int[numTasks][];
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
                    localHists[taskId] = local;
                });
            }
            Task.WaitAll(tasks);

            // Merge tất cả histogram cục bộ
            int[] hist = new int[Config.MAX_VALUE];
            for (int t = 0; t < numTasks; t++)
                for (int v = 0; v < Config.MAX_VALUE; v++)
                    hist[v] += localHists[t][v];

            sw.Stop();
            elapsedMs = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine($"    [Thread/Task]  Time: {elapsedMs:F4} ms  (Tasks: {numTasks}, Chunk: {chunkSize:N0} items/task)");
            return hist;
        }

        // --- 1c. Parallel.For ---
        public static int[] ParallelFor(int[] data, out double elapsedMs)
        {
            var sw = Stopwatch.StartNew();
            int[] hist = new int[Config.MAX_VALUE];
            object lockObj = new object();

            Parallel.For(0, data.Length,
                () => new int[Config.MAX_VALUE],  // localInit: histogram cục bộ mỗi thread
                (i, state, localHist) =>
                {
                    localHist[data[i]]++;
                    return localHist;
                },
                (localHist) =>  // localFinally: merge vào histogram chung
                {
                    lock (lockObj)
                    {
                        for (int v = 0; v < Config.MAX_VALUE; v++)
                            hist[v] += localHist[v];
                    }
                });

            sw.Stop();
            elapsedMs = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine($"    [Parallel.For] Time: {elapsedMs:F4} ms");
            return hist;
        }
    }
}
