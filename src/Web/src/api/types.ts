export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface UserDto {
  id: string
  email: string
  nickName: string
  createdAtUtc: string
}

export interface FileAssetDto {
  id: string
  originalName: string
  storedName: string
  relativePath: string
  contentType: string
  sizeInBytes: number
  /** 鉴权回读端点（需要 Bearer）；浏览器直开请先换短时签名链接。 */
  url: string
  createdAtUtc: string
}

export interface FileLinkDto {
  url: string
  expiresAtUtc: string
}

export interface MessageDto {
  id: string
  conversationId: string
  senderId: string
  content: string
  sentAtUtc: string
}

export interface MessagePage {
  items: MessageDto[]
  nextBefore: string | null
}

/** 登录 / 刷新的响应：refresh token 只走 HttpOnly Cookie，不会出现在这里。 */
export interface AuthSessionDto {
  accessToken: string
  tokenType: string
  accessTokenExpiresAtUtc: string
  user: UserDto
}

/** 在线管理：管理员看到的实时在线用户。 */
export interface OnlineUserDto {
  id: string
  email: string
  nickName: string
  connections: number
}

/** 管理后台账号（后端用户），与聊天用户分离。 */
export interface AdminAccountDto {
  id: string
  email: string
  displayName: string
  isActive: boolean
  createdAtUtc: string
}
