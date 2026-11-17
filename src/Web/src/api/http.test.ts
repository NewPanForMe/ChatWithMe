import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, configureAuth, describeError, request } from './http'

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

interface FetchCall {
  url: string
  authorization: string | null
  credentials: RequestCredentials | undefined
}

function stubFetch(handler: (call: FetchCall, index: number) => Response | Promise<Response>): FetchCall[] {
  const calls: FetchCall[] = []

  vi.stubGlobal('fetch', async (input: RequestInfo | URL, init?: RequestInit) => {
    const headers = new Headers(init?.headers)
    const call: FetchCall = {
      url: String(input),
      authorization: headers.get('Authorization'),
      credentials: init?.credentials,
    }

    calls.push(call)

    return handler(call, calls.length - 1)
  })

  return calls
}

describe('request', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('带上 Bearer 与凭据，并发 401 只刷新一次并重试原请求', async () => {
    let accessToken: string | null = 'stale'
    const refreshSession = vi.fn(async () => {
      accessToken = 'fresh'

      return 'fresh'
    })

    configureAuth({
      getAccessToken: () => accessToken,
      refreshSession,
      onAuthLost: () => {
        throw new Error('不应走到登出分支')
      },
    })

    const calls = stubFetch((_call, index) => (index < 2 ? new Response('', { status: 401 }) : jsonResponse({ ok: true })))

    const [first, second] = await Promise.all([
      request<{ ok: boolean }>('/api/users'),
      request<{ ok: boolean }>('/api/users'),
    ])

    expect(first).toEqual({ ok: true })
    expect(second).toEqual({ ok: true })
    expect(refreshSession).toHaveBeenCalledTimes(1)
    expect(calls).toHaveLength(4)
    expect(calls[0].credentials).toBe('include')
    expect(calls[0].authorization).toBe('Bearer stale')
    expect(calls[2].authorization).toBe('Bearer fresh')
    expect(calls[3].authorization).toBe('Bearer fresh')
  })

  it('刷新失败时回调登出并抛出 401 的 ApiError', async () => {
    const onAuthLost = vi.fn()

    configureAuth({
      getAccessToken: () => 'stale',
      refreshSession: async () => {
        throw new Error('refresh rejected')
      },
      onAuthLost,
    })

    stubFetch(() => new Response(JSON.stringify({ title: '身份验证失败' }), {
      status: 401,
      headers: { 'Content-Type': 'application/json' },
    }))

    await expect(request('/api/users')).rejects.toMatchObject({ status: 401 })
    expect(onAuthLost).toHaveBeenCalledTimes(1)
  })

  it('204 返回 undefined，错误响应映射为 ApiError 的 title/detail', async () => {
    configureAuth({
      getAccessToken: () => 'token',
      refreshSession: async () => 'token',
      onAuthLost: () => undefined,
    })

    stubFetch((call) =>
      call.url.endsWith('/api/auth/logout')
        ? new Response(null, { status: 204 })
        : new Response(JSON.stringify({ title: '翻页游标不合法', detail: 'before 格式错误' }), {
            status: 400,
            headers: { 'Content-Type': 'application/json' },
          }),
    )

    await expect(request<void>('/api/auth/logout')).resolves.toBeUndefined()

    const error = await request('/api/messages').catch((reason: unknown) => reason)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).status).toBe(400)
    expect((error as ApiError).message).toBe('翻页游标不合法')
    expect(describeError(error)).toBe('翻页游标不合法：before 格式错误')
  })

  it('auth: false 的请求不带 Authorization，也不触发刷新', async () => {
    const refreshSession = vi.fn(async () => 'token')

    configureAuth({
      getAccessToken: () => 'token',
      refreshSession,
      onAuthLost: () => undefined,
    })

    const calls = stubFetch(() => jsonResponse({ ok: true }))

    await request('/api/users', {}, { auth: false })

    expect(calls[0].authorization).toBeNull()
    expect(refreshSession).not.toHaveBeenCalled()
  })
})
