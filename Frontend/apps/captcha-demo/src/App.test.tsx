import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi } from 'vitest';
import { App } from './App';

const apiMocks = vi.hoisted(() => ({ verifyCaptcha: vi.fn() }));

vi.mock('./api', () => ({
  CHALLENGE_URL: '/challenge',
  verifyCaptcha: apiMocks.verifyCaptcha,
}));

vi.mock('./Altcha', () => ({
  Altcha: ({ onChange }: { onChange: (state: 'verified', payload: string) => void }) => (
    <button type="button" onClick={() => onChange('verified', 'test-payload')}>CAPTCHA testini doğrula</button>
  ),
}));

describe('captcha demo', () => {
  it('renders an accessible form and disables submission until verification', () => {
    render(<App />);
    expect(screen.getByRole('heading', { name: /botları durdurun/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/e-posta/i)).toBeRequired();
    expect(screen.getByRole('button', { name: /mesajı gönder/i })).toBeDisabled();
  });

  it('keeps the completed submission notice in the success style', async () => {
    apiMocks.verifyCaptcha.mockResolvedValueOnce({ verified: true, error: null });
    const user = userEvent.setup();
    render(<App />);

    await user.type(screen.getByLabelText('Adınız'), 'Test Kullanıcısı');
    await user.type(screen.getByLabelText('E-posta'), 'test@example.invalid');
    await user.type(screen.getByLabelText('Mesajınız'), 'Bu bir başarı bildirimi test mesajıdır.');
    await user.click(screen.getByRole('button', { name: 'CAPTCHA testini doğrula' }));
    await user.click(screen.getByRole('button', { name: 'Mesajı gönder' }));

    expect(await screen.findByText(/demo formu başarıyla tamamlandı/i)).toHaveClass('success');
    expect(screen.getByRole('status')).not.toHaveClass('error');
  });
});
