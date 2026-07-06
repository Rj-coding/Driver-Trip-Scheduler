import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { within } from '@testing-library/react';
import AssignTrip from '../AssignTrip';
import { MemoryRouter } from 'react-router-dom';
import { vi } from 'vitest';
import axios from 'axios';

// Mock axios
vi.mock('axios');

// Mock toast
vi.mock('react-toastify', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

describe('AssignTrip Page Tests', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    //  Mock localStorage token
    vi.spyOn(window.localStorage.__proto__, 'getItem').mockReturnValue('test-token');

    // Mock URL query for test mode
    Object.defineProperty(window, 'location', {
      value: {
        search: '?test=true',
      },
      writable: true,
    });
  });

  test('renders heading and submit button', async () => {
    axios.get
      .mockResolvedValueOnce({ data: [] }) // cities
      .mockResolvedValueOnce({ data: [] }) // drivers
      .mockResolvedValueOnce({ data: [] }); // vehicles

    render(
      <MemoryRouter>
        <AssignTrip />
      </MemoryRouter>
    );

    expect(screen.getByText('Assign New Trip')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Assign Trip/i })).toBeInTheDocument();
  });

  test('loads city, driver, and vehicle dropdowns', async () => {
    axios.get
      .mockResolvedValueOnce({ data: [{ cityId: 1, name: 'Lucknow' }] }) // cities
      .mockResolvedValueOnce({ data: [{ driverId: 1, name: 'John', phoneNumber: '1234567890' }] }) // drivers
      .mockResolvedValueOnce({ data: [{ vehicleId: 1, type: 'Truck', vehicleNumber: 'UP32AB1234' }] }); // vehicles

    render(
      <MemoryRouter>
        <AssignTrip />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getAllByText('Lucknow').length).toBeGreaterThanOrEqual(1);
      expect(screen.getByText(/John - 1234567890/i)).toBeInTheDocument();
      expect(screen.getByText(/Truck - UP32AB1234/i)).toBeInTheDocument();
    });
  });

  test('submits form with valid data', async () => {
    // Mock axios.get in the order used in component
    axios.get
      .mockResolvedValueOnce({ data: [{ cityId: 1, name: 'Lucknow' }] }) // cities
      .mockResolvedValueOnce({ data: [{ driverId: 1, name: 'John', phoneNumber: '1234567890' }] }) // drivers
      .mockResolvedValueOnce({ data: [{ vehicleId: 1, type: 'Truck', vehicleNumber: 'UP32AB1234' }] }) // vehicles
      .mockResolvedValueOnce({ data: [{ areaId: 10, name: 'Alambagh' }] }) // origin areas
      .mockResolvedValueOnce({ data: [{ areaId: 20, name: 'Hazratganj' }] }); // destination areas

    axios.post = vi.fn().mockResolvedValue({ data: {} });

    render(
      <MemoryRouter initialEntries={['/assigntrip?test=true']}>
        <AssignTrip />
      </MemoryRouter>
    );

    const originCitySelect = await screen.findByLabelText(/Origin City/i);
    fireEvent.change(originCitySelect, { target: { value: '1' } });

    const originAreaSelect = await screen.findByLabelText(/Origin Area/i);
    fireEvent.change(originAreaSelect, { target: { value: '10' } });

    const destCitySelect = await screen.findByLabelText(/Destination City/i);
    fireEvent.change(destCitySelect, { target: { value: '1' } });

    const destAreaSelect = await screen.findByLabelText(/Destination Area/i);
    fireEvent.change(destAreaSelect, { target: { value: '20' } });

    const driverSelect = await screen.findByLabelText(/Driver/i);
    fireEvent.change(driverSelect, { target: { value: '1' } });

    const vehicleSelect = await screen.findByLabelText(/Vehicle/i);
    fireEvent.change(vehicleSelect, { target: { value: '1' } });

    fireEvent.change(screen.getByLabelText(/Start Time/i), {
      target: { value: '2025-08-01T10:00' },
    });

    fireEvent.change(screen.getByLabelText(/End Time/i), {
      target: { value: '2025-08-01T12:00' },
    });

    fireEvent.click(screen.getByRole('button', { name: /Assign Trip/i }));

    await waitFor(() => {
      expect(axios.post).toHaveBeenCalledTimes(1);
      expect(axios.post).toHaveBeenCalledWith(
        expect.stringContaining('/Trip'),
        expect.objectContaining({
          originCityId: 1,
          originAreaId: 10,
          destinationCityId: 1,
          destinationAreaId: 20,
          driverId: 1,
          vehicleId: 1,
          tripStartTime: '2025-08-01T10:00',
          tripEndTime: '2025-08-01T12:00',
        }),
        expect.objectContaining({
          headers: expect.objectContaining({
            Authorization: 'Bearer test-token',
          }),
        })
      );
    });
  });
});
