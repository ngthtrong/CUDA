Bạn là một trợ lý lập trình chuyên nghiệp. Hãy thực hiện đầy đủ bài tập Session 6 theo các yêu cầu và ràng buộc dưới đây.

---

## THÔNG TIN SINH VIÊN

- **Họ tên:** Nguyễn Thanh Trọng
- **MSSV:** B2305615
- **Môn học:** CT225H - Parallel Programming with CUDA

---

## NỘI DUNG BÀI HỌC SESSION 6 - PREFIX SUM PROBLEM

### Nền tảng lý thuyết (từ Session 9):

- **Concurrency vs Parallelism** (Đồng thời vs Song song)
- **Algorithm patterns:** Reduction, **Prefix Sum** (Tổng tiền tố)
- **Techniques:** Thread/Task, Parallel, Channel, CUDA
- **DAG (Directed Acyclic Graph):** Mô hình hóa sự phụ thuộc giữa các tác vụ

### Bài toán Prefix Sum:

Cho mảng đầu vào $A = [a_0, a_1, a_2, ..., a_{N-1}]$, tính mảng kết quả $P$ sao cho:

- **Inclusive Prefix Sum:** $P[i] = \sum_{j=0}^{i} A[j]$
- **Exclusive Prefix Sum:** $P[i] = \sum_{j=0}^{i-1} A[j]$ (với $P[0] = 0$)

Ví dụ: Input = [1, 2, 3, 4] → Inclusive = [1, 3, 6, 10], Exclusive = [0, 1, 3, 6]

---

## YÊU CẦU THỰC HIỆN

### A. PHẦN CUDA (file .ipynb cho Google Colab)

**Tạo file:** `ss10/session10_cuda.ipynb`

**Cấu trúc notebook:**

1. Cell markdown: Tiêu đề, thông tin sinh viên, mô tả bài toán
2. Cell code: `!nvidia-smi` (kiểm tra GPU)
3. Cell markdown: Giải thích thuật toán Prefix Sum
4. Cell code: `%%writefile prefix_sum.cu` — Viết mã CUDA
5. Cell code: `!nvcc -o prefix_sum prefix_sum.cu -arch=sm_75 && ./prefix_sum` — Biên dịch và chạy

**Yêu cầu mã CUDA (prefix_sum.cu):**

- **CPU Sequential version** (baseline): vòng lặp tuần tự tính prefix sum
- **GPU Parallel version:** Cài đặt thuật toán Hillis-Steele hoặc Blelloch (work-efficient) scan
  - Sử dụng shared memory
  - Xử lý cả inclusive và exclusive prefix sum
  - Hỗ trợ mảng lớn (N = 1,000,000) với multi-block scan (cần scan các block partial sums)
- **So sánh hiệu năng:** In thời gian CPU vs GPU, tính speedup
- **Verification:** Kiểm tra kết quả GPU khớp với CPU
- Sử dụng `cudaEvent_t` để đo thời gian GPU chính xác
- Sử dụng `clock()` hoặc tương tự để đo thời gian CPU
- In kết quả ra stdout với format rõ ràng (tên phương pháp, thời gian, speedup, correct/incorrect)

### B. PHẦN C# (Thread/Task, Parallel, Channel)

**Tạo dự án .NET console** trực tiếp trong thư mục `ss10/`:

```
ss10/CSharpParallel/
    CSharpParallel.csproj
    Program.cs
```

**Tạo bằng lệnh:** `dotnet new console -n CSharpParallel` (chạy trong thư mục ss10/)

**Yêu cầu Program.cs:** Cài đặt Prefix Sum bằng 4 kỹ thuật:

1. **Sequential (Baseline):** Vòng lặp tuần tự
2. **Thread/Task:** Chia mảng thành P chunks (P = Environment.ProcessorCount), mỗi Task tính prefix sum cho chunk riêng, sau đó điều chỉnh offset giữa các chunk
3. **Parallel.For:** Sử dụng TPL (Task Parallel Library) để song song hóa
4. **Channel:** Producer/Consumer pattern — các producer tính prefix sum từng chunk và gửi kết quả qua Channel, consumer tổng hợp

**Yêu cầu chung cho C#:**

- Sử dụng `System.Diagnostics.Stopwatch` để đo thời gian
- Kích thước mảng: N = 1,000,000
- In kết quả với format: `[Tên phương pháp] Time: X.XXXX ms  Correct: Yes/No`
- Verify bằng cách so sánh với kết quả sequential
- Build và chạy với: `dotnet run -c Release`

### C. BÁO CÁO LATEX

**Tạo 2 file báo cáo:**

#### 1. `ss10/report.tex` (Báo cáo ngắn, tối đa 3 trang)

- Thiết kế thuật toán (cấu hình Grid/Block, mô tả chunking)
- Bảng so sánh hiệu năng (lấy từ kết quả chạy thực tế)
- Khó khăn & Giải pháp

#### 2. `ss10/full-report.tex` (Báo cáo chi tiết đầy đủ)

Bao gồm tất cả nội dung sau:

- **Mục lục** tự động (`\tableofcontents`)
- **Header/Footer:** CT225H bên trái, Session 10 bên phải, số trang ở giữa dưới
- **Sơ đồ TikZ:**
  - Sơ đồ tổng quan khái niệm (mind map)
  - Sơ đồ DAG cho thuật toán Prefix Sum (up-sweep + down-sweep cho Blelloch, hoặc các bước của Hillis-Steele)
  - Sơ đồ so sánh các kỹ thuật
- **Code listings** với syntax highlighting:
  - Style `cudastyle` cho CUDA (nền xám, keyword xanh, morekeywords cho __global__, __shared__, etc.)
  - Style `csharpstyle` cho C# (keyword tím)
  - Đưa code mẫu đầy đủ (các kernel/function chính, KHÔNG phải toàn bộ file)
- **Output boxes** (`tcolorbox`): Copy CHÍNH XÁC kết quả trả về từ chương trình (không được tự bịa số liệu)
- **Bảng so sánh** chi tiết (booktabs): Nền tảng, Phương pháp, Thời gian, Speedup, Correct
- **Biểu đồ cột** (pgfplots `ybar`): So sánh thời gian thực thi giữa các phương pháp
- **Biểu đồ speedup** riêng cho CUDA
- **Phân tích và thảo luận:** Giải thích tại sao mỗi phương pháp nhanh/chậm
- **Bảng khó khăn & giải pháp** (3 cột: Vấn đề, Nguyên nhân, Giải pháp)

**LaTeX packages cần dùng:**

```
inputenc, babel(vietnamese), geometry, graphicx, listings, xcolor, booktabs, 
amsmath, amssymb, hyperref, float, enumitem, array, pgfplots, tikz, 
fancyhdr, caption, subcaption, multicol, tcolorbox
```

**Biên dịch:** `pdflatex -interaction=nonstopmode <file>.tex` (chạy 2 lần cho full-report để mục lục đúng)

---

## RÀNG BUỘC QUAN TRỌNG

1. **Kết quả thực thi phải chính xác:** Khi đưa kết quả vào báo cáo, PHẢI copy chính xác output từ chương trình sau khi chạy. KHÔNG ĐƯỢC tự bịa số liệu.
2. **CUDA notebook (.ipynb):**

   - Sử dụng `%%writefile` magic command để ghi file .cu
   - Sử dụng `!nvcc` và `!./` để biên dịch và chạy
   - Notebook được thiết kế cho Google Colab (GPU T4, cần chọn Runtime > Change runtime type > T4 GPU)
3. **C# project:** Tạo trực tiếp trong thư mục `ss10/` bằng `dotnet new console -n CSharpParallel`, cấu trúc đơn giản (chỉ 1 file Program.cs)
4. **Khi không rõ yêu cầu:** PHẢI hỏi lại người dùng để được cung cấp thêm thông tin, KHÔNG tự quyết định.
5. **Báo cáo bằng LaTeX:** Không dùng Markdown cho báo cáo.
6. **Quy trình làm việc:**

   - Bước 1: Tạo CUDA notebook → Yêu cầu người dùng chạy trên Colab và gửi lại output
   - Bước 2: Tạo C# project → Chạy trực tiếp trên máy (`dotnet run -c Release`) và lấy output
   - Bước 3: Sau khi có ĐẦY ĐỦ output từ cả CUDA và C# → Viết báo cáo LaTeX với số liệu thực
   - Bước 4: Biên dịch LaTeX thành PDF (`pdflatex`)
7. **Cấu trúc thư mục cuối cùng:**

```
ss10/
    baihoc.md                    (nội dung bài học - nếu có)
    session10_cuda.ipynb         (CUDA notebook cho Colab)
    report.tex                   (báo cáo ngắn 3 trang)
    report.pdf
    full-report.tex              (báo cáo chi tiết)
    full-report.pdf
    CSharpParallel/
        CSharpParallel.csproj
        Program.cs
```

---

## GỢI Ý KỸ THUẬT CHO PREFIX SUM

### CUDA:

- **Hillis-Steele Scan (Inclusive):** Đơn giản hơn, work = O(N log N), nhưng step complexity = O(log N). Mỗi bước d, thread i tính: `out[i] = in[i] + in[i - 2^d]`
- **Blelloch Scan (Exclusive, Work-efficient):** work = O(N), gồm 2 pha:
  - **Up-sweep (Reduce):** Xây dựng cây tổng từ dưới lên
  - **Down-sweep:** Phân phối kết quả từ trên xuống
- Với mảng lớn (N > block size): cần scan theo block → thu thập block sums → scan block sums → cộng ngược lại

### C#:

- **Thread/Task Prefix Sum:**
  1. Chia mảng thành P chunks
  2. Mỗi task tính local prefix sum cho chunk của mình
  3. Thu thập phần tử cuối mỗi chunk → tính prefix sum của các block sums
  4. Cộng offset vào mỗi chunk
- **Channel:** Tương tự nhưng dùng channel để giao tiếp giữa các phase

---

## LƯU Ý VỀ MÔI TRƯỜNG

- **OS:** Windows
- **Shell:** PowerShell
- **LaTeX:** MiKTeX (pdflatex tại `C:\Users\jhiny\AppData\Local\Programs\MiKTeX\miktex\bin\x64\pdflatex.exe`)
- **Biên dịch LaTeX dùng:** `cmd /c "cd /d <thư_mục> && pdflatex -interaction=nonstopmode <file>.tex 2>&1"`
- **.NET:** Đã cài, dùng `dotnet new console` và `dotnet run -c Release`
- **CUDA:** Không có GPU trên máy local → notebook chạy trên Google Colab
