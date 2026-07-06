import { Routes, Route } from 'react-router-dom';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Register from './pages/Register';
import AssignTrip from './pages/AssignTrip';
import ManageDrivers from './pages/ManageDrivers';
import 'bootstrap/dist/css/bootstrap.min.css';
import { ManageVehicles } from './pages/ManageVehicles';
import ViewTrips from './pages/ViewTrips';
import { ToastContainer } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';
import MyTrips from './pages/MyTrips';
import LandingPage from './pages/LandingPage';




function App() {
  return (
    <div className="container mt-4">
      <ToastContainer position="top-right" autoClose={3000} />
      <Routes>
        <Route path="/" element={<LandingPage />} />
        <Route path="/register" element={<Register />} />
        <Route path="/login" element={<Login />} />
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/trips" element={<AssignTrip />} />
        <Route path="/drivers" element={<ManageDrivers />} />
        <Route path="/vehicles" element={<ManageVehicles />} />
        <Route path="/viewtrips" element={<ViewTrips />} />
        <Route path="/mytrips" element={<MyTrips />} />

      </Routes>
    </div>
  );
}

export default App;
