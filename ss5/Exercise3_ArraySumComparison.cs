using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Session5
{
    /// <summary>
    /// Exercise 3: Compare Array Sum Algorithms
    /// - Sequential
    /// - Parallel
    /// - Channel (Producer-Consumer pattern)
    /// </summary>
    public class ArraySumComparison
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Array Sum Algorithm Comparison ===\n");
            
            // Create array of size n
            int n = 100_000_000;
            int[] array = new int[n];
            
            // Initialize array with values (1, 2, 3, ..., n)
            for (int i = 0; i < n; i++)
            {
                array[i] = i + 1;
            }
            
            // Expected sum using formula: n * (n + 1) / 2
            long expectedSum = (long)n * (n + 1) / 2;
            
            Console.WriteLine($"Array size: {n:N0}");
            Console.WriteLine($"Array: [1, 2, 3, ..., {n}]");
            Console.WriteLine($"Expected sum (formula): {expectedSum:N0}\n");
            Console.WriteLine(new string('=', 60));
            
            // ========== 1. Sequential Sum ==========
            Console.WriteLine("\n1. SEQUENTIAL SUM");
            Console.WriteLine(new string('-', 40));
            
            var watchSeq = Stopwatch.StartNew();
            long sequentialSum = SequentialSum(array);
            watchSeq.Stop();
            
            Console.WriteLine($"   Result: {sequentialSum:N0}");
            Console.WriteLine($"   Time: {watchSeq.ElapsedMilliseconds} ms");
            Console.WriteLine($"   Correct: {sequentialSum == expectedSum}");
            
            // ========== 2. Parallel Sum ==========
            Console.WriteLine("\n2. PARALLEL SUM (with local state)");
            Console.WriteLine(new string('-', 40));
            
            var watchPar = Stopwatch.StartNew();
            long parallelSum = ParallelSum(array);
            watchPar.Stop();
            
            Console.WriteLine($"   Result: {parallelSum:N0}");
            Console.WriteLine($"   Time: {watchPar.ElapsedMilliseconds} ms");
            Console.WriteLine($"   Correct: {parallelSum == expectedSum}");
            
            // ========== 3. Channel Sum ==========
            Console.WriteLine("\n3. CHANNEL SUM (Producer-Consumer)");
            Console.WriteLine(new string('-', 40));
            
            var watchChan = Stopwatch.StartNew();
            long channelSum = await ChannelSum(array);
            watchChan.Stop();
            
            Console.WriteLine($"   Result: {channelSum:N0}");
            Console.WriteLine($"   Time: {watchChan.ElapsedMilliseconds} ms");
            Console.WriteLine($"   Correct: {channelSum == expectedSum}");
            
            // ========== Summary ==========
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("PERFORMANCE SUMMARY");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"{"Algorithm",-25} {"Time (ms)",-15} {"Speedup",-10}");
            Console.WriteLine(new string('-', 50));
            Console.WriteLine($"{"Sequential",-25} {watchSeq.ElapsedMilliseconds,-15} {"1.00x",-10}");
            
            double parallelSpeedup = (double)watchSeq.ElapsedMilliseconds / Math.Max(watchPar.ElapsedMilliseconds, 1);
            Console.WriteLine($"{"Parallel",-25} {watchPar.ElapsedMilliseconds,-15} {parallelSpeedup:F2}x");
            
            double channelSpeedup = (double)watchSeq.ElapsedMilliseconds / Math.Max(watchChan.ElapsedMilliseconds, 1);
            Console.WriteLine($"{"Channel",-25} {watchChan.ElapsedMilliseconds,-15} {channelSpeedup:F2}x");
            
            Console.WriteLine(new string('=', 60));
        }
        
        /// <summary>
        /// Sequential sum - simple loop iteration
        /// </summary>
        static long SequentialSum(int[] array)
        {
            long sum = 0;
            for (int i = 0; i < array.Length; i++)
            {
                sum += array[i];
            }
            return sum;
        }
        
        /// <summary>
        /// Parallel sum using Parallel.For with local state (reduction pattern)
        /// Each thread maintains a local sum, then combines at the end
        /// </summary>
        static long ParallelSum(int[] array)
        {
            long totalSum = 0;
            object lockObj = new object();
            
            Parallel.For(0, array.Length,
                // Initialize local state for each thread
                () => 0L,
                // Body: accumulate to local state
                (i, loopState, localSum) =>
                {
                    return localSum + array[i];
                },
                // Final action: combine local sums into global sum (thread-safe)
                (localSum) =>
                {
                    lock (lockObj)
                    {
                        totalSum += localSum;
                    }
                }
            );
            
            return totalSum;
        }
        
        /// <summary>
        /// Channel-based sum using Producer-Consumer pattern
        /// Multiple producers compute partial sums, one consumer aggregates
        /// </summary>
        static async Task<long> ChannelSum(int[] array)
        {
            int numWorkers = Environment.ProcessorCount;
            int chunkSize = array.Length / numWorkers;
            
            // Create a bounded channel to hold partial sums
            var channel = Channel.CreateBounded<long>(numWorkers);
            var writer = channel.Writer;
            var reader = channel.Reader;
            
            // Producer tasks: Each computes partial sum of a chunk
            var producerTasks = new Task[numWorkers];
            for (int w = 0; w < numWorkers; w++)
            {
                int workerId = w;
                int start = workerId * chunkSize;
                int end = (workerId == numWorkers - 1) ? array.Length : start + chunkSize;
                
                producerTasks[workerId] = Task.Run(async () =>
                {
                    long partialSum = 0;
                    for (int i = start; i < end; i++)
                    {
                        partialSum += array[i];
                    }
                    await writer.WriteAsync(partialSum);
                });
            }
            
            // Wait for all producers to complete, then close the channel
            _ = Task.Run(async () =>
            {
                await Task.WhenAll(producerTasks);
                writer.Complete();
            });
            
            // Consumer: Aggregate all partial sums
            long totalSum = 0;
            await foreach (var partialSum in reader.ReadAllAsync())
            {
                totalSum += partialSum;
            }
            
            return totalSum;
        }
    }
}
