using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Session5
{
    /// <summary>
    /// Exercise 2a: Implement vector sum using Parallel (Reduction pattern)
    /// </summary>
    public class VectorSumParallel
    {
        public static void Run()
        {
            Console.WriteLine("=== Vector Sum using Parallel (Reduction) ===\n");
            
            // Create a sample vector
            int vectorSize = 1000000;
            int[] vector = new int[vectorSize];
            
            // Initialize vector with values (1, 2, 3, ..., n)
            for (int i = 0; i < vectorSize; i++)
            {
                vector[i] = i + 1;
            }
            
            Console.WriteLine($"Vector size: {vectorSize:N0}");
            Console.WriteLine($"Vector: [1, 2, 3, ..., {vectorSize}]");
            
            // ========== Method 1: Sequential Sum ==========
            Console.WriteLine("\n--- Method 1: Sequential Sum ---");
            var watch1 = System.Diagnostics.Stopwatch.StartNew();
            
            long sequentialSum = 0;
            for (int i = 0; i < vectorSize; i++)
            {
                sequentialSum += vector[i];
            }
            
            watch1.Stop();
            Console.WriteLine($"Sequential Sum = {sequentialSum:N0}");
            Console.WriteLine($"Time: {watch1.ElapsedMilliseconds} ms");
            
            // ========== Method 2: Parallel with Interlocked (Thread-safe) ==========
            Console.WriteLine("\n--- Method 2: Parallel with Interlocked ---");
            var watch2 = System.Diagnostics.Stopwatch.StartNew();
            
            long parallelSum1 = 0;
            Parallel.For(0, vectorSize, i =>
            {
                Interlocked.Add(ref parallelSum1, vector[i]);
            });
            
            watch2.Stop();
            Console.WriteLine($"Parallel Sum (Interlocked) = {parallelSum1:N0}");
            Console.WriteLine($"Time: {watch2.ElapsedMilliseconds} ms");
            
            // ========== Method 3: Parallel with Local State (More efficient) ==========
            Console.WriteLine("\n--- Method 3: Parallel with Local State ---");
            var watch3 = System.Diagnostics.Stopwatch.StartNew();
            
            long parallelSum2 = 0;
            object lockObj = new object();
            
            Parallel.For(0, vectorSize,
                // Initialize local state for each thread
                () => 0L,
                // Body: accumulate to local state
                (i, loopState, localSum) =>
                {
                    return localSum + vector[i];
                },
                // Final action: combine local sums into global sum
                (localSum) =>
                {
                    lock (lockObj)
                    {
                        parallelSum2 += localSum;
                    }
                }
            );
            
            watch3.Stop();
            Console.WriteLine($"Parallel Sum (Local State) = {parallelSum2:N0}");
            Console.WriteLine($"Time: {watch3.ElapsedMilliseconds} ms");
            
            // ========== Method 4: Parallel with Partitioning ==========
            Console.WriteLine("\n--- Method 4: Parallel with Partitioning ---");
            var watch4 = System.Diagnostics.Stopwatch.StartNew();
            
            long parallelSum3 = 0;
            int numPartitions = Environment.ProcessorCount;
            int partitionSize = vectorSize / numPartitions;
            long[] partialSums = new long[numPartitions];
            
            Parallel.For(0, numPartitions, p =>
            {
                int start = p * partitionSize;
                int end = (p == numPartitions - 1) ? vectorSize : start + partitionSize;
                
                long localSum = 0;
                for (int i = start; i < end; i++)
                {
                    localSum += vector[i];
                }
                partialSums[p] = localSum;
            });
            
            // Combine partial sums
            for (int p = 0; p < numPartitions; p++)
            {
                parallelSum3 += partialSums[p];
            }
            
            watch4.Stop();
            Console.WriteLine($"Parallel Sum (Partitioning, {numPartitions} partitions) = {parallelSum3:N0}");
            Console.WriteLine($"Time: {watch4.ElapsedMilliseconds} ms");
            
            // ========== Method 5: PLINQ (Parallel LINQ) ==========
            Console.WriteLine("\n--- Method 5: PLINQ (Parallel LINQ) ---");
            var watch5 = System.Diagnostics.Stopwatch.StartNew();
            
            long plinqSum = vector.AsParallel().Sum(x => (long)x);
            
            watch5.Stop();
            Console.WriteLine($"PLINQ Sum = {plinqSum:N0}");
            Console.WriteLine($"Time: {watch5.ElapsedMilliseconds} ms");
            
            // ========== Verification ==========
            Console.WriteLine("\n=== Verification ===");
            long expectedSum = (long)vectorSize * (vectorSize + 1) / 2; // Formula: n(n+1)/2
            Console.WriteLine($"Expected Sum (formula n(n+1)/2) = {expectedSum:N0}");
            
            bool allCorrect = (sequentialSum == expectedSum) && 
                              (parallelSum1 == expectedSum) && 
                              (parallelSum2 == expectedSum) &&
                              (parallelSum3 == expectedSum) &&
                              (plinqSum == expectedSum);
            
            Console.WriteLine($"All results correct: {allCorrect}");
        }
    }
}
