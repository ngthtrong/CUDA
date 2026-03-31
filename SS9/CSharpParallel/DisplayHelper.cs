using System;

namespace CSharpParallel
{
    /// <summary>
    /// Các phương thức hiển thị: header, bảng tổng hợp, summary.
    /// </summary>
    static class DisplayHelper
    {
        public static void PrintSectionHeader(string title)
        {
            Console.WriteLine("┌──────────────────────────────────────────────────────────┐");
            Console.WriteLine($"│  {title,-56} │");
            Console.WriteLine("└──────────────────────────────────────────────────────────┘");
            Console.WriteLine();
        }

        public static void PrintAlgorithmSummary(string name, double[] times)
        {
            double speedupTask = times[0] / times[1];
            double speedupPar = times[0] / times[2];

            Console.WriteLine($"  ── Summary: {name} ──");
            Console.WriteLine("  {0,-16} {1,12} {2,10}", "Method", "Time (ms)", "Speedup");
            Console.WriteLine("  {0,-16} {1,12} {2,10}", "────────────────", "──────────", "────────");
            Console.WriteLine("  {0,-16} {1,12:F4} {2,10}", "Sequential", times[0], "1.00x");
            Console.WriteLine("  {0,-16} {1,12:F4} {2,9:F2}x", "Thread/Task", times[1], speedupTask);
            Console.WriteLine("  {0,-16} {1,12:F4} {2,9:F2}x", "Parallel.For", times[2], speedupPar);

            // Tìm phương pháp nhanh nhất
            string[] names = { "Sequential", "Thread/Task", "Parallel.For" };
            int bestIdx = Array.IndexOf(times, times.Min());
            Console.WriteLine($"  ★ Fastest: {names[bestIdx]} ({times[bestIdx]:F4} ms)");
        }

        public static void PrintGrandRow(string name, double[] times)
        {
            Console.WriteLine("║ {0,-20} │ {1,8:F2}ms │ {2,8:F2}ms │ {3,8:F2}ms ║",
                name, times[0], times[1], times[2]);
        }

        public static void PrintSpeedupRow(string name, double[] times)
        {
            Console.WriteLine("║ {0,-20} │ {1,10} │ {2,9:F2}x │ {3,9:F2}x ║",
                name, "—", times[0] / times[1], times[0] / times[2]);
        }
    }
}
