import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import Login from '../Login';
import { vi } from 'vitest';
import { BrowserRouter } from 'react-router-dom';
import { api } from '../../api';


// Mock useNavigate from react-router-dom
const mockNavigate = vi.fn();

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});


vi.mock('../../api', () => ({
  api: {
    post: vi.fn(),
    defaults: {
      headers: { common: {} },
    },
  },
}));

describe('Login component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    vi.useFakeTimers(); 
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  const setup = () =>
    render(
      <BrowserRouter>
        <Login />
      </BrowserRouter>
    );

  it('renders username and password input fields', () => {
    setup();
    expect(screen.getByLabelText(/username/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
  });

  it('allows user to type in username and password fields', () => {
    setup();
    const usernameInput = screen.getByLabelText(/username/i);
    const passwordInput = screen.getByLabelText(/password/i);

    fireEvent.change(usernameInput, { target: { value: 'demoUser' } });
    fireEvent.change(passwordInput, { target: { value: 'password123' } });

    expect(usernameInput.value).toBe('demoUser');
    expect(passwordInput.value).toBe('password123');
  });

  
  //   });

  //   setup();

  //   fireEvent.change(screen.getByLabelText(/username/i), {
  //     target: { value: 'demoUser' },
  //   });
  //   fireEvent.change(screen.getByLabelText(/password/i), {
  //     target: { value: 'password123' },
  //   });

  //   fireEvent.click(screen.getByRole('button', { name: /login/i }));

  //   // Fast-forward the delay BEFORE assertions
  //   vi.advanceTimersByTime(2000);
  //   await vi.runOnlyPendingTimersAsync(); // flush any remaining timers

  //   await waitFor(() => {
  //     expect(api.post).toHaveBeenCalledWith('/Auth/login', {
  //       username: 'demoUser',
  //       password: 'password123',
  //     });

  //     expect(localStorage.getItem('token')).toBe('fakeToken');
  //     expect(localStorage.getItem('role')).toBe('Manager');
  //     expect(mockNavigate).toHaveBeenCalledWith('/dashboard');
  //   });
  // });
});
