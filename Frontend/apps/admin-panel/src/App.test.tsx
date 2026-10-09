import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { App } from './App';

describe('admin panel', () => {
  beforeEach(() => sessionStorage.clear());
  it('keeps access token in session storage and opens the dashboard', async () => {
    const user = userEvent.setup();
    render(<App />);
    await user.type(screen.getByLabelText(/yönetici anahtarı/i), 'demo-token');
    await user.click(screen.getByRole('button', { name: /devam et/i }));
    expect(sessionStorage.getItem('netcha.admin.token')).toBe('demo-token');
    expect(screen.getByRole('button', { name: /^gösterge paneli$/i })).toHaveClass('active');
  });
});
