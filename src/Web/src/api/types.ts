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
  url: string
  createdAtUtc: string
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

export interface AuthTokenDto {
  accessToken: string
  refreshToken: string
  tokenType: string
  accessTokenExpiresAtUtc: string
  refreshTokenExpiresAtUtc: string
  user: UserDto
}
