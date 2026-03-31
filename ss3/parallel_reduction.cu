#include <stdio.h>
#include <stdlib.h>
#include <cuda_runtime.h>
#include <time.h>

#define BLOCK_SIZE 512

// Simple reduction - reduceNeighbors
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

// Better solution - reduceNeighbors2
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

// Interleaved pair reduction
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

// Initialize array
void initData(int *a, int n) {
    for (int i = 0; i < n; i++) {
        a[i] = 1;  // Use 1 for easy verification
    }
}

// CPU sum for verification
int cpuSum(int *a, int n) {
    int sum = 0;
    for (int i = 0; i < n; i++) {
        sum += a[i];
    }
    return sum;
}

// Run reduction and measure time
float runReduction(void (*kernel)(int*, int*, int), int *dA, int *dB, int *hb, 
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
    
    cudaMemcpy(hb, dB, gridSize * sizeof(int), cudaMemcpyDeviceToHost);
    
    int sum = 0;
    for (int i = 0; i < gridSize; i++) {
        sum += hb[i];
    }
    
    printf("%s - Sum: %d, Time: %.4f ms\n", name, sum, milliseconds);
    
    cudaEventDestroy(start);
    cudaEventDestroy(stop);
    
    return milliseconds;
}

int main() {
    // Test with different input sizes
    int sizes[] = {1 << 16, 1 << 18, 1 << 20, 1 << 22, 1 << 24};
    int numSizes = sizeof(sizes) / sizeof(sizes[0]);
    
    printf("=== Parallel Reduction Benchmark (int) ===\n\n");
    
    for (int s = 0; s < numSizes; s++) {
        int n = sizes[s];
        int blockSize = BLOCK_SIZE;
        int gridSize = (n + blockSize - 1) / blockSize;
        
        printf("Input size: %d (2^%d)\n", n, (int)(log2(n)));
        printf("---------------------------------\n");
        
        // Allocate host memory
        int *ha = (int *)malloc(n * sizeof(int));
        int *hb = (int *)malloc(gridSize * sizeof(int));
        
        initData(ha, n);
        int cpuResult = cpuSum(ha, n);
        printf("CPU Sum (verification): %d\n", cpuResult);
        
        // Allocate device memory
        int *dA, *dB;
        cudaMalloc((void **)&dA, n * sizeof(int));
        cudaMalloc((void **)&dB, gridSize * sizeof(int));
        
        // Test reduceNeighbors
        cudaMemcpy(dA, ha, n * sizeof(int), cudaMemcpyHostToDevice);
        cudaMemset(dB, 0, gridSize * sizeof(int));
        runReduction(reduceNeighbors, dA, dB, hb, n, gridSize, blockSize, "reduceNeighbors  ");
        
        // Test reduceNeighbors2
        cudaMemcpy(dA, ha, n * sizeof(int), cudaMemcpyHostToDevice);
        cudaMemset(dB, 0, gridSize * sizeof(int));
        runReduction(reduceNeighbors2, dA, dB, hb, n, gridSize, blockSize, "reduceNeighbors2 ");
        
        // Test reduceInterLeaves
        cudaMemcpy(dA, ha, n * sizeof(int), cudaMemcpyHostToDevice);
        cudaMemset(dB, 0, gridSize * sizeof(int));
        runReduction(reduceInterLeaves, dA, dB, hb, n, gridSize, blockSize, "reduceInterLeaves");
        
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
