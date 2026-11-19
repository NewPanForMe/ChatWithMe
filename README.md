# ChatWithMe

### 全员通知实施计划（2026-11-19，已确认）

目标：管理员 Web 填写标题和正文发送全员通知；PostgreSQL 保存后通过 SignalR 广播，桌面端浮动通知提供“查看详情”，通知列表支持离线登录后补拉。

- [x] 通知领域模型、仓储、应用服务及迁移：标题最多 100 字，正文最多 10000 字；先保存再推送，失败不丢失已保存通知。
- [x] 管理员发送/分页查询接口及聊天用户列表/详情接口，校验身份类型；仅向聊天用户连接广播。
- [x] Web 新增通知管理页、发送表单及历史列表，结果沿用标题为“通知”的 Notification。
- [x] 桌面端独立通知列表/详情页，实时弹窗按钮可打开详情；启动和重连刷新，按 Id 去重。
- [x] 验证输入边界、先保存后广播、广播失败仍可补拉、客户端去重及隔离；完成 .NET/Web 构建与测试。

架构：沿用四层 DDD、PostgreSQL/EF Core、SignalR、Avalonia/Arc、Vue/TDesign；正文作为纯文本显示。通知面向所有聊天用户，历史通知允许登录用户查询。分页每页 20 条，最大 100 条。不新增外部依赖；数据库迁移单独执行并验证。

使用入口：管理后台 `#/notifications` 默认显示通知列表，点击“添加通知”打开居中的“新增通知”弹窗，填写标题、正文后点击“发送通知”。发送成功关闭弹窗并刷新列表，失败保留填写内容；发送期间防止重复提交和关闭弹窗。桌面端收到标题为“通知”的浮动提示后，点击“查看详情”进入通知页面，也可从侧栏“通知”查询历史。历史通知每页 20 条，支持加载更多；重新登录、重连和手动刷新会补拉第一页，较早通知通过“加载更多”查看。推送与补拉按 Id 合并，最新通知排在前面，详情仅接受最后一次点击的响应。

接口：管理员 `GET/POST /api/admin/notifications`；聊天用户 `GET /api/notifications`、`GET /api/notifications/{id}`。实时事件为 `notificationReceived`。HTTP 与 SignalR 的时间格式统一使用配置时区及 `yyyy/MM/dd HH:mm:ss`。广播成功表示服务端已提交推送，不代表客户端已读；广播失败时仍保留通知并明确告知管理员，可通过列表补拉。

部署前先应用迁移 `20261119005806_AddAnnouncements`（新增 `announcements` 表及索引），再启动更新后的后端和客户端。2026-11-19 排查登录后通知加载失败时，确认该迁移尚未执行，现已应用并验证通知计数及列表查询成功，待执行迁移为 0。可审核并在已应用此前迁移的 PostgreSQL 库执行 `artifacts/add-announcements.sql`（幂等脚本），或在设置好设计时连接配置后运行：

```powershell
dotnet ef database update --project src/Server/ChatWithMe.Infrastructure --startup-project src/Server/ChatWithMe.Infrastructure
```

验证：后端 114 项、桌面端 10 项、Web 8 项测试通过；包含本地真实 HTTP/WebSocket 两客户端广播和权限测试。数据库模型与迁移一致；已应用真实 PostgreSQL 通知表迁移并完成只读查询验证，未进行桌面界面手工联调。

一个用于学习新技术的聊天软件：.NET 10 四层 DDD 后端（PostgreSQL + MongoDB）+ Vue 3 管理后台。

---

## 1. 目录结构

```
ChatWithMe/
├─ README.md                       # 唯一文档：说明 + 变更日志
├─ ChatWithMe.slnx                 # 全仓解决方案（后端 4 层 + 测试 + 桌面客户端）
├─ doc/聊天软件用户需求说明书.md
└─ src/
   ├─ Server/                      # 后端（.NET 10 / net10.0）
   │  ├─ ChatWithMe.sln
   │  ├─ ChatWithMe.Domain/        # 领域层：零 PackageReference、零项目引用
   │  ├─ ChatWithMe.Application/   # 应用层：用例、DTO、抽象（IFileStorage/IClock…）
   │  ├─ ChatWithMe.Infrastructure/# 基础设施：EF Core、MongoDB、JWT、本地存储、连接串解密
   │  └─ ChatWithMe.Api/           # 宿主：控制器、鉴权、CORS、Scalar、异常处理
   └─ Web/                           # 管理后台（Vue 3 + Vite + TS + TDesign）
      ├─ src/                        # 路由级懒加载 + TDesign 按需引入
      ├─ vite.config.ts              # unplugin-vue-components 解析 TDesign
      └─ package.json                # dev / build / test（vitest）
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
- 换机器必须重设：两个密钥缺失或格式错误时应用**启动即失败**（`JwtOptions.Validate()` 与 `DeliveryKeyProvider.ValidateAtStartup()` 都 fail-fast），不再让数据库相关端点静默降级。
- `appsettings.json` 不含任何密钥。

### 3.1.1 构建（全仓）

```powershell
dotnet build ChatWithMe.slnx                       # 全仓：0 warning / 0 error
dotnet test ChatWithMe.slnx                        # 后端单元测试
dotnet build src/Client/ChatWithMe.Client.slnx     # 只构建桌面客户端
```

根目录 `ChatWithMe.slnx` 覆盖 `src/Server` 的四个项目 + `ChatWithMe.Tests` + `src/Client/ChatWithMe.Client`；`src/Client/ChatWithMe.Client.slnx` 只包含桌面客户端；`src/Server/ChatWithMe.sln` 是后端历史解决方案，保留兼容。

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
cd src/Web
npm install
npm run dev          # http://localhost:5173/#/login
npm test             # vitest：8 个用例，覆盖 http 层与内存会话
```

- **必须用 `localhost` 访问**（`vite.config.ts` 的 `server.host` 固定为 `localhost`）：刷新令牌是 `SameSite=Strict` 的 HttpOnly Cookie，`127.0.0.1` 与 `localhost` 属不同站点，用 IP 打开会带不上 Cookie。
- 后端地址默认 `http://localhost:5217`，可用环境变量 `VITE_API_BASE_URL` 覆盖；跨域时后端 `Cors:AllowedOrigins` 需包含后台源（`AllowCredentials` 已开启）。
- 生产构建：`npm run build`（先 `vue-tsc --noEmit` 再 `vite build`），产物在 `dist/`。
- 需要先有一个账号：`POST /api/users` 注册（匿名），或直接登录已有账号。

---

## 4. 架构与关键决策（已冻结）

| 议题 | 结论 |
| --- | --- |
| 架构 | DDD 四层，`net10.0` |
| 主库 | PostgreSQL，EF Core + Npgsql，迁移 `20261116054648_InitialCreate`、`20261116060400_AddRefreshTokens` |
| 连接串 | 优先从 `https://www.tunn.work/ToolsConsole/ConnectionString/GetByConnName` 取，AES-256-GCM 解密（`nonce[12] + ciphertext + tag[16]`，Base64）；成功后写入本地加密缓存，远端不可用时回退缓存 |
| 配置故障策略 | 关键密钥（`Jwt:SigningKey`、`Security:DeliveryKey`）缺失或格式错误 → **启动 fail-fast**；外部依赖（ToolsConsole / PostgreSQL / MongoDB）不可用 → 降级并在启动日志与 `/api/health/*` 显式暴露 |
| 双库分工 | PostgreSQL 存用户 / 刷新令牌 / 文件元数据；MongoDB 存聊天消息 |
| JWT | access 15 分钟 + refresh 7 天（轮换、可吊销、只存 SHA-256 哈希）；access 只在内存，refresh 只放 HttpOnly/Secure/SameSite Cookie；注册匿名，其余 `[Authorize]` |
| 文件 | `IFileStorage` 抽象 + 本地 `wwwroot/uploads`（**不挂静态目录**）；回读走 `GET /api/files/{id}/content`（Bearer 或短时签名 URL）；FormOptions + Kestrel 限制 + 资源阶段短路 413 |
| 时间 | 存 UTC；JSON 输出统一 UTC+8、`yyyy/MM/dd HH:mm:ss`（无小数秒） |
| 时区 | `Time:TimeZoneId`（默认 `Asia/Shanghai`，回退 `China Standard Time`） |
| 异步 | 只要求业务层（Application 服务 / Controller / 仓储）async；连接串加载逻辑保持同步 |
| 管理后台 | Vue 3 + Vite + TS + `tdesign-vue-next`（unplugin-vue-components 按需）+ `vue-router`（hash 路由、路由级懒加载），三段式 `t-layout` |
| 文档 | 不再新增文档文件，所有说明与日志只写本 README |

---

## 5. 后端接口

| 方法 | 路径 | 鉴权 | 说明 |
| --- | --- | --- | --- |
| POST | `/api/users` | 匿名 | 注册 |
| GET | `/api/users` | **Admin** | 用户分页列表，`page`/`pageSize`/`keyword`（邮箱或昵称，大小写不敏感） |
| GET | `/api/users/{id}` | Bearer | 用户详情（聊天端解析昵称用） |
| DELETE | `/api/users/{id}` | **Admin** | 删除聊天用户：清理会话成员/刷新令牌/好友/拉黑，消息保留 |
| POST | `/api/admin/auth/login` | 匿名 | 管理后台登录（账号来自 `admin_users`，与聊天用户分离） |
| GET | `/api/admin/accounts` | **Admin** | 管理后台账号（后端用户）列表 |
| POST | `/api/admin/accounts` | **Admin** | 新建管理员（邮箱/名称/密码，密码至少 8 位） |
| DELETE | `/api/admin/accounts/{id}` | **Admin** | 删除管理员（不能删除当前登录账号） |
| POST | `/api/auth/login` | 匿名 | 登录，返回 access（refresh 只走 HttpOnly Cookie） |
| POST | `/api/auth/refresh` | Cookie | 刷新（轮换，旧 refresh 立即失效，新 Cookie 覆盖旧值） |
| POST | `/api/auth/logout` | Cookie | 吊销 refresh 并清除 Cookie |
| POST | `/api/files/upload` | Bearer | 单文件上传 |
| POST | `/api/files/upload-batch` | Bearer | 多文件上传（上限 `MaxFilesPerRequest`） |
| GET | `/api/files` | Bearer | 文件分页列表，`page`/`pageSize`/`keyword`（原始文件名） |
| GET | `/api/files/{id}` | 匿名 | 文件元数据 |
| GET | `/api/files/{id}/content` | Bearer 或签名 | 文件回读；`exp`+`sig` 为短时签名链接参数 |
| GET | `/api/files/{id}/link` | Bearer | 生成短时签名回读链接（`FileStorage:LinkLifetimeSeconds`，默认 300 秒） |
| POST | `/api/conversations` | Bearer | 创建会话：`{"type":"Direct","peerUserId":"..."}` 或 `{"type":"Group","title":"..."}`；单聊复用已存在的两人会话 |
| GET | `/api/conversations` | Bearer | 我参与的会话列表 |
| GET | `/api/conversations/{id}` | Bearer | 会话详情（成员、角色、已读位置），非成员 403 |
| POST | `/api/messages` | Bearer | 发送消息（写 MongoDB）；需会话成员；`clientMessageId` 幂等 |
| GET | `/api/messages` | Bearer | 消息倒序分页，`conversationId`/`before`/`limit`；`before` 为 `(SentAtUtc, Id)` 复合游标；非成员 403 |
| GET | `/api/health/db` | 匿名 | PostgreSQL 连通性 + 连接串来源（`remote`/`cache`） |
| GET | `/api/health/mongo` | 匿名 | MongoDB 连通性（不可用返回 503） |

分页列表统一返回 `{ items, total, page, pageSize }`。会话与成员存在 PostgreSQL，消息存在 MongoDB（集合 `cwm_messages`）。

---

## 6. 时间口径（重点）

- **存储**：数据库与 MongoDB 一律 UTC。
- **输出**：`LocalDateTimeConverter` 把每个 `DateTime` 转到 `Time:TimeZoneId` 并格式化为 `yyyy/MM/dd HH:mm:ss`，不带 `Z`/偏移/小数秒。例：`2026/11/16 16:38:48`。
- **输入**：`before` 以 `string` 接收并显式解析（不走 query 的 `DateTime` 绑定，避免跟随服务器区域设置），接受：
  - `yyyy/MM/dd HH:mm:ss`（展示格式）
  - `yyyy/MM/dd HH:mm:ss.fff`（游标格式）
  - ISO-8601（带偏移则按其自身偏移）
- **游标**：`nextBefore` 是 `(SentAtUtc, Id)` 复合游标，文本形如 `yyyy/MM/dd HH:mm:ss.fff|{messageId}`（配置时区毫秒时间 + 消息 Id）。只按时间戳翻页时，同一毫秒内的多条消息跨页会重复或遗漏；加上 Id 兜底后整体排序是严格全序。不带 `|` 的旧式时间戳游标仍然接受。
- 时区解析见 `TimeZoneResolution.Resolve`：依次尝试 `Time:TimeZoneId` → `Time:FallbackTimeZoneId` → `Asia/Shanghai` → `China Standard Time` → UTC。

---

## 7. MongoDB 说明

- 连接名：`Security:ConnectionNames:Mongo = ChatWithMeMongo`（ToolsConsole 实测 `code=0`）。
- 远端下发的连接串不含 `authSource`，而账号建在 `admin` 库，驱动会拿业务库去认证 → `SCRAM-SHA-256` 认证失败。因此新增 `Mongo:AuthSource = admin`，由 `MongoConnectionSettings.BuildClientSettings` 在连接串未自带 `authSource` 时补齐；连接串自带则以其为准。
- 库名用小写 `chatwithme`：远端实例已存在该库，MongoDB 拒绝创建仅大小写不同的 `ChatWithMe`（报 `db already exists with different case`）。
- 集合 `cwm_messages`；索引 `ix_cwm_messages_conversation_sent_at_id`（ConversationId 升序 + SentAtUtc 降序 + Id 降序，对应复合游标翻页）与唯一索引 `ux_cwm_messages_conversation_client_message_id`，启动时由 `MongoIndexInitializer` 创建。旧索引 `ix_cwm_messages_conversation_sent_at` 已被前者取代，确认新索引生效后可手工 `dropIndex`。
- 该库中另有 `mylogs1`、`dbx_init` 两个与本项目无关的集合，**不要删除**。

---

## 8. 管理后台

- 路由（hash）：`#/login`、`#/users`、`#/files`、`#/messages`，未登录访问受保护页会带 `redirect` 跳登录；四个页面与 `AdminLayout` 全部路由级动态 `import`，各自成 chunk。
- 布局：`t-layout` 三段式（aside 菜单 + header 标题/用户/退出 + content）。
- 令牌：**access token 只存内存**（`stores/auth.ts` 的 `reactive`，不写 `localStorage`/`sessionStorage`）；refresh token 由后端放进 `HttpOnly; Secure; SameSite=Strict; Path=/api/auth` 的 Cookie，前端 JS 读不到。启动时 `bootstrapSession()` 用该 Cookie 静默换回 access token；请求带 `Authorization: Bearer`，遇 401 走**单飞刷新**（并发只刷一次）后重试原请求，失败则清会话回登录页。
- TDesign：`unplugin-vue-components` + `TDesignResolver` 按需解析，模板里用到的组件才进产物（`src/components.d.ts` 自动生成）。
- 操作反馈统一通过 `utils/notify.ts` 使用 TDesign Notification（右上角、可关闭、自动消失）：登录/退出、用户及管理员新增删除、手动刷新、查询和加载消息、强制下线等均显示操作结果；首次列表加载成功不弹通知，失败显示错误通知。
- 列表行的删除用户、删除管理员及强制下线操作统一通过 `utils/confirm.ts` 显示屏幕居中的模态确认框，显示目标账号和操作影响，确认后执行，执行期间防止重复提交。
- 页面：登录；用户列表（分页 + 关键字 + 删除聊天用户）；**后端用户（管理员账号：列表 / 新建 / 删除，当前账号不可删）**；文件列表（分页 + 关键字，点文件名先换短时签名链接再打开）；消息查询（会话 ID + limit + `nextBefore` 加载更多）；在线管理（SignalR 在线用户 + 踢下线）。

---

### 桌面客户端（Avalonia + TKCode.Arc）

> 原计划用 WPF，但 [TKCode.Arc](https://github.com/AIDotNet/TKCode.Arc) 是 **Avalonia 12 组件库**（原生 Avalonia 控件，依赖 `Avalonia >= 12.1.5`），无法在 WPF 中引用；经确认客户端改用 Avalonia 12，WPF 代码与 XAML 不能直接复用。

- 位置：`src/Client/ChatWithMe.Client`；Avalonia 12.1.5 + `TKCode.Arc` 1.1.0（主题、控件、字体、Lucide 图标）+ CommunityToolkit.Mvvm + Microsoft.Extensions.DependencyInjection。
- 运行：`dotnet run --project src/Client/ChatWithMe.Client`
- 后端地址：默认 `http://localhost:5217`，环境变量 `CHATWITHME_API` 覆盖。
- 已实现：
- **登录 + 注册**：登录窗口内切换「登录 / 注册」；注册校验邮箱格式、密码 ≥ 6 位、两次密码一致，注册成功后自动登录。
- **聊天页**：左栏展示已添加的好友，点击好友即复用/创建单聊并选中会话；消息拉取（接口倒序转正序展示）、发送（带 `clientMessageId` 幂等，Enter 发送）。
- **消息时间按需显示**：气泡默认不显示时间；滚动消息区时全部显示 1.5 秒，鼠标悬停单条时显示。
- **好友页**：搜索用户 / 发申请 / 同意拒绝 / 好友列表 / 删除好友 / 发消息。
- 删除好友前弹出确认框（显示昵称和邮箱），取消不执行删除；确认并删除成功后立即移除该好友并重新加载好友及申请列表。
- 好友页的删除、刷新、好友申请处理等操作结果统一使用 Arc Notification 浮动通知（`Toast`），区分成功/失败；首次加载成功不弹提示，操作成功但刷新失败会单独说明。
- **实时消息**：SignalR 连接 `/hubs/chat`，自动重连、加入会话组；收到 `messageReceived` 按 Id 去重后追加并自动滚到底部。单实例用进程内推送，**不需要 RabbitMQ**；多实例时再给 SignalR 加 Redis backplane。
- **强制下线回退登录**：实时连接断开/重连时校验当前会话；HTTP 请求刷新令牌被拒绝或刷新后仍返回 401 时清理会话，关闭聊天窗口并返回登录页。重新登录使用全新的令牌、Cookie、实时连接和页面状态；被踢出后不会触发联调自动登录。普通网络故障或服务暂时不可用保留会话及自动重连。
- 侧边栏导航 + 设置页。
- 联调便利：设置环境变量 `CHATWITHME_TEST_EMAIL` / `CHATWITHME_TEST_PASSWORD` 可预填登录框（只预填，不自动登录）。
- 时间字段：后端输出 `yyyy/MM/dd HH:mm:ss`（非 ISO-8601），客户端 DTO 一律用 `string` 接收，避免 `System.Text.Json` 解析失败。
- 待办：群聊创建与邀请确认、未读数/已读位置、撤回、退出登录、错误提示改用 Arc 通知组件、消息气泡左右分栏。

---

## 9. 变更日志

### 2026-11-17 · 架构评审整改（15 条）

**安全**

- 令牌存储改造：refresh token 改走 `HttpOnly; Secure; SameSite=Strict; Path=/api/auth` Cookie（`Auth:Cookie` 可配），access token 只存内存；`/api/auth/refresh`、`/api/auth/logout` 改为从 Cookie 读令牌，响应体不再返回 refresh token。前端删除 `localStorage` 会话持久化，启动时静默刷新恢复。
- `/uploads` 不再挂静态目录：新增 `GET /api/files/{id}/content`（Bearer 或短时签名 URL）与 `GET /api/files/{id}/link`；签名用 `Jwt:SigningKey` 派生子密钥（HMAC-SHA256），绑定文件 Id + 过期时间（默认 300 秒）。
- 文件页改为"先换签名链接再打开"，`FileAssetDto.Url` 指向鉴权回读端点。

**分页**

- 消息游标改为 `(SentAtUtc, Id)` 复合键：`nextBefore` 形如 `yyyy/MM/dd HH:mm:ss.fff|{messageId}`，Mongo 过滤 `SentAtUtc < t OR (SentAtUtc = t AND Id < id)`，排序 `SentAtUtc DESC, Id DESC`；索引改为 `ix_cwm_messages_conversation_sent_at_id`。旧时间戳游标仍兼容。

**配置与外部依赖**

- 关键密钥统一 fail-fast：`Security:DeliveryKey` 在启动阶段校验（新增 `DeliveryKeyProvider`），与 `Jwt:SigningKey` 一致；不再出现"数据库端点静默降级"。
- 连接串新增本地加密缓存（AES-256-GCM，目录 `Security:ConnectionStringCacheDirectory`，默认 `App_Data/connection-string-cache`）：远端 ToolsConsole 不可用时回退最近一次成功值。
- `/api/health/db` 新增 `connectionStringSource`（`remote`/`cache`）与 `connectionStringCachedAtUtc`；启动日志打印连接串来源。

**前端**

- 路由级懒加载（4 个页面 + `AdminLayout`）。
- TDesign 改 `unplugin-vue-components` 按需引入，移除 `app.use(TDesign)` 与全量 CSS 入口。
- 新增 vitest（8 个用例：http 单飞刷新 / 401 登出 / 204 与错误映射 / `auth:false`；会话内存化与静默刷新）。

**文档**

- README 目录树、启动命令、接口表、集合名、Mongo 索引、决策表全面对齐 `src/Web` 与 `cwm_messages`。

**实测**

| 项 | 结果 |
| --- | --- |
| `dotnet build ChatWithMe.sln` | 0 warning / 0 error |
| `dotnet test` | 88 passed / 0 failed |
| `npm test`（vitest） | 8 passed / 0 failed |
| `npm run build` | 通过；入口 106 KB、布局 41 KB，按路由分包，最大 chunk 为 table 351 KB（改前单 chunk 1.36 MB） |
| 登录页渲染（vite dev） | 正常，无 TDesign 组件解析告警 |
| 登录响应体 | 200，仅 `accessToken`/`accessTokenExpiresAtUtc`/`tokenType`/`user`，不含 refreshToken |
| 刷新令牌 Cookie | `HttpOnly`、`SameSite=Strict`、`Path=/api/auth`（Development 下 `Secure=false`，生产为 `true`） |
| 启动日志 | `连接串来源：Remote`；缓存文件已写入 `App_Data/connection-string-cache/*.cache`（内容为密文） |
| `/api/health/db` | 200，`connectionStringSource=remote` |
| `/api/files/{id}/link` → 匿名打开 | 200，内容一致；无签名 / 篡改签名 → 401 |
| `/uploads/...` 直链 | 404（静态目录已下线） |
| 5 条消息 `limit=2` 翻页 | 3 页取满 5 条，`nextBefore` 形如 `…09:37:02.164|99c3a624…`，无重复无遗漏 |
| 非法游标（`not-a-date` / 非法 Id） | 400 |
| 登出 → 再刷新 | 204 → 401（Cookie 已清除） |

### 2026-11-17 · 客户端接入 SignalR 实时消息

- 新增 `Microsoft.AspNetCore.SignalR.Client` 10.0.14 与 `Services/ChatConnection`：连接 `/hubs/chat`、`WithAutomaticReconnect()`、`AccessTokenProvider` 使用当前访问令牌。
- `ChatPageViewModel` 初始化时连接并加入所有会话组；打开/新建会话时补加入组；重连后重新入组。
- 收到 `messageReceived` 后在 UI 线程按消息 Id 去重追加到当前会话；SignalR 的 ISO 时间统一转成本地 `yyyy/MM/dd HH:mm:ss`。
- 新消息（含实时推送）自动滚到底部，避免"其实收到了但看不到"。
- 结论：**不需要 RabbitMQ**。客户端实时性由 WebSocket（SignalR）负责；RabbitMQ 适合服务端解耦/削峰，多实例 SignalR 只需 Redis backplane。
- 实测：ad-a 通过 API 发消息，ad-b 客户端未刷新即在底部出现新消息（服务端日志 received → VM appended，截图确认）。

### 2026-11-17 · 客户端修复：搜索、刷新图标、时间显示

- **令牌自动刷新**：`ApiClient.SendAsync` 遇 401 时用 HttpOnly Cookie 调 `POST /api/auth/refresh` 换新令牌后重试一次。修复"登录超过 15 分钟后搜索/列表请求 401、界面空白、看起来像找不到用户"的问题。
- **发起请求的释放 bug**：`SendOnceAsync` 原先未 `await` 就返回，`using` 提前释放 `HttpRequestMessage`；已改为 await 后再返回。
- **好友搜索反馈**：搜索按钮下方就地显示「找到 N 个用户 / 没有找到匹配的用户（不能搜索自己…）/ 接口错误」。
- **好友列表不渲染**：Arc 的 `ListBox` 在该用法下不显示行，聊天页好友列表与好友页搜索结果改为 `ItemsControl` + 按钮行；点击好友复用/创建单聊并加载消息。
- **聊天页刷新按钮**：从整行按钮改为「好友」标题右上角的刷新图标（Lucide `RefreshCw`）。
- **消息时间**：默认隐藏；鼠标滚轮滚动时全部显示 1.5 秒；鼠标停在单条消息上显示该条。
- 实测：默认无时间 → 悬停首条显示 `2026/11/17 13:27:25` → 滚轮后所有可见消息显示时间；聊天页好友行可点击进入会话。

### 2026-11-17 · 客户端：注册 / 好友聊天 / 时间按需显示

- **注册**：登录窗口内切换登录与注册模式；客户端校验邮箱正则（`^[^\s@]+@[^\s@]+\.[^\s@]+$`，≤256）、密码 ≥ 6 位、确认密码一致；昵称取邮箱 `@` 前缀；注册成功自动登录。错误映射：409 →「该邮箱已注册，请直接登录。」
- **聊天页改造**：移除「输入邮箱发起单聊」，左栏改为好友列表（`GET /api/friends`），点击好友调用 `POST /api/conversations`（复用已有单聊）并选中会话；好友页「发消息」复用同一入口。
- **消息时间**：`MessageItem` 改为可观察对象，新增 `IsTimeVisible`；`ScrollViewer.ScrollChanged` 触发全部显示并在 1.5 秒后隐藏，气泡 `PointerEntered/Exited` 控制单条显示。
- 实测：`dotnet build ChatWithMe.slnx` 0 warning / 0 error；注册接口 201 → 登录 200、重复邮箱 409、非法邮箱 400、短密码 400。（客户端 UI 截图验证待解锁桌面后补）

### 2026-11-17 · 账号分离 + 用户删除 + 好友页

- **管理端账号独立**：新增 `admin_users` 表与 `AdminUser` 领域对象（迁移 `20261117024943_AddAdminUsers`）；新增 `POST /api/admin/auth/login`；访问令牌新增 `typ=admin|user` 声明，`SessionVersionMiddleware` 按 `typ` 分别校验 `admin_users` / `users`。`Admin` 策略同时要求 `role=Admin` 与 `typ=admin`，聊天用户即使 `role` 被改成 Admin 也进不了管理端。
- **默认管理员**：`Security:BootstrapAdminEmail` / `Security:BootstrapAdminPassword`（`appsettings.Development.json`，**仅开发环境**，默认 `admin@chatwithme.local` / `Admin@12345`，生产必须修改）。
- **用户删除**：`DELETE /api/users/{id}`（Admin），清理会话成员、空会话、刷新令牌、好友、好友申请、拉黑记录；Mongo 消息保留。
- **好友接口补全**：`FriendRequestDto` 增加申请人与接收人的邮箱/昵称，客户端无需再逐个查用户。
- **客户端好友页**：侧边栏新增「好友」（搜索、申请、同意/拒绝、好友列表、删除、发消息跳转聊天）。
- **管理后台**：登录改走 `/api/admin/auth/login`；用户页新增删除操作；新增「后端用户」页（管理员账号列表 / 新建 / 删除，不能删除当前登录账号）。
- 实测：管理端登录 200、管理端账号登录聊天端 401、聊天用户访问管理列表/删除 403、管理员列表 200、删除用户 204 且再次登录 401、好友申请 DTO 返回 `requesterNickName/requesterEmail`；`dotnet build` 0 warning / 0 error；测试 **99 passed**。

### 2026-11-17 · 桌面客户端首版（Avalonia + TKCode.Arc）

- 新建 `src/Client/ChatWithMe.Client`：`dotnet new tkcode-arc --layout sidebar --mvvm toolkit --di --ui-language zh-CN --framework net10.0 --custom-titlebar`。
- 新增 `Services/ApiClient`（登录 / 会话 / 消息 / 用户搜索）、`Services/Session`、`Views/LoginWindow`、`Pages/ChatPage` + `ViewModels/ChatPageViewModel`；删除模板自带的首页/项目页与示例数据。
- 实测：`dotnet build` 0 warning / 0 error；窗口启动、用测试账号登录成功、主界面（侧边栏 + 会话列表 + 消息区 + 发送框）渲染正常。

### 2026-11-17 · 迭代 I1（好友与联系人）

**领域与数据**

- 新增 `Friendship`（好友关系由 A→B、B→A 两行组成，备注各存各的）、`FriendRequest`（Pending / Accepted / Rejected）、`UserBlock`。
- 新迁移 `20261117010337_AddFriendships`：`friendships`、`friend_requests`、`user_blocks`，已 `database update`。

**接口**（均需 Bearer）

- `GET /api/friends/search?keyword=`：按邮箱/昵称搜索，排除自己；关键字为空返回空列表。
- `POST /api/friends/requests`：发起申请（不能加自己、不能重复申请、已是好友/存在拉黑则拒绝）。
- `GET /api/friends/requests`、`GET /api/friends/requests/outgoing`、`POST /api/friends/requests/{id}/accept|reject`。
- `GET /api/friends`、`PATCH /api/friends/{userId}`（备注）、`DELETE /api/friends/{userId}`。
- `GET /api/friends/blocked`、`POST|DELETE /api/friends/blocked/{userId}`；拉黑会同时解除好友关系。
- 发消息与会话创建新增拉黑校验：存在拉黑关系时返回 403（"对方已将你拉黑…" / "存在拉黑关系…"）。

**状态码**：重复申请/已是好友 → 409；非被邀请人处理申请 → 403；目标不存在 → 404。

**实测**：搜索命中；申请 201 → 重复 409；对方列表 1 条；同意后双向好友；备注写入；拉黑后好友列表清零且对方发消息 403、发申请 403；解除拉黑后申请恢复 201。

### 2026-11-17 · 迭代 I2（SignalR 实时与在线管理）

**实时通道**

- 新增 SignalR Hub `/hubs/chat`（`[Authorize]`）：连接时登记在线；`JoinConversation(conversationId)` 校验成员后加入会话组，`LeaveConversation` 退出。
- 消息落库成功后通过 `IChatNotifier` 向会话组广播 `messageReceived`（Application 不直接依赖 SignalR；幂等命中不重复广播）。
- 在线登记表为进程内实现（多端登录计多条连接），多实例部署时替换为 Redis。

**强制下线（踢人）**

- `users` 新增 `session_version`（迁移 `20261117020101_AddSessionVersion`）；JWT 新增 `role`、`sv` 两个声明。
- `SessionVersionMiddleware`：每次认证请求比对令牌 `sv` 与库中会话版本，不一致立即 401「会话已失效」——不必等 15 分钟令牌过期。
- 新增 `Admin` 授权策略（`role=Admin`）；`Security:BootstrapAdminEmail` 可在启动时把指定账号提升为管理员（只提升一次，失败只记日志）。
- `POST /api/admin/users/{id}/kick`：会话版本自增 + 吊销该用户全部有效刷新令牌 + 服务端 `Abort()` 所有 SignalR 连接。
- `GET /api/admin/users/online`：在线用户（邮箱/昵称/连接数）。

**前端**

- 新增「在线管理」页：在线列表 + 踢下线二次确认，踢完自动刷新。

**实测**

| 项 | 结果 |
| --- | --- |
| 管理员登录后 `GET /api/admin/users/online` | 200 |
| 普通用户访问管理接口 | 403 |
| `POST /hubs/chat/negotiate` | 200（返回 connectionToken） |
| 踢人响应 | `sessionVersion=1`、`revokedRefreshTokens=1` |
| 被踢用户旧 access token 再请求 | 401「会话已失效」（立即生效） |
| 被踢用户刷新令牌 | 401「刷新令牌无效、已吊销或已过期」 |

### 2026-11-17 · 迭代 I0（会话模型与权限）

**领域与数据**

- 新增 `Conversation`（`Direct`/`Group`）、`ConversationMember`（Owner/Admin/Member、`LastReadMessageId`/`LastReadAtUtc`/`MutedUntilUtc`）与 `IConversationRepository`。
- `User` 新增 `Role`（`Member`/`Admin`，默认 Member）。
- 新迁移 `20261117005612_AddConversations`：`conversations`、`conversation_members`、`users.role`，已 `database update`。
- MongoDB 集合加前缀：`messages` → **`cwm_messages`**（以 `$out` 迁移 5 条历史数据后删除旧集合）；新增幂等唯一索引 `ux_cwm_messages_conversation_client_message_id`（partial，仅对存在 `ClientMessageId` 的文档生效）。

**权限与幂等**

- `POST /api/conversations`、`GET /api/conversations`、`GET /api/conversations/{id}`；单聊自动复用。
- 发消息/拉消息前校验会话成员：非成员 **403**，会话不存在 **404**；管理员（`Role=Admin`）可跨会话只读（审计用）。
- 消息新增 `clientMessageId` 幂等键：同会话重复提交返回同一条消息（唯一索引兜底）。
- 异常映射新增 403/404。

**测试**

- 新建 `ChatWithMe.Tests`（xUnit），45 个用例覆盖：会话领域规则、消息领域规则、分页收敛、时间转换器（含毫秒游标往返）、`ConversationAppService`、`MessageAppService`（成员/非成员/管理员/幂等）。
- 统一 EF Core 包版本到 10.0.15，消除测试项目的 MSB3277 冲突告警。

**实测**

| 项 | 结果 |
| --- | --- |
| `dotnet build ChatWithMe.sln` | 0 warning / 0 error |
| `dotnet test` | 45 passed / 0 failed |
| 建单聊（A↔B） | 201，`members=2`，Owner=A |
| 重复建同一单聊 | 201，返回**同一**会话 ID |
| 发送 + 同 `clientMessageId` 重发 | 201，两次返回同一消息 ID |
| B 拉取 | 200，1 条 |
| C（非成员）拉取 / 发送 | 403 + "不是该会话成员…" |
| 建群 | 201，`type=Group`，创建者唯一成员 |
| 对不存在的用户建单聊 | 404 |
| 管理员跨会话读取 | 允许（单测覆盖） |

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

- 新建 `src/Client/admin`（后整体迁到 `src/Web`）：Vue 3 + Vite 8 + TS + TDesign 1.20 + vue-router（hash）。
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

- **测试覆盖有限**：`ChatWithMe.Tests` 有 94 个单元测试（领域 / 应用服务 / 序列化 / 游标编解码 / 文件链接签名 / 连接串缓存 / 在线登记与踢人），`src/Web` 有 8 个 vitest 用例；仍无 API 级集成测试与真实数据库测试，后续补 `WebApplicationFactory` + 测试库。
- **历史消息无会话记录**：I0 之前写入的测试消息没有对应的 `conversations` 行，按新权限模型不可读（数据仍保留在 `cwm_messages`）。
- **前端包体**：TDesign 已按需引入并按路由分包，最大 chunk 仍是 table 组件（约 351 KB / gzip 93 KB）；如需再降，可把消息页与文件页的表格换成分页列表或异步加载 table。
- **回读链接与签名密钥**：短时链接的 HMAC 子密钥派生自 `Jwt:SigningKey`，轮换签名密钥会使已签发但未过期的链接立即失效（默认最长 300 秒，影响可接受）。
- **Cookie 跨站部署**：默认 `SameSite=Strict; Path=/api/auth`，要求后台与 API 同站点（如都在 `localhost` 或同一域名）。若前后端分属不同站点，必须改 `Auth:Cookie:Secure=true` + `SameSite=None`，并把 `Cors:AllowedOrigins` 设成精确源（不能通配）。
- **连接串缓存位置**：默认落在运行目录下 `App_Data/connection-string-cache`（即 `bin/Debug/net10.0/`），清理 `bin` 会一并删除；部署时可用 `Security:ConnectionStringCacheDirectory` 指到持久目录。
- **旧 Mongo 索引**：`ix_cwm_messages_conversation_sent_at` 不再创建，旧库中仍可能存在，确认新索引生效后可手工 `dropIndex`。
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
5. 前端全局提示统一走 `src/Web/src/utils/notify.ts`（TDesign `NotifyPlugin`，`placement: 'top-right'` 固定右上角，成功 4 秒 / 错误 6 秒自动消失、可手动关闭）；页面内持久性错误仍用 `t-alert`，不要再自建内联提示状态。
5. 关键密钥/配置错误一律启动 fail-fast；运行期外部依赖失败可以降级，但必须在启动日志与 `/api/health/*` 里显式可见——不允许"静默不可用"。

---

## 12. 迭代计划与已定决策（2026-11-17 确认）

### 12.1 已定决策

| 议题 | 结论 |
| --- | --- |
| 实时通道 | SignalR（`/hubs/chat`），单实例先不做 Redis backplane，保留 `IHubContext` 抽象 |
| 单聊模型 | 单聊也建 `conversation`，消息模型统一 |
| 幂等 | 客户端生成 `clientMessageId`（UUID），Mongo 唯一索引兜底 |
| 已读 | 会话级"最后已读位置"（`last_read_message_id` / `last_read_at_utc`），不做逐条回执 |
| 强制下线 | 吊销刷新令牌 + 断开 SignalR 连接 + 在线管理页"直接踢出"；新增长期会话版本校验以堵住 access token 剩余有效期 |
| 配置管理 | 后端 + 管理后台新增"配置管理"页，按分类维护可配置项（群上限、单文件大小、消息长度、归档保留期等） |
| 管理员角色 | 直接在 `users` 加 `role`（已落地） |
| 自动化测试 | 持续补全，I0 已建骨架；后续每个迭代带测试 |
| 双写 | 成员校验走 PostgreSQL，消息落 MongoDB；失败返回 503，不做跨库事务 |
| 令牌存储 | access 只在内存，refresh 只放 HttpOnly/Secure/SameSite Cookie（2026-11-17 架构评审整改落地） |
| 文件回读 | 不再有匿名静态目录；Bearer 或短时签名 URL（`/api/files/{id}/link`） |
| 消息分页 | `(SentAtUtc, Id)` 复合游标，跨毫秒/同毫秒都稳定 |
| Mongo 集合 | 统一 `cwm_` 前缀（已落地 `cwm_messages`） |
| 群邀请 | 需被邀请人确认（`conversation_invitations`，I4） |
| 撤回 | 任意时间可撤回本人消息（无 2 分钟限制），管理员可撤回任意消息 |
| 消息保留 | 永久保留；超期记录先归档为 JSON 并入库，再删除 Mongo 对应文档 |
| 归档 | 后端目录下的 `chat-history/` 写 JSON（便于读取/恢复），新表记录归档批次；完成后删 Mongo |
| 客户端框架 | Avalonia 12（非 WPF：指定的 TKCode.Arc 只支持 Avalonia），TKCode.Arc 组件库 |
| 账号体系 | 管理后台账号（`admin_users`）与聊天用户（`users`）完全分离，两套凭据不能互相登录 |

### 12.2 迭代状态

| 迭代 | 范围 | 状态 |
| --- | --- | --- |
| I0 | 会话/成员模型、权限（403/404）、幂等、`cwm_` 前缀、`users.role`、测试骨架 | ✅ 已完成 |
| I1 | 好友与联系人（搜索、申请、备注、删除、拉黑） | ✅ 已完成 |
| I2 | SignalR 实时通道、在线状态、心跳重连、在线管理页（踢人 + 吊销刷新令牌） | ✅ 已完成（心跳/断线重连在 I3 补） |
| I3 | 已读位置、任意撤回、离线补齐、发送状态机 | 待开始 |
| I4 | 群聊治理（踢人/退群/转让/禁言/公告）+ 邀请确认 | 待开始 |
| I5 | 管理后台治理（封禁/解散/审计）+ 配置管理页 | 待开始 |
| I6 | 超期消息归档 JSON + 归档表 + 删除 Mongo 记录 | 待开始 |
| I7 | 桌面客户端（Avalonia + TKCode.Arc）：登录 / 会话 / 消息 / SignalR | 🚧 首版已跑通，实时订阅待接 |

### 12.3 待确认（影响 I2/I4/I6）

1. 踢人后 access token 剩余有效期如何处理——建议加"用户会话版本号"（每次鉴权比对），需要一次迁移与每请求一次轻量校验。
2. 归档触发时间（默认 180 天？）与归档目录（建议 `src/Server/ChatWithMe.Api/App_Data/chat-history/{yyyy}/{MM}/`）。
3. 配置管理页的写权限（仅 Admin）与是否支持热更新（不重启生效）。

### 2026-11-19 登录后通知加载失败排查

原因：`20261119005806_AddAnnouncements` 尚未执行，通知列表请求访问不存在的表。已执行该增量迁移；验证待执行迁移为 0、通知计数与列表查询均成功（当前 0 条）。无需重启即可重新刷新通知列表。此前 MongoDB 认证报错另行进行了只读复查：远端连接串的有效认证库为 `admin`，当前 `ping` 成功；这只能说明本次检查正常，不能据此断定此前失败的具体原因。
