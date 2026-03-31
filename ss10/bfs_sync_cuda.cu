#include <cuda_runtime.h>

#include <algorithm>
#include <chrono>
#include <cstdlib>
#include <fstream>
#include <iomanip>
#include <iostream>
#include <regex>
#include <sstream>
#include <stdexcept>
#include <string>
#include <unordered_map>
#include <vector>

#define CHECK_CUDA(call)                                                                  \
    do {                                                                                  \
        cudaError_t _err = (call);                                                       \
        if (_err != cudaSuccess) {                                                       \
            std::cerr << "CUDA error at " << __FILE__ << ":" << __LINE__ << " -> "    \
                      << cudaGetErrorString(_err) << std::endl;                          \
            std::exit(EXIT_FAILURE);                                                      \
        }                                                                                 \
    } while (0)

struct GraphInput {
    std::vector<int> nodes;
    std::vector<std::pair<int, int>> edges;
};

struct CsrGraph {
    int n = 0;
    int m = 0;
    std::vector<int> nodeIds;
    std::unordered_map<int, int> idToIndex;
    std::vector<int> rowPtr;
    std::vector<int> colIdx;
};

struct BfsStats {
    std::vector<int> level;
    std::vector<int> parent;
    std::vector<std::vector<int>> rounds;
    unsigned long long messages = 0;
    int communicationRounds = 0;
    double elapsedMs = 0.0;
};

static std::string readAllText(const std::string& path) {
    std::ifstream file(path);
    if (!file.is_open()) {
        throw std::runtime_error("Cannot open input file: " + path);
    }

    std::ostringstream buffer;
    buffer << file.rdbuf();
    return buffer.str();
}

static std::string extractArraySection(const std::string& content, const std::string& key) {
    std::string quoted = "\"" + key + "\"";
    std::size_t keyPos = content.find(quoted);
    if (keyPos == std::string::npos) {
        throw std::runtime_error("Missing key in JSON: " + key);
    }

    std::size_t start = content.find('[', keyPos);
    if (start == std::string::npos) {
        throw std::runtime_error("Invalid JSON array for key: " + key);
    }

    int depth = 0;
    for (std::size_t i = start; i < content.size(); ++i) {
        if (content[i] == '[') {
            depth++;
        } else if (content[i] == ']') {
            depth--;
            if (depth == 0) {
                return content.substr(start, i - start + 1);
            }
        }
    }

    throw std::runtime_error("Unbalanced array brackets for key: " + key);
}

static std::vector<int> parseNumberArray(const std::string& section) {
    std::vector<int> values;
    std::regex numberRegex(R"((-?\d+))");
    for (std::sregex_iterator it(section.begin(), section.end(), numberRegex), end; it != end; ++it) {
        values.push_back(std::stoi((*it)[1].str()));
    }
    return values;
}

static std::vector<std::pair<int, int>> parseEdgeArray(const std::string& section) {
    std::vector<std::pair<int, int>> edges;
    std::regex edgeRegex(R"(\[\s*(-?\d+)\s*,\s*(-?\d+)\s*\])");
    for (std::sregex_iterator it(section.begin(), section.end(), edgeRegex), end; it != end; ++it) {
        int u = std::stoi((*it)[1].str());
        int v = std::stoi((*it)[2].str());
        edges.emplace_back(u, v);
    }
    return edges;
}

static GraphInput parseGraphJson(const std::string& jsonPath) {
    std::string text = readAllText(jsonPath);

    GraphInput input;
    try {
        input.nodes = parseNumberArray(extractArraySection(text, "nodes"));
    } catch (const std::exception&) {
        input.nodes.clear();
    }
    input.edges = parseEdgeArray(extractArraySection(text, "edges"));

    if (input.edges.empty()) {
        throw std::runtime_error("JSON 'edges' is empty.");
    }

    return input;
}

static CsrGraph buildCsrGraph(const GraphInput& input) {
    CsrGraph graph;
    std::vector<int> allNodes = input.nodes;
    allNodes.reserve(input.nodes.size() + input.edges.size() * 2);

    for (const auto& edge : input.edges) {
        allNodes.push_back(edge.first);
        allNodes.push_back(edge.second);
    }

    std::sort(allNodes.begin(), allNodes.end());
    allNodes.erase(std::unique(allNodes.begin(), allNodes.end()), allNodes.end());

    if (allNodes.empty()) {
        throw std::runtime_error("Graph has no nodes.");
    }

    graph.nodeIds = allNodes;
    graph.n = static_cast<int>(graph.nodeIds.size());

    for (int i = 0; i < graph.n; ++i) {
        graph.idToIndex[graph.nodeIds[i]] = i;
    }

    std::vector<std::vector<int>> adjacency(graph.n);
    for (const auto& edge : input.edges) {
        auto itU = graph.idToIndex.find(edge.first);
        auto itV = graph.idToIndex.find(edge.second);
        if (itU == graph.idToIndex.end() || itV == graph.idToIndex.end()) {
            continue;
        }

        int u = itU->second;
        int v = itV->second;
        if (u == v) {
            continue;
        }

        adjacency[u].push_back(v);
        adjacency[v].push_back(u);
    }

    graph.rowPtr.resize(graph.n + 1, 0);
    for (int u = 0; u < graph.n; ++u) {
        auto& neighbors = adjacency[u];
        std::sort(neighbors.begin(), neighbors.end());
        neighbors.erase(std::unique(neighbors.begin(), neighbors.end()), neighbors.end());
        graph.rowPtr[u + 1] = graph.rowPtr[u] + static_cast<int>(neighbors.size());
    }

    graph.colIdx.reserve(graph.rowPtr.back());
    for (int u = 0; u < graph.n; ++u) {
        graph.colIdx.insert(graph.colIdx.end(), adjacency[u].begin(), adjacency[u].end());
    }

    graph.m = static_cast<int>(graph.colIdx.size() / 2);
    return graph;
}

static BfsStats runCpuSynchronousBfs(const CsrGraph& graph, int startIdx) {
    BfsStats stats;
    stats.level.assign(graph.n, -1);
    stats.parent.assign(graph.n, -1);

    std::vector<int> visited(graph.n, 0);
    std::vector<int> frontier = {startIdx};

    visited[startIdx] = 1;
    stats.level[startIdx] = 0;
    stats.rounds.push_back({startIdx});

    int currentLevel = 0;
    auto start = std::chrono::high_resolution_clock::now();

    while (!frontier.empty()) {
        stats.communicationRounds++;

        std::vector<int> nextFrontier;
        std::vector<int> scheduled(graph.n, 0);

        for (int u : frontier) {
            for (int e = graph.rowPtr[u]; e < graph.rowPtr[u + 1]; ++e) {
                int v = graph.colIdx[e];
                stats.messages++;
                if (!visited[v] && !scheduled[v]) {
                    scheduled[v] = 1;
                    stats.level[v] = currentLevel + 1;
                    stats.parent[v] = u;
                    nextFrontier.push_back(v);
                }
            }
        }

        for (int v : nextFrontier) {
            visited[v] = 1;
        }

        stats.rounds.push_back(nextFrontier);
        frontier.swap(nextFrontier);
        currentLevel++;
    }

    auto stop = std::chrono::high_resolution_clock::now();
    stats.elapsedMs =
        std::chrono::duration<double, std::milli>(stop - start).count();
    return stats;
}

__global__ void bfsExpandKernel(
    int n,
    const int* rowPtr,
    const int* colIdx,
    const int* frontier,
    int* nextFrontier,
    int* visited,
    int* parent,
    int* level,
    int currentLevel,
    int* nextCount,
    unsigned long long* messageCount) {
    int u = blockIdx.x * blockDim.x + threadIdx.x;
    if (u >= n || frontier[u] == 0) {
        return;
    }

    for (int edgeIdx = rowPtr[u]; edgeIdx < rowPtr[u + 1]; ++edgeIdx) {
        int v = colIdx[edgeIdx];
        atomicAdd(messageCount, 1ULL);

        if (atomicCAS(&visited[v], 0, 1) == 0) {
            parent[v] = u;
            level[v] = currentLevel + 1;
            nextFrontier[v] = 1;
            atomicAdd(nextCount, 1);
        }
    }
}

static BfsStats runGpuSynchronousBfs(const CsrGraph& graph, int startIdx) {
    BfsStats stats;
    stats.level.assign(graph.n, -1);
    stats.parent.assign(graph.n, -1);
    stats.rounds.push_back({startIdx});

    int* d_rowPtr = nullptr;
    int* d_colIdx = nullptr;
    int* d_frontier = nullptr;
    int* d_nextFrontier = nullptr;
    int* d_visited = nullptr;
    int* d_parent = nullptr;
    int* d_level = nullptr;
    int* d_nextCount = nullptr;
    unsigned long long* d_messageCount = nullptr;

    CHECK_CUDA(cudaMalloc(&d_rowPtr, sizeof(int) * graph.rowPtr.size()));
    CHECK_CUDA(cudaMalloc(&d_colIdx, sizeof(int) * graph.colIdx.size()));
    CHECK_CUDA(cudaMalloc(&d_frontier, sizeof(int) * graph.n));
    CHECK_CUDA(cudaMalloc(&d_nextFrontier, sizeof(int) * graph.n));
    CHECK_CUDA(cudaMalloc(&d_visited, sizeof(int) * graph.n));
    CHECK_CUDA(cudaMalloc(&d_parent, sizeof(int) * graph.n));
    CHECK_CUDA(cudaMalloc(&d_level, sizeof(int) * graph.n));
    CHECK_CUDA(cudaMalloc(&d_nextCount, sizeof(int)));
    CHECK_CUDA(cudaMalloc(&d_messageCount, sizeof(unsigned long long)));

    CHECK_CUDA(cudaMemcpy(
        d_rowPtr,
        graph.rowPtr.data(),
        sizeof(int) * graph.rowPtr.size(),
        cudaMemcpyHostToDevice));
    CHECK_CUDA(cudaMemcpy(
        d_colIdx,
        graph.colIdx.data(),
        sizeof(int) * graph.colIdx.size(),
        cudaMemcpyHostToDevice));

    std::vector<int> h_frontier(graph.n, 0);
    std::vector<int> h_visited(graph.n, 0);
    std::vector<int> h_parent(graph.n, -1);
    std::vector<int> h_level(graph.n, -1);

    h_frontier[startIdx] = 1;
    h_visited[startIdx] = 1;
    h_level[startIdx] = 0;

    CHECK_CUDA(cudaMemcpy(d_frontier, h_frontier.data(), sizeof(int) * graph.n, cudaMemcpyHostToDevice));
    CHECK_CUDA(cudaMemcpy(d_visited, h_visited.data(), sizeof(int) * graph.n, cudaMemcpyHostToDevice));
    CHECK_CUDA(cudaMemcpy(d_parent, h_parent.data(), sizeof(int) * graph.n, cudaMemcpyHostToDevice));
    CHECK_CUDA(cudaMemcpy(d_level, h_level.data(), sizeof(int) * graph.n, cudaMemcpyHostToDevice));

    int frontierCount = 1;
    int currentLevel = 0;
    const int blockSize = 256;
    const int gridSize = (graph.n + blockSize - 1) / blockSize;

    cudaEvent_t startEvent, stopEvent;
    CHECK_CUDA(cudaEventCreate(&startEvent));
    CHECK_CUDA(cudaEventCreate(&stopEvent));
    CHECK_CUDA(cudaEventRecord(startEvent));

    while (frontierCount > 0) {
        stats.communicationRounds++;

        CHECK_CUDA(cudaMemset(d_nextFrontier, 0, sizeof(int) * graph.n));
        CHECK_CUDA(cudaMemset(d_nextCount, 0, sizeof(int)));
        CHECK_CUDA(cudaMemset(d_messageCount, 0, sizeof(unsigned long long)));

        bfsExpandKernel<<<gridSize, blockSize>>>(
            graph.n,
            d_rowPtr,
            d_colIdx,
            d_frontier,
            d_nextFrontier,
            d_visited,
            d_parent,
            d_level,
            currentLevel,
            d_nextCount,
            d_messageCount);
        CHECK_CUDA(cudaGetLastError());
        CHECK_CUDA(cudaDeviceSynchronize());

        int h_nextCount = 0;
        unsigned long long h_messages = 0;
        CHECK_CUDA(cudaMemcpy(&h_nextCount, d_nextCount, sizeof(int), cudaMemcpyDeviceToHost));
        CHECK_CUDA(cudaMemcpy(&h_messages, d_messageCount, sizeof(unsigned long long), cudaMemcpyDeviceToHost));
        stats.messages += h_messages;

        std::vector<int> h_nextFrontier(graph.n, 0);
        CHECK_CUDA(cudaMemcpy(
            h_nextFrontier.data(),
            d_nextFrontier,
            sizeof(int) * graph.n,
            cudaMemcpyDeviceToHost));

        std::vector<int> roundNodes;
        roundNodes.reserve(h_nextCount);
        for (int i = 0; i < graph.n; ++i) {
            if (h_nextFrontier[i] == 1) {
                roundNodes.push_back(i);
            }
        }
        stats.rounds.push_back(roundNodes);

        frontierCount = h_nextCount;
        if (frontierCount == 0) {
            break;
        }

        std::swap(d_frontier, d_nextFrontier);
        currentLevel++;
    }

    CHECK_CUDA(cudaEventRecord(stopEvent));
    CHECK_CUDA(cudaEventSynchronize(stopEvent));
    float elapsedMs = 0.0f;
    CHECK_CUDA(cudaEventElapsedTime(&elapsedMs, startEvent, stopEvent));
    stats.elapsedMs = static_cast<double>(elapsedMs);

    CHECK_CUDA(cudaMemcpy(stats.level.data(), d_level, sizeof(int) * graph.n, cudaMemcpyDeviceToHost));
    CHECK_CUDA(cudaMemcpy(stats.parent.data(), d_parent, sizeof(int) * graph.n, cudaMemcpyDeviceToHost));

    CHECK_CUDA(cudaEventDestroy(startEvent));
    CHECK_CUDA(cudaEventDestroy(stopEvent));
    CHECK_CUDA(cudaFree(d_rowPtr));
    CHECK_CUDA(cudaFree(d_colIdx));
    CHECK_CUDA(cudaFree(d_frontier));
    CHECK_CUDA(cudaFree(d_nextFrontier));
    CHECK_CUDA(cudaFree(d_visited));
    CHECK_CUDA(cudaFree(d_parent));
    CHECK_CUDA(cudaFree(d_level));
    CHECK_CUDA(cudaFree(d_nextCount));
    CHECK_CUDA(cudaFree(d_messageCount));

    return stats;
}

static bool compareLevels(const std::vector<int>& lhs, const std::vector<int>& rhs) {
    if (lhs.size() != rhs.size()) {
        return false;
    }

    for (std::size_t i = 0; i < lhs.size(); ++i) {
        if (lhs[i] != rhs[i]) {
            return false;
        }
    }
    return true;
}

static void printRoundTrace(const CsrGraph& graph, const std::vector<std::vector<int>>& rounds, const std::string& title) {
    std::cout << title << std::endl;
    for (std::size_t round = 0; round < rounds.size(); ++round) {
        std::cout << "  Round " << round << ": ";
        if (rounds[round].empty()) {
            std::cout << "[]";
        } else {
            std::cout << "[";
            for (std::size_t i = 0; i < rounds[round].size(); ++i) {
                if (i > 0) {
                    std::cout << ", ";
                }
                std::cout << graph.nodeIds[rounds[round][i]];
            }
            std::cout << "]";
        }
        std::cout << std::endl;
    }
}

int main(int argc, char** argv) {
    try {
        if (argc < 3) {
            std::cout << "Usage: ./bfs_sync_cuda <graph.json> <start_node_id>" << std::endl;
            std::cout << "Example: ./bfs_sync_cuda graph_sample.json 0" << std::endl;
            return 0;
        }

        std::string graphPath = argv[1];
        int startNodeId = std::stoi(argv[2]);

        GraphInput input = parseGraphJson(graphPath);
        CsrGraph graph = buildCsrGraph(input);

        auto startIt = graph.idToIndex.find(startNodeId);
        if (startIt == graph.idToIndex.end()) {
            throw std::runtime_error("Start node is not present in the graph: " + std::to_string(startNodeId));
        }
        int startIdx = startIt->second;

        BfsStats cpuStats = runCpuSynchronousBfs(graph, startIdx);
        BfsStats gpuStats = runGpuSynchronousBfs(graph, startIdx);

        bool levelMatch = compareLevels(cpuStats.level, gpuStats.level);

        std::cout << "=== Synchronous BFS on Undirected Graph (CUDA) ===" << std::endl;
        std::cout << "Nodes: " << graph.n << ", Edges: " << graph.m << std::endl;
        std::cout << "Start node: " << startNodeId << std::endl;
        std::cout << std::fixed << std::setprecision(4);
        std::cout << "[CPU] time(ms): " << cpuStats.elapsedMs
                  << ", rounds: " << cpuStats.communicationRounds
                  << ", messages: " << cpuStats.messages << std::endl;
        std::cout << "[GPU] time(ms): " << gpuStats.elapsedMs
                  << ", rounds: " << gpuStats.communicationRounds
                  << ", messages: " << gpuStats.messages << std::endl;

        if (gpuStats.elapsedMs > 0.0) {
            std::cout << "Speedup (CPU/GPU): " << (cpuStats.elapsedMs / gpuStats.elapsedMs) << "x" << std::endl;
        }
        std::cout << "Level match (CPU vs GPU): " << (levelMatch ? "Yes" : "No") << std::endl;
        std::cout << std::endl;

        std::cout << "Node level table:" << std::endl;
        std::cout << "  node_id | cpu_level | gpu_level | gpu_parent" << std::endl;
        for (int idx = 0; idx < graph.n; ++idx) {
            int parentNodeId = gpuStats.parent[idx] < 0 ? -1 : graph.nodeIds[gpuStats.parent[idx]];
            std::cout << "  " << std::setw(7) << graph.nodeIds[idx]
                      << " | " << std::setw(9) << cpuStats.level[idx]
                      << " | " << std::setw(9) << gpuStats.level[idx]
                      << " | " << std::setw(10) << parentNodeId << std::endl;
        }
        std::cout << std::endl;

        printRoundTrace(graph, gpuStats.rounds, "GPU round trace:");

        return 0;
    } catch (const std::exception& ex) {
        std::cerr << "Error: " << ex.what() << std::endl;
        return 1;
    }
}
