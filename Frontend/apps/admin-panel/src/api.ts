import { ApiError, createApiClient } from '@netcha/shared';

export const TOKEN_KEY = 'netcha.admin.token';
const API_URL = (import.meta.env.VITE_API_URL || 'http://localhost:5080').replace(/\/$/, '');
const client = createApiClient(
  API_URL,
  () => sessionStorage.getItem(TOKEN_KEY),
  { header: 'Authorization', prefix: 'Bearer ' },
);

const paths = {
  health: import.meta.env.VITE_HEALTH_PATH || '/health',
  stats: import.meta.env.VITE_STATS_PATH || '/api/v1/admin/stats',
  audit: import.meta.env.VITE_AUDIT_PATH || '/api/v1/admin/audit',
  settings: import.meta.env.VITE_SETTINGS_PATH || '/api/v1/admin/settings',
};

export interface HealthStatus {
  status: string;
  database?: string;
  cache?: string;
  timestamp?: string;
}

export interface Stats {
  total: number;
  succeeded: number;
  failed: number;
  lastAttemptAt: string | null;
}

export interface AuditEntry {
  id: number;
  occurredAt: string;
  succeeded: boolean;
  error: string | null;
  clientIp: string | null;
  userAgent: string | null;
}

export interface Settings {
  maxNumber: number;
  expiresInSeconds: number;
}

export interface AdminUser {
  id: number;
  username: string;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: AdminUser;
}

export const adminApi = {
  login: async (username: string, password: string): Promise<LoginResponse> => {
    const response = await fetch(`${API_URL}/api/v1/admin/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password }),
    });
    if (!response.ok) throw new ApiError(response.status === 401 ? 'Kullanıcı adı veya parola hatalı.' : 'Giriş yapılamadı.', response.status);
    return response.json() as Promise<LoginResponse>;
  },
  logout: () => client.request<void>('/api/v1/admin/auth/logout', { method: 'POST' }),
  me: () => client.request<AdminUser>('/api/v1/admin/auth/me'),
  changePassword: (currentPassword: string, newPassword: string) => client.request<void>('/api/v1/admin/auth/change-password', {
    method: 'POST',
    body: JSON.stringify({ currentPassword, newPassword }),
  }),
  health: async (): Promise<HealthStatus> => {
    let response: Response;
    try { response = await fetch(`${API_URL}${paths.health}`); }
    catch { throw new ApiError('Sağlık servisine ulaşılamadı.', 0); }
    if (!response.ok) throw new ApiError('Sağlık bilgisi alınamadı.', response.status);
    return { status: await response.text(), timestamp: new Date().toISOString() };
  },
  stats: () => client.request<Stats>(paths.stats),
  audit: () => client.request<AuditEntry[]>(`${paths.audit}?limit=100`),
  settings: () => client.request<Settings>(paths.settings),
};
