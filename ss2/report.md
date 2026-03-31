# BÁO CÁO THỰC HÀNH: LẬP TRÌNH SONG SONG VỚI CUDA

**Chủ đề:** Tối ưu hóa cấu hình Grid/Block cho bài toán Cộng Vector và Cộng Ma trận

**Họ tên:** Nguyễn Thanh Trọng
**MSSV:** B2305615
**Ngày thực hiện:** 06/01/2026

---

## I. Giới thiệu chung

### 1. Mục tiêu

Báo cáo này trình bày kết quả thực nghiệm việc triển khai lại hai bài toán cơ bản trong lập trình CUDA:

1. **Vector Sum (Cộng Vector):** Tính tổng hai mảng 1 chiều lớn.
2. **Matrix Sum (Cộng Ma trận):** Tính tổng hai ma trận 2 chiều.

Mục tiêu chính là khảo sát ảnh hưởng của các cấu hình phân luồng (Grid/Block dimensions) khác nhau đến thời gian thực thi (Kernel Execution Time) trên GPU NVIDIA RTX 2060 Super.

### 2. Cơ sở lý thuyết

* **Mô hình SIMT:** GPU thực thi các lệnh theo nhóm thread gọi là Warp (kích thước chuẩn là 32 threads trên kiến trúc NVIDIA).
* **Occupancy:** Hiệu suất phụ thuộc vào khả năng che giấu độ trễ bộ nhớ (memory latency hiding) bằng cách duy trì đủ số lượng warp hoạt động trên mỗi Streaming Multiprocessor (SM).

---

## II. Môi trường thực nghiệm

Để đảm bảo tính nhất quán, thực nghiệm được tiến hành trên nền tảng đám mây LeetGPU với cấu hình sau:

* **Phần cứng (GPU):** NVIDIA GeForce RTX 2060 Super
  * **Kiến trúc:** Turing
  * **Số nhân CUDA:** 2176 Cores
  * **Bộ nhớ VRAM:** 8 GB GDDR6
  * **Bus Width:** 256-bit (Ảnh hưởng lớn đến tốc độ truyền tải dữ liệu).
* **Phần mềm:**
  * **OS:** Ubuntu 22.04 LTS (Container Environment)
  * **CUDA Toolkit:** Version 12.8.0
  * **Compiler:** nvcc

---

## III. Bài toán 1: Cộng Vector (Vector Sum)

### 1. Thiết lập bài toán

* **Kích thước Vector ($N$):** $2^{24} = 16,777,216$ phần tử (tương đương khoảng 64MB dữ liệu mỗi vector).
* **Công thức Kernel:** `int i = blockIdx.x * blockDim.x + threadIdx.x;`
* **Grid Size:** Được tính tự động theo công thức `(N + blockDim.x - 1) / blockDim.x`.

### 2. Kết quả thực nghiệm

Dưới đây là bảng so sánh thời gian thực thi kernel (đã loại bỏ thời gian copy dữ liệu Host-to-Device) với các kích thước Block khác nhau.

*Đơn vị: mili-giây (ms)*

| STT | Cấu hình (Threads/Block) | Grid Size (Blocks) | Thời gian Kernel (ms) | Nhận xét hiệu năng                                                      |
| :-: | :------------------------: | :----------------: | :--------------------: | :-------------------------------------------------------------------------- |
|  1  |   **32** (1 Warp)   |      524,288      |      4183.55 ms      | **Chậm nhất.** Overhead quản lý block quá lớn, occupancy thấp. |
|  2  |  **128** (4 Warps)  |      131,072      |      3882.84 ms      | Hiệu năng có cải thiện .                                               |
|  3  |  **256** (8 Warps)  |       65,536       | **3307.27 ms** | **Tối ưu nhất.** Cân bằng tốt giữa số block và số warp.     |
|  4  |  **512** (16 Warps)  |       32,768       |      3394.46 ms      | Tương đương 256, chênh lệch không đáng kể.                       |
|  5  | **1024** (32 Warps) |       16,384       |      3771.71 ms      | Hơi chậm lại do giảm tính linh hoạt trong lập lịch block.           |

### 3. Phân tích

* **Cấu hình Block = 32:** Hiệu năng rất tệ vì mỗi block chỉ có 1 warp. Khi warp này đợi truy cập bộ nhớ toàn cục (Global Memory), phần cứng không có warp nào khác trong cùng block để chuyển sang thực thi, dẫn đến lãng phí tài nguyên tính toán.
* **Cấu hình Block = 256:** Đây là "điểm ngọt" (sweet spot) cho RTX 2060 Super. Số lượng threads đủ lớn để che giấu độ trễ bộ nhớ, nhưng cũng tạo ra đủ số lượng blocks để phân phối đều cho 34 SM (Streaming Multiprocessors) của GPU này.

---

## IV. Bài toán 2: Cộng Ma trận (Matrix Sum)

### 1. Thiết lập bài toán

* **Kích thước Ma trận:** $4096 \times 4096$ ($M \times N$). Tổng số phần tử: $\approx 16.7$ triệu (tương đương bài toán Vector).
* **Ánh xạ mảng:** Ma trận 2D được trải phẳng (flatten) thành mảng 1D trong bộ nhớ.
* **Công thức Index:** `idx = (threadIdx.y + blockIdx.y * blockDim.y) * Width + (threadIdx.x + blockIdx.x * blockDim.x);`

### 2. Kết quả thực nghiệm

Thay đổi kích thước `blockDim` theo 2 chiều (x, y).

| STT | Cấu hình Block (x, y) | Tổng Threads | Grid Size (x, y) |    Thời gian (ms)    | Nhận xét                                                                   |
| :-: | :---------------------: | :-----------: | :--------------: | :-------------------: | :--------------------------------------------------------------------------- |
|  1  |     **8 x 8**     |      64      |    512 x 512    |      9706.36 ms      | Chậm. Block size quá nhỏ.                                                 |
|  2  |    **16 x 16**    |      256      |    256 x 256    | **6874.49 ms** | **Rất tốt.** Đạt hiệu năng tương đương Vector Sum tối ưu. |
|  3  |    **32 x 32**    |     1024     |    128 x 128    |      7375.5 ms      | Khá tốt, nhưng max threads/block làm giảm occupancy lý thuyết.        |
|  4  |    **32 x 8**    |      256      |    128 x 512    | **6470.51 ms** | **Tối ưu nhất.** Warp (32 threads) được căn chỉnh hoàn hảo.  |
|  5  |    **8 x 32**    |      256      |    512 x 128    |      8996.50 ms      | **Kém.** Do vấn đề *Memory Coalescing*.                          |

### 3. Phân tích chuyên sâu

* **Tại sao 32x8 nhanh hơn 8x32 dù cùng số thread?**
  * Trong CUDA, các thread trong một Warp (32 threads liên tiếp) truy cập bộ nhớ tốt nhất khi chúng đọc các địa chỉ bộ nhớ liền kề nhau (Memory Coalescing).
  * **Cấu hình 32x8:** `threadIdx.x` biến thiên nhanh nhất (từ 0 đến 31). Điều này khớp với việc dữ liệu ma trận được lưu trữ theo hàng (Row-major). 32 threads đọc 32 số float liên tiếp -> **1 giao dịch bộ nhớ.**
  * **Cấu hình 8x32:** `threadIdx.x` chỉ chạy từ 0 đến 7. Một Warp sẽ bị ngắt quãng dòng dữ liệu, dẫn đến việc bộ điều khiển bộ nhớ phải thực hiện nhiều giao dịch lẻ tẻ để lấy dữ liệu -> **Băng thông giảm sút nghiêm trọng.**

---

## V. Kết luận và Kiến nghị

### 1. Kết luận chung

Từ kết quả thực nghiệm trên RTX 2060 Super với CUDA 12.8, ta rút ra các kết luận sau:

1. **Block Size không nên quá nhỏ:** Kích thước dưới 128 threads thường dẫn đến hiệu suất kém do không tận dụng hết khả năng che giấu độ trễ của GPU.
2. **Block Size không nên quá lớn:** 1024 threads (tối đa) không phải lúc nào cũng tốt nhất do giới hạn về thanh ghi (registers) và bộ nhớ chia sẻ (shared memory) trên mỗi SM.
3. **Vấn đề chiều của Block (Dimension):** Đối với xử lý dữ liệu 2D/Ma trận, chiều `x` của block (`blockDim.x`) nên là bội số của 32 (Warp size) để tối ưu hóa truy cập bộ nhớ (Memory Coalescing).

### 2. Cấu hình khuyến nghị cho RTX 2060 Super

* **Bài toán Vector 1D:** Sử dụng `blockDim.x = 256` hoặc `512`.
* **Bài toán Ma trận 2D:** Sử dụng `blockDim(32, 8)` hoặc `blockDim(32, 16)`. Tránh các cấu hình có chiều `x` nhỏ hơn 16.

---

## Tài liệu tham khảo

1. **NVIDIA Corporation.** *CUDA C++ Programming Guide*. [Online]. Available: https://docs.nvidia.com/cuda/cuda-c-programming-guide/
2. **Tài liệu bài giảng môn học.** (Các ví dụ Example 2 - Slide 17, Timer - Slide 21).
