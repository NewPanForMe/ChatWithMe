# 变更日志

> 只追加、不回改历史条目；新条目写在最上面。

## 2026-11-19 · 全员通知与迁移修复

- 新增 `announcements` 表及迁移 `20261119005806_AddAnnouncements`；管理员发送/分页查询、聊天用户列表/详情接口。
- SignalR 新增 `notificationReceived` 事件，仅向聊天用户连接广播；Web 新增通知管理页，桌面端新增通知列表/详情与浮动提示。
- 登录后通知加载失败排查：迁移未执行导致访问不存在的表；执行迁移后恢复正常。
- 测试：后端 114 项、桌面端 10 项、Web 8 项通过。

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
