import { useState } from 'react';
import { TOKEN_KEY } from './api';
import { Audit, Dashboard, Health, SettingsPage } from './pages';

type Page = 'dashboard' | 'health' | 'audit' | 'settings';
const pages: { id: Page; label: string; icon: string }[] = [
  { id: 'dashboard', label: 'Gösterge paneli', icon: '▦' },
  { id: 'health', label: 'Sistem sağlığı', icon: '♡' },
  { id: 'audit', label: 'Audit kayıtları', icon: '≡' },
  { id: 'settings', label: 'Ayarlar', icon: '⚙' },
];

function Login({ onLogin }: { onLogin: () => void }) {
  const [token, setToken] = useState('');
  return <main className="login-shell"><section className="login card"><div className="logo">N</div><p className="eyebrow">NETCHA YÖNETİM</p><h1>Yönetim paneline giriş</h1><p className="hint">Yönetici anahtarınız yalnızca bu tarayıcı sekmesinde tutulur ve API'ye X-Admin-Key başlığıyla gönderilir.</p><form onSubmit={(e) => { e.preventDefault(); sessionStorage.setItem(TOKEN_KEY, token.trim()); onLogin(); }}><label>Yönetici anahtarı<input className="input" type="password" autoComplete="current-password" value={token} onChange={(e) => setToken(e.target.value)} required /></label><button className="button" disabled={!token.trim()}>Devam et</button></form></section></main>;
}

export function App() {
  const [authenticated, setAuthenticated] = useState(() => Boolean(sessionStorage.getItem(TOKEN_KEY)));
  const [page, setPage] = useState<Page>('dashboard');
  const [menuOpen, setMenuOpen] = useState(false);
  if (!authenticated) return <Login onLogin={() => setAuthenticated(true)} />;
  const CurrentPage = { dashboard: Dashboard, health: Health, audit: Audit, settings: SettingsPage }[page];
  function logout() { sessionStorage.removeItem(TOKEN_KEY); setAuthenticated(false); }
  return <div className="admin-shell"><a className="skip-link" href="#content">İçeriğe geç</a><aside className={menuOpen ? 'open' : ''}><div className="aside-head"><div className="logo">N</div><div><strong>NetCha</strong><small>Yönetim</small></div></div><nav aria-label="Yönetim menüsü">{pages.map((item) => <button key={item.id} className={page === item.id ? 'active' : ''} onClick={() => { setPage(item.id); setMenuOpen(false); }}><span aria-hidden="true">{item.icon}</span>{item.label}</button>)}</nav><button className="logout" onClick={logout}>Oturumu kapat</button></aside><div className="admin-main"><header><button className="menu-button" aria-label="Menüyü aç" aria-expanded={menuOpen} onClick={() => setMenuOpen(!menuOpen)}>☰</button><span>NetCha Yönetim</span><div className="admin-chip"><span>Y</span>Yönetici</div></header><main id="content"><CurrentPage /></main></div></div>;
}
