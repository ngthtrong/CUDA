
# SESSION 3 - PARALLEL REDUCTION

## I. INTRODUCTION TO REDUCTION

Reduction can be considered as a computational pattern that allows reducing a large amount of data into a single value within an aggregates operation such as: sum, max, min, etc.

**Example 1:**

```c
int reduce(const int *a, int n, int (*op)(int, int)){ 
    int i; 
    int result = 0; 
    for(i = 0; i <n; i++){ 
        result = (*op)(result, a[i]); 
    } 
    return result; 
} 

int main(){ 
    int n = 10; 
    int *a = (int*)malloc(n*sizeof(int)); 
    int (*p)(int, int) = add; 
    srand(time(NULL)); 
    init(a, n); 
    print(a, n); 
    int result = reduce(a, n, p); 
    printf("%d\n", result); 
    free(a); 
    return 0; 
} 
```

## II. PARALLEL REDUCTION

The serial version in the Example 1 becomes inefficient when dealing with a large dataset or parallel architectures likes GPU. Indeed, each thread iterates through the data for accumulation may cause a concurrency issue.

The parallel reduction, on the other hand, allows dividing data into smaller chunks, and performing operations simultaneously across multiple processors or cores. The idea is to partition input into chunks, then assign a thread to calculate the partial sum of each chunk, and add partial results into the final sum.

The parallel reduction process can be visualized as a tree, where each level represents a reduction step. Figure 1 is an example of a sum reduction tree where elements are paired with their immediate neighbor in each reduction step.

> **Figure 1.** A sum reduction tree allows pairing consecutive elements in each step.

### II.1 A simple reduction

The following device function is a simple version of sum reduction. The kernel function uses 2 arrays: one for storing the input data and another for maintaining partial sums of each thread block. The reduction step is described by a loop with the assistance of a `__syncthreads()` statement ensures the thread must finish its tasks and store values to global memory before threads in that block enter a new iteration. After finishing the reduction, the result is stored into a position of the output array.

```cpp
__global__ void reduceNeighbors(int *diData, int *doData, int n){ 
    int stride; 
    int idx = threadIdx.x; 
    int id = threadIdx.x + blockIdx.x * blockDim.x; 
    int *iData = diData + blockIdx.x * blockDim.x; 
  
    for(stride = 1; stride < blockDim.x; stride *= 2){ 
        if ((idx % (2 * stride)) == 0){ 
            iData[idx] += iData[idx + stride]; 
        } 
        __syncthreads(); 
    } 
  
    if (idx == 0){ 
        doData[blockIdx.x] = iData[0]; 
    } 
} 
```

The main function can be as follow:

```cpp
int main(){ 
    int n = 1 << 16; 
    int nSizes, sum; 
    int blockSize = 512; 
 
    nSizes = n * sizeof(int); 
    int *ha = (int *)malloc(nSizes); 
    int *tmp = (int *)malloc(nSizes); 
    dim3 block(blockSize, 1); 
    dim3 grid((n + block.x - 1)/block.x, 1); 
 
    int *hb = (int *)malloc(grid.x * sizeof(int)); 
    int *dA, *dB; 
    initData(ha, n); 
 
    cudaMalloc((int **)&dA, nSizes); 
    cudaMalloc((int **)&dB, grid.x * sizeof(int)); // Note: OCR showed sizeof(ll) potentially, but int context implies int
    cudaMemcpy(dA, ha, nSizes, cudaMemcpyHostToDevice); 
    cudaMemset(dB, 0, grid.x * sizeof(int)); 
 
    reduceNeighbors<<<grid, block>>>(dA, dB, n); 
    cudaDeviceSynchronize(); 
    cudaMemcpy(hb, dB, grid.x*sizeof(int), cudaMemcpyDeviceToHost); 
 
    sum = 0; 
    for(int i = 0; i < grid.x; i++){ 
        sum += hb[i]; 
    } 
 
    printf("Sum is %d\n", sum); 
 
    free(ha); 
    free(hb); 
    cudaFree(dA); 
    cudaFree(dB); 
 
    cudaDeviceReset(); 
    return 0; 
} 
```

### II.2 A better solution

The above solution causes highly divergent wrap where few thread enter the condition where others are scheduled. It can be solved by rearranging the loop as follow:

```cpp
__global__ void reduceNeighbors2(ll *diData, ll *doData, int n){ 
    int stride; 
    int tid = threadIdx.x; 
    int idx = threadIdx.x + blockIdx.x * blockDim.x; 
    int *iData = diData + blockIdx.x * blockDim.x; 
 
    for(stride = 1; stride < blockDim.x; stride *= 2){ 
        int index = 2 * stride * tid; 
        if (index < blockDim.x){ 
            iData[index] += iData[index + stride]; 
        } 
        __syncthreads(); 
    } 
    if (tid == 0){ 
        doData[blockIdx.x] = iData[0]; 
    } 
} 
```

### II.3 Reducing with interleaved pair

This solution is to pair elements in a distance of `stride`, where `stride` is started at a half of the thread block size (figure 2).

> **Figure 2.** A sum reduction tree allows pairing elements in a distance of stride.

```cpp
__global__ void reduceInterLeaves(ll *diData, ll *doData, int n){ 
    int stride; 
    int tid = threadIdx.x; 
    int idx = threadIdx.x + blockIdx.x * blockDim.x; 
    int *iData = diData + blockIdx.x * blockDim.x; 
  
    for(stride = blockDim.x/2; stride > 0; stride >>= 1){ 
        if (tid < stride){ 
            iData[tid] += iData[tid + stride]; 
        } 
        __syncthreads(); 
    } 
    if (tid == 0){ 
        doData[blockIdx.x] = iData[0]; 
    } 
} 
```

## III. EXERCISES

1. Implement the serial reduction with the sum, max, product operations.
2. Implement the sum parallel reduction using algorithms in **II**. Experiment those algorithms with different input sizes and visualize the execution times as charts.
3. Implement those algorithms using **float** data type. Are there any differences in comparison with **int** data type.
