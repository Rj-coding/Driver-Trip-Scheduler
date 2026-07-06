import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import '../App.css';


function Dashboard() {
  const navigate = useNavigate();
  const [role, setRole] = useState('');
  const isTest= typeof window !="undefined" &&  window.location.search.includes("test=true");

  useEffect(() => {
    const token = localStorage.getItem('token');
    const savedRole = localStorage.getItem('role');

    if (!token || !savedRole) {
      navigate('/login');
      return;
    }

    setRole(savedRole);
  }, [navigate]);

  const handleLogout = () => {
    localStorage.clear();
    navigate('/login');
  };

  return (
    <div className="dashboard-container">
      <div className="dashboard-header">
        <h2>Welcome to the {role} Dashboard</h2>
        <button className="btn btn-danger" onClick={handleLogout}>
          Logout
        </button>
      </div>

      <p className="dashboard-subtitle">Logged in as: <strong>{role}</strong></p>

      <div className="dashboard-cards mt-4">
        {role === 'Manager' ? (
          <>
            <div className="dashboard-card" data-testid="manageDrivers" onClick={() => navigate('/drivers')}>
              <h5>Manage Drivers</h5>
              <p>Add, remove, or update driver details easily.</p>
            </div>
            <div className="dashboard-card" onClick={() => navigate('/vehicles')} data-testid="manageVehicles">
              <h5>Manage Vehicles</h5>
              <p>Track, assign, and update fleet vehicles.</p>
            </div>
            <div className="dashboard-card" data-testid="assignTrips" onClick={() => navigate('/trips')}>
              <h5>Assign Trips</h5>
              <p>Plan and dispatch new trips to drivers.</p>
            </div>
            <div className="dashboard-card" name="viewAllTrips" onClick={() => navigate('/viewtrips')}>
              <h5>View All Trips</h5>
              <p>See trip history and current assignments.</p>
            </div>
          </>
        ) : (
          <div className="dashboard-card"  name="myTrips" onClick={() => navigate('/mytrips')}>
            <h5>My Trips</h5>
            <p>Check all your assigned and completed trips.</p>
          </div>
        )}
      </div>
    </div>
  );
}

export default Dashboard;
