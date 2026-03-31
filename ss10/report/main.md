# Báo cáo Session 10 - BFS Synchronous Network với CUDA

- Notebook triển khai: `ss10/bfs_sync_cuda.ipynb`
- Mã nguồn CUDA: `ss10/bfs_sync_cuda.cu`
- Báo cáo LaTeX: `report/4/main.tex`

## Nội dung chính
- Mô tả bài toán BFS trong synchronous rounds.
- Biểu diễn đồ thị JSON và chuyển sang CSR.
- Triển khai CPU baseline và GPU CUDA kernel-based frontier expansion.
- So sánh thời gian chạy, số rounds, số messages, và độ đúng `level`.
- Đề xuất các hướng tối ưu tiếp theo.

## Cách biên dịch báo cáo
```bash
cd report/4
pdflatex main.tex
```
