import { reactive } from 'vue'
import { configureAuth, request } from '../api/http'
import type { AuthSessionDto, UserDto } from '../api/types'

/**
 * 会话状态只存在内存里：access token 不落 localStorage，刷新令牌在 HttpOnly Cookie 中，
 * 前端 JS 读不到。刷新页面后由 bootstrapSession() 用 Cookie 静默换回新的 access token。
 */
interface SessionState {
  accessToken: string | null
  user: UserDto | null
}

export const session = reactive<SessionState>({
  accessToken: null,
  user: null,
})

function applySession(data: AuthSessionDto): void {
  session.accessToken = data.accessToken
  session.user = data.user
}

export function isAuthenticated(): boolean {
  return Boolean(session.accessToken)
}

export function clearSession(): void {
  session.accessToken = null
  session.user = null
}

function refreshSession(): Promise<AuthSessionDto> {
  return request<AuthSessionDto>(
    '/api/auth/refresh',
    { method: 'POST' },
    { auth: false, retryOnUnauthorized: false },
  )
}

/** 启动时用 HttpOnly 刷新令牌 Cookie 静默恢复会话；没有有效 Cookie 就保持未登录。 */
export async function bootstrapSession(): Promise<void> {
  try {
    applySession(await refreshSession())
  } catch {
    clearSession()
  }
}

export async function login(email: string, password: string): Promise<void> {
  const data = await request<AuthSessionDto>(
    '/api/admin/auth/login',
    { method: 'POST', body: JSON.stringify({ email, password }) },
    { auth: false, retryOnUnauthorized: false },
  )

  applySession(data)
}

export async function logout(): Promise<void> {
  try {
    await request<void>(
      '/api/auth/logout',
      { method: 'POST' },
      { auth: false, retryOnUnauthorized: false },
    )
  } catch {
    // 本地会话已清空，服务端吊销失败不影响退出。
  } finally {
    clearSession()
  }
}

configureAuth({
  getAccessToken: () => session.accessToken,
  refreshSession: async () => {
    const data = await refreshSession()
    applySession(data)

    return data.accessToken
  },
  onAuthLost: () => {
    clearSession()

    if (window.location.hash !== '#/login') {
      window.location.hash = '#/login'
    }
  },
})
