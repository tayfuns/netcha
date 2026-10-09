import { useEffect, useRef } from 'react';
import 'altcha';

type AltchaState = 'unverified' | 'verifying' | 'verified' | 'error' | 'expired';

interface AltchaChangeDetail {
  state: AltchaState;
  payload?: string;
}

export function Altcha({ challengeUrl, resetKey, onChange }: {
  challengeUrl: string;
  resetKey: number;
  onChange: (state: AltchaState, payload: string) => void;
}) {
  const ref = useRef<HTMLElement>(null);

  useEffect(() => {
    const node = ref.current;
    if (!node) return;
    const handler = (event: Event) => {
      const detail = (event as CustomEvent<AltchaChangeDetail>).detail;
      onChange(detail.state, detail.payload ?? '');
    };
    node.addEventListener('statechange', handler);
    return () => node.removeEventListener('statechange', handler);
  }, [onChange]);

  useEffect(() => {
    const node = ref.current as (HTMLElement & { reset?: () => void }) | null;
    if (resetKey > 0) node?.reset?.();
  }, [resetKey]);

  return (
    <div className="netcha-challenge">
      <altcha-widget
        ref={ref}
        challengeurl={challengeUrl}
        name="netcha"
        language="tr"
        hidefooter
        hidelogo
        aria-label="İnsan olduğunuzu doğrulayın"
      />
      <div className="netcha-challenge-brand" aria-label="NetCha ile korunuyor">
        <span aria-hidden="true">N</span>
        NetCha ile korunuyor
      </div>
    </div>
  );
}
