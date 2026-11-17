import { afterEach, describe, expect, it, vi } from 'vitest'
import type { AuthSessionDto } from '../api/types'
import { bootstrapSession, clearSession, isAuthenticated, login, logout, session } from './auth'

const authSession: AuthSessionDto = {
  accessToken: 'access-token',
  tokenType: 'Bearer',
  accessTokenExpiresAtUtc: '2026/11/17 10:00:00',
  user: {
    id: '6f1c1e0e-0000-0000-0000-000000000000',
    email: 'admin@example.com',
    nickName: '管理员',
    createdAtUtc: '2026/11/17 09:00:00',
  },
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

describe('auth store', () => {
  afterEach(() => {
    clearSession()
    vi.unstubAllGlobals()
  })

  it('登录只把 access token 放在内存里，不写 localStorage', async () => {
    const fetchMock = vi.fn(async () => jsonResponse(authSession))
    vi.stubGlobal('fetch', fetchMock)

    await login('admin@example.com', 'secret')

    expect(session.accessToken).toBe('access-token')
    expect(session.user?.email).toBe('admin@example.com')
    expect(isAuthenticated()).toBe(true)
    // 旧实现会把 refresh token 写进这个键；现在不应再有任何持久化。
    expect((globalThis as { localStorage?: Storage }).localStorage?.getItem('chatwithme.admin.session')).toBeFalsy()
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('启动时用 Cookie 静默刷新恢复会话', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(authSession)))

    await bootstrapSession()

    expect(isAuthenticated()).toBe(true)
  })

  it('没有有效刷新 Cookie 时保持未登录', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response('', { status: 401 })))

    await bootstrapSession()

    expect(isAuthenticated()).toBe(false)
    expect(session.user).toBeNull()
  })

  it('登出无论服务端是否成功都会清空本地会话', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => jsonResponse(authSession)))
    await login('admin@example.com', 'secret')

    vi.stubGlobal('fetch', vi.fn(async () => new Response('', { status: 500 })))

    await logout()

    expect(isAuthenticated()).toBe(false)
    expect(session.user).toBeNull()
  })
})
