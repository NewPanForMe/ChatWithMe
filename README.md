# ChatWithMe

一个用于学习新技术的聊天软件：**.NET 10 四层 DDD 后端**（PostgreSQL + MongoDB + SignalR）+ **Vue 3 管理后台** + **Avalonia 桌面客户端**。

| 端 | 技术栈 | 位置 |
| --- | --- | --- |
| 后端 | .NET 10、EF Core/Npgsql、MongoDB.Driver、JWT、SignalR、Scalar | `src/Server` |
| 管理后台 | Vue 3、Vite、TypeScript、TDesign、vue-router（hash） | `src/Web` |
| 桌面客户端 | Avalonia 12、TKCode.Arc、CommunityToolkit.Mvvm | `src/Client/ChatWithMe.Client` |

## 仓库结构

```
ChatWithMe/
├─ README.md                 # 总览与文档导航（本文件）
├─ CHANGELOG.md              # 变更日志（只追加）
├─ ChatWithMe.slnx           # 全仓解决方案（后端 + 测试 + 桌面客户端）
├─ doc/                      # 按职责拆分的文档（见下表）
└─ src/
   ├─ Server/                # Domain / Application / Infrastructure / Api / Tests
   ├─ Web/                   # 管理后台
   └─ Client/                # Avalonia 桌面端（含 Tests）
```

依赖方向：`Api → Infrastructure → Application → Domain`；EF Core / MongoDB / JWT 只在 Infrastructure 出现。

## 文档导航

| 文档 | 职责 | 路径 |
| --- | --- | --- |
| 用户需求说明书 | 业务目标、用户故事、验收口径（V0.1 草案） | [doc/需求/用户需求说明书.md](doc/需求/用户需求说明书.md) |
| 迭代计划与决策 | 已冻结决策、迭代状态、待确认事项、全员通知记录 | [doc/需求/迭代计划与决策.md](doc/需求/迭代计划与决策.md) |
| 已知缺口与限制 | 测试/数据/部署/安全方面的已知限制 | [doc/需求/已知缺口与限制.md](doc/需求/已知缺口与限制.md) |
| 架构与决策 | 分层、双库分工、连接串与故障策略 | [doc/技术/架构与决策.md](doc/技术/架构与决策.md) |
| 接口清单 | HTTP 接口、鉴权、错误约定 | [doc/技术/接口清单.md](doc/技术/接口清单.md) |
| 数据与存储 | 数据库迁移、表结构、MongoDB 集合与索引 | [doc/技术/数据与存储.md](doc/技术/数据与存储.md) |
| 时间口径 | UTC 存储、UTC+8 输出、游标格式 | [doc/技术/时间口径.md](doc/技术/时间口径.md) |
| 实时通信 | SignalR Hub、事件、在线状态、踢人、扩展 | [doc/技术/实时通信.md](doc/技术/实时通信.md) |
| 账号与权限 | 聊天用户/管理端账号分离、令牌、策略、踢人、删除 | [doc/账号/账号与权限.md](doc/账号/账号与权限.md) |
| 管理后台 | Vue 后台的路由、令牌、页面与交互 | [doc/客户端/管理后台.md](doc/客户端/管理后台.md) |
| 桌面客户端 | Avalonia 客户端的功能、运行、待办 | [doc/客户端/桌面客户端.md](doc/客户端/桌面客户端.md) |
| 运行与部署 | 环境、密钥、构建、启动、迁移、降级 | [doc/运维/运行与部署.md](doc/运维/运行与部署.md) |
| 开发约定 | 提交前必须遵守的工程约定 | [doc/运维/开发约定.md](doc/运维/开发约定.md) |
| 变更日志 | 每次改动的内容与实测结果 | [CHANGELOG.md](CHANGELOG.md) |

文档职责边界：需求文档只写“要什么/为什么”，技术文档写“怎么实现”，账号文档写“谁能做什么”，运维文档写“怎么跑起来”。

## 快速开始

### 1. 配置密钥（只放 user-secrets）

```powershell
cd src/Server/ChatWithMe.Api
dotnet user-secrets set "Security:DeliveryKey" "<32 字节 Base64>"   # 连接串解密
dotnet user-secrets set "Jwt:SigningKey" "<至少 32 字节 Base64>"    # HS256 签名
```

缺失或格式错误时应用启动即失败（fail-fast）。详见 [运行与部署](doc/运维/运行与部署.md)。

### 2. 构建与测试

```powershell
dotnet build ChatWithMe.slnx        # 0 warning / 0 error
dotnet test ChatWithMe.slnx         # 后端测试
cd src/Web; npm install; npm test   # 管理后台测试
```

### 3. 启动

```powershell
dotnet run --project src/Server/ChatWithMe.Api --launch-profile http   # http://localhost:5217
cd src/Web; npm run dev                                                # http://localhost:5173/#/login
dotnet run --project src/Client/ChatWithMe.Client                      # 桌面端
```

管理后台默认开发账号：`admin@chatwithme.local` / `Admin@12345`（仅开发环境，生产必须修改）。聊天账号用 `POST /api/users` 注册或桌面端「注册」。

## 当前状态

- 后端：会话/成员模型、好友、SignalR 实时、在线管理（踢人）、全员通知、账号分离、用户删除已完成；I3~I6（已读/撤回/群治理/配置/归档）待做。
- 管理后台：登录、用户、后端用户、文件、消息、在线管理、通知管理。
- 桌面端：登录/注册、好友、单聊收发、SignalR 实时、通知列表与浮动提示；群聊与已读/撤回待做。
- 测试：后端 114 项、桌面端 10 项、管理后台 8 项通过（详见变更日志）。

## 约定

- 文档按职责放 `doc/` 下对应目录，根 README 只做导航；变更写入 [CHANGELOG.md](CHANGELOG.md)。
- 每次改动后 `dotnet build` 必须 0 warning / 0 error。
- 不写 `.Result` / `.Wait()` / `.GetAwaiter().GetResult()`。
- 秘密只进 user-secrets。
- 前端全局提示统一走 `src/Web/src/utils/notify.ts`；页面内持久错误用 `t-alert`。
