using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CSharpParallel
{
    class Program
    {
        const int VECTOR_SIZE = 1_000_000;
        const int MATRIX_ROWS = 1024;
        const int MATRIX_COLS = 1024;

        static void Main(string[] args)
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine("  Session 9 - C# Parallel Programming Techniques");
            Console.WriteLine("  CT225H - Parallel Programming with CUDA");
            Console.WriteLine("  Nguyen Thanh Trong - B2305615");
            Console.WriteLine("==========================================================\n");

            // Khởi tạo dữ liệu chung
            var rand = new Random(42);
            float[] a = new float[VECTOR_SIZE];
            float[] b = new float[VECTOR_SIZE];
            for (int i = 0; i < VECTOR_SIZE; i++)
            {
                a[i] = (float)(rand.Next(100)) / 100.0f;
                b[i] = (float)(rand.Next(100)) / 100.0f;
            }

            float[,] matrix = new float[MATRIX_ROWS, MATRIX_COLS];
            rand = new Random(42);
            for (int i = 0; i < MATRIX_ROWS; i++)
                for (int j = 0; j < MATRIX_COLS; j++)
                    matrix[i, j] = (float)(rand.Next(1000)) / 100.0f;

            // ====== 1. Sequential (baseline) ======
            Console.WriteLine("============ 1. SEQUENTIAL (Baseline) ============");
            RunSequential(a, b, matrix);

            // ====== 2. Thread/Task ======
            Console.WriteLine("\n============ 2. THREAD / TASK ============");
            RunThreadTask(a, b, matrix);

            // ====== 3. Parallel (Parallel.For) ======
            Console.WriteLine("\n============ 3. PARALLEL (Parallel.For/ForEach) ============");
            RunParallel(a, b, matrix);

            // ====== 4. Channel ======
            Console.WriteLine("\n============ 4. CHANNEL ============");
            RunChannel(a, b).Wait();
        }

        // =====================================================================
        // 1. SEQUENTIAL
        // =====================================================================
        static void RunSequential(float[] a, float[] b, float[,] matrix)
        {
            var sw = Stopwatch.StartNew();

            // Dot Product
            double dotResult = 0.0;
            for (int i = 0; i < a.Length; i++)
                dotResult += a[i] * b[i];
            sw.Stop();
            double dotTime = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"[Dot Product] Result: {dotResult:F6}  Time: {dotTime:F4} ms");

            // Matrix Transpose
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            float[,] transposed = new float[cols, rows];

            sw.Restart();
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    transposed[j, i] = matrix[i, j];
            sw.Stop();
            double transTime = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"[Transpose]   Time: {transTime:F4} ms  Correct: {VerifyTranspose(matrix, transposed)}");
        }

        // =====================================================================
        // 2. THREAD / TASK - Chia dữ liệu thành chunks và gán cho các Task
        // =====================================================================
        static void RunThreadTask(float[] a, float[] b, float[,] matrix)
        {
            int numTasks = Environment.ProcessorCount;
            int chunkSize = a.Length / numTasks;

            // --- Dot Product với Task ---
            double[] partialSums = new double[numTasks];
            var sw = Stopwatch.StartNew();

            Task[] tasks = new Task[numTasks];
            for (int t = 0; t < numTasks; t++)
            {
                int taskId = t;
                int start = taskId * chunkSize;
                int end = (taskId == numTasks - 1) ? a.Length : start + chunkSize;

                tasks[taskId] = Task.Run(() =>
                {
                    double sum = 0.0;
                    for (int i = start; i < end; i++)
                        sum += a[i] * b[i];
                    partialSums[taskId] = sum;
                });
            }
            Task.WaitAll(tasks);
            double dotResult = partialSums.Sum();
            sw.Stop();
            double dotTime = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"[Dot Product] Result: {dotResult:F6}  Time: {dotTime:F4} ms  (Tasks: {numTasks})");

            // --- Matrix Transpose với Task ---
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            float[,] transposed = new float[cols, rows];
            int rowChunk = rows / numTasks;

            sw.Restart();
            Task[] tTasks = new Task[numTasks];
            for (int t = 0; t < numTasks; t++)
            {
                int taskId = t;
                int startRow = taskId * rowChunk;
                int endRow = (taskId == numTasks - 1) ? rows : startRow + rowChunk;

                tTasks[taskId] = Task.Run(() =>
                {
                    for (int i = startRow; i < endRow; i++)
                        for (int j = 0; j < cols; j++)
                            transposed[j, i] = matrix[i, j];
                });
            }
            Task.WaitAll(tTasks);
            sw.Stop();
            double transTime = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"[Transpose]   Time: {transTime:F4} ms  Correct: {VerifyTranspose(matrix, transposed)}  (Tasks: {numTasks})");
        }

        // =====================================================================
        // 3. PARALLEL - Sử dụng Parallel.For để song song hóa tự động
        // =====================================================================
        static void RunParallel(float[] a, float[] b, float[,] matrix)
        {
            // --- Dot Product với Parallel.For + localInit ---
            var sw = Stopwatch.StartNew();
            double dotResult = 0.0;
            object lockObj = new object();

            Parallel.For(0, a.Length,
                () => 0.0,  // localInit
                (i, state, localSum) =>
                {
                    return localSum + a[i] * b[i];
                },
                (localSum) =>  // localFinally
                {
                    lock (lockObj)
                    {
                        dotResult += localSum;
                    }
                });
            sw.Stop();
            double dotTime = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"[Dot Product] Result: {dotResult:F6}  Time: {dotTime:F4} ms");

            // --- Matrix Transpose với Parallel.For ---
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            float[,] transposed = new float[cols, rows];

            sw.Restart();
            Parallel.For(0, rows, i =>
            {
                for (int j = 0; j < cols; j++)
                    transposed[j, i] = matrix[i, j];
            });
            sw.Stop();
            double transTime = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"[Transpose]   Time: {transTime:F4} ms  Correct: {VerifyTranspose(matrix, transposed)}");
        }

        // =====================================================================
        // 4. CHANNEL - Producer/Consumer pattern sử dụng System.Threading.Channels
        // Áp dụng cho bài toán Dot Product: producer gửi kết quả từng chunk,
        // consumer nhận và cộng dồn.
        // =====================================================================
        static async Task RunChannel(float[] a, float[] b)
        {
            int numProducers = Environment.ProcessorCount;
            int chunkSize = a.Length / numProducers;

            var channel = Channel.CreateUnbounded<double>();
            var sw = Stopwatch.StartNew();

            // Producers: mỗi producer tính dot product cho 1 chunk rồi gửi vào channel
            var producerTasks = new Task[numProducers];
            for (int t = 0; t < numProducers; t++)
            {
                int taskId = t;
                int start = taskId * chunkSize;
                int end = (taskId == numProducers - 1) ? a.Length : start + chunkSize;

                producerTasks[taskId] = Task.Run(async () =>
                {
                    double sum = 0.0;
                    for (int i = start; i < end; i++)
                        sum += a[i] * b[i];
                    await channel.Writer.WriteAsync(sum);
                });
            }

            // Đợi tất cả producer hoàn thành, sau đó đóng channel
            Task.WaitAll(producerTasks);
            channel.Writer.Complete();

            // Consumer: đọc kết quả từ channel và cộng dồn
            double dotResult = 0.0;
            await foreach (var partialSum in channel.Reader.ReadAllAsync())
            {
                dotResult += partialSum;
            }
            sw.Stop();
            double dotTime = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"[Dot Product] Result: {dotResult:F6}  Time: {dotTime:F4} ms  (Producers: {numProducers})");
            Console.WriteLine($"  -> Channel pattern: {numProducers} producers -> 1 consumer (via Channel<double>)");
        }

        // =====================================================================
        // Verification helper
        // =====================================================================
        static string VerifyTranspose(float[,] original, float[,] transposed)
        {
            int rows = original.GetLength(0);
            int cols = original.GetLength(1);
            for (int i = 0; i < Math.Min(rows, 100); i++)
                for (int j = 0; j < Math.Min(cols, 100); j++)
                    if (Math.Abs(original[i, j] - transposed[j, i]) > 1e-5)
                        return "No";
            return "Yes";
        }
    }
}
