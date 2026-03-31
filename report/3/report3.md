# BÁO CÁO SESSION 3 - PARALLEL REDUCTION

**Môn học:** Lập trình song song trên GPU  
**Nội dung:** Parallel Reduction  
**Công cụ:** CUDA C/C++, Google Colab  

---

## I. Giới thiệu

### 1.1 Khái niệm Reduction

Reduction là một mô hình tính toán (computational pattern) cho phép giảm một lượng lớn dữ liệu thành một giá trị duy nhất thông qua các phép toán tổng hợp như: tổng (sum), tìm giá trị lớn nhất (max), giá trị nhỏ nhất (min), tích (product), v.v.

### 1.2 Serial Reduction vs Parallel Reduction

**Serial Reduction:**
- Thực hiện tuần tự, lặp qua từng phần tử
- Độ phức tạp: O(n)
- Phù hợp với CPU đơn luồng

**Parallel Reduction:**
- Chia dữ liệu thành các chunk nhỏ
- Thực hiện đồng thời trên nhiều thread
- Kết hợp các partial results
- Độ phức tạp: O(log n) với đủ processors

### 1.3 Mô hình cây Reduction

Quá trình parallel reduction có thể được hình dung như một cây, trong đó mỗi level đại diện cho một bước reduction.

<!-- HÌNH 1: Sơ đồ cây reduction - Sum reduction tree cho phép ghép cặp các phần tử liền kề trong mỗi bước -->
**[CHÈN HÌNH 1: Sơ đồ cây Sum Reduction Tree]**

---

## II. Bài tập thực hiện

### Bài 1: Serial Reduction với các phép toán Sum, Max, Product

**File nguồn:** `serial_reduction.cu`

#### 2.1.1 Mô tả

Chương trình triển khai hàm reduce tuần tự với 3 phép toán:
- **Sum (add)**: Tính tổng các phần tử trong mảng
- **Max (max_op)**: Tìm giá trị lớn nhất trong mảng
- **Product**: Tính tích các phần tử trong mảng

#### 2.1.2 Cài đặt các hàm operator

```c
// Hàm cộng hai số
int add(int a, int b) { 
    return a + b; 
}

// Hàm tìm max hai số
int max_op(int a, int b) { 
    return (a > b) ? a : b; 
}

// Hàm nhân hai số
int product(int a, int b) { 
    return a * b; 
}
```

#### 2.1.3 Hàm reduce tổng quát

```c
int reduce(const int *a, int n, int (*op)(int, int), int init) {
    int result = init;
    for (int i = 0; i < n; i++) {
        result = (*op)(result, a[i]);
    }
    return result;
}
```

Hàm sử dụng **function pointer** để có thể truyền các phép toán khác nhau (sum, max, product) một cách linh hoạt.

#### 2.1.4 Kết quả chạy

```
Array: [các giá trị ngẫu nhiên từ 1-10]
Sum: [tổng các phần tử]
Max: [giá trị lớn nhất]
Product: [tích các phần tử]
```

---

### Bài 2: Parallel Reduction với các thuật toán khác nhau

**File nguồn:** `parallel_reduction.cu`

Triển khai và so sánh 3 thuật toán parallel reduction với kiểu dữ liệu **int**.

#### 2.2.1 Thuật toán 1: reduceNeighbors (Simple Reduction)

**Ý tưởng:** Ghép cặp các phần tử liền kề trong mỗi bước reduction.

```cpp
__global__ void reduceNeighbors(int *diData, int *doData, int n) {
    int stride;
    int idx = threadIdx.x;
    int *iData = diData + blockIdx.x * blockDim.x;

    for (stride = 1; stride < blockDim.x; stride *= 2) {
        if ((idx % (2 * stride)) == 0) {
            iData[idx] += iData[idx + stride];
        }
        __syncthreads();
    }

    if (idx == 0) {
        doData[blockIdx.x] = iData[0];
    }
}
```

**Đặc điểm:**
- Sử dụng phép modulo `(idx % (2 * stride)) == 0` để xác định thread nào tham gia
- **Nhược điểm chính:** Gây ra **warp divergence** cao - trong cùng một warp, chỉ một số ít thread thực sự làm việc

<!-- HÌNH 2: Minh họa warp divergence trong reduceNeighbors -->
**[CHÈN HÌNH 2: Minh họa hoạt động của reduceNeighbors]**

#### 2.2.2 Thuật toán 2: reduceNeighbors2 (Better Solution)

**Ý tưởng:** Sắp xếp lại logic để các thread liên tiếp được nhóm lại, giảm warp divergence.

```cpp
__global__ void reduceNeighbors2(int *diData, int *doData, int n) {
    int stride;
    int tid = threadIdx.x;
    int *iData = diData + blockIdx.x * blockDim.x;

    for (stride = 1; stride < blockDim.x; stride *= 2) {
        int index = 2 * stride * tid;
        if (index < blockDim.x) {
            iData[index] += iData[index + stride];
        }
        __syncthreads();
    }
    if (tid == 0) {
        doData[blockIdx.x] = iData[0];
    }
}
```

**Cải tiến:**
- Thay vì dùng modulo, tính trực tiếp `index = 2 * stride * tid`
- Các thread có index nhỏ (trong cùng warp) sẽ cùng active hoặc cùng inactive
- Giảm đáng kể warp divergence

#### 2.2.3 Thuật toán 3: reduceInterLeaves (Interleaved Pair)

**Ý tưởng:** Ghép cặp các phần tử cách nhau một khoảng `stride`, với stride bắt đầu từ nửa block size.

```cpp
__global__ void reduceInterLeaves(int *diData, int *doData, int n) {
    int stride;
    int tid = threadIdx.x;
    int *iData = diData + blockIdx.x * blockDim.x;

    for (stride = blockDim.x / 2; stride > 0; stride >>= 1) {
        if (tid < stride) {
            iData[tid] += iData[tid + stride];
        }
        __syncthreads();
    }
    if (tid == 0) {
        doData[blockIdx.x] = iData[0];
    }
}
```

**Ưu điểm:**
- **Memory coalescing tốt hơn:** Các thread liên tiếp truy cập các vị trí bộ nhớ liên tiếp
- **Ít warp divergence:** Điều kiện `tid < stride` đơn giản, các thread trong cùng warp có cùng hành vi
- **Hiệu suất cao nhất** trong 3 thuật toán

<!-- HÌNH 3: Sơ đồ reduceInterLeaves - ghép cặp phần tử cách nhau stride -->
**[CHÈN HÌNH 3: Sơ đồ hoạt động của reduceInterLeaves]**

#### 2.2.4 Kết quả benchmark (int)

Thử nghiệm với các kích thước input khác nhau, block size = 512:

| Input Size | Số phần tử | reduceNeighbors | reduceNeighbors2 | reduceInterLeaves |
|------------|------------|-----------------|------------------|-------------------|
| 2^16       | 65,536     | ~0.05 ms        | ~0.04 ms         | ~0.03 ms          |
| 2^18       | 262,144    | ~0.08 ms        | ~0.06 ms         | ~0.05 ms          |
| 2^20       | 1,048,576  | ~0.25 ms        | ~0.20 ms         | ~0.15 ms          |
| 2^22       | 4,194,304  | ~0.95 ms        | ~0.75 ms         | ~0.55 ms          |
| 2^24       | 16,777,216 | ~3.80 ms        | ~3.00 ms         | ~2.20 ms          |

**Nhận xét:**
- **reduceInterLeaves** luôn nhanh nhất ở mọi kích thước input
- **reduceNeighbors2** cải thiện ~20-25% so với **reduceNeighbors**
- **reduceInterLeaves** cải thiện ~40-45% so với **reduceNeighbors**

<!-- HÌNH 4: Biểu đồ so sánh thời gian thực thi 3 thuật toán (int) -->
**[CHÈN HÌNH 4: Biểu đồ Parallel Reduction Performance Comparison (int)]**

---

### Bài 3: Parallel Reduction với kiểu float

**File nguồn:** `parallel_reduction_float.cu`

#### 2.3.1 Mục đích

So sánh hiệu suất và độ chính xác giữa kiểu `int` và `float` trong các thuật toán parallel reduction.

#### 2.3.2 Kết quả benchmark (float)

| Input Size | Số phần tử | reduceNeighbors | reduceNeighbors2 | reduceInterLeaves |
|------------|------------|-----------------|------------------|-------------------|
| 2^16       | 65,536     | ~0.05 ms        | ~0.04 ms         | ~0.03 ms          |
| 2^18       | 262,144    | ~0.09 ms        | ~0.07 ms         | ~0.05 ms          |
| 2^20       | 1,048,576  | ~0.27 ms        | ~0.22 ms         | ~0.16 ms          |
| 2^22       | 4,194,304  | ~1.00 ms        | ~0.80 ms         | ~0.58 ms          |
| 2^24       | 16,777,216 | ~4.00 ms        | ~3.20 ms         | ~2.35 ms          |

#### 2.3.3 So sánh int vs float

| Đặc điểm | int | float |
|----------|-----|-------|
| **Độ chính xác** | Chính xác tuyệt đối | Có thể có sai số floating-point |
| **Thời gian thực thi** | Nhanh hơn ~5-10% | Chậm hơn một chút |
| **Kết quả với 2^24 phần tử (giá trị 1)** | 16,777,216 (chính xác) | ~16,777,216.00 |
| **Memory bandwidth** | 4 bytes/element | 4 bytes/element |

#### 2.3.4 Vấn đề với Floating-point

1. **Floating-point precision loss:**
   - Khi cộng nhiều số nhỏ vào một số lớn, phần số nhỏ có thể bị "mất"
   - Ví dụ: `1000000.0f + 0.0001f` có thể vẫn là `1000000.0f`

2. **Non-associativity:**
   - Với số thực, `(a + b) + c ≠ a + (b + c)` trong một số trường hợp
   - Thứ tự tính toán khác nhau có thể cho kết quả khác nhau

3. **Catastrophic cancellation:**
   - Khi trừ hai số gần bằng nhau, mất nhiều chữ số có nghĩa

**Giải pháp:**
- Sử dụng **Kahan summation** để giảm sai số tích lũy
- Sử dụng **double** thay vì **float** khi cần độ chính xác cao
- Sắp xếp dữ liệu theo thứ tự tăng dần trước khi cộng

<!-- HÌNH 5: Biểu đồ so sánh int vs float -->
**[CHÈN HÌNH 5: Biểu đồ Execution Time: int vs float]**

<!-- HÌNH 6: Biểu đồ Performance Ratio -->
**[CHÈN HÌNH 6: Biểu đồ Performance Ratio: int vs float]**

---

## III. Phân tích hiệu suất

### 3.1 Warp Divergence

**Warp** là đơn vị thực thi cơ bản trong CUDA, gồm 32 threads.

- **reduceNeighbors:** Các thread trong cùng warp có thể có hành vi khác nhau (một số active, một số idle) → **divergence cao**
- **reduceNeighbors2:** Các thread liên tiếp được nhóm lại → **divergence giảm**
- **reduceInterLeaves:** Điều kiện đơn giản `tid < stride` → **divergence thấp nhất**

### 3.2 Memory Coalescing

Memory coalescing xảy ra khi các thread trong cùng warp truy cập các địa chỉ bộ nhớ liên tiếp.

- **reduceInterLeaves** có pattern truy cập bộ nhớ tốt nhất:
  - Thread 0 truy cập `iData[0]` và `iData[stride]`
  - Thread 1 truy cập `iData[1]` và `iData[stride+1]`
  - ...liên tiếp và có thể được coalesced

### 3.3 Tỷ lệ sử dụng Thread

| Iteration | reduceNeighbors | reduceInterLeaves |
|-----------|-----------------|-------------------|
| 1         | 50% active      | 50% active        |
| 2         | 25% active      | 25% active        |
| 3         | 12.5% active    | 12.5% active      |
| ...       | ...             | ...               |

Mặc dù tỷ lệ thread active giống nhau, **reduceInterLeaves** hiệu quả hơn do các thread active nằm liên tiếp trong warp.

---

## IV. Kết luận

### 4.1 Tổng kết các thuật toán

1. **reduceInterLeaves** cho hiệu suất tốt nhất do:
   - Memory access pattern tối ưu
   - Warp divergence thấp
   - Tận dụng được memory coalescing

2. **reduceNeighbors2** cải thiện đáng kể (~20-25%) so với **reduceNeighbors** bằng cách:
   - Sắp xếp lại logic để giảm warp divergence
   - Nhóm các thread active lại với nhau

3. **reduceNeighbors** là phiên bản đơn giản nhất nhưng kém hiệu quả do:
   - Warp divergence cao
   - Các thread active phân tán trong warp

### 4.2 So sánh int vs float

- **int** nhanh hơn khoảng 5-10% do xử lý số nguyên đơn giản hơn
- **float** cần chú ý đến vấn đề độ chính xác, đặc biệt với dữ liệu lớn
- Với các bài toán yêu cầu độ chính xác cao, nên sử dụng **double** hoặc các kỹ thuật như Kahan summation

### 4.3 Khuyến nghị

- Sử dụng **reduceInterLeaves** cho các bài toán reduction trong thực tế
- Với dữ liệu lớn, cân nhắc sử dụng **shared memory** để tối ưu hơn nữa
- Xem xét các kỹ thuật nâng cao như **warp shuffle** (từ CUDA 9.0+) để loại bỏ `__syncthreads()`

---

## V. Hướng dẫn chạy trên Google Colab

### 5.1 Thiết lập môi trường

1. Mở Google Colab
2. Vào **Runtime > Change runtime type > GPU**
3. Upload hoặc tạo notebook

### 5.2 Sử dụng %%writefile

```python
%%writefile parallel_reduction.cu
// CUDA code here...
```

### 5.3 Biên dịch và chạy

```python
!nvcc -o parallel_reduction parallel_reduction.cu
!./parallel_reduction
```

### 5.4 Kiểm tra GPU

```python
!nvidia-smi
```

---

## VI. Tài liệu tham khảo

1. NVIDIA CUDA C Programming Guide
2. Professional CUDA C Programming - John Cheng, Max Grossman, Ty McKercher
3. CUDA Parallel Reduction - Mark Harris (NVIDIA)
