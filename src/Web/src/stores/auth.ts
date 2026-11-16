import { reactive } from 'vue'
import { configureAuth, request } from '../api/http'
import type { AuthTokenDto, UserDto } from '../api/types'

const STORAGE_KEY = 'chatwithme.admin.session'

interface SessionState {
  accessToken: string | null
  refreshToken: string | null
  user: UserDto | null
}

export const session = reactive<SessionState>({
  accessToken: null,
  refreshToken: null,
  user: null,
})

function persist(): void {
  if (!session.accessToken || !session.refreshToken) {
    localStorage.removeItem(STORAGE_KEY)
    return
  }

  localStorage.setItem(
    STORAGE_KEY,
    JSON.stringify({
      accessToken: session.accessToken,
      refreshToken: session.refreshToken,
      user: session.user,
    }),
  )
}

export function restoreSession(): void {
  const raw = localStorage.getItem(STORAGE_KEY)

  if (!raw) {
    return
  }

  try {
    const parsed = JSON.parse(raw) as Partial<SessionState>
    session.accessToken = parsed.accessToken ?? null
    session.refreshToken = parsed.refreshToken ?? null
    session.user = parsed.user ?? null
  } catch {
    localStorage.removeItem(STORAGE_KEY)
  }
}

export function isAuthenticated(): boolean {
  return Boolean(session.accessToken)
}

export function clearSession(): void {
  session.accessToken = null
  session.refreshToken = null
  session.user = null
  persist()
}

export async function login(email: string, password: string): Promise<void> {
  const tokens = await request<AuthTokenDto>(
    '/api/auth/login',
    { method: 'POST', body: JSON.stringify({ email, password }) },
    { auth: false, retryOnUnauthorized: false },
  )

  session.accessToken = tokens.accessToken
  session.refreshToken = tokens.refreshToken
  session.user = tokens.user
  persist()
}

export async function logout(): Promise<void> {
  const refreshToken = session.refreshToken

  clearSession()

  if (!refreshToken) {
    return
  }

  try {
    await request<void>(
      '/api/auth/logout',
      { method: 'POST', body: JSON.stringify({ refreshToken }) },
      { auth: false, retryOnUnauthorized: false },
    )
  } catch {
    // 本地会话已清空，服务端吊销失败不影响退出。
  }
}

configureAuth({
  getAccessToken: () => session.accessToken,
  getRefreshToken: () => session.refreshToken,
  updateTokens: (accessToken, refreshToken) => {
    session.accessToken = accessToken
    session.refreshToken = refreshToken
    persist()
  },
  onAuthLost: () => {
    clearSession()

    if (window.location.hash !== '#/login') {
      window.location.hash = '#/login'
    }
  },
})
