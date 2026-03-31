
### 1. Trích xuất nội dung (`Session9`)

Các khái niệm và kỹ thuật của buổi học (Session 9), đóng vai trò là "gợi ý" hoặc nền tảng lý thuyết để giải quyết bài tập.

**Nội dung:**

> **1. Concurrency vs parallelism** (Đồng thời và Song song)
>
> **2. Implementation** (Cài đặt/Thực thi)
>
> * Sequence (Tuần tự)
> * Parallel (Song song)
> * 1 alg: composed of some tasks -> dependency --> create DAG (1 thuật toán: bao gồm nhiều tác vụ -> có sự phụ thuộc lẫn nhau --> tạo đồ thị DAG - Directed Acyclic Graph)
> * Parallel: divide input into some chunks (Song song: chia dữ liệu đầu vào thành nhiều phần/cục nhỏ)
>
> **3. Some alg patterns** (Một số mẫu thuật toán)
>
> * Reduction: collections + operation -> a scalar -> vector sum, max, min (Rút gọn: tập hợp + phép toán -> một giá trị vô hướng -> ví dụ: tính tổng vector, tìm max, min)
> * Prefix sum (i^{th}) (Tổng tiền tố)
>   * + 1, 2, 3: DAG: 3 --> 2 --> 1
>
> **4. Techniques** (Các kỹ thuật)
>
> * Thread/Task (Luồng/Tác vụ)
> * Parallel (Song song)
> * Channel (Kênh giao tiếp)
> * CUDA: + (Sử dụng công nghệ NVIDIA CUDA)

---

### 2. Yêu cầu Bài tập chi tiết

Đây là mô tả chi tiết cho bài tập lớn/tiểu luận của bạn:

**TÊN BÀI TẬP:** Cài đặt Thuật toán Song song với CUDA (Dựa trên Session 9)

**A. MỤC TIÊU BÀI TẬP**
Áp dụng các kiến thức về lập trình song song (Parallelism), phân chia dữ liệu (chunking), và các mẫu thuật toán (Algorithm patterns) như Reduction để tối ưu hóa hiệu năng tính toán bằng công nghệ **CUDA** trên GPU.

**B. YÊU CẦU LẬP TRÌNH (IMPLEMENTATION)**
Sinh viên cần viết mã nguồn (thường là C/C++ với CUDA) để thực hiện hai bài toán sau:

**Bài toán 1: Tính tích vô hướng của hai Vector (Dot Product)**

* **Mô tả:** Cho hai vector $A$ và $B$ có cùng kích thước. Viết kernel CUDA để tính tích vô hướng: $Kết\ quả = \sum (A_i * B_i)$.
* **Yêu cầu kỹ thuật áp dụng:**
  * Phân chia mảng đầu vào thành các "chunks" để giao cho các Grid/Block xử lý song song.
  * Sử dụng mẫu thuật toán **Reduction** (đã học ở phần 3). Bạn sẽ cần thực hiện phép nhân từng cặp phần tử song song, sau đó dùng kỹ thuật Reduction (tính tổng theo cây/DAG) để cộng dồn các kết quả lại thành một giá trị vô hướng (scalar) duy nhất.

**Bài toán 2: Chuyển vị Ma trận (Matrix Transpose)**

* **Mô tả:** Cho một ma trận $A$ (kích thước $N \times M$). Viết kernel CUDA để tạo ra ma trận chuyển vị $A^T$ (kích thước $M \times N$).
* **Yêu cầu kỹ thuật áp dụng:**
  * Sử dụng kiến trúc Thread/Block của CUDA để ánh xạ (map) từng luồng vào các phần tử của ma trận.
  * Tối ưu hóa việc truy cập bộ nhớ (Memory Coalescing hoặc sử dụng Shared Memory) để tăng tốc độ đọc/ghi dữ liệu khi đổi hàng thành cột.

**C. YÊU CẦU BÁO CÁO (REPORTING)**
**Bài toán 3: Viết Báo cáo (Report up to 3 pages)**

* Sinh viên nộp một báo cáo ngắn gọn, **tối đa 3 trang**.
* *Gợi ý nội dung báo cáo (dựa trên các khái niệm đã học):*
  1. **Thiết kế thuật toán:** Trình bày cách bạn chia nhỏ dữ liệu (divide input) và cấu hình số lượng Thread/Block cho từng bài toán.
  2. **Mô hình hóa (nếu có):** Vẽ sơ đồ DAG hoặc giải thích luồng dữ liệu đối với thuật toán Reduction ở Bài 1.
  3. **Đánh giá hiệu năng:** So sánh thời gian chạy (Execution time) giữa phiên bản chạy Tuần tự (Sequence - trên CPU) và phiên bản chạy Song song (Parallel - sử dụng CUDA trên GPU).
  4. **Khó khăn & Giải pháp:** Các vấn đề gặp phải về đồng bộ hóa (concurrency issues) hoặc quản lý bộ nhớ và cách giải quyết.
