using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace DotProductCS
{
    class Program
    {
        const int N = 1_000_000;

        static void Main(string[] args)
        {
            Console.WriteLine($"=== DOT PRODUCT (C#) - N = {N} ===\n");

            // Initialize vectors
            var rng = new Random(42);
            double[] A = new double[N];
            double[] B = new double[N];
            for (int i = 0; i < N; i++)
            {
                A[i] = (rng.Next(100)) / 100.0;
                B[i] = (rng.Next(100)) / 100.0;
            }

            // 1. Sequential
            var sw = Stopwatch.StartNew();
            double seqResult = DotProductSequential(A, B);
            sw.Stop();
            double seqTime = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine($"[Sequential]   Result: {seqResult:F6}  Time: {seqTime:F4} ms");

            // 2. Thread/Task
            sw.Restart();
            double taskResult = DotProductTask(A, B);
            sw.Stop();
            double taskTime = sw.Elapsed.TotalMilliseconds;
            bool taskCorrect = Math.Abs(seqResult - taskResult) < 1e-6;
            Console.WriteLine($"[Thread/Task]  Result: {taskResult:F6}  Time: {taskTime:F4} ms  Correct: {(taskCorrect ? "Yes" : "No")}");

            // 3. Channel
            sw.Restart();
            double chanResult = DotProductChannel(A, B).GetAwaiter().GetResult();
            sw.Stop();
            double chanTime = sw.Elapsed.TotalMilliseconds;
            bool chanCorrect = Math.Abs(seqResult - chanResult) < 1e-6;
            Console.WriteLine($"[Channel]      Result: {chanResult:F6}  Time: {chanTime:F4} ms  Correct: {(chanCorrect ? "Yes" : "No")}");

            // Speedup
            Console.WriteLine($"\n--- Speedup ---");
            Console.WriteLine($"Sequential / Thread-Task: {seqTime / taskTime:F2}x");
            Console.WriteLine($"Sequential / Channel:     {seqTime / chanTime:F2}x");
        }

        // ===================== Sequential =====================
        static double DotProductSequential(double[] A, double[] B)
        {
            double sum = 0.0;
            for (int i = 0; i < A.Length; i++)
                sum += A[i] * B[i];
            return sum;
        }

        // ===================== Thread/Task =====================
        static double DotProductTask(double[] A, double[] B)
        {
            int numChunks = Environment.ProcessorCount;
            int chunkSize = (A.Length + numChunks - 1) / numChunks;
            double[] partialSums = new double[numChunks];
            Task[] tasks = new Task[numChunks];

            for (int c = 0; c < numChunks; c++)
            {
                int chunkIdx = c;
                int start = chunkIdx * chunkSize;
                int end = Math.Min(start + chunkSize, A.Length);

                tasks[c] = Task.Run(() =>
                {
                    double localSum = 0.0;
                    for (int i = start; i < end; i++)
                        localSum += A[i] * B[i];
                    partialSums[chunkIdx] = localSum;
                });
            }

            Task.WaitAll(tasks);

            double total = 0.0;
            for (int i = 0; i < numChunks; i++)
                total += partialSums[i];
            return total;
        }

        // ===================== Channel =====================
        static async Task<double> DotProductChannel(double[] A, double[] B)
        {
            int numChunks = Environment.ProcessorCount;
            int chunkSize = (A.Length + numChunks - 1) / numChunks;

            var channel = Channel.CreateUnbounded<double>();

            // Producers: compute partial dot products and write to channel
            var producers = new Task[numChunks];
            for (int c = 0; c < numChunks; c++)
            {
                int chunkIdx = c;
                int start = chunkIdx * chunkSize;
                int end = Math.Min(start + chunkSize, A.Length);

                producers[c] = Task.Run(async () =>
                {
                    double localSum = 0.0;
                    for (int i = start; i < end; i++)
                        localSum += A[i] * B[i];
                    await channel.Writer.WriteAsync(localSum);
                });
            }

            // Complete writing after all producers finish
            _ = Task.Run(async () =>
            {
                await Task.WhenAll(producers);
                channel.Writer.Complete();
            });

            // Consumer: read partial sums and accumulate
            double total = 0.0;
            await foreach (var partialSum in channel.Reader.ReadAllAsync())
            {
                total += partialSum;
            }

            return total;
        }
    }
}
