using System;
using System.Threading.Tasks;

namespace Session5
{
    /// <summary>
    /// Exercise 1a: Compute factorial using Parallel.For
    /// </summary>
    public class FactorialParallel
    {
        public static void Run()
        {
            Console.WriteLine("=== Factorial using Parallel.For ===\n");
            
            int n = 10; // Compute factorial from 0 to n-1
            
            Console.WriteLine($"Computing factorials from 0! to {n-1}!:\n");
            
            // Using Parallel.For to compute factorials in parallel
            Parallel.For(0, n, i =>
            {
                // Add small random delay to demonstrate parallel execution order
                
                long factorial = ComputeFactorial(i);
                Console.WriteLine($"{i}! = {factorial}");
            });
            
            Console.WriteLine("\n--- Parallel execution completed ---");
            Console.WriteLine("Note: Results may appear in different order due to parallel execution");
            
            // // Sequential version for comparison
            // Console.WriteLine("\n=== Sequential version for comparison ===\n");
            // for (int i = 0; i < n; i++)
            // {
            //     long factorial = ComputeFactorial(i);
            //     Console.WriteLine($"{i}! = {factorial}");
            // }
        }
        
        /// <summary>
        /// Computes factorial of a number
        /// </summary>
        public static long ComputeFactorial(int num)
        {
            long result = 1;
            for (int j = 1; j <= num; j++)
            {
                result *= j;
            }
            return result;
        }
    }
}
