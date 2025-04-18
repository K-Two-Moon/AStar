# A* 寻路性能对比示例

## 示例概述
本项目展示了 A* 算法在不同实现方案下的性能表现对比，特别是在 100×100 节点地图中对相同逻辑的实现效果。通过对同步、异步及 Job System 的优化分析，帮助开发者选择适合的方案。

---

## 性能对比

### 同步方案
---
- **描述**: 主线程计算寻路。
- **性能表现**:  
  - **500个节点**: 耗时 10~20ms  
  - **5000个节点（死路场景）**: 耗时 400ms  
- **问题**: 严重堵塞主线程，导致游戏帧率大幅下降。

---

- **描述**: 使用 Job 和 Burst 优化（同步方案）。
- **性能表现**:
  - **500个节点**: 耗时 3~7ms
  - **5000节点（死路场景）**: 耗时 35ms
- **性能提升计算**:
  - **500节点**: 性能提升约 71%~85%。  
  - **5000节点**: 性能提升约 91%。



---

### 异步方案
---
- **描述**: 使用由 .NET 管理的线程池 Task。
- **性能表现**:  
  - **500个节点以内**: 耗时 40~45ms  
    - 实际耗时受到异步延迟（下一帧执行，约38ms）的影响。
  - **5000节点（死路场景）**: 耗时 500ms  
- **原因分析**:
  1. Task 的线程池调度耗时较高。
  2. 数据的 L1/L2 缓存隐式拷贝导致性能下降。

---

使用 Job 系统异步调度 + Burst 优化
- **性能表现**:
  - **1~5000节点**: 耗时 40~50ms
  - 不受死路影响，耗时稳定。
- **优化原理**:
  - Job 系统在调度过程中对缓存命中的优化发挥了作用。

---


## 总结与推荐

### 适用场景
1. **主线程寻路（同步方案）**:  
   - 适合小规模寻路任务，但大规模节点时会导致严重卡顿，建议仅用于简单的休闲类小游戏。
2. **Task 异步方案**:  
   - 对小规模寻路性能表现一般，但在复杂场景（如死路）中，缓存命中问题显著影响性能，不推荐用于性能敏感场景。
3. **Job + Burst（同步或异步方案）**:  
   - 适合大规模路径寻路，特别是在复杂地图中表现尤为优异，推荐用于实际开发中的大多数场景。

---

## 未来计划
即将推出基于 DOTS 的大规模对象同时寻路方案，进一步提升并行性能和效率，敬请期待。
