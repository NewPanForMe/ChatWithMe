# ChatWithMe

一个用于学习新技术的聊天软件：.NET 10 四层 DDD 后端（PostgreSQL + MongoDB）+ Vue 3 管理后台。

---

## 1. 目录结构

```
ChatWithMe/
├─ README.md                       # 唯一文档：说明 + 变更日志
├─ doc/聊天软件用户需求说明书.md
└─ src/
   ├─ Server/                      # 后端（.NET 10 / net10.0）
   │  ├─ ChatWithMe.sln
   │  ├─ ChatWithMe.Domain/        # 领域层：零 PackageReference、零项目引用
   │  ├─ ChatWithMe.Application/   # 应用层：用例、DTO、抽象（IFileStorage/IClock…）
   │  ├─ ChatWithMe.Infrastructure/# 基础设施：EF Core、MongoDB、JWT、本地存储、连接串解密
   │  └─ ChatWithMe.Api/           # 宿主：控制器、鉴权、CORS、Scalar、异常处理
   └─ Client/
      └─ admin/                    # 管理后台（Vue 3 + Vite + TS + TDesign）
```

依赖方向：`Api → Infrastructure → Application → Domain`。EF Core / MongoDB / JWT 只在 Infrastructure 出现。

---

## 2. 环境要求

| 组件 | 版本 |
| --- | --- |
| .NET SDK | 10.0.403（net10.0，C# 14） |
| Node.js / npm | 20+（本次实测 v24.19.0 / npm 12） |
| 数据库 | 远端 PostgreSQL、MongoDB（连接串由 ToolsConsole 下发） |

---

## 3. 快速开始

### 3.1 两个秘密（只放 user-secrets，禁止写入仓库）

```powershell
cd src/Server/ChatWithMe.Api

# 连接串解密密钥：32 字节 Base64（AES-256-GCM）
dotnet user-secrets set "Security:DeliveryKey" "<32 字节 Base64>"

# JWT 签名密钥：至少 32 字节 Base64（HS256）
dotnet user-secrets set "Jwt:SigningKey" "<32 字节 Base64>"
```

- `UserSecretsId` = `ChatWithMe-Api-7f3c`（见 `ChatWithMe.Api.csproj`）。
- 换机器必须重设：缺少 `Security:DeliveryKey` 时数据库相关端点降级；缺少 `Jwt:SigningKey` 时应用启动即失败（`JwtOptions.Validate()` fail-fast）。
- `appsettings.json` 不含任何密钥。

### 3.2 启动后端

```powershell
cd src/Server
dotnet run --project ChatWithMe.Api --launch-profile http
```

- 地址：`http://localhost:5217`
- 根路径 `GET /` → `302` → `/scalar/`（Scalar 替代 Swagger，OpenAPI 文档在 `/openapi/v1.json`）
- 健康检查：`/api/health/db`、`/api/health/mongo`

### 3.3 启动管理后台

```powershell
cd src/Client/admin
npm install
npm run dev          # http://127.0.0.1:5173/#/login
```

- 后端地址默认 `http://localhost:5217`，可用环境变量 `VITE_API_BASE_URL` 覆盖。
- 生产构建：`npm run build`（先 `vue-tsc --noEmit` 再 `vite build`），产物在 `dist/`。
- 需要先有一个账号：`POST /api/users` 注册（匿名），或直接登录已有账号。

---

## 4. 架构与关键决策（已冻结）

| 议题 | 结论 |
| --- | --- |
| 架构 | DDD 四层，`net10.0` |
| 主库 | PostgreSQL，EF Core + Npgsql，迁移 `20261116054648_InitialCreate`、`20261116060400_AddRefreshTokens` |
| 连接串 | 只从 `https://www.tunn.work/ToolsConsole/ConnectionString/GetByConnName` 取，AES-256-GCM 解密（`nonce[12] + ciphertext + tag[16]`，Base64） |
| 双库分工 | PostgreSQL 存用户 / 刷新令牌 / 文件元数据；MongoDB 存聊天消息 |
| JWT | access 15 分钟 + refresh 7 天（轮换、可吊销、只存 SHA-256 哈希）；注册匿名，其余 `[Authorize]` |
| 文件 | `IFileStorage` 抽象 + 本地 `wwwroot/uploads`；FormOptions + Kestrel 限制 + 资源阶段短路 413 |
| 时间 | 存 UTC；JSON 输出统一 UTC+8、`yyyy/MM/dd HH:mm:ss`（无小数秒） |
| 时区 | `Time:TimeZoneId`（默认 `Asia/Shanghai`，回退 `China Standard Time`） |
| 异步 | 只要求业务层（Application 服务 / Controller / 仓储）async；连接串加载逻辑保持同步 |
| 管理后台 | Vue 3 + Vite + TS + `tdesign-vue-next` + `vue-router`（hash 路由），三段式 `t-layout` |
| 文档 | 不再新增文档文件，所有说明与日志只写本 README |

---

## 5. 后端接口

| 方法 | 路径 | 鉴权 | 说明 |
| --- | --- | --- | --- |
| POST | `/api/users` | 匿名 | 注册 |
| GET | `/api/users` | Bearer | 用户分页列表，`page`/`pageSize`/`keyword`（邮箱或昵称，大小写不敏感） |
| GET | `/api/users/{id}` | Bearer | 用户详情 |
| POST | `/api/auth/login` | 匿名 | 登录，返回 access + refresh |
| POST | `/api/auth/refresh` | 匿名 | 刷新（轮换，旧 refresh 立即失效） |
| POST | `/api/auth/logout` | 匿名 | 吊销 refresh |
| POST | `/api/files/upload` | Bearer | 单文件上传 |
| POST | `/api/files/upload-batch` | Bearer | 多文件上传（上限 `MaxFilesPerRequest`） |
| GET | `/api/files` | Bearer | 文件分页列表，`page`/`pageSize`/`keyword`（原始文件名） |
| GET | `/api/files/{id}` | 匿名 | 文件元数据 |
| GET | `/uploads/...` | 匿名 | 静态回读 |
| POST | `/api/messages` | Bearer | 发送消息（写 MongoDB） |
| GET | `/api/messages` | Bearer | 消息倒序分页，`conversationId`/`before`/`limit` |
| GET | `/api/health/db` | 匿名 | PostgreSQL 连通性 |
| GET | `/api/health/mongo` | 匿名 | MongoDB 连通性（不可用返回 503） |

分页列表统一返回 `{ items, total, page, pageSize }`。

---

## 6. 时间口径（重点）

- **存储**：数据库与 MongoDB 一律 UTC。
- **输出**：`LocalDateTimeConverter` 把每个 `DateTime` 转到 `Time:TimeZoneId` 并格式化为 `yyyy/MM/dd HH:mm:ss`，不带 `Z`/偏移/小数秒。例：`2026/11/16 16:38:48`。
- **输入**：`before` 以 `string` 接收并显式解析（不走 query 的 `DateTime` 绑定，避免跟随服务器区域设置），接受：
  - `yyyy/MM/dd HH:mm:ss`（展示格式）
  - `yyyy/MM/dd HH:mm:ss.fff`（游标格式）
  - ISO-8601（带偏移则按其自身偏移）
- **游标**：`nextBefore` 是翻页游标，不是展示时间，保留毫秒（`yyyy/MM/dd HH:mm:ss.fff`）。若把展示用的秒级文本当游标，同一秒内多条消息跨页会漏；实测 5 条同秒消息用毫秒游标翻页无重无漏。
- 时区解析见 `TimeZoneResolution.Resolve`：依次尝试 `Time:TimeZoneId` → `Time:FallbackTimeZoneId` → `Asia/Shanghai` → `China Standard Time` → UTC。

---

## 7. MongoDB 说明

- 连接名：`Security:ConnectionNames:Mongo = ChatWithMeMongo`（ToolsConsole 实测 `code=0`）。
- 远端下发的连接串不含 `authSource`，而账号建在 `admin` 库，驱动会拿业务库去认证 → `SCRAM-SHA-256` 认证失败。因此新增 `Mongo:AuthSource = admin`，由 `MongoConnectionSettings.BuildClientSettings` 在连接串未自带 `authSource` 时补齐；连接串自带则以其为准。
- 库名用小写 `chatwithme`：远端实例已存在该库，MongoDB 拒绝创建仅大小写不同的 `ChatWithMe`（报 `db already exists with different case`）。
- 集合 `messages`，索引 `ix_messages_conversation_sent_at`（ConversationId 升序 + SentAtUtc 降序），启动时由 `MongoIndexInitializer` 创建。
- 该库中另有 `mylogs1`、`dbx_init` 两个与本项目无关的集合，**不要删除**。

---

## 8. 管理后台

- 路由（hash）：`#/login`、`#/users`、`#/files`、`#/messages`，未登录访问受保护页会带 `redirect` 跳登录。
- 布局：`t-layout` 三段式（aside 菜单 + header 标题/用户/退出 + content）。
- 令牌：存 `localStorage`（键 `chatwithme.admin.session`）；请求带 `Authorization: Bearer`；遇 401 走**单飞刷新**（并发只刷一次）后重试原请求，失败则清会话回登录页。
- 页面：登录；用户列表（分页 + 关键字）；文件列表（分页 + 关键字 + 打开链接）；消息查询（会话 ID + limit + `nextBefore` 加载更多）。

---

## 9. 变更日志

### 2026-11-16 会话（接手交接单后）

**修复：MongoDB 正向链路（此前只验证过 503 降级）**

- `MongoOptions` 新增 `AuthSource`；`MongoConnectionSettings` 改为用 `MongoUrlBuilder` 构造并补齐 `authSource`，默认连接名改为 `ChatWithMeMongo`。
- `Mongo:DatabaseName` 改为 `chatwithme`。
- 实测：`/api/health/mongo` → **200**（`MongoDB ping 成功，库 chatwithme，集合 messages`）；启动日志 `MongoDB 消息集合索引已就绪：ix_messages_conversation_sent_at`。

**新增接口**

- `GET /api/users`、`GET /api/files`：分页 + `keyword`。
- 领域仓储新增 `ListAsync`/`CountAsync`；应用层新增 `PagedResult<T>`、`Paging.Normalize`；`ILIKE` 关键字做了 `%`/`_`/`\` 转义。

**时间口径补丁**

- 新增 `IClock` + `SystemClock`，`AuthAppService`（3 处）与 `JwtTokenIssuer` 改用注入时钟。
- 新增 `LocalDateTimeConverter`、`TimeZoneResolution`；`Program.cs` 注册时区单例、JSON 转换器，并打印 `时间输出时区：Asia/Shanghai (UTC+08:00)`。
- `before` 修正：由 `DateTime?` 绑定改为 `string?` + 显式解析，按配置时区转 UTC；非法游标返回 400。
- `nextBefore` 改为毫秒精度游标（API 响应类型 `MessagePageResponse`）。
- 删除 `appsettings.json` 中硬编码的 `Jwt:SigningKey`（违反"秘密只在 user-secrets"）。

**前端**

- 新建 `src/Client/admin`：Vue 3 + Vite 8 + TS + TDesign 1.20 + vue-router（hash）。
- 登录 / 用户 / 文件 / 消息四页，三段式布局；401 自动刷新令牌并重试（已用篡改 token 的方式实测通过）。

**实测记录**

| 项 | 结果 |
| --- | --- |
| `dotnet build` | 0 warning / 0 error |
| `npm run build`（vue-tsc + vite） | 通过；单 chunk 1.36 MB（TDesign 全量引入，仅告警） |
| `/api/health/mongo` | 200 |
| 用户注册 / 登录 | 201 / 200 |
| `GET /api/users?page=1&pageSize=3` | 200，`total` 正确，时间格式 `2026/11/16 16:38:48` |
| `GET /api/users?keyword=ACC_` | 200，大小写不敏感命中 |
| `GET /api/users?keyword=%` | 200，`total=0`（通配符已转义） |
| 文件上传 / 列表 / 无 token | 201 / 200 / 401 |
| 消息发送 5 条 → `limit=2` 翻 3 页 | 5 条全部命中，无重复无遗漏（同一秒内的 5 条） |
| `before=not-a-date` | 400，提示合法格式 |
| 篡改 localStorage accessToken 后进入用户页 | 401 → 刷新 → 重试成功，令牌被替换为有效 JWT |
| `/` / `/scalar` / `/scalar/` | 302 / 302 → `scalar/` / 200 |
| `/api/users`（无 token） | 401 |

---

## 10. 已知缺口与注意事项

- **无自动化测试**：当前全部为手工/脚本实测。
- **同毫秒并发**：游标为时间戳，理论上同一毫秒内的多条消息在跨页时仍可能重复或遗漏；如需强一致可改为 `(SentAtUtc, Id)` 复合游标。
- **前端包体**：TDesign 全量引入，单 chunk 约 1.36 MB；如在意可改按需引入 + 路由懒加载。
- **`/uploads` 无鉴权**：静态文件可被匿名读取（文件名随机）。
- **Mongo 库共享**：`chatwithme` 库中还有别的集合，删除数据前先确认。
- **EF 工具**：仓库内没有 `dotnet-tools.json`，执行 `dotnet ef` 前需自行 `dotnet tool install --global dotnet-ef --version 10.0.15`（或建本地清单）。
- **遗留测试数据**：远端库有若干 `probe*` / `acc_*` / `msg_*` 账号与 `wwwroot/uploads` 下的测试文件；删文件会导致对应库行指向空，清理时需一并处理。
- **Windows 环境坑**：
  - `dotnet build` 报 `Value cannot be null (Parameter 'path1')`（NuGet.targets `_GetRestoreSettings`）时，是进程缺少 `USERPROFILE`/`ProgramData`/`APPDATA` 等标准环境变量，不是代码问题。
  - 本机首次给工作区授权时出现过 `SetNamedSecurityInfoW failed (Win32 5)`，属于目录缺少 `WRITE_OWNER`；修复后已可正常读写。

---

## 11. 开发约定

1. 文档只写本 README，不新建文档文件。
2. 每次改动后 `dotnet build` 必须 0 warning / 0 error。
3. 代码中不写 `.Result` / `.Wait()` / `.GetAwaiter().GetResult()`（连接串同步加载处为既有例外，位于基础设施加载逻辑）。
4. 秘密只进 user-secrets，禁止提交到仓库。
