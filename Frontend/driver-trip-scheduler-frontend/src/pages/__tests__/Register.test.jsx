import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { vi } from 'vitest';
import Register from '../Register';
import { api } from '../../api';


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
  },
}));

describe('Register component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    vi.useFakeTimers(); // fake timers because component uses setTimeout
  });

  afterEach(() => {
    vi.useRealTimers(); // always restore after
  });

  const setup = () =>
    render(
      <BrowserRouter>
        <Register />
      </BrowserRouter>
    );

  it('renders all form fields and button', () => {
    setup();
    expect(screen.getByLabelText(/username/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/role/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /register/i })).toBeInTheDocument();
  });

  it('allows user to type in username, password, and select role', () => {
    setup();

    fireEvent.change(screen.getByLabelText(/username/i), {
      target: { value: 'testuser' },
    });
    fireEvent.change(screen.getByLabelText(/password/i), {
      target: { value: 'testpass' },
    });
    fireEvent.change(screen.getByLabelText(/role/i), {
      target: { value: 'Manager' },
    });

    expect(screen.getByLabelText(/username/i).value).toBe('testuser');
    expect(screen.getByLabelText(/password/i).value).toBe('testpass');
    expect(screen.getByLabelText(/role/i).value).toBe('Manager');
  });

  // it('submits form and navigates to login on success', async () => {
  //   api.post.mockResolvedValueOnce({}); // simulate API success
  //   setup();

  //   // Fill form
  //   fireEvent.change(screen.getByLabelText(/username/i), {
  //     target: { value: 'testuser' },
  //   });
  //   fireEvent.change(screen.getByLabelText(/password/i), {
  //     target: { value: 'testpass' },
  //   });
  //   fireEvent.change(screen.getByLabelText(/role/i), {
  //     target: { value: 'Manager' },
  //   });

  //   fireEvent.click(screen.getByRole('button', { name: /register/i }));

  //   // Wait for toast success (you can skip this if not asserting toast)
  //   await waitFor(() =>
  //     expect(api.post).toHaveBeenCalledWith('/Auth/register', {
  //       username: 'testuser',
  //       password: 'testpass',
  //       role: 'Manager',
  //     })
  //   );

  //   // Simulate 2000ms delay from setTimeout
  //   vi.advanceTimersByTime(2000);

  //   expect(mockNavigate).toHaveBeenCalledWith('/login');
  // });
});
