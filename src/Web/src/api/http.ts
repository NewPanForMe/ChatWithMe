const API_BASE = ((import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:5217').replace(/\/+$/, '')

export const apiBaseUrl = API_BASE

export class ApiError extends Error {
  readonly status: number

  readonly detail: string | null

  constructor(status: number, message: string, detail: string | null = null) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.detail = detail
  }
}

interface AuthHandlers {
  getAccessToken: () => string | null
  /** 用 HttpOnly Cookie 换新的 access token；失败抛错或返回 null。 */
  refreshSession: () => Promise<string | null>
  onAuthLost: () => void
}

let handlers: AuthHandlers = {
  getAccessToken: () => null,
  refreshSession: async () => null,
  onAuthLost: () => undefined,
}

/** 由会话模块注入 token 读写与失效回调，避免 http 层直接依赖 store。 */
export function configureAuth(next: AuthHandlers): void {
  handlers = next
}

let refreshInFlight: Promise<string | null> | null = null

/**
 * 单飞刷新：并发 401 只会触发一次 refresh，其余请求复用同一个 Promise。
 * 刷新令牌在 HttpOnly Cookie 里，前端只更新内存中的 access token。
 */
function refreshAccessToken(): Promise<string | null> {
  if (!refreshInFlight) {
    refreshInFlight = (async () => {
      try {
        return await handlers.refreshSession()
      } catch {
        return null
      } finally {
        refreshInFlight = null
      }
    })()
  }

  return refreshInFlight
}

function buildHeaders(init: RequestInit, withAuth: boolean): Headers {
  const headers = new Headers(init.headers)

  if (init.body !== undefined && !(init.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json')
  }

  if (withAuth) {
    const token = handlers.getAccessToken()

    if (token) {
      headers.set('Authorization', `Bearer ${token}`)
    }
  }

  return headers
}

async function toApiError(response: Response): Promise<ApiError> {
  const fallback = `请求失败（HTTP ${response.status}）`
  let title = fallback
  let detail: string | null = null

  const text = await response.text().catch(() => '')

  if (text) {
    try {
      const body = JSON.parse(text) as { title?: string; detail?: string; message?: string }
      title = body.title ?? fallback
      detail = body.detail ?? body.message ?? null
    } catch {
      detail = text.slice(0, 300)
    }
  }

  return new ApiError(response.status, title, detail)
}

export interface RequestOptions {
  auth?: boolean
  retryOnUnauthorized?: boolean
}

export async function request<T>(path: string, init: RequestInit = {}, options: RequestOptions = {}): Promise<T> {
  const withAuth = options.auth ?? true
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    // 刷新令牌走 HttpOnly Cookie，必须带凭据；access token 只在内存里加 Authorization。
    credentials: 'include',
    headers: buildHeaders(init, withAuth),
  })

  if (response.status === 401 && withAuth && (options.retryOnUnauthorized ?? true)) {
    const accessToken = await refreshAccessToken()

    if (accessToken) {
      return request<T>(path, init, { ...options, retryOnUnauthorized: false })
    }

    handlers.onAuthLost()
  }

  if (!response.ok) {
    throw await toApiError(response)
  }

  if (response.status === 204) {
    return undefined as T
  }

  const text = await response.text()

  return (text ? JSON.parse(text) : undefined) as T
}

export function describeError(error: unknown): string {
  if (error instanceof ApiError) {
    return error.detail ? `${error.message}：${error.detail}` : error.message
  }

  return error instanceof Error ? error.message : String(error)
}
