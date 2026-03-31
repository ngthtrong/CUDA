/*
 * File: matrix_sum.cu
 * Dựa trên Example 3 (Slide 19) - Xử lý mảng 2D
 */
#include <cuda_runtime.h>
#include <stdio.h>
#include <sys/time.h>
#include <time.h>   

// Thay đổi kích thước Block ở đây cho báo cáo (ví dụ: 16x16, 32x32, 32x16...)
#define DIM_X 32
#define DIM_Y 32

double cpuSecond() {
    struct timeval tp;
    gettimeofday(&tp, NULL);
    return ((double)tp.tv_sec + (double)tp.tv_usec * 1.e-6);
}

void initData(float *ip, int size) {
    time_t t;
    srand((unsigned)time(&t));
    for (int i = 0; i < size; i++) {
        ip[i] = (float)(rand() & 0xFF) / 10.0f;
    }
}

// Kernel: Matrix Sum (Slide 19)
// m: so cot (width), n: so hang (height) theo logic cua slide
__global__ void sumDevice(float *a, float *b, float *c, int m, int n) {
    int ix = threadIdx.x + blockIdx.x * blockDim.x;
    int iy = threadIdx.y + blockIdx.y * blockDim.y;
    
    // Slide 19: idx = iy * m + ix
    int idx = iy * m + ix;

    if (ix < m && iy < n) {
        c[idx] = a[idx] + b[idx];
    }
}

void checkResult(float *hostRef, float *gpuRef, const int size) {
    double epsilon = 1.0E-8;
    bool match = 1;
    for (int i = 0; i < size; i++) {
        if (abs(hostRef[i] - gpuRef[i]) > epsilon) {
            match = 0;
            printf("Mismatch at %d: Host %f gpu %f\n", i, hostRef[i], gpuRef[i]);
            break;
        }
    }
    if (match) printf("Ket qua chinh xac!\n");
    else printf("Ket qua SAI!\n");
}

void sumHost(float *a, float *b, float *c, int size) {
    for (int i = 0; i < size; i++) c[i] = a[i] + b[i];
}

int main(int argc, char **argv) {
    printf("--- Bat dau Example 3: Matrix Sum ---\n");

    // 1. Kich thuoc ma tran (n hang, m cot)
    // Slide 19: m = 1<<13 (8192), n = 1<<13. Code mẫu dùng kích thước nhỏ hơn chút để test nhanh
    int m = 1 << 12; // Width (Columns) = 4096
    int n = 1 << 12; // Height (Rows) = 4096
    int size = m * n;
    printf("Matrix size: %d x %d\n", n, m); // Rows x Cols
    size_t nBytes = size * sizeof(float);

    // 2. Host memory
    float *hA, *hB, *hostRef, *gpuRef;
    hA = (float *)malloc(nBytes);
    hB = (float *)malloc(nBytes);
    hostRef = (float *)malloc(nBytes);
    gpuRef  = (float *)malloc(nBytes);

    initData(hA, size);
    initData(hB, size);
    memset(hostRef, 0, nBytes);
    memset(gpuRef, 0, nBytes);

    // 3. Device memory
    float *dA, *dB, *dC;
    cudaMalloc((void **)&dA, nBytes);
    cudaMalloc((void **)&dB, nBytes);
    cudaMalloc((void **)&dC, nBytes);

    // 4. H2D Transfer
    cudaMemcpy(dA, hA, nBytes, cudaMemcpyHostToDevice);
    cudaMemcpy(dB, hB, nBytes, cudaMemcpyHostToDevice);

    // 5. Cau hinh Kernel
    // Block 2D (Slide 19 dung dimx=32, dimy=32)
    dim3 block(DIM_X, DIM_Y); 
    // Grid 2D: Tinh so block can thiet theo chieu x va y
    dim3 grid((m + block.x - 1) / block.x, (n + block.y - 1) / block.y);

    printf("Configuration: Block(%d, %d), Grid(%d, %d)\n", block.x, block.y, grid.x, grid.y);

    // 6. Chay Kernel
    double iStart = cpuSecond();
    sumDevice<<<grid, block>>>(dA, dB, dC, m, n);
    cudaDeviceSynchronize();
    double iElaps = cpuSecond() - iStart;
    printf("Time elapsed for Matrix Sum Kernel: %f ms\n", iElaps * 1000);

    // 7. D2H Transfer
    cudaMemcpy(gpuRef, dC, nBytes, cudaMemcpyDeviceToHost);

    // 8. Verify
    sumHost(hA, hB, hostRef, size);
    checkResult(hostRef, gpuRef, size);

    // 9. Free
    cudaFree(dA); cudaFree(dB); cudaFree(dC);
    free(hA); free(hB); free(hostRef); free(gpuRef);

    return 0;
}