using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Session5
{
    /// <summary>
    /// Exercise 1b: Compute factorial using Channel (Producer-Consumer pattern)
    /// </summary>
    public class FactorialChannel
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Factorial using Channel (Producer-Consumer) ===\n");
            
            int n = 10; // Compute factorial from 0 to n-1
            
            // Create an unbounded channel to hold (number, factorial) pairs
            var channel = Channel.CreateUnbounded<(int number, long factorial)>();
            var writer = channel.Writer;
            var reader = channel.Reader;
            
            // Producer: Computes factorials and writes to channel
            var producerTask = Task.Run(async () =>
            {
                Console.WriteLine("[Producer] Starting to compute factorials...\n");
                
                for (int i = 0; i < n; i++)
                {
                    long factorial = ComputeFactorial(i);
                    await writer.WriteAsync((i, factorial));
                    Console.WriteLine($"[Producer] Computed and sent: {i}! = {factorial}");
                }
                
                // Signal that no more data will be written
                writer.Complete();
                Console.WriteLine("\n[Producer] Completed writing all factorials.");
            });
            
            // Small delay to let producer start first (for demonstration)
            await Task.Delay(100);
            
            Console.WriteLine("\n[Consumer] Starting to read results...\n");
            
            // Consumer: Reads factorials from channel
            await foreach (var item in reader.ReadAllAsync())
            {
                Console.WriteLine($"[Consumer] Received: {item.number}! = {item.factorial}");
            }
            
            Console.WriteLine("\n[Consumer] Finished reading all results.");
            
            // Wait for producer to complete
            await producerTask;
            
            Console.WriteLine("\n--- Channel-based execution completed ---");
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
