import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { jwtDecode } from 'jwt-decode'; 
import { toast } from 'react-toastify';

const MyTrips = () => {
  const [trips, setTrips] = useState([]);
  const [loading, setLoading] = useState(true);

  const backendUrl = 'http://localhost:5038/api';

  useEffect(() => {
    const token = localStorage.getItem('token');
    if (!token) {
      toast.error('User not logged in. Please login again.');
      setLoading(false);
      return;
    }

    let username = null;
    try {
      const decoded = jwtDecode(token);
      username = decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"];
    } catch (err) {
      toast.error('Invalid token. Please log in again.');
      setLoading(false);
      return;
    }

    fetchDriverTrips(username, token);
  }, []);

  const fetchDriverTrips = async (username, token) => {
    try {
      const params = new URLSearchParams({ driverName: username });
      const response = await axios.get(`${backendUrl}/Trip/filter?${params}`, {
        headers: {
          Authorization: `Bearer ${token}`
        }
      });

      setTrips(response.data);
      toast.success("Trips loaded successfully");
    } catch (error) {
      toast.error("Failed to load trips");
      console.error("Error fetching driver trips:", error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container mt-4">
      <h3 className="mb-4 text-center">My Trips</h3>
      {loading ? (
        <div className="text-center">Loading...</div>
      ) : trips.length === 0 ? (
        <div className="alert alert-info text-center">
          No trips found.
        </div>
      ) : (
        <div className="table-responsive">
          <table className="table table-striped table-bordered">
            <thead className="table-dark">
              <tr>
                <th>Trip ID</th>
                <th>From</th>
                <th>To</th>
                <th>Start Time</th>
                <th>End Time</th>
                <th>Vehicle</th>
              </tr>
            </thead>
            <tbody>
              {trips.map((trip) => (
                <tr key={trip.tripId}>
                  <td>{trip.tripId}</td>
                  <td>{trip.originCityName} - {trip.originAreaName}</td>
                  <td>{trip.destinationCityName} - {trip.destinationAreaName}</td>
                  <td>{new Date(trip.tripStartTime).toLocaleString()}</td>
                  <td>{new Date(trip.tripEndTime).toLocaleString()}</td>
                  <td>{trip.vehicleNumber}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

export default MyTrips;
