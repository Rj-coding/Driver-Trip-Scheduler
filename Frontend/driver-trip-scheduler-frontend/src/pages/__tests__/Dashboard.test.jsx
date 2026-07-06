import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, vi, beforeEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import Dashboard from '../Dashboard';

const mockNavigate = vi.fn();

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

describe('Dashboard Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('redirects to /login if token or role is missing', () => {
    render(<Dashboard />, { wrapper: MemoryRouter });
    expect(mockNavigate).toHaveBeenCalledWith('/login');
  });

  it('renders Driver dashboard with My Trips card', () => {
    localStorage.setItem('token', 'fake-token');
    localStorage.setItem('role', 'Driver');

    render(<Dashboard />, { wrapper: MemoryRouter });

    expect(screen.getByText(/Welcome to the Driver Dashboard/i)).toBeInTheDocument();
    expect(screen.getByText(/My Trips/i)).toBeInTheDocument();
  });

  it('renders Manager dashboard with all manager cards', () => {
    localStorage.setItem('token', 'fake-token');
    localStorage.setItem('role', 'Manager');

    render(<Dashboard />, { wrapper: MemoryRouter });

    expect(screen.getByText(/Welcome to the Manager Dashboard/i)).toBeInTheDocument();
    expect(screen.getByText(/Manage Drivers/i)).toBeInTheDocument();
    expect(screen.getByText(/Manage Vehicles/i)).toBeInTheDocument();
    expect(screen.getByText(/Assign Trips/i)).toBeInTheDocument();
    expect(screen.getByText(/View All Trips/i)).toBeInTheDocument();
  });

  it('clears localStorage and navigates to /login on logout', () => {
    localStorage.setItem('token', 'fake-token');
    localStorage.setItem('role', 'Manager');

    render(<Dashboard />, { wrapper: MemoryRouter });

    const logoutBtn = screen.getByRole('button', { name: /logout/i });
    fireEvent.click(logoutBtn);

    expect(localStorage.getItem('token')).toBeNull();
    expect(localStorage.getItem('role')).toBeNull();
    expect(mockNavigate).toHaveBeenCalledWith('/login');
  });

  it('navigates to /vehicles when "Manage Vehicles" card is clicked', () => {
    localStorage.setItem('token', 'fake-token');
    localStorage.setItem('role', 'Manager');

    render(<Dashboard />, { wrapper: MemoryRouter });

    const manageVehiclesCard = screen.getByText(/Manage Vehicles/i).closest('.dashboard-card');
    fireEvent.click(manageVehiclesCard);

    expect(mockNavigate).toHaveBeenCalledWith('/vehicles');
  });

  it('navigates to /trips when "Assign Trips" card is clicked', () => {
    localStorage.setItem('token', 'fake-token');
    localStorage.setItem('role', 'Manager');

    render(<Dashboard />, { wrapper: MemoryRouter });

    const assignTripsCard = screen.getByText(/Assign Trips/i).closest('.dashboard-card');
    fireEvent.click(assignTripsCard);

    expect(mockNavigate).toHaveBeenCalledWith('/trips');
  });

  it('navigates to /viewtrips when "View All Trips" card is clicked', () => {
    localStorage.setItem('token', 'fake-token');
    localStorage.setItem('role', 'Manager');

    render(<Dashboard />, { wrapper: MemoryRouter });

    const viewAllTripsCard = screen.getByText(/View All Trips/i).closest('.dashboard-card');
    fireEvent.click(viewAllTripsCard);

    expect(mockNavigate).toHaveBeenCalledWith('/viewtrips');
  });

  it('navigates to /drivers when "Manage Drivers" card is clicked', () => {
    localStorage.setItem('token', 'fake-token');
    localStorage.setItem('role', 'Manager');

    render(<Dashboard />, { wrapper: MemoryRouter });

    const manageDriversCard = screen.getByText(/Manage Drivers/i).closest('.dashboard-card');
    fireEvent.click(manageDriversCard);

    expect(mockNavigate).toHaveBeenCalledWith('/drivers');
  });
});
