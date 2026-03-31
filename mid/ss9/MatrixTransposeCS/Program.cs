using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace MatrixTransposeCS
{
    class Program
    {
        const int ROWS = 1024;
        const int COLS = 2048;

        static void Main(string[] args)
        {
            Console.WriteLine($"=== MATRIX TRANSPOSE (C#) - {ROWS} x {COLS} ===\n");

            // Initialize matrix
            var rng = new Random(42);
            double[,] A = new double[ROWS, COLS];
            for (int i = 0; i < ROWS; i++)
                for (int j = 0; j < COLS; j++)
                    A[i, j] = (rng.Next(1000)) / 100.0;

            // 1. Sequential
            var sw = Stopwatch.StartNew();
            double[,] seqResult = TransposeSequential(A);
            sw.Stop();
            double seqTime = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine($"[Sequential]   Time: {seqTime:F4} ms");

            // 2. Thread/Task
            sw.Restart();
            double[,] taskResult = TransposeTask(A);
            sw.Stop();
            double taskTime = sw.Elapsed.TotalMilliseconds;
            bool taskCorrect = Verify(seqResult, taskResult);
            Console.WriteLine($"[Thread/Task]  Time: {taskTime:F4} ms  Correct: {(taskCorrect ? "Yes" : "No")}");

            // 3. Channel
            sw.Restart();
            double[,] chanResult = TransposeChannel(A).GetAwaiter().GetResult();
            sw.Stop();
            double chanTime = sw.Elapsed.TotalMilliseconds;
            bool chanCorrect = Verify(seqResult, chanResult);
            Console.WriteLine($"[Channel]      Time: {chanTime:F4} ms  Correct: {(chanCorrect ? "Yes" : "No")}");

            // Speedup
            Console.WriteLine($"\n--- Speedup ---");
            Console.WriteLine($"Sequential / Thread-Task: {seqTime / taskTime:F2}x");
            Console.WriteLine($"Sequential / Channel:     {seqTime / chanTime:F2}x");

            // Print small sample for verification
            Console.WriteLine($"\n--- Sample (first 4x4 of A) ---");
            for (int i = 0; i < Math.Min(4, ROWS); i++)
            {
                for (int j = 0; j < Math.Min(4, COLS); j++)
                    Console.Write($"{A[i, j],8:F2}");
                Console.WriteLine();
            }
            Console.WriteLine($"\n--- Sample (first 4x4 of A^T) ---");
            for (int i = 0; i < Math.Min(4, COLS); i++)
            {
                for (int j = 0; j < Math.Min(4, ROWS); j++)
                    Console.Write($"{seqResult[i, j],8:F2}");
                Console.WriteLine();
            }
        }

        // ===================== Sequential =====================
        static double[,] TransposeSequential(double[,] A)
        {
            int rows = A.GetLength(0);
            int cols = A.GetLength(1);
            double[,] result = new double[cols, rows];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    result[j, i] = A[i, j];

            return result;
        }

        // ===================== Thread/Task =====================
        static double[,] TransposeTask(double[,] A)
        {
            int rows = A.GetLength(0);
            int cols = A.GetLength(1);
            double[,] result = new double[cols, rows];

            int numChunks = Environment.ProcessorCount;
            int chunkSize = (rows + numChunks - 1) / numChunks;
            Task[] tasks = new Task[numChunks];

            for (int c = 0; c < numChunks; c++)
            {
                int startRow = c * chunkSize;
                int endRow = Math.Min(startRow + chunkSize, rows);

                tasks[c] = Task.Run(() =>
                {
                    for (int i = startRow; i < endRow; i++)
                        for (int j = 0; j < cols; j++)
                            result[j, i] = A[i, j];
                });
            }

            Task.WaitAll(tasks);
            return result;
        }

        // ===================== Channel =====================
        static async Task<double[,]> TransposeChannel(double[,] A)
        {
            int rows = A.GetLength(0);
            int cols = A.GetLength(1);
            double[,] result = new double[cols, rows];

            int numChunks = Environment.ProcessorCount;
            int chunkSize = (rows + numChunks - 1) / numChunks;

            // Channel carries (startRow, endRow) — signal that a chunk is done
            var channel = Channel.CreateUnbounded<(int startRow, int endRow)>();

            // Producer Tasks: transpose each chunk and signal completion
            var producers = new Task[numChunks];
            for (int c = 0; c < numChunks; c++)
            {
                int startRow = c * chunkSize;
                int endRow = Math.Min(startRow + chunkSize, rows);

                producers[c] = Task.Run(async () =>
                {
                    for (int i = startRow; i < endRow; i++)
                        for (int j = 0; j < cols; j++)
                            result[j, i] = A[i, j];

                    await channel.Writer.WriteAsync((startRow, endRow));
                });
            }

            // Complete channel after all producers finish
            _ = Task.Run(async () =>
            {
                await Task.WhenAll(producers);
                channel.Writer.Complete();
            });

            // Consumer: wait for all chunks to be processed
            int chunksReceived = 0;
            await foreach (var chunk in channel.Reader.ReadAllAsync())
            {
                chunksReceived++;
            }

            return result;
        }

        // ===================== Verification =====================
        static bool Verify(double[,] expected, double[,] actual)
        {
            int rows = expected.GetLength(0);
            int cols = expected.GetLength(1);
            if (actual.GetLength(0) != rows || actual.GetLength(1) != cols)
                return false;

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    if (Math.Abs(expected[i, j] - actual[i, j]) > 1e-10)
                        return false;
            return true;
        }
    }
}
