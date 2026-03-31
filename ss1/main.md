
**BÁO CÁO THỰC HÀNH: BUỔI 1 - TỔNG QUAN VÀ THIẾT LẬP MÔI TRƯỜNG CUDA**

**Họ và tên:** [Tên của bạn]
**Mã số sinh viên:** [MSSV của bạn]
**Môn học:** Lập trình song song với CUDA

---

**1. MỞ ĐẦU**
Buổi học đầu tiên của môn "Lập trình song song với CUDA" tập trung vào việc giới thiệu tổng quan về sự cần thiết của tính toán hiệu năng cao (High Performance Computing) và sự chuyển dịch từ xử lý tuần tự trên CPU sang xử lý song song trên GPU. Vấn đề cốt lõi được giải quyết trong buổi này là làm sao để tiếp cận và thực hành lập trình CUDA khi gặp hạn chế về phần cứng (không có GPU NVIDIA) hoặc phần mềm (cài đặt môi trường phức tạp trên Windows/Linux).

**2. NỘI DUNG BUỔI HỌC**



**a. Lập trình Tuần tự (Sequential) và Lập trình Song song (Parallel)**
Giảng viên đã phân tích sự khác biệt cơ bản giữa hai mô hình xử lý dựa trên kiến trúc phần cứng:
*   **Lập trình tuần tự (trên CPU):**
    *   Các chỉ thị được thực thi lần lượt, cái này nối tiếp cái kia.
    *   Dựa trên kiến trúc CPU với số lượng lõi ít nhưng tốc độ xung nhịp cao, tối ưu cho xử lý logic phức tạp và độ trễ thấp (Low latency).
*   **Lập trình song song (trên GPU):**
    *   Bài toán lớn được chia nhỏ thành nhiều tác vụ con để thực thi đồng thời.
    *   Dựa trên kiến trúc GPU với hàng nghìn lõi xử lý nhỏ, tối ưu cho băng thông dữ liệu lớn (High throughput) và các tác vụ tính toán lặp lại.

**b. Giới thiệu về CUDA (Compute Unified Device Architecture)**
*   **Khái niệm:** CUDA là nền tảng tính toán song song và mô hình lập trình được NVIDIA phát triển. Nó cho phép các lập trình viên sử dụng ngôn ngữ cấp cao (như C, C++) để khai thác sức mạnh của GPU cho các mục đích tính toán tổng quát (GPGPU - General Purpose GPU).
*   **Mô hình lập trình:** Chương trình CUDA bao gồm phần chạy trên CPU (được gọi là **Host**) và phần chạy trên GPU (được gọi là **Device**). Các hàm thực thi song song trên Device được gọi là **Kernel**.


**a. Yêu cầu hệ thống và Nền tảng biên dịch**
Giảng viên đã giới thiệu các yêu cầu cơ bản để phát triển ứng dụng CUDA:

* **Phần cứng:** Yêu cầu máy tính phải có Card đồ họa (GPU) rời của NVIDIA hỗ trợ kiến trúc CUDA.
* **Phần mềm:** Cần cài đặt NVIDIA Driver và CUDA Toolkit tương thích trên hệ điều hành (Windows hoặc Linux).
* **Khắc phục hạn chế:** Đối với sinh viên không đáp ứng được yêu cầu phần cứng hoặc gặp khó khăn trong việc cài đặt môi trường cục bộ (Local environment), các giải pháp thay thế dựa trên nền tảng đám mây (Cloud) và Online Compiler đã được đề xuất:
  * **Google Colab:** Cung cấp môi trường Jupyter Notebook miễn phí có tích hợp GPU Tesla của NVIDIA.
  * **Compiler Explorer (Godbolt):** Công cụ trực tuyến giúp kiểm tra mã nguồn và xem kết quả biên dịch nhanh chóng.

**b. Tự nghiên cứu: Chạy chương trình Hello World trên Google Colab**
Trong phần hoạt động tự tìm hiểu, tôi đã nghiên cứu cách thiết lập môi trường và chạy thành công chương trình CUDA đơn giản trên Google Colab dựa trên các tài liệu tham khảo trực tuyến:

* **Về cú pháp code:** Tham khảo bài viết trên *Viblo* để hiểu cấu trúc cơ bản của một kernel CUDA (hàm chạy trên GPU) và cách gọi từ Host (CPU).
* **Về kỹ thuật thực thi trên Colab:**
  * Để chạy được mã nguồn C/C++ CUDA trong môi trường Notebook (vốn mặc định hỗ trợ Python), cần sử dụng các lệnh shell hoặc plugin hỗ trợ.
  * Thông qua các thảo luận trên *NVIDIA Developer Forums* và *Stack Overflow*, tôi đã nắm được cách sử dụng lệnh `!nvcc` để biên dịch file `.cu` và cách liên kết thư viện.
  * Khắc phục lỗi liên kết thư viện (linking libraries) bằng cách chỉ định rõ đường dẫn hoặc sử dụng các script cài đặt môi trường nếu cần thiết.
* **Công cụ hỗ trợ:** Sử dụng *Compiler Explorer* để kiểm tra nhanh các đoạn mã assembly hoặc debug lỗi cú pháp trước khi đưa vào Colab.

**3. KẾT LUẬN**
Có thể rút ra một số kết luận về CUDA:

* **Sức mạnh tính toán:** CUDA mở ra khả năng tận dụng hàng nghìn lõi tính toán của GPU để giải quyết các bài toán song song, vượt trội so với CPU trong các tác vụ cụ thể.
* **Tính linh hoạt của môi trường học tập:** Mặc dù CUDA là công nghệ gắn liền với phần cứng NVIDIA, nhưng sự hỗ trợ từ các nền tảng đám mây như Google Colab đã xóa bỏ rào cản về trang thiết bị. Việc biên dịch và chạy code CUDA hiện nay có thể thực hiện hoàn toàn trực tuyến, giúp sinh viên dễ dàng tiếp cận môn học mà không cần đầu tư máy trạm đắt tiền ngay từ đầu.

**4. TÀI LIỆU THAM KHẢO**
Dưới đây là các nguồn tài liệu và cộng đồng trực tuyến đã được sử dụng để hỗ trợ việc cài đặt và chạy thử nghiệm chương trình trong buổi học:

1. **Viblo.** *[Lập trình song song] Bài 3: Hello world cuda-C*. Truy cập tại: [https://viblo.asia/p/lap-trinh-song-song-bai-3-hello-world-cuda-c-0gdJzxbkVz5](https://viblo.asia/p/lap-trinh-song-song-bai-3-hello-world-cuda-c-0gdJzxbkVz5) (Tham khảo cú pháp Hello World cơ bản).
2. **Stack Overflow.** *How to link the libraries when executing CUDA program on Google Colab?* Truy cập tại: [https://stackoverflow.com/questions/56854243](https://stackoverflow.com/questions/56854243/how-to-link-the-libraries-when-executing-cuda-program-on-google-colab/56908350#56908350) (Tham khảo cách xử lý lỗi liên kết thư viện).
3. **NVIDIA Developer Forums.** *Running C and ++ Cuda in Google Colab*. Truy cập tại: [https://forums.developer.nvidia.com/t/running-c-and-cuda-in-google-colab/237108](https://forums.developer.nvidia.com/t/running-c-and-cuda-in-google-colab/237108) (Tham khảo quy trình cài đặt trên Colab).
4. **Compiler Explorer.** *CUDA Mode*. Truy cập tại: [https://godbolt.org/noscript/cuda](https://godbolt.org/noscript/cuda) (Công cụ biên dịch và kiểm tra mã nhanh).

---
