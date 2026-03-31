/*
 * File: vector_sum.cu
 * Dựa trên Example 2 (Slide 17) và Timer (Slide 21)
 */
#include <cuda_runtime.h>
#include <stdio.h>
#include <time.h>     // Needed for time() function
#include <sys/time.h> // Dùng cho Linux/WSL. Nếu dùng Windows Visual Studio cần thư viện thay thế.

// Kích thước Block mặc định (Thay đổi số này cho báo cáo: 32, 128, 256, 512...)
#define BLOCK_SIZE 256 

// Hàm đo thời gian (Slide 21)
double cpuSecond() {
    struct timeval tp;
    gettimeofday(&tp, NULL);
    return ((double)tp.tv_sec + (double)tp.tv_usec * 1.e-6);
}

// Hàm khởi tạo dữ liệu ngẫu nhiên
void initData(float *ip, int size) {
    time_t t;
    srand((unsigned)time(&t));
    for (int i = 0; i < size; i++) {
        ip[i] = (float)(rand() & 0xFF) / 10.0f;
    }
}

// Kernel: Vector Sum trên Device (Slide 17)
__global__ void sumDevice(float *a, float *b, float *c, int n) {
    // Tính chỉ số i dựa trên block và thread index
    int i = blockIdx.x * blockDim.x + threadIdx.x;
    
    // Kiểm tra biên (Boundary check) - Rất quan trọng để tránh lỗi truy cập bộ nhớ
    if (i < n) {
        c[i] = a[i] + b[i];
    }
}

// Hàm kiểm tra kết quả (So khớp Host vs Device)
void checkResult(float *hostRef, float *gpuRef, const int N) {
    double epsilon = 1.0E-8;
    bool match = 1;
    for (int i = 0; i < N; i++) {
        if (abs(hostRef[i] - gpuRef[i]) > epsilon) {
            match = 0;
            printf("Mismatch at %d: Host %f gpu %f\n", i, hostRef[i], gpuRef[i]);
            break;
        }
    }
    if (match) printf("Ket qua chinh xac!\n");
    else printf("Ket qua SAI!\n");
}

void sumHost(float *a, float *b, float *c, int n) {
    for (int i = 0; i < n; i++) c[i] = a[i] + b[i];
}

int main(int argc, char **argv) {
    printf("--- Bat dau Example 2: Vector Sum ---\n");

    // 1. Thiet lap kich thuoc du lieu
    int n = 1 << 24; // Kích thước lớn (khoảng 16 triệu phần tử) để thấy rõ thời gian
    printf("Vector size: %d elements\n", n);
    size_t nBytes = n * sizeof(float);

    // 2. Cap phat bo nho Host
    float *hA, *hB, *hostRef, *gpuRef;
    hA = (float *)malloc(nBytes);
    hB = (float *)malloc(nBytes);
    hostRef = (float *)malloc(nBytes); // Ket qua tinh tren CPU
    gpuRef  = (float *)malloc(nBytes); // Ket qua tinh tu GPU copy ve

    // 3. Khoi tao du lieu
    initData(hA, n);
    initData(hB, n);
    memset(hostRef, 0, nBytes);
    memset(gpuRef, 0, nBytes);

    // 4. Cap phat bo nho Device (GPU)
    float *dA, *dB, *dC;
    cudaMalloc((void **)&dA, nBytes);
    cudaMalloc((void **)&dB, nBytes);
    cudaMalloc((void **)&dC, nBytes);

    // 5. Copy du lieu tu Host sang Device
    cudaMemcpy(dA, hA, nBytes, cudaMemcpyHostToDevice);
    cudaMemcpy(dB, hB, nBytes, cudaMemcpyHostToDevice);

    // 6. Cau hinh Kernel (Grid và Block)
    dim3 block(BLOCK_SIZE);
    dim3 grid((n + block.x - 1) / block.x);

    printf("Kernel execution configuration: <<<%d, %d>>>\n", grid.x, block.x);

    // 7. Chay Kernel va do thoi gian
    double iStart = cpuSecond();
    
    sumDevice<<<grid, block>>>(dA, dB, dC, n);
    cudaDeviceSynchronize(); // Cho GPU chay xong moi do thoi gian

    double iElaps = cpuSecond() - iStart;
    printf("Time elapsed for Kernel: %f ms\n", iElaps * 1000);

    // 8. Copy ket qua tu Device ve Host
    cudaMemcpy(gpuRef, dC, nBytes, cudaMemcpyDeviceToHost);

    // 9. Kiem tra ket qua (Tinh tren CPU de so sanh)
    sumHost(hA, hB, hostRef, n);
    checkResult(hostRef, gpuRef, n);

    // 10. Giai phong bo nho
    cudaFree(dA); cudaFree(dB); cudaFree(dC);
    free(hA); free(hB); free(hostRef); free(gpuRef);

    return 0;
}