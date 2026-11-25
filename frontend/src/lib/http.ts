import type { ApiDocument } from './openapi'

export class ApiError extends Error {
  status: number
  constructor(message: string, status: number) {
    super(message)
    this.status = status
  }
}

async function interpret(res: Response): Promise<unknown> {
  if (res.status === 204) return null
  const text = await res.text()
  if (!text) return null
  try { return JSON.parse(text) } catch { return text }
}

function errorText(data: unknown, status: number): string {
  if (data && typeof data === 'object') {
    const obj = data as Record<string, unknown>
    const errors = obj.errors
    if (errors && typeof errors === 'object') {
      const msgs = Object.values(errors).flatMap(x => Array.isArray(x) ? x : [x]).map(String)
      if (msgs.length) return msgs.slice(0, 5).join(' · ')
    }
    for (const key of ['message', 'detail', 'title', 'error']) {
      if (typeof obj[key] === 'string' && obj[key]) return obj[key] as string
    }
  }
  if (status === 401) return 'Credenciales incorrectas o sesión caducada.'
  if (status === 403) return 'Tu cuenta no tiene permiso para realizar esta operación.'
  if (status === 404) return 'El recurso solicitado no existe.'
  return `La solicitud devolvió el código ${status}.`
}

export async function apiRequest(
  path: string,
  opts: { method?: string; token?: string | null; body?: unknown; signal?: AbortSignal } = {},
): Promise<unknown> {
  let response: Response
  try {
    response = await fetch(path, {
      method: opts.method || 'GET',
      headers: {
        Accept: 'application/json',
        ...(opts.body === undefined ? {} : { 'Content-Type': 'application/json' }),
        ...(opts.token ? { Authorization: `Bearer ${opts.token}` } : {}),
      },
      ...(opts.body === undefined ? {} : { body: JSON.stringify(opts.body) }),
      signal: opts.signal,
      cache: 'no-store',
    })
  } catch (err) {
    if (err instanceof Error && err.name === 'AbortError') throw err
    throw new ApiError('No se puede conectar con el backend. Comprueba que .NET esté ejecutándose en localhost:5265.', 0)
  }
  const data = await interpret(response)
  if (!response.ok) throw new ApiError(errorText(data, response.status), response.status)
  return data
}

export async function getSwagger(signal?: AbortSignal): Promise<ApiDocument> {
  const data = await apiRequest('/swagger/v1/swagger.json', { signal })
  if (!data || typeof data !== 'object' || !('paths' in data)) {
    throw new Error('Swagger no devolvió una definición válida de la API.')
  }
  return data as ApiDocument
}

export function getRows(data: unknown): Record<string, unknown>[] {
  if (Array.isArray(data)) return data.map(row => typeof row === 'object' && row !== null && !Array.isArray(row) ? row : { valor: row })
  if (!data || typeof data !== 'object') return []
  const obj = data as Record<string, unknown>
  for (const key of ['items', 'data', 'results', 'records', 'content', 'value', '$values']) {
    const value = obj[key]
    if (Array.isArray(value)) return getRows(value)
    if (value && typeof value === 'object') return getRows(value)
  }
  return [obj]
}

export function shortValue(value: unknown): string {
  if (value === null || value === undefined || value === '') return '—'
  if (typeof value === 'boolean') return value ? 'Sí' : 'No'
  if (Array.isArray(value)) return `${value.length} elementos`
  if (typeof value === 'object') return 'Ver detalle'
  const text = String(value)
  return text.length > 85 ? `${text.slice(0, 82)}…` : text
}
