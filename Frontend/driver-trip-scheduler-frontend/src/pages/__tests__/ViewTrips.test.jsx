

import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { act } from 'react-dom/test-utils'
import ViewTrips from '../ViewTrips'
import axios from 'axios'

//  Fix mocking inside vi.mock to avoid hoisting error
vi.mock('react-toastify', () => {
    return {
        toast: {
            success: vi.fn(),
            error: vi.fn(),
            warning: vi.fn(),
        },
    }
})

// Mock axios
vi.mock('axios')
const mockedAxios = axios

const mockTrips = [
    {
        tripId: 1,
        originCityName: 'City A',
        originAreaName: 'Area A',
        destinationCityName: 'City B',
        destinationAreaName: 'Area B',
        driverName: 'John Doe',
        vehicleNumber: 'UP32AB1234',
        tripStartTime: '2025-08-01T10:00:00',
        tripEndTime: '2025-08-01T11:00:00'
    }
]

describe('ViewTrips page', () => {
    beforeEach(() => {
        vi.clearAllMocks()

        vi.stubGlobal('localStorage', {
            getItem: vi.fn(() => 'fake-token'),
        })

        mockedAxios.get.mockImplementation((url) => {
            if (url.includes('/City')) return Promise.resolve({ data: [] })
            if (url.includes('/Driver')) return Promise.resolve({ data: [] })
            if (url.includes('/Vehicle')) return Promise.resolve({ data: [] })
            return Promise.resolve({ data: [] })
        })
    })

    afterEach(() => {
        vi.unstubAllGlobals()
    })

    it('shows "No trips available." if no trips are returned', async () => {
        global.fetch = vi.fn(() =>
            Promise.resolve({
                ok: true,
                json: () => Promise.resolve([]),
            })
        )

        render(<ViewTrips />)

        await waitFor(() =>
            expect(screen.getByText('No trips available.')).toBeInTheDocument()
        )
    })

    it('renders trip table when trips are available', async () => {
        global.fetch = vi.fn(() =>
            Promise.resolve({
                ok: true,
                json: () => Promise.resolve(mockTrips),
            })
        )

        render(<ViewTrips />)

        await waitFor(() => {
            expect(screen.getByText('City A - Area A')).toBeInTheDocument()
            expect(screen.getByText('City B - Area B')).toBeInTheDocument()
            expect(screen.getByText('John Doe')).toBeInTheDocument()
            expect(screen.getByText('UP32AB1234')).toBeInTheDocument()
        })
    })

    it('shows warning toast if Apply is clicked with no filter values', async () => {
        global.fetch = vi.fn(() =>
            Promise.resolve({
                ok: true,
                json: () => Promise.resolve(mockTrips),
            })
        )

        render(<ViewTrips />)

        const applyBtn = screen.getByRole('button', { name: /apply/i })
        fireEvent.click(applyBtn)

        const { toast } = await import('react-toastify')

        await waitFor(() => {
            expect(toast.warning).toHaveBeenCalledWith('Please provide at least one filter.')
        })
    })

    it('calls fetch with filter and shows filtered trips', async () => {
        const filterDriverName = 'John Doe'
        const filteredTrips = [mockTrips[0]]

        const fetchSpy = vi.fn(() =>
            Promise.resolve({
                ok: true,
                json: () => Promise.resolve(filteredTrips),
            })
        )
        global.fetch = fetchSpy

        render(<ViewTrips />)

        const driverInput = screen.getByPlaceholderText(/filter by driver name/i)
        fireEvent.change(driverInput, { target: { value: filterDriverName } })

        const applyBtn = screen.getByRole('button', { name: /apply/i })

        await act(async () => {
            fireEvent.click(applyBtn)
        })

        await waitFor(() => {
            // Looser match on URL
            expect(fetchSpy).toHaveBeenCalledWith(
                expect.stringContaining('/Trip/filter'),
                expect.anything()
            )

            expect(screen.getByText('John Doe')).toBeInTheDocument()
        })
    })

    it('resets filters and shows all trips again', async () => {
        const allTrips = mockTrips

        const fetchSpy = vi.fn(() =>
            Promise.resolve({
                ok: true,
                json: () => Promise.resolve(allTrips),
            })
        )
        global.fetch = fetchSpy

        render(<ViewTrips />)

        const driverInput = screen.getByPlaceholderText(/filter by driver name/i)
        fireEvent.change(driverInput, { target: { value: 'John Doe' } })

        
        const resetBtn = screen.getByRole('button', { name: /reset/i })

        await act(async () => {
            fireEvent.click(resetBtn)
        })

        
        await waitFor(() => {
            
            expect(driverInput.value).toBe('')
            
            expect(screen.getByText('John Doe')).toBeInTheDocument()
        })
    })
    it('deletes a trip after confirmation and shows success toast', async () => {
       
        vi.spyOn(window, 'confirm').mockReturnValueOnce(true)

        const { toast } = await import('react-toastify')

        
        mockedAxios.delete.mockResolvedValueOnce({
            data: 'Trip deleted successfully.'
        })

        // Initial fetch returns one trip
        global.fetch = vi.fn()
            .mockResolvedValueOnce({
                ok: true,
                json: () => Promise.resolve(mockTrips),
            })
            // After deletion, fetch again with empty list
            .mockResolvedValueOnce({
                ok: true,
                json: () => Promise.resolve([]),
            })

        render(<ViewTrips />)

        await waitFor(() => {
            expect(screen.getByText('John Doe')).toBeInTheDocument()
        })

        const deleteBtn = screen.getByRole('button', { name: /delete/i })
        fireEvent.click(deleteBtn)

        await waitFor(() => {
            expect(toast.success).toHaveBeenCalledWith('Trip deleted successfully.')
            expect(screen.queryByText('John Doe')).not.toBeInTheDocument()
        })
    })


})




