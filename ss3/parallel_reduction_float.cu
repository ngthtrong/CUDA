#include <stdio.h>
#include <stdlib.h>
#include <cuda_runtime.h>
#include <time.h>
#include <math.h>

#define BLOCK_SIZE 512

// Simple reduction - reduceNeighbors (float)
__global__ void reduceNeighborsFloat(float *diData, float *doData, int n) {
    int stride;
    int idx = threadIdx.x;
    float *iData = diData + blockIdx.x * blockDim.x;

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

// Better solution - reduceNeighbors2 (float)
__global__ void reduceNeighbors2Float(float *diData, float *doData, int n) {
    int stride;
    int tid = threadIdx.x;
    float *iData = diData + blockIdx.x * blockDim.x;

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

// Interleaved pair reduction (float)
__global__ void reduceInterLeavesFloat(float *diData, float *doData, int n) {
    int stride;
    int tid = threadIdx.x;
    float *iData = diData + blockIdx.x * blockDim.x;

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

// Initialize array with float values
void initDataFloat(float *a, int n) {
    for (int i = 0; i < n; i++) {
        a[i] = 1.0f;  // Use 1.0 for easy verification
    }
}

// CPU sum for verification
float cpuSumFloat(float *a, int n) {
    float sum = 0.0f;
    for (int i = 0; i < n; i++) {
        sum += a[i];
    }
    return sum;
}

// Run reduction and measure time (float version)
float runReductionFloat(void (*kernel)(float*, float*, int), float *dA, float *dB, float *hb, 
                        int n, int gridSize, int blockSize, const char *name) {
    cudaEvent_t start, stop;
    cudaEventCreate(&start);
    cudaEventCreate(&stop);
    
    dim3 block(blockSize, 1);
    dim3 grid(gridSize, 1);
    
    cudaEventRecord(start);
    kernel<<<grid, block>>>(dA, dB, n);
    cudaDeviceSynchronize();
    cudaEventRecord(stop);
    cudaEventSynchronize(stop);
    
    float milliseconds = 0;
    cudaEventElapsedTime(&milliseconds, start, stop);
    
    cudaMemcpy(hb, dB, gridSize * sizeof(float), cudaMemcpyDeviceToHost);
    
    float sum = 0.0f;
    for (int i = 0; i < gridSize; i++) {
        sum += hb[i];
    }
    
    printf("%s - Sum: %.2f, Time: %.4f ms\n", name, sum, milliseconds);
    
    cudaEventDestroy(start);
    cudaEventDestroy(stop);
    
    return milliseconds;
}

int main() {
    // Test with different input sizes
    int sizes[] = {1 << 16, 1 << 18, 1 << 20, 1 << 22, 1 << 24};
    int numSizes = sizeof(sizes) / sizeof(sizes[0]);
    
    printf("=== Parallel Reduction Benchmark (float) ===\n\n");
    
    for (int s = 0; s < numSizes; s++) {
        int n = sizes[s];
        int blockSize = BLOCK_SIZE;
        int gridSize = (n + blockSize - 1) / blockSize;
        
        printf("Input size: %d (2^%d)\n", n, (int)(log2f((float)n)));
        printf("---------------------------------\n");
        
        // Allocate host memory
        float *ha = (float *)malloc(n * sizeof(float));
        float *hb = (float *)malloc(gridSize * sizeof(float));
        
        initDataFloat(ha, n);
        float cpuResult = cpuSumFloat(ha, n);
        printf("CPU Sum (verification): %.2f\n", cpuResult);
        
        // Allocate device memory
        float *dA, *dB;
        cudaMalloc((void **)&dA, n * sizeof(float));
        cudaMalloc((void **)&dB, gridSize * sizeof(float));
        
        // Test reduceNeighborsFloat
        cudaMemcpy(dA, ha, n * sizeof(float), cudaMemcpyHostToDevice);
        cudaMemset(dB, 0, gridSize * sizeof(float));
        runReductionFloat(reduceNeighborsFloat, dA, dB, hb, n, gridSize, blockSize, "reduceNeighbors  ");
        
        // Test reduceNeighbors2Float
        cudaMemcpy(dA, ha, n * sizeof(float), cudaMemcpyHostToDevice);
        cudaMemset(dB, 0, gridSize * sizeof(float));
        runReductionFloat(reduceNeighbors2Float, dA, dB, hb, n, gridSize, blockSize, "reduceNeighbors2 ");
        
        // Test reduceInterLeavesFloat
        cudaMemcpy(dA, ha, n * sizeof(float), cudaMemcpyHostToDevice);
        cudaMemset(dB, 0, gridSize * sizeof(float));
        runReductionFloat(reduceInterLeavesFloat, dA, dB, hb, n, gridSize, blockSize, "reduceInterLeaves");
        
        printf("\n");
        
        // Cleanup
        free(ha);
        free(hb);
        cudaFree(dA);
        cudaFree(dB);
    }
    
    cudaDeviceReset();
    return 0;
}
