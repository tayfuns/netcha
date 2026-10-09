import { ReactNode } from 'react';

export function Loading({ label = 'Yükleniyor…' }: { label?: string }) {
  return <div className="loading" role="status"><span className="spinner" aria-hidden="true" />{label}</div>;
}

export function Empty({ children }: { children: ReactNode }) {
  return <div className="empty"><span aria-hidden="true">◇</span><p>{children}</p></div>;
}

export function ErrorState({ message, onRetry }: { message: string; onRetry: () => void }) {
  return <div className="status error" role="alert"><p>{message}</p><button className="button secondary" onClick={onRetry}>Tekrar dene</button></div>;
}

export function StatusDot({ healthy }: { healthy: boolean }) {
  return <span className={`status-dot ${healthy ? 'healthy' : 'unhealthy'}`} aria-hidden="true" />;
}
