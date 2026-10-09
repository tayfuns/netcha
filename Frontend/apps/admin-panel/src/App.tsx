import { useState, type FormEvent } from 'react';
import { adminApi, TOKEN_KEY } from './api';
import { Account, Audit, Dashboard, Health, SettingsPage } from './pages';

type Page = 'dashboard' | 'health' | 'audit' | 'settings' | 'account';
const pages: { id: Page; label: string; icon: string }[] = [
  { id: 'dashboard', label: 'Gösterge paneli', icon: '▦' },
  { id: 'health', label: 'Sistem sağlığı', icon: '♡' },
  { id: 'audit', label: 'Audit kayıtları', icon: '≡' },
  { id: 'settings', label: 'Ayarlar', icon: '⚙' },
  { id: 'account', label: 'Hesap güvenliği', icon: '●' },
];

function Login({ onLogin }: { onLogin: () => void }) {
  const [username, setUsername] = useState('admin');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  async function submit(event: FormEvent) {
    event.preventDefault(); setLoading(true); setError('');
    try { const result = await adminApi.login(username.trim(), password); sessionStorage.setItem(TOKEN_KEY, result.token); onLogin(); }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Giriş yapılamadı.'); }
    finally { setLoading(false); }
  }
  return <main className="login-shell"><section className="login card"><div className="logo">N</div><p className="eyebrow">NETCHA YÖNETİM</p><h1>Yönetim paneline giriş</h1><p className="hint">Oturumunuz yalnızca bu tarayıcı sekmesinde tutulur.</p>{error && <div className="status error" role="alert">{error}</div>}<form onSubmit={submit}><label>Kullanıcı adı<input className="input" autoComplete="username" value={username} onChange={(e) => setUsername(e.target.value)} required /></label><label>Parola<input className="input" type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} required /></label><button className="button" disabled={!username.trim() || !password || loading}>{loading ? 'Giriş yapılıyor…' : 'Giriş yap'}</button></form></section></main>;
}

export function App() {
  const [authenticated, setAuthenticated] = useState(() => Boolean(sessionStorage.getItem(TOKEN_KEY)));
  const [page, setPage] = useState<Page>('dashboard');
  const [menuOpen, setMenuOpen] = useState(false);
  if (!authenticated) return <Login onLogin={() => setAuthenticated(true)} />;
  const CurrentPage = { dashboard: Dashboard, health: Health, audit: Audit, settings: SettingsPage, account: Account }[page];
  async function logout() { try { await adminApi.logout(); } finally { sessionStorage.removeItem(TOKEN_KEY); setAuthenticated(false); } }
  return <div className="admin-shell"><a className="skip-link" href="#content">İçeriğe geç</a><aside className={menuOpen ? 'open' : ''}><div className="aside-head"><div className="logo">N</div><div><strong>NetCha</strong><small>Yönetim</small></div></div><nav aria-label="Yönetim menüsü">{pages.map((item) => <button key={item.id} className={page === item.id ? 'active' : ''} onClick={() => { setPage(item.id); setMenuOpen(false); }}><span aria-hidden="true">{item.icon}</span>{item.label}</button>)}</nav><button className="logout" onClick={logout}>Oturumu kapat</button></aside><div className="admin-main"><header><button className="menu-button" aria-label="Menüyü aç" aria-expanded={menuOpen} onClick={() => setMenuOpen(!menuOpen)}>☰</button><span>NetCha Yönetim</span><div className="admin-chip"><span>Y</span>Yönetici</div></header><main id="content"><CurrentPage /></main></div></div>;
}
