import { FormEvent, useCallback, useState } from 'react';
import { ApiError } from '@netcha/shared';
import { Altcha } from './Altcha';
import { CHALLENGE_URL, verifyCaptcha } from './api';

type ViewState = 'idle' | 'verifying' | 'verified' | 'submitting' | 'success' | 'error' | 'expired';
type NoticeTone = 'success' | 'warning' | 'error' | '';

export function App() {
  const [state, setState] = useState<ViewState>('idle');
  const [payload, setPayload] = useState('');
  const [notice, setNotice] = useState('');
  const [noticeTone, setNoticeTone] = useState<NoticeTone>('');
  const [resetKey, setResetKey] = useState(0);

  const handleCaptcha = useCallback((next: string, nextPayload: string) => {
    if (next === 'verifying') setState('verifying');
    else if (next === 'verified') {
      setPayload(nextPayload);
      setState('verified');
      setNotice('Doğrulama tamamlandı. Formu gönderebilirsiniz.');
      setNoticeTone('success');
    } else if (next === 'expired') {
      setPayload('');
      setState('expired');
      setNotice('Doğrulamanın süresi doldu. Yeni bir doğrulama başlatın.');
      setNoticeTone('warning');
    } else if (next === 'error') {
      setPayload('');
      setState('error');
      setNotice('Doğrulama yüklenemedi. Sunucu bağlantısını kontrol edip tekrar deneyin.');
      setNoticeTone('error');
    } else {
      setPayload('');
      setState('idle');
    }
  }, []);

  function retry() {
    setPayload('');
    setNotice('');
    setNoticeTone('');
    setState('idle');
    setResetKey((value) => value + 1);
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    if (!payload) {
      setState('error');
      setNotice('Göndermeden önce insan olduğunuzu doğrulayın.');
      setNoticeTone('error');
      return;
    }
    setState('submitting');
    setNotice('');
    try {
      const result = await verifyCaptcha({ payload });
      if (!result.verified) throw new ApiError(result.error || 'Doğrulama reddedildi.', 400);
      formElement.reset();
      setPayload('');
      setState('success');
      setNotice('CAPTCHA doğrulandı; demo formu başarıyla tamamlandı.');
      setNoticeTone('success');
      setResetKey((value) => value + 1);
    } catch (error) {
      const apiError = error as ApiError;
      const expired = apiError.status === 408 || apiError.status === 410 || apiError.message.includes('expired');
      setPayload('');
      setState(expired ? 'expired' : 'error');
      setNotice(expired
        ? 'Doğrulamanın süresi doldu. Lütfen yeniden deneyin.'
        : apiError.message === 'challenge_replayed'
          ? 'Bu doğrulama daha önce kullanıldı. Lütfen yeni bir doğrulama başlatın.'
          : 'Doğrulama başarısız oldu. Lütfen yeniden deneyin.');
      setNoticeTone(expired ? 'warning' : 'error');
      setResetKey((value) => value + 1);
    }
  }

  const isBusy = state === 'submitting' || state === 'verifying';
  return (
    <>
      <a className="skip-link" href="#demo-form">Forma geç</a>
      <header className="demo-header">
        <a className="brand" href={import.meta.env.BASE_URL} aria-label="NetCha ana sayfa">
          <span className="brand-mark" aria-hidden="true">N</span>
          NetCha
        </a>
        <span className="privacy-pill">Takip yok · Çerez yok</span>
      </header>
      <main className="demo-layout">
        <section className="intro" aria-labelledby="page-title">
          <p className="eyebrow">Gizlilik dostu doğrulama</p>
          <h1 id="page-title">Botları durdurun, insanları bekletmeyin.</h1>
          <p className="lead">NetCha, tarayıcınızda küçük bir hesaplama yaparak gerçek kullanıcıları doğrular. Görsel seçmek veya kişisel veri paylaşmak gerekmez.</p>
          <ul className="benefits" aria-label="NetCha özellikleri">
            <li><span aria-hidden="true">✓</span><div><strong>Gizlilik öncelikli</strong><small>Kişisel davranış verisi toplamaz.</small></div></li>
            <li><span aria-hidden="true">✓</span><div><strong>Erişilebilir</strong><small>Görsel ya da sesli bulmaca gerektirmez.</small></div></li>
            <li><span aria-hidden="true">✓</span><div><strong>Hızlı ve açık</strong><small>NetCha proof-of-work protokolü ile çalışır.</small></div></li>
          </ul>
        </section>

        <section className="form-card card" aria-labelledby="form-title">
          <div>
            <p className="step">CANLI DEMO</p>
            <h2 id="form-title">Bize bir mesaj bırakın</h2>
            <p className="hint">Tüm alanlar zorunludur.</p>
          </div>
          <form id="demo-form" onSubmit={submit}>
            <label>Adınız<input className="input" name="name" autoComplete="name" required maxLength={80} /></label>
            <label>E-posta<input className="input" type="email" name="email" autoComplete="email" required maxLength={160} /></label>
            <label>Mesajınız<textarea name="message" required minLength={10} maxLength={1000} /></label>
            <div className="captcha-box" aria-busy={state === 'verifying'}>
              <Altcha challengeUrl={CHALLENGE_URL} resetKey={resetKey} onChange={handleCaptcha} />
            </div>
            {notice && (
              <div className={`status ${noticeTone}`} role="status" aria-live="polite">
                {notice}
              </div>
            )}
            {(state === 'error' || state === 'expired') && <button className="button secondary" type="button" onClick={retry}>Yeniden dene</button>}
            <button className="button submit" type="submit" disabled={isBusy || !payload}>
              {isBusy && <span className="spinner" aria-hidden="true" />}
              {state === 'submitting' ? 'Gönderiliyor…' : state === 'verifying' ? 'Doğrulanıyor…' : 'Mesajı gönder'}
            </button>
          </form>
        </section>
      </main>
      <footer>NetCha · Açık kaynak, gizlilik dostu CAPTCHA</footer>
    </>
  );
}
