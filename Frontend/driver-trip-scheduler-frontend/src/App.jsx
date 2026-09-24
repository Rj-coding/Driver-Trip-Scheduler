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
import ProtectedRoute, { PublicRoute } from './ProtectedRoute';
import './api'; // registers the global 401 auto-logout interceptor at startup




function App() {
  return (
    <div className="container mt-4">
      <ToastContainer position="top-right" autoClose={3000} />
      <Routes>
        <Route path="/" element={<LandingPage />} />
        <Route path="/register" element={<PublicRoute><Register /></PublicRoute>} />
        <Route path="/login" element={<PublicRoute><Login /></PublicRoute>} />
        <Route path="/dashboard" element={<ProtectedRoute><Dashboard /></ProtectedRoute>} />
        <Route path="/trips" element={<ProtectedRoute role="Manager"><AssignTrip /></ProtectedRoute>} />
        <Route path="/drivers" element={<ProtectedRoute role="Manager"><ManageDrivers /></ProtectedRoute>} />
        <Route path="/vehicles" element={<ProtectedRoute role="Manager"><ManageVehicles /></ProtectedRoute>} />
        <Route path="/viewtrips" element={<ProtectedRoute role="Manager"><ViewTrips /></ProtectedRoute>} />
        <Route path="/mytrips" element={<ProtectedRoute role="Driver"><MyTrips /></ProtectedRoute>} />

      </Routes>
    </div>
  );
}

export default App;
