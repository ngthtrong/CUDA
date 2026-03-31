# Báo cáo: Lập trình Song song trong C# - Reduction Pattern

**Session 5: Parallel Programming & Reduction Techniques**

---

## Mục lục
1. [Giới thiệu](#1-giới-thiệu)
2. [Bài toán Factorial](#2-bài-toán-factorial)
3. [Bài toán Vector Sum](#3-bài-toán-vector-sum)
4. [Bài toán Array Sum](#4-bài-toán-array-sum-comparison)
5. [Đánh giá và So sánh](#5-đánh-giá-và-so-sánh)
6. [Kết luận](#6-kết-luận)
7. [Tài liệu tham khảo](#7-tài-liệu-tham-khảo)

---

## 1. Giới thiệu

### 1.1. Tổng quan về Lập trình Song song

Lập trình song song (Parallel Programming) là kỹ thuật chia nhỏ một tác vụ lớn thành nhiều tác vụ nhỏ hơn để thực thi đồng thời trên nhiều nhân CPU, từ đó tăng hiệu suất và giảm thời gian xử lý.

### 1.2. Reduction Pattern

**Reduction** là một design pattern phổ biến trong lập trình song song, dùng để kết hợp (combine) nhiều giá trị thành một giá trị duy nhất thông qua một phép toán kết hợp (associative operation) như:
- Tính tổng (Sum)
- Tính tích (Product)
- Tìm min/max
- Ghép nối (Concatenation)

### 1.3. Các công cụ được sử dụng trong C#

| Công cụ | Mô tả | Use case |
|---------|-------|----------|
| `Parallel.For` | Vòng lặp song song | Tính toán độc lập trên từng phần tử |
| `Channel<T>` | Hàng đợi thread-safe | Producer-Consumer pattern |
| `Interlocked` | Atomic operations | Cập nhật biến chia sẻ an toàn |
| `PLINQ` | Parallel LINQ | Truy vấn song song trên collections |

---

## 2. Bài toán Factorial

### 2.1. Mô tả bài toán

Tính giai thừa của các số từ 0 đến n-1. Bài toán này minh họa hai cách tiếp cận song song cơ bản.

### 2.2. Cách tiếp cận 1: Sử dụng Parallel.For

#### Ý tưởng
- Mỗi phép tính `i!` là **độc lập** với các phép tính khác
- Sử dụng `Parallel.For` để phân phối công việc tự động cho các thread

#### Sơ đồ thực thi

```
┌─────────────────────────────────────────────────────────┐
│                    Parallel.For(0, n)                   │
├─────────┬─────────┬─────────┬─────────┬─────────────────┤
│ Thread1 │ Thread2 │ Thread3 │ Thread4 │      ...        │
│  0! = 1 │  1! = 1 │  2! = 2 │  3! = 6 │                 │
│  7!=5040│  4! = 24│  5!=120 │  6!=720 │                 │
└─────────┴─────────┴─────────┴─────────┴─────────────────┘
          Thứ tự output KHÔNG được đảm bảo
```

#### Code minh họa

```csharp
Parallel.For(0, n, i =>
{
    long factorial = ComputeFactorial(i);
    Console.WriteLine($"{i}! = {factorial}");
});
```

#### Kết quả thực thi

```
=== Factorial using Parallel.For ===

Computing factorials from 0! to 9!:

0! = 1
7! = 5040
2! = 2
1! = 1
9! = 362880
4! = 24
6! = 720
5! = 120
8! = 40320
3! = 6

--- Parallel execution completed ---
Note: Results may appear in different order due to parallel execution
```

> **Quan sát:** Thứ tự in ra không theo thứ tự tăng dần (0, 7, 2, 1, 9...) vì các thread chạy song song và hoàn thành tại các thời điểm khác nhau.

### 2.3. Cách tiếp cận 2: Sử dụng Channel (Producer-Consumer)

#### Ý tưởng
- **Producer**: Tính toán giai thừa và gửi kết quả vào channel
- **Consumer**: Đọc kết quả từ channel và hiển thị
- Channel đảm bảo **thread-safe** và **thứ tự** (FIFO)

#### Sơ đồ Producer-Consumer

```
┌──────────────────┐         ┌──────────────────┐         ┌──────────────────┐
│    PRODUCER      │         │     CHANNEL      │         │    CONSUMER      │
│                  │         │                  │         │                  │
│  Compute 0!  ────┼────────►│  ═══════════════►│─────────┼───► Display 0!   │
│  Compute 1!  ────┼────────►│     (Buffer)     │─────────┼───► Display 1!   │
│  Compute 2!  ────┼────────►│                  │─────────┼───► Display 2!   │
│      ...         │         │      FIFO        │         │      ...         │
│  Compute 9!  ────┼────────►│                  │─────────┼───► Display 9!   │
│                  │         │                  │         │                  │
│  Complete() ─────┼────────►│  ═══════════════►│─────────┼───► End          │
└──────────────────┘         └──────────────────┘         └──────────────────┘
```

#### Code minh họa

```csharp
// Tạo channel
var channel = Channel.CreateUnbounded<(int number, long factorial)>();
var writer = channel.Writer;
var reader = channel.Reader;

// Producer
var producerTask = Task.Run(async () =>
{
    for (int i = 0; i < n; i++)
    {
        long factorial = ComputeFactorial(i);
        await writer.WriteAsync((i, factorial));
    }
    writer.Complete();
});

// Consumer
await foreach (var item in reader.ReadAllAsync())
{
    Console.WriteLine($"[Consumer] Received: {item.number}! = {item.factorial}");
}
```

#### Kết quả thực thi

```
=== Factorial using Channel (Producer-Consumer) ===

[Producer] Starting to compute factorials...

[Producer] Computed and sent: 0! = 1
[Producer] Computed and sent: 1! = 1
[Producer] Computed and sent: 2! = 2
...
[Producer] Computed and sent: 9! = 362880

[Producer] Completed writing all factorials.

[Consumer] Starting to read results...

[Consumer] Received: 0! = 1
[Consumer] Received: 1! = 1
...
[Consumer] Received: 9! = 362880

[Consumer] Finished reading all results.
```

> **Quan sát:** Kết quả được Consumer nhận theo đúng thứ tự FIFO do Channel đảm bảo.

### 2.4. So sánh hai cách tiếp cận

| Tiêu chí | Parallel.For | Channel |
|----------|--------------|---------|
| Độ phức tạp | Đơn giản | Phức tạp hơn |
| Thứ tự kết quả | Không đảm bảo | Đảm bảo FIFO |
| Mô hình | Data parallelism | Producer-Consumer |
| Use case | Tính toán độc lập | Pipeline processing |

---

## 3. Bài toán Vector Sum

### 3.1. Mô tả bài toán

Tính tổng tất cả phần tử trong một vector (mảng) có kích thước lớn. Đây là bài toán **Reduction** điển hình.

```
Vector = [1, 2, 3, ..., 1,000,000]
Sum = 1 + 2 + 3 + ... + 1,000,000 = 500,000,500,000
```

### 3.2. Các phương pháp tiếp cận

#### Method 1: Sequential Sum (Tuần tự)

```csharp
long sum = 0;
for (int i = 0; i < vectorSize; i++)
{
    sum += vector[i];
}
```

**Đặc điểm:** Đơn giản, dễ hiểu, nhưng không tận dụng được đa nhân CPU.

#### Method 2: Parallel with Interlocked

```csharp
long parallelSum = 0;
Parallel.For(0, vectorSize, i =>
{
    Interlocked.Add(ref parallelSum, vector[i]);
});
```

**Vấn đề:** Sử dụng `Interlocked.Add` cho mỗi phần tử gây ra **contention** (tranh chấp) cao giữa các thread.

#### Method 3: Parallel with Local State (Khuyến nghị)

```csharp
long parallelSum = 0;
object lockObj = new object();

Parallel.For(0, vectorSize,
    () => 0L,                              // Init local state
    (i, loopState, localSum) =>            // Body
    {
        return localSum + vector[i];
    },
    (localSum) =>                          // Final (combine)
    {
        lock (lockObj) { parallelSum += localSum; }
    }
);
```

**Sơ đồ Reduction với Local State:**

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           PHASE 1: Parallel Accumulate                  │
├─────────────────┬─────────────────┬─────────────────┬───────────────────┤
│    Thread 1     │    Thread 2     │    Thread 3     │    Thread N       │
│  localSum = 0   │  localSum = 0   │  localSum = 0   │  localSum = 0     │
│  += v[0..249k]  │  += v[250k..499]│  += v[500k..749]│  += v[750k..999k] │
│  = partial_1    │  = partial_2    │  = partial_3    │  = partial_N      │
└────────┬────────┴────────┬────────┴────────┬────────┴─────────┬─────────┘
         │                 │                 │                  │
         ▼                 ▼                 ▼                  ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      PHASE 2: Final Combine (lock)                      │
│                                                                         │
│   totalSum = partial_1 + partial_2 + partial_3 + ... + partial_N        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

#### Method 4: Manual Partitioning

```csharp
int numPartitions = Environment.ProcessorCount;
long[] partialSums = new long[numPartitions];

Parallel.For(0, numPartitions, p =>
{
    int start = p * partitionSize;
    int end = (p == numPartitions - 1) ? vectorSize : start + partitionSize;
    
    long localSum = 0;
    for (int i = start; i < end; i++)
        localSum += vector[i];
    partialSums[p] = localSum;
});

// Combine
for (int p = 0; p < numPartitions; p++)
    parallelSum += partialSums[p];
```

**Đặc điểm:** Kiểm soát hoàn toàn việc phân chia công việc.

#### Method 5: PLINQ

```csharp
long plinqSum = vector.AsParallel().Sum(x => (long)x);
```

**Đặc điểm:** Cú pháp ngắn gọn, declarative, nhưng có overhead.

### 3.3. Kết quả thực thi

```
=== Vector Sum using Parallel (Reduction) ===

Vector size: 1,000,000
Vector: [1, 2, 3, ..., 1000000]

--- Method 1: Sequential Sum ---
Sequential Sum = 500,000,500,000
Time: 1 ms

--- Method 2: Parallel with Interlocked ---
Parallel Sum (Interlocked) = 500,000,500,000
Time: 28 ms

--- Method 3: Parallel with Local State ---
Parallel Sum (Local State) = 500,000,500,000
Time: 3 ms

--- Method 4: Parallel with Partitioning ---
Parallel Sum (Partitioning, 12 partitions) = 500,000,500,000
Time: 0 ms

--- Method 5: PLINQ (Parallel LINQ) ---
PLINQ Sum = 500,000,500,000
Time: 8 ms

=== Verification ===
Expected Sum (formula n(n+1)/2) = 500,000,500,000
All results correct: True
```

### 3.4. Phân tích kết quả

| Method | Thời gian | Nhận xét |
|--------|-----------|----------|
| Sequential | 1 ms | Baseline, tận dụng CPU cache tốt |
| Interlocked | 28 ms | **Chậm nhất** do contention cao |
| Local State | 3 ms | Hiệu quả, giảm contention |
| Partitioning | < 1 ms | **Nhanh nhất**, ít overhead |
| PLINQ | 8 ms | Tiện lợi nhưng có overhead |

> **Quan sát quan trọng:** Với dữ liệu nhỏ (1 triệu phần tử), overhead của parallel có thể làm chậm hơn sequential. Parallel hiệu quả hơn với dữ liệu lớn.

---

## 4. Bài toán Array Sum Comparison

### 4.1. Mô tả bài toán

So sánh 3 phương pháp tính tổng mảng với kích thước lớn (100 triệu phần tử):
1. **Sequential** - Tuần tự
2. **Parallel** - Song song với Local State
3. **Channel** - Producer-Consumer pattern

### 4.2. Thiết kế giải pháp

#### Architecture Overview

```
┌────────────────────────────────────────────────────────────────────────────┐
│                         ARRAY [1, 2, 3, ..., 100,000,000]                  │
└────────────────────────────────────────────────────────────────────────────┘
                    │                    │                    │
                    ▼                    ▼                    ▼
         ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
         │    SEQUENTIAL    │  │     PARALLEL     │  │     CHANNEL      │
         │                  │  │                  │  │                  │
         │  Single thread   │  │   Multi-thread   │  │  Producer-       │
         │  Simple loop     │  │   Local State    │  │  Consumer        │
         └────────┬─────────┘  └────────┬─────────┘  └────────┬─────────┘
                  │                     │                     │
                  ▼                     ▼                     ▼
         ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
         │   Result: Sum    │  │   Result: Sum    │  │   Result: Sum    │
         │   Time: T1       │  │   Time: T2       │  │   Time: T3       │
         └──────────────────┘  └──────────────────┘  └──────────────────┘
```

#### Channel-based Implementation

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    CHANNEL SUM (Producer-Consumer)                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                        │
│  │  Producer 1  │ │  Producer 2  │ │  Producer N  │   N = CPU cores        │
│  │ chunk[0..k]  │ │ chunk[k..2k] │ │ chunk[...n]  │                        │
│  │ partial_1    │ │ partial_2    │ │ partial_N    │                        │
│  └──────┬───────┘ └──────┬───────┘ └──────┬───────┘                        │
│         │                │                │                                 │
│         ▼                ▼                ▼                                 │
│  ┌─────────────────────────────────────────────────────────────┐           │
│  │              BOUNDED CHANNEL (capacity = N)                 │           │
│  │  ┌─────────┬─────────┬─────────┬─────────┬─────────────────┐│           │
│  │  │partial_1│partial_2│partial_3│   ...   │    partial_N    ││           │
│  │  └─────────┴─────────┴─────────┴─────────┴─────────────────┘│           │
│  └─────────────────────────────────────────────────────────────┘           │
│                              │                                              │
│                              ▼                                              │
│                    ┌──────────────────┐                                    │
│                    │     CONSUMER     │                                    │
│                    │                  │                                    │
│                    │  totalSum = Σ    │                                    │
│                    │  partial_i       │                                    │
│                    └──────────────────┘                                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.3. Kết quả thực thi

```
=== Array Sum Algorithm Comparison ===

Array size: 100,000,000
Array: [1, 2, 3, ..., 100000000]
Expected sum (formula): 5,000,000,050,000,000

============================================================

1. SEQUENTIAL SUM
----------------------------------------
   Result: 5,000,000,050,000,000
   Time: 111 ms
   Correct: True

2. PARALLEL SUM (with local state)
----------------------------------------
   Result: 5,000,000,050,000,000
   Time: 68 ms
   Correct: True

3. CHANNEL SUM (Producer-Consumer)
----------------------------------------
   Result: 5,000,000,050,000,000
   Time: 44 ms
   Correct: True

============================================================
PERFORMANCE SUMMARY
============================================================
Algorithm                 Time (ms)       Speedup
--------------------------------------------------
Sequential                111             1.00x
Parallel                  68              1.63x
Channel                   44              2.52x
============================================================
```

### 4.4. Phân tích Speedup

```
                    Speedup Chart (Higher is Better)
    
    Channel   ████████████████████████████████████████ 2.52x
    Parallel  ██████████████████████████ 1.63x
    Sequential████████████████ 1.00x (baseline)
              ├────────┼────────┼────────┼────────┼────────┤
              0       0.5       1       1.5       2       2.5
```

---

## 5. Đánh giá và So sánh

### 5.1. Bảng tổng hợp các phương pháp

| Phương pháp | Ưu điểm | Nhược điểm | Khi nào sử dụng |
|-------------|---------|------------|-----------------|
| **Sequential** | Đơn giản, dễ debug, cache-friendly | Không tận dụng đa nhân | Dữ liệu nhỏ, logic phức tạp |
| **Parallel.For** | Tự động phân chia công việc | Overhead với dữ liệu nhỏ | Tính toán độc lập |
| **Interlocked** | Thread-safe, atomic | Contention cao | Cập nhật ít, đơn giản |
| **Local State** | Giảm contention, hiệu quả | Code phức tạp hơn | **Reduction pattern** |
| **Partitioning** | Kiểm soát hoàn toàn | Phải tự quản lý | Cần tối ưu cao |
| **PLINQ** | Cú pháp đẹp, declarative | Overhead | Prototype, đọc dễ |
| **Channel** | Pipeline, decoupling | Phức tạp, overhead | Producer-Consumer, streaming |

### 5.2. Khi nào nên song song hóa?

```
                       Decision Tree: Parallelize or Not?
    
                      ┌─────────────────────┐
                      │  Data size > 10K?   │
                      └──────────┬──────────┘
                           ┌─────┴─────┐
                          Yes          No
                           │            │
                           ▼            ▼
              ┌────────────────┐   ┌────────────────┐
              │ Task độc lập?  │   │   Sequential   │
              └───────┬────────┘   └────────────────┘
                ┌─────┴─────┐
               Yes          No
                │            │
                ▼            ▼
      ┌─────────────────┐  ┌─────────────────┐
      │    PARALLEL     │  │ Có thể chia     │
      │   Parallel.For  │  │ thành chunks?   │
      │   PLINQ         │  └────────┬────────┘
      └─────────────────┘      ┌────┴────┐
                              Yes        No
                               │          │
                               ▼          ▼
                    ┌──────────────┐  ┌──────────────┐
                    │   CHANNEL    │  │  Sequential  │
                    │   Pipeline   │  │  or Rethink  │
                    └──────────────┘  └──────────────┘
```

### 5.3. Trade-offs

#### Complexity vs Performance

```
High ┌─────────────────────────────────────────────┐
     │                                    Channel │
     │                                   ●        │
     │                          Partitioning      │
     │                             ●              │
     │                    Local State             │
C    │                       ●                    │
O    │              PLINQ                         │
M    │                ●                           │
P    │         Interlocked                        │
L    │            ●                               │
E    │    Parallel.For                            │
X    │       ●                                    │
I    │                                            │
T    │ Sequential                                 │
Y    │    ●                                       │
Low  └─────────────────────────────────────────────┘
     Low ◄────── PERFORMANCE ──────► High
```

### 5.4. Best Practices

1. **Measure First**: Luôn đo đạc trước khi tối ưu
2. **Start Simple**: Bắt đầu với Parallel.For hoặc PLINQ
3. **Reduce Contention**: Sử dụng Local State thay vì Interlocked
4. **Mind the Overhead**: Parallel không phải lúc nào cũng nhanh hơn
5. **Consider Data Size**: Parallel hiệu quả với dữ liệu lớn (>10K phần tử)

---

## 6. Kết luận

### 6.1. Những điểm chính đã học

1. **Reduction Pattern** là pattern quan trọng trong parallel programming để tổng hợp kết quả từ nhiều thread.

2. **Parallel.For với Local State** là cách tiếp cận hiệu quả nhất cho bài toán reduction, vì:
   - Giảm thiểu contention giữa các thread
   - Mỗi thread làm việc với bản sao local
   - Chỉ đồng bộ một lần ở cuối

3. **Channel** phù hợp cho:
   - Producer-Consumer pattern
   - Pipeline processing
   - Khi cần decouple các stage xử lý

4. **Không phải lúc nào parallel cũng nhanh hơn**:
   - Overhead của thread management
   - Cache effects
   - Contention

### 6.2. Recommendations

| Scenario | Recommended Approach |
|----------|---------------------|
| Simple reduction | `Parallel.For` with Local State |
| Stream processing | `Channel<T>` |
| Query-like operations | `PLINQ` |
| Fine-grained control | Manual Partitioning |
| Small data | Sequential |

---

## 7. Tài liệu tham khảo

1. **Microsoft Docs - Parallel Programming in .NET**
   - https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/

2. **Microsoft Docs - System.Threading.Channels**
   - https://docs.microsoft.com/en-us/dotnet/core/extensions/channels

3. **Microsoft Docs - Parallel.For Method**
   - https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.parallel.for

4. **Microsoft Docs - PLINQ (Parallel LINQ)**
   - https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/introduction-to-plinq

5. **Microsoft Docs - Thread-Safe Collections**
   - https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/

6. **Stephen Toub - Patterns of Parallel Programming**
   - https://www.microsoft.com/en-us/download/details.aspx?id=19222

7. **.NET Blog - An Introduction to System.Threading.Channels**
   - https://devblogs.microsoft.com/dotnet/an-introduction-to-system-threading-channels/

---

*Báo cáo được tạo ngày: 03/02/2026*

*Môi trường thực thi: .NET 8.0, Windows, 12 CPU cores*
