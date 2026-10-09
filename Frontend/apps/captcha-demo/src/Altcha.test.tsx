import { render, screen } from '@testing-library/react';
import { Altcha } from './Altcha';

describe('NetCha challenge', () => {
  it('uses NetCha branding and hides upstream widget attribution', () => {
    const { container } = render(
      <Altcha challengeUrl="/challenge" resetKey={0} onChange={() => undefined} />,
    );

    expect(screen.getByLabelText('NetCha ile korunuyor')).toBeInTheDocument();
    const widget = container.querySelector('altcha-widget') as HTMLElement & {
      hidefooter: boolean;
      hidelogo: boolean;
      name: string;
    };
    expect(widget.hidefooter).toBe(true);
    expect(widget.hidelogo).toBe(true);
    expect(widget.name).toBe('netcha');
  });
});
