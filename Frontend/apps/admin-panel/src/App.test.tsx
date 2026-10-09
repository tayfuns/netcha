import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi } from 'vitest';
import { App } from './App';

describe('admin panel', () => {
  beforeEach(() => {
    sessionStorage.clear();
    vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith('/auth/login')) return new Response(JSON.stringify({ token: 'session-token', expiresAt: new Date().toISOString(), user: { id: 1, username: 'admin', createdAt: new Date().toISOString(), lastLoginAt: null } }), { status: 200, headers: { 'Content-Type': 'application/json' } });
      if (url.endsWith('/health')) return new Response('Healthy', { status: 200 });
      if (url.endsWith('/stats')) return new Response(JSON.stringify({ total: 0, succeeded: 0, failed: 0, lastAttemptAt: null }), { status: 200, headers: { 'Content-Type': 'application/json' } });
      return new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } });
    }));
  });
  it('keeps the database session token in session storage and opens the dashboard', async () => {
    const user = userEvent.setup();
    render(<App />);
    await user.type(screen.getByLabelText(/parola/i), 'initial-admin-password');
    await user.click(screen.getByRole('button', { name: /giriş yap/i }));
    expect(sessionStorage.getItem('netcha.admin.token')).toBe('session-token');
    expect(screen.getByRole('button', { name: /^gösterge paneli$/i })).toHaveClass('active');
  });
});
