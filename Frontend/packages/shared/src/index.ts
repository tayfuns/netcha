export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export function createApiClient(
  baseUrl: string,
  getToken?: () => string | null,
  auth: { header: string; prefix: string } = { header: 'Authorization', prefix: 'Bearer ' },
) {
  const root = baseUrl.replace(/\/$/, '');

  async function request<T>(path: string, init?: RequestInit): Promise<T> {
    const headers = new Headers(init?.headers);
    const token = getToken?.();
    if (init?.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json');
    if (token) headers.set(auth.header, `${auth.prefix}${token}`);

    let response: Response;
    try {
      response = await fetch(`${root}${path}`, { ...init, headers });
    } catch {
      throw new ApiError('Sunucuya ulaşılamadı. Bağlantınızı kontrol edip tekrar deneyin.', 0);
    }

    if (!response.ok) {
      const body = await response.json().catch(() => null) as { message?: string; detail?: string; error?: string } | null;
      throw new ApiError(body?.message ?? body?.detail ?? body?.error ?? 'İstek tamamlanamadı.', response.status);
    }
    if (response.status === 204) return undefined as T;
    return response.json() as Promise<T>;
  }

  return { request };
}

export function formatDate(value: string | null | undefined): string {
  if (!value) return '—';
  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}
