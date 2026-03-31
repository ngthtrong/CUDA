using System;
using System.Threading.Tasks;

namespace Session5
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Nếu có tham số command line, chạy trực tiếp bài tập tương ứng
            if (args.Length > 0)
            {
                await RunExercise(args[0]);
                return;
            }

            // Nếu không có tham số, hiển thị menu tương tác
            while (true)
            {
                Console.WriteLine("╔══════════════════════════════════════════════════════╗");
                Console.WriteLine("║          SESSION 5 - REDUCTION 2 (C# & CUDA)         ║");
                Console.WriteLine("║          Parallel Programming in C#                  ║");
                Console.WriteLine("╠══════════════════════════════════════════════════════╣");
                Console.WriteLine("║  1. Exercise 1a: Factorial using Parallel.For        ║");
                Console.WriteLine("║  2. Exercise 1b: Factorial using Channel             ║");
                Console.WriteLine("║  3. Exercise 2a: Vector Sum using Parallel           ║");
                Console.WriteLine("║  4. Exercise 3:  Array Sum Comparison (Seq/Par/Chan) ║");
                Console.WriteLine("║  0. Exit                                             ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════╝");
                Console.Write("\nSelect an option (1-4 or 0): ");

                string? choice = Console.ReadLine();

                if (choice == "0")
                {
                    Console.WriteLine("Goodbye!");
                    return;
                }

                Console.WriteLine();
                await RunExercise(choice);

                Console.WriteLine("\n\nPress Enter to continue...");
                Console.ReadLine();
                Console.WriteLine("\n" + new string('=', 60) + "\n");
            }
        }

        static async Task RunExercise(string? choice)
        {
            switch (choice)
            {
                case "1":
                    FactorialParallel.Run();
                    break;
                case "2":
                    await FactorialChannel.RunAsync();
                    break;
                case "3":
                    VectorSumParallel.Run();
                    break;
                case "4":
                    await ArraySumComparison.RunAsync();
                    break;
                default:
                    Console.WriteLine("Invalid option. Please enter 1-4 or 0.");
                    break;
            }
        }
    }
}
