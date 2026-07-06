import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { format } from 'date-fns';
import { toast } from 'react-toastify';

function ViewTrips() {
  const backendUrl = 'http://localhost:5038/api';

  const [trips, setTrips] = useState([]);
  const [driverName, setDriverName] = useState('');
  const [vehicleNumber, setVehicleNumber] = useState('');

  // For editing
  const [showModal, setShowModal] = useState(false);
  const [editingTrip, setEditingTrip] = useState(null);

  const [cities, setCities] = useState([]);
  const [drivers, setDrivers] = useState([]);
  const [vehicles, setVehicles] = useState([]);
  const [originAreas, setOriginAreas] = useState([]);
  const [destinationAreas, setDestinationAreas] = useState([]);

  //use this when toast messages is not working
  useEffect(() => {
    toast.error("Test toast on load");
  }, []);

  useEffect(() => {
    fetchTrips();
    fetchReferenceData();
  }, []);

  const fetchReferenceData = async () => {
    try {
      const [citiesRes, driversRes, vehiclesRes] = await Promise.all([
        axios.get(`${backendUrl}/City`),
        axios.get(`${backendUrl}/Driver`),
        axios.get(`${backendUrl}/Vehicle`)
      ]);
      setCities(citiesRes.data);
      setDrivers(driversRes.data);
      setVehicles(vehiclesRes.data);
    } catch (err) {
      toast.error('Failed to fetch reference data');
    }
  };

  const fetchTrips = async (filter = false) => {
    try {
      const token = localStorage.getItem('token');
      let url = `${backendUrl}/Trip`;

      if (filter && (driverName || vehicleNumber)) {
        const params = new URLSearchParams();
        if (driverName) params.append('driverName', driverName);
        if (vehicleNumber) params.append('vehicleNumber', vehicleNumber);
        url = `${backendUrl}/Trip/filter?${params.toString()}`;
      }

      const response = await fetch(url, {
        headers: { Authorization: `Bearer ${token}` },
      });

      if (!response.ok) throw new Error('Failed to fetch trips');
      const data = await response.json();
      setTrips(data);
    } catch (error) {
      console.error(error);
      toast.error('Error fetching trips');
    }
  };

  const handleFilterSubmit = (e) => {
    e.preventDefault();
    if (!driverName && !vehicleNumber) {
      toast.warning("Please provide at least one filter.");
      return;
    }
    fetchTrips(true);
  };

  const handleReset = () => {
    setDriverName('');
    setVehicleNumber('');
    fetchTrips();
  };

  const handleEditClick = async (trip) => {
    try {
      const [originAreaRes, destinationAreaRes] = await Promise.all([
        axios.get(`${backendUrl}/City/${trip.originCityId}/areas`),
        axios.get(`${backendUrl}/City/${trip.destinationCityId}/areas`)
      ]);

      setOriginAreas(originAreaRes.data);
      setDestinationAreas(destinationAreaRes.data);

      setEditingTrip({
        ...trip,
        originCityId: trip.originCityId.toString(),
        originAreaId: trip.originAreaId.toString(),
        destinationCityId: trip.destinationCityId.toString(),
        destinationAreaId: trip.destinationAreaId.toString(),
        driverId: trip.driverId.toString(),
        vehicleId: trip.vehicleId.toString(),
        tripStartTime: new Date(trip.tripStartTime).toISOString().slice(0, 16),
        tripEndTime: new Date(trip.tripEndTime).toISOString().slice(0, 16),
      });

      setShowModal(true);
    } catch (err) {
      toast.error('Failed to load area data');
    }
  };

  const handleModalChange = (e) => {
    const { name, value } = e.target;
    setEditingTrip(prev => ({
      ...prev,
      [name]: value
    }));
  };
  const getTripStatus = (startTime, endTime) => {
    const now = new Date();
    const start = new Date(startTime);
    const end = new Date(endTime);

    if (now < start) return 'Not Started';
    if (now >= start && now <= end) return 'Running';
    return 'Completed';
  };


  const handleTripUpdate = async () => {
    try {
      const token = localStorage.getItem('token');

      await axios.put(`${backendUrl}/Trip/${editingTrip.tripId}`, {
        tripId: editingTrip.tripId,
        originCityId: parseInt(editingTrip.originCityId),
        originAreaId: parseInt(editingTrip.originAreaId),
        destinationCityId: parseInt(editingTrip.destinationCityId),
        destinationAreaId: parseInt(editingTrip.destinationAreaId),
        driverId: parseInt(editingTrip.driverId),
        vehicleId: parseInt(editingTrip.vehicleId),
        tripStartTime: editingTrip.tripStartTime,
        tripEndTime: editingTrip.tripEndTime
      }, {
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      toast.success('Trip updated successfully');
      setShowModal(false);
      fetchTrips();

    } catch (err) {
      console.error("Trip update error:", err); // Keep for debugging

      let errorMessage = 'Trip update failed.';

      if (err.response) {
        const { data } = err.response;

        if (typeof data === 'string') {
          errorMessage = data;
        } else if (typeof data === 'object' && data.message) {
          errorMessage = data.message;
        }
      } else if (err.message) {
        errorMessage = err.message;
      }

      toast.error(errorMessage);
    }

  };
  const handleDeleteTrip = async (tripId) => {
    const confirmDelete = window.confirm("Are you sure you want to delete this trip?");
    if (!confirmDelete) return;

    try {
      const token = localStorage.getItem('token');
      const response = await axios.delete(`${backendUrl}/Trip/${tripId}`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      toast.success(response.data || 'Trip deleted successfully.');
      fetchTrips(); // Refresh the list
    } catch (err) {
      console.error("Delete error:", err);

      let errorMessage = 'Trip deletion failed.';
      if (err.response && typeof err.response.data === 'string') {
        errorMessage = err.response.data;
      }

      toast.error(errorMessage);
    }
  };




  //  Dynamically load areas when city changes in modal
  useEffect(() => {
    if (editingTrip?.originCityId) {
      axios.get(`${backendUrl}/City/${editingTrip.originCityId}/areas`)
        .then(res => setOriginAreas(res.data))
        .catch(err => console.error('Error loading origin areas:', err));
    }
  }, [editingTrip?.originCityId]);

  useEffect(() => {
    if (editingTrip?.destinationCityId) {
      axios.get(`${backendUrl}/City/${editingTrip.destinationCityId}/areas`)
        .then(res => setDestinationAreas(res.data))
        .catch(err => console.error('Error loading destination areas:', err));
    }
  }, [editingTrip?.destinationCityId]);

  const formatDateTime = (dateTime) => {
    try {
      return format(new Date(dateTime), 'dd/MM/yyyy hh:mm a');
    } catch {
      return 'Invalid Date';
    }
  };

  return (
    <div className="container mt-5">
      <h2>All Trips</h2>

      <form className="row g-3 mb-4" onSubmit={handleFilterSubmit}>
        <div className="col-md-5">
          <input
            type="text"
            className="form-control"
            placeholder="Filter by Driver Name"
            value={driverName}
            onChange={(e) => setDriverName(e.target.value)}
          />
        </div>
        <div className="col-md-5">
          <input
            type="text"
            className="form-control"
            placeholder="Filter by Vehicle Number"
            value={vehicleNumber}
            onChange={(e) => setVehicleNumber(e.target.value)}
          />
        </div>
        <div className="col-md-2 d-flex">
          <button type="submit" className="btn btn-primary me-2 w-100">Apply</button>
          <button type="button" className="btn btn-secondary w-100" onClick={handleReset}>Reset</button>
        </div>
      </form>

      {trips.length === 0 ? (
        <p>No trips available.</p>
      ) : (
        <div className="table-responsive">
          <table className="table table-bordered">
            <thead className="table-dark">
              <tr>
                <th>Trip ID</th>
                <th>Origin</th>
                <th>Destination</th>
                <th>Driver</th>
                <th>Vehicle</th>
                <th>Start Time</th>
                <th>End Time</th>
                <th>Status</th>
                <th>Actions</th>


              </tr>
            </thead>
            <tbody>
              {trips.map((trip) => (
                <tr key={trip.tripId}>
                  <td>{trip.tripId}</td>
                  <td>{trip.originCityName} - {trip.originAreaName}</td>
                  <td>{trip.destinationCityName} - {trip.destinationAreaName}</td>
                  <td>{trip.driverName}</td>
                  <td>{trip.vehicleNumber}</td>
                  <td>{formatDateTime(trip.tripStartTime)}</td>
                  <td>{formatDateTime(trip.tripEndTime)}</td>
                  <td>
                    <span className={`badge bg-${getTripStatus(trip.tripStartTime, trip.tripEndTime) === 'Running' ? 'warning'
                        : getTripStatus(trip.tripStartTime, trip.tripEndTime) === 'Completed' ? 'success'
                          : 'secondary'
                      }`}>
                      {getTripStatus(trip.tripStartTime, trip.tripEndTime)}
                    </span>
                  </td>

                  <td>
                    <button className="btn btn-sm btn-warning me-2" onClick={() => handleEditClick(trip)}>
                      Edit
                    </button>
                    <button data-testid={`delete-${trip.tripId}`} className="btn btn-sm btn-danger" onClick={() => handleDeleteTrip(trip.tripId)}>
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Edit Modal */}
      {showModal && editingTrip && (
        <div className="modal show d-block" tabIndex="-1">
          <div className="modal-dialog modal-lg">
            <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title">Edit Trip</h5>
                <button type="button" className="btn-close" onClick={() => setShowModal(false)}></button>
              </div>
              <div className="modal-body row g-3">
                {/* Same structure as AssignTrip */}
                <div className="col-md-6">
                  <label>Origin City</label>
                  <select className="form-control" name="originCityId" value={editingTrip.originCityId} onChange={handleModalChange}>
                    <option value="">Select City</option>
                    {cities.map(c => <option key={c.cityId} value={c.cityId}>{c.name}</option>)}
                  </select>
                </div>
                <div className="col-md-6">
                  <label>Origin Area</label>
                  <select className="form-control" name="originAreaId" value={editingTrip.originAreaId} onChange={handleModalChange}>
                    <option value="">Select Area</option>
                    {originAreas.map(a => <option key={a.areaId} value={a.areaId}>{a.name}</option>)}
                  </select>
                </div>

                <div className="col-md-6">
                  <label>Destination City</label>
                  <select className="form-control" name="destinationCityId" value={editingTrip.destinationCityId} onChange={handleModalChange}>
                    <option value="">Select City</option>
                    {cities.map(c => <option key={c.cityId} value={c.cityId}>{c.name}</option>)}
                  </select>
                </div>
                <div className="col-md-6">
                  <label>Destination Area</label>
                  <select className="form-control" name="destinationAreaId" value={editingTrip.destinationAreaId} onChange={handleModalChange}>
                    <option value="">Select Area</option>
                    {destinationAreas.map(a => <option key={a.areaId} value={a.areaId}>{a.name}</option>)}
                  </select>
                </div>

                <div className="col-md-6">
                  <label>Driver</label>
                  <select className="form-control" name="driverId" value={editingTrip.driverId} onChange={handleModalChange}>
                    <option value="">Select Driver</option>
                    {drivers.map(driver => (
                      <option key={driver.driverId} value={driver.driverId}>
                        {driver.name}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="col-md-6">
                  <label>Vehicle</label>
                  <select className="form-control" name="vehicleId" value={editingTrip.vehicleId} onChange={handleModalChange}>
                    <option value="">Select Vehicle</option>
                    {vehicles.map(vehicle => (
                      <option key={vehicle.vehicleId} value={vehicle.vehicleId}>
                        {vehicle.vehicleNumber}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="col-md-6">
                  <label htmlFor="tripStartTime">Start Time</label>
                  <input type="datetime-local" className="form-control" name="tripStartTime" value={editingTrip.tripStartTime} onChange={handleModalChange} />
                </div>
                <div className="col-md-6">
                  <label htmlFor="tripEndTime">End Time</label>
                  <input type="datetime-local" className="form-control" name="tripEndTime" value={editingTrip.tripEndTime} onChange={handleModalChange} />
                </div>
              </div>
              <div className="modal-footer">
                <button className="btn btn-secondary" onClick={() => setShowModal(false)}>Cancel</button>
                <button className="btn btn-success" onClick={handleTripUpdate}>Update Trip</button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default ViewTrips;
