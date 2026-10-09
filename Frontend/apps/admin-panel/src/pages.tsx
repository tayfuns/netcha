import { useEffect, useState } from 'react';
import { formatDate } from '@netcha/shared';
import { adminApi, AuditEntry } from './api';
import { Empty, ErrorState, Loading, StatusDot } from './components';

type AsyncState<T> = { data?: T; loading: boolean; error?: string };

function useLoad<T>(loader: () => Promise<T>) {
  const [state, setState] = useState<AsyncState<T>>({ loading: true });
  const load = () => {
    setState((current) => ({ ...current, loading: true, error: undefined }));
    loader().then((data) => setState({ data, loading: false })).catch((error: Error) => setState({ loading: false, error: error.message }));
  };
  useEffect(load, []);
  return { ...state, reload: load };
}

function Metric({ label, value, detail }: { label: string; value: string; detail?: string }) {
  return <article className="metric card"><p>{label}</p><strong>{value}</strong>{detail && <small>{detail}</small>}</article>;
}

export function Dashboard() {
  const stats = useLoad(adminApi.stats);
  const health = useLoad(adminApi.health);
  if (stats.loading || health.loading) return <Loading label="Gösterge paneli hazırlanıyor…" />;
  if (stats.error || health.error) return <ErrorState message={stats.error || health.error || 'Veriler alınamadı.'} onRetry={() => { stats.reload(); health.reload(); }} />;
  const data = stats.data!;
  const healthy = health.data?.status?.toLowerCase() === 'healthy';
  const successRate = data.total ? (data.succeeded / data.total) * 100 : 0;
  return <>
    <div className="page-heading"><div><p className="eyebrow">GENEL BAKIŞ</p><h1>Gösterge paneli</h1><p>NetCha doğrulama trafiğinin anlık görünümü.</p></div><div className="health-badge"><StatusDot healthy={healthy} />Sistem {healthy ? 'sağlıklı' : 'kontrol gerekli'}</div></div>
    <section className="metric-grid" aria-label="Doğrulama istatistikleri">
      <Metric label="Toplam doğrulama" value={data.total.toLocaleString('tr-TR')} />
      <Metric label="Başarılı doğrulama" value={data.succeeded.toLocaleString('tr-TR')} detail={`%${successRate.toLocaleString('tr-TR', { maximumFractionDigits: 1 })} başarı`} />
      <Metric label="Başarısız" value={data.failed.toLocaleString('tr-TR')} />
      <Metric label="Son deneme" value={data.lastAttemptAt ? formatDate(data.lastAttemptAt) : '—'} />
    </section>
    <section className="card overview-card"><div><h2>Doğrulama performansı</h2><p className="hint">Başarılı isteklerin toplam doğrulamalara oranı</p></div><div className="progress-wrap"><strong>%{Math.round(successRate)}</strong><div className="progress"><span style={{ width: `${Math.min(100, successRate)}%` }} /></div></div></section>
  </>;
}

export function Health() {
  const { data, loading, error, reload } = useLoad(adminApi.health);
  if (loading) return <Loading />;
  if (error) return <ErrorState message={error} onRetry={reload} />;
  const items = [['API', data?.status], ['PostgreSQL', data?.database], ['Redis', data?.cache]];
  return <><div className="page-heading"><div><p className="eyebrow">ALTYAPI</p><h1>Sistem sağlığı</h1><p>Bağımlılıkların erişilebilirlik durumu.</p></div><button className="button secondary" onClick={reload}>Yenile</button></div><section className="service-list card">{items.map(([name, value]) => { const good = ['healthy', 'ok', 'available', 'up'].includes(value?.toLowerCase() || ''); return <div className="service" key={name}><div><StatusDot healthy={good} /><strong>{name}</strong></div><span className={good ? 'good' : 'bad'}>{value || 'Bilinmiyor'}</span></div>; })}<p className="hint">Son kontrol: {formatDate(data?.timestamp)}</p></section></>;
}

export function Audit() {
  const { data, loading, error, reload } = useLoad(adminApi.audit);
  const [query, setQuery] = useState('');
  if (loading) return <Loading />;
  if (error) return <ErrorState message={error} onRetry={reload} />;
  const rows = (data || []).filter((row) => `${row.succeeded ? 'başarılı' : 'başarısız'} ${row.error} ${row.clientIp} ${row.userAgent}`.toLocaleLowerCase('tr').includes(query.toLocaleLowerCase('tr')));
  return <><div className="page-heading"><div><p className="eyebrow">GÜVENLİK</p><h1>Audit kayıtları</h1><p>Son 100 doğrulama olayı.</p></div><button className="button secondary" onClick={reload}>Yenile</button></div><label className="search">Kayıtlarda ara<input className="input" type="search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Sonuç, hata, IP veya tarayıcı" /></label>{rows.length === 0 ? <Empty>{query ? 'Aramanızla eşleşen kayıt yok.' : 'Henüz audit kaydı yok.'}</Empty> : <div className="table-wrap card"><table><thead><tr><th>Zaman</th><th>Sonuç</th><th>IP</th><th>Hata</th><th>Tarayıcı</th></tr></thead><tbody>{rows.map((row: AuditEntry) => <tr key={row.id}><td>{formatDate(row.occurredAt)}</td><td><span className={`result ${row.succeeded ? 'ok' : 'fail'}`}>{row.succeeded ? 'Başarılı' : 'Başarısız'}</span></td><td><code>{row.clientIp || '—'}</code></td><td>{row.error || '—'}</td><td title={row.userAgent || ''}>{row.userAgent || '—'}</td></tr>)}</tbody></table></div>}</>;
}

export function SettingsPage() {
  const { data, loading, error, reload } = useLoad(adminApi.settings);
  if (loading) return <Loading />;
  if (error || !data) return <ErrorState message={error || 'Ayarlar alınamadı.'} onRetry={reload} />;
  return <><div className="page-heading"><div><p className="eyebrow">YAPILANDIRMA</p><h1>Ayarlar</h1><p>Backend tarafından kullanılan challenge güvenlik ayarları.</p></div><button className="button secondary" onClick={reload}>Yenile</button></div><section className="settings-form card"><div className="settings-grid"><div><p className="hint">Maksimum sayı</p><strong className="setting-value">{data.maxNumber.toLocaleString('tr-TR')}</strong><p className="hint">Proof-of-work hesaplama üst sınırı.</p></div><div><p className="hint">Challenge süresi</p><strong className="setting-value">{data.expiresInSeconds} saniye</strong><p className="hint">Üretilen challenge'ın geçerli kalacağı süre.</p></div></div><div className="status warning">Ayarlar sunucu yapılandırmasından okunur. Değişiklikler backend ortam değişkenleri üzerinden uygulanır.</div></section></>;
}
