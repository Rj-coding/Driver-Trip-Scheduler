import { useEffect, useState } from 'react';
import axios from 'axios';
import { toast } from 'react-toastify';
import { useNavigate } from 'react-router-dom'; 
function AssignTrip() {
    const [cities, setCities] = useState([]);
    const [drivers, setDrivers] = useState([]);
    const [vehicles, setVehicles] = useState([]);

    const [originCityId, setOriginCityId] = useState('');
    const [originAreaId, setOriginAreaId] = useState('');
    const [originAreas, setOriginAreas] = useState([]);

    const [destinationCityId, setDestinationCityId] = useState('');
    const [destinationAreaId, setDestinationAreaId] = useState('');
    const [destinationAreas, setDestinationAreas] = useState([]);

    const [driverId, setDriverId] = useState('');
    const [vehicleId, setVehicleId] = useState('');
    const [startTime, setStartTime] = useState('');
    const [endTime, setEndTime] = useState('');
    const isTest = typeof window !== "undefined" && localStorage.getItem("testEnv") === "true";

    const [isSubmitting, setIsSubmitting] = useState(false);

    const backendUrl = 'http://localhost:5038/api';
     const navigate = useNavigate();

    useEffect(() => {
        const fetchData = async () => {
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
                toast.error('Error fetching data from server.');
                console.error(err);
            }
        };

        fetchData();
    }, []);

    useEffect(() => {
        if (originCityId) {
            axios.get(`${backendUrl}/City/${originCityId}/areas`)
                .then(res => setOriginAreas(res.data))
                .catch(() => toast.error('Error loading origin areas.'));
        } else {
            setOriginAreas([]);
            setOriginAreaId('');
        }
    }, [originCityId]);

    useEffect(() => {
        if (destinationCityId) {
            axios.get(`${backendUrl}/City/${destinationCityId}/areas`)
                .then(res => setDestinationAreas(res.data))
                .catch(() => toast.error('Error loading destination areas.'));
        } else {
            setDestinationAreas([]);
            setDestinationAreaId('');
        }
    }, [destinationCityId]);

   const handleSubmit = async (e) => {
    e.preventDefault();

    const token = localStorage.getItem('token');
    if (!token) {
        toast.error("Unauthorized. Please log in.");
        return;
    }

    // Validation: Start time should be before End time
    if (new Date(startTime) >= new Date(endTime)) {
        toast.error("Start time must be earlier than End time.");
        return;
    }

    const trip = {
        originCityId: parseInt(originCityId),
        originAreaId: parseInt(originAreaId),
        destinationCityId: parseInt(destinationCityId),
        destinationAreaId: parseInt(destinationAreaId),
        driverId: parseInt(driverId),
        vehicleId: parseInt(vehicleId),
        tripStartTime: startTime,
        tripEndTime: endTime
    };

    setIsSubmitting(true);

    try {
        await axios.post(`${backendUrl}/Trip`, trip, {
            headers: {
                Authorization: `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        toast.success('Trip Assigned Successfully!');
         navigate('/viewtrips');

        // Optional: Reset form
        setOriginCityId('');
        setOriginAreaId('');
        setDestinationCityId('');
        setDestinationAreaId('');
        setDriverId('');
        setVehicleId('');
        setStartTime('');
        setEndTime('');
    } catch (err) {
        console.error('Trip assignment failed:', err);

        const backendMessage = typeof err.response?.data === 'string'
            ? err.response.data
            : err.response?.data?.message;

        toast.error(backendMessage || 'Failed to assign trip.');
    } finally {
        setIsSubmitting(false);
    }
};


    return (
        <div className="container mt-4">
            <h3>Assign New Trip</h3>
            <form onSubmit={handleSubmit} className="row g-3 mt-2">

                {/* Origin */}
                <div className="col-md-6">
                    <label htmlFor="originCity">Origin City</label>
                    <select id="originCity" name="originCity" className="form-control" value={originCityId} onChange={e => setOriginCityId(e.target.value)} required>
                        <option value="">Select City</option>
                        {cities.map(c => <option key={c.cityId} value={c.cityId}>{c.name}</option>)}
                    </select>
                </div>
                <div className="col-md-6">
                    <label htmlFor="originArea">Origin Area</label>
                    <select id="originArea" name="originArea" className="form-control" value={originAreaId} onChange={e => setOriginAreaId(e.target.value)} required>
                        <option value="">Select Area</option>
                        {originAreas.map(a => <option key={a.areaId} value={a.areaId}>{a.name}</option>)}
                    </select>
                </div>

                {/* Destination */}
                <div className="col-md-6">
                    <label htmlFor="destinationCity">Destination City</label>
                    <select id="destinationCity" name="destinationCity" className="form-control" value={destinationCityId} onChange={e => setDestinationCityId(e.target.value)} required>
                        <option value="">Select City</option>
                        {cities.map(c => <option key={c.cityId} value={c.cityId}>{c.name}</option>)}
                    </select>
                </div>
                <div className="col-md-6">
                    <label htmlFor="destinationArea">Destination Area</label>
                    <select id="destinationArea" name="destinationArea" className="form-control" value={destinationAreaId} onChange={e => setDestinationAreaId(e.target.value)} required>
                        <option value="">Select Area</option>
                        {destinationAreas.map(a => <option key={a.areaId} value={a.areaId}>{a.name}</option>)}
                    </select>
                </div>

                {/* Driver */}
                <div className="col-md-6">
                    <label htmlFor="driver">Driver</label>
                    <select id="driver" name="driver" className="form-control" value={driverId} onChange={e => setDriverId(e.target.value)} required>
                        <option value="">Select Driver</option>
                        {drivers.map(driver => (
                            <option key={driver.driverId} value={driver.driverId}>
                                {driver.name} - {driver.phoneNumber}
                            </option>
                        ))}
                    </select>
                </div>

                {/* Vehicle */}
                <div className="col-md-6">
                    <label  htmlFor="vehicle">Vehicle</label>
                    <select id="vehicle" name="vehicle" className="form-control" value={vehicleId} onChange={e => setVehicleId(e.target.value)} required>
                        <option value="">Select Vehicle</option>
                        {vehicles.map(vehicle => (
                            <option key={vehicle.vehicleId} value={vehicle.vehicleId}>
                                {vehicle.type} - {vehicle.vehicleNumber}            
                            </option>
                        ))}
                    </select>
                </div>

                {/* Time */}
                <div className="col-md-6">
                    <label htmlFor="startTime">Start Time</label>
                    <input id='startTime' name="startTime" type={isTest ? 'text' : 'datetime-local'} className="form-control" value={startTime} onChange={e => setStartTime(e.target.value)} required />
                </div>
                <div className="col-md-6">
                    <label htmlFor="endTime">End Time</label>
                    <input id='endTime' name="endTime" type={isTest ? 'text' : 'datetime-local'} className="form-control" value={endTime} onChange={e => setEndTime(e.target.value)} required />
                </div>

                <div className="col-12">
                    <button type="submit" className="btn btn-success" disabled={isSubmitting}>
                        {isSubmitting ? 'Assigning...' : 'Assign Trip'}
                    </button>
                </div>
            </form>
        </div>
    );
}

export default AssignTrip;


