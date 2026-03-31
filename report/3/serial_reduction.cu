#include <stdio.h>
#include <stdlib.h>
#include <time.h>

// Operation functions
int add(int a, int b) { return a + b; }
int max_op(int a, int b) { return (a > b) ? a : b; }
int product(int a, int b) { return a * b; }

// Serial reduction function
int reduce(const int *a, int n, int (*op)(int, int), int init) {
    int result = init;
    for (int i = 0; i < n; i++) {
        result = (*op)(result, a[i]);
    }
    return result;
}

// Initialize array with random values
void initData(int *a, int n) {
    for (int i = 0; i < n; i++) {
        a[i] = rand() % 10 + 1;  // Random values 1-10
    }
}

// Print array
void printArray(int *a, int n) {
    printf("Array: ");
    for (int i = 0; i < n; i++) {
        printf("%d ", a[i]);
    }
    printf("\n");
}

int main() {
    int n = 10;
    int *a = (int *)malloc(n * sizeof(int));
    
    srand(time(NULL));
    initData(a, n);
    printArray(a, n);
    
    // Sum reduction
    int sum_result = reduce(a, n, add, 0);
    printf("Sum: %d\n", sum_result);
    
    // Max reduction
    int max_result = reduce(a, n, max_op, a[0]);
    printf("Max: %d\n", max_result);
    
    // Product reduction
    int product_result = reduce(a, n, product, 1);
    printf("Product: %d\n", product_result);
    
    free(a);
    return 0;
}
