using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace CSharpParallel
{
    class Program
    {
        static void Main(string[] args)
        {
            var totalSw = Stopwatch.StartNew();

            // ================================================================
            //  HEADER + ENVIRONMENT INFO
            // ================================================================
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Session 9 - Sorting & Counting Parallel Algorithms     ║");
            Console.WriteLine("║  CT225H - Parallel Programming with CUDA                ║");
            Console.WriteLine("║  Nguyen Thanh Trong - B2305615                          ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Thông tin môi trường
            Console.WriteLine("──────────────── ENVIRONMENT INFO ────────────────────────");
            Console.WriteLine($"  OS            : {RuntimeInformation.OSDescription}");
            Console.WriteLine($"  Architecture  : {RuntimeInformation.OSArchitecture}");
            Console.WriteLine($"  .NET Version  : {RuntimeInformation.FrameworkDescription}");
            Console.WriteLine($"  CPU Cores     : {Environment.ProcessorCount} logical processors");
            Console.WriteLine($"  Timestamp     : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  Process Memory: {Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0:F1} MB (startup)");
            Console.WriteLine("──────────────────────────────────────────────────────────");
            Console.WriteLine();

            // ================================================================
            //  KHỞI TẠO DỮ LIỆU
            // ================================================================
            Console.WriteLine("► Khởi tạo dữ liệu...");
            var initSw = Stopwatch.StartNew();
            var rand = new Random(42);
            int[] data = new int[Config.ARRAY_SIZE];
            for (int i = 0; i < Config.ARRAY_SIZE; i++)
                data[i] = rand.Next(Config.MAX_VALUE);
            initSw.Stop();

            Console.WriteLine($"  Array size    : {Config.ARRAY_SIZE:N0} elements");
            Console.WriteLine($"  Value range   : [0, {Config.MAX_VALUE - 1}]");
            Console.WriteLine($"  Init time     : {initSw.Elapsed.TotalMilliseconds:F2} ms");
            Console.WriteLine($"  Memory after  : {Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0:F1} MB");

            // Xem trước dữ liệu
            Console.WriteLine($"  First 10      : [{string.Join(", ", data.Take(10))}]");
            Console.WriteLine($"  Last  10      : [{string.Join(", ", data.Skip(Config.ARRAY_SIZE - 10))}]");

            // Thống kê cơ bản
            int minVal = data.Min();
            int maxVal = data.Max();
            double avg = data.Average();
            Console.WriteLine($"  Min / Max     : {minVal} / {maxVal}");
            Console.WriteLine($"  Average       : {avg:F2}");
            Console.WriteLine();

            // Lưu kết quả timing để tạo bảng tổng hợp
            double[] timesCountOcc = new double[3];
            double[] timesCountSort = new double[3];
            double[] timesRadixSort = new double[3];

            // ================================================================
            //  BÀI TOÁN 1: ĐẾM SỐ LẦN XUẤT HIỆN
            // ================================================================
            DisplayHelper.PrintSectionHeader("BÀI TOÁN 1: ĐẾM SỐ LẦN XUẤT HIỆN CỦA MỖI SỐ");

            Console.WriteLine("  ► Running Sequential...");
            int[] seqHist = CountingOccurrences.Sequential(data, out timesCountOcc[0]);

            Console.WriteLine("  ► Running Thread/Task...");
            int[] taskHist = CountingOccurrences.ThreadTask(data, out timesCountOcc[1]);

            Console.WriteLine("  ► Running Parallel.For...");
            int[] parHist = CountingOccurrences.ParallelFor(data, out timesCountOcc[2]);

            Console.WriteLine();
            Console.WriteLine("  ── Verification ──");
            Console.WriteLine($"  Thread/Task vs Sequential : {VerificationHelper.VerifyHistogram(seqHist, taskHist)}");
            Console.WriteLine($"  Parallel    vs Sequential : {VerificationHelper.VerifyHistogram(seqHist, parHist)}");

            // Hiển thị mẫu histogram: top 5 giá trị xuất hiện nhiều nhất
            Console.WriteLine();
            Console.WriteLine("  ── Histogram Sample (top 5 most frequent values) ──");
            var top5 = seqHist.Select((count, idx) => (Value: idx, Count: count))
                              .OrderByDescending(x => x.Count)
                              .Take(5);
            Console.WriteLine("  {0,-10} {1,-15}", "Value", "Occurrences");
            foreach (var item in top5)
                Console.WriteLine("  {0,-10} {1,-15:N0}", item.Value, item.Count);

            DisplayHelper.PrintAlgorithmSummary("Counting Occurrences", timesCountOcc);
            Console.WriteLine();

            // ================================================================
            //  BÀI TOÁN 2: COUNTING SORT
            // ================================================================
            DisplayHelper.PrintSectionHeader("BÀI TOÁN 2: COUNTING SORT");

            Console.WriteLine("  ► Running Sequential...");
            int[] seqSorted = CountingSort.Sequential(data, out timesCountSort[0]);

            Console.WriteLine("  ► Running Thread/Task...");
            int[] taskSorted = CountingSort.ThreadTask(data, out timesCountSort[1]);

            Console.WriteLine("  ► Running Parallel.For...");
            int[] parSorted = CountingSort.ParallelFor(data, out timesCountSort[2]);

            Console.WriteLine();
            Console.WriteLine("  ── Verification ──");
            Console.WriteLine($"  Thread/Task vs Sequential : {VerificationHelper.VerifyArray(seqSorted, taskSorted)}");
            Console.WriteLine($"  Parallel    vs Sequential : {VerificationHelper.VerifyArray(seqSorted, parSorted)}");

            // Hiển thị mẫu kết quả sắp xếp
            Console.WriteLine();
            Console.WriteLine("  ── Sorted Output Sample ──");
            Console.WriteLine($"  First 10 : [{string.Join(", ", seqSorted.Take(10))}]");
            Console.WriteLine($"  Last  10 : [{string.Join(", ", seqSorted.Skip(Config.ARRAY_SIZE - 10))}]");

            DisplayHelper.PrintAlgorithmSummary("Counting Sort", timesCountSort);
            Console.WriteLine();

            // ================================================================
            //  BÀI TOÁN 3: RADIX SORT
            // ================================================================
            DisplayHelper.PrintSectionHeader("BÀI TOÁN 3: RADIX SORT (LSD, base 256)");

            Console.WriteLine("  ► Running Sequential...");
            int[] seqRadix = RadixSort.Sequential(data, out timesRadixSort[0]);

            Console.WriteLine("  ► Running Thread/Task...");
            int[] taskRadix = RadixSort.ThreadTask(data, out timesRadixSort[1]);

            Console.WriteLine("  ► Running Parallel.For...");
            int[] parRadix = RadixSort.ParallelFor(data, out timesRadixSort[2]);

            Console.WriteLine();
            Console.WriteLine("  ── Verification ──");
            Console.WriteLine($"  Thread/Task vs Sequential : {VerificationHelper.VerifyArray(seqRadix, taskRadix)}");
            Console.WriteLine($"  Parallel    vs Sequential : {VerificationHelper.VerifyArray(seqRadix, parRadix)}");

            // Hiển thị mẫu kết quả
            Console.WriteLine();
            Console.WriteLine("  ── Sorted Output Sample ──");
            Console.WriteLine($"  First 10 : [{string.Join(", ", seqRadix.Take(10))}]");
            Console.WriteLine($"  Last  10 : [{string.Join(", ", seqRadix.Skip(Config.ARRAY_SIZE - 10))}]");

            // Kiểm tra đúng thứ tự sắp xếp
            bool isSorted = true;
            for (int i = 1; i < seqRadix.Length; i++)
            {
                if (seqRadix[i] < seqRadix[i - 1]) { isSorted = false; break; }
            }
            Console.WriteLine($"  Is sorted?   : {(isSorted ? "YES ✓" : "NO ✗")}");

            DisplayHelper.PrintAlgorithmSummary("Radix Sort", timesRadixSort);
            Console.WriteLine();

            // ================================================================
            //  GRAND SUMMARY TABLE
            // ================================================================
            totalSw.Stop();

            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║             GRAND SUMMARY - ALL ALGORITHMS              ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════╣");
            Console.WriteLine("║ {0,-20} │ {1,10} │ {2,10} │ {3,10} ║",
                "Algorithm", "Sequential", "Task", "Parallel");
            Console.WriteLine("╠══════════════════════════════════════════════════════════╣");

            DisplayHelper.PrintGrandRow("Count Occurrences", timesCountOcc);
            DisplayHelper.PrintGrandRow("Counting Sort", timesCountSort);
            DisplayHelper.PrintGrandRow("Radix Sort", timesRadixSort);

            Console.WriteLine("╠══════════════════════════════════════════════════════════╣");
            Console.WriteLine("║ {0,-20} │ {1,10} │ {2,10} │ {3,10} ║",
                "Speedup", "", "vs Seq", "vs Seq");
            Console.WriteLine("╠══════════════════════════════════════════════════════════╣");

            DisplayHelper.PrintSpeedupRow("Count Occurrences", timesCountOcc);
            DisplayHelper.PrintSpeedupRow("Counting Sort", timesCountSort);
            DisplayHelper.PrintSpeedupRow("Radix Sort", timesRadixSort);

            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            Console.WriteLine($"  Total execution time : {totalSw.Elapsed.TotalSeconds:F2} seconds");
            Console.WriteLine($"  Final memory usage   : {Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0:F1} MB");
            Console.WriteLine();
            Console.WriteLine("══════════════════════════════════════════════════════════");
            Console.WriteLine("  HOÀN TẤT TẤT CẢ CÁC BÀI TOÁN ✓");
            Console.WriteLine("══════════════════════════════════════════════════════════");
        }
    }
}
