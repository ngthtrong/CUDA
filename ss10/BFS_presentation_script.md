# Script trình bày SS10 - BFS trong synchronous network

## Mở đầu (0:00 - 0:30)
Chào thầy/cô và cả lớp. Hôm nay em trình bày Session 10: BFS trong synchronous network. Mục tiêu là giải thích mô hình đồng bộ theo vòng (round), thuật toán BFS phân tán, cách mô phỏng, và phần CUDA để tăng tốc.

## Nội dung (0:30 - 0:50)
Em sẽ đi qua 4 phần:
1) Mô hình synchronous network
2) BFS theo round và ý nghĩa message
3) Mô hình dữ liệu và triển khai
4) CUDA kernel và kết quả/hướng tối ưu

## 1) Synchronous network (0:50 - 2:00)
- Mô hình đồ thị vô hướng G = (V, E). Mỗi đỉnh là một process, mỗi cạnh là 2 kênh hướng ngược nhau.
- Thuật toán chạy theo round đồng bộ. Mỗi round, mỗi process: nhận thông điệp, cập nhật trạng thái, gửi thông điệp đi.
- Chi phí đánh giá: số round (thời gian) và số message (truyền thông).
- Đính kèm hình minh họa: đồ thị A-B-C-D-E-F. (Em sẽ chỉ điểm bắt đầu A để mô tả BFS.)

## 2) BFS theo round (2:00 - 3:30)
- Khởi tạo: đánh dấu start (ví dụ A). Frontier round 0 là A.
- Round j: process trong frontier gửi probing message cho hàng xóm.
- Nếu đỉnh chưa được đánh dấu nhận message, nó:
  + đánh dấu (level = j+1), ghi parent nếu cần,
  + sẽ vào frontier của round tiếp theo.
- Dừng lại khi frontier rỗng.
- Ý nghĩa: BFS trong synchronous network sẽ lan ra theo lớp mức (level). Số round bằng độ sâu BFS.

## 3) Mô hình dữ liệu và triển khai (3:30 - 6:00)
- Input: đồ thị đọc từ JSON (graph_sample.json), chuyển sang adjacency list.
- Trong báo cáo, em đề cập các hướng triển khai: Thread/Task, Parallel.For, Channel, và CUDA.
- Thực hiện BFS theo round: ở mỗi round, duyệt frontier, mở rộng hàng xóm, tạo frontier mới.
- Thống kê: số round, số message, mảng level và parent.

## 4) CUDA và so sánh (6:00 - 8:00)
- Mục tiêu CUDA: tăng tốc mở rộng frontier (kernel-based).
- Biểu diễn CSR: prefix sum để trỏ vào danh sách kề.
- Mỗi thread xử lý một đỉnh trong frontier, cập nhật neighbor nếu chưa thăm.
- So sánh: CPU baseline vs GPU; đo thời gian, số round, số message và độ đúng level.
- Kết quả chính: số round không đổi, thời gian giảm khi đồ thị lớn.

## Demo ngắn (8:00 - 9:00)
- Mở notebook `bfs_sync_cuda.ipynb`.
- Chạy cell đọc JSON, chuyển CSR.
- Chạy BFS CPU, xem level/parent.
- Chạy CUDA kernel, so sánh thời gian và level.

## Kết luận (9:00 - 9:30)
- BFS synchronous network mô hình hóa tốt cho tính toán phân tán theo round.
- CUDA giúp tăng tốc phần mở rộng frontier, phù hợp đồ thị lớn.
- Hướng tối ưu: coalesced memory, loại bỏ xung đột ghi, và tối ưu luồng frontier.

## Hỏi đáp (9:30 - 10:00)
Em cảm ơn thầy/cô. Em sẵn sàng trả lời câu hỏi.
