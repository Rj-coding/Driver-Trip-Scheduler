import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { api } from '../api';
import { toast, ToastContainer } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';

function Register() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState('Driver');
  const navigate = useNavigate();

  const handleRegister = async (e) => {
    e.preventDefault();
    try {
      await api.post('/Auth/register', {
        username,
        password,
        role
      });

      toast.success('Registration successful!', { autoClose: 2000 });
      setTimeout(() => navigate('/login'), 2000);
    } catch (error) {
      const errMsg = error.response?.data?.message || 'Please try again with different username';
      toast.error(`Registration failed: ${errMsg}`, { autoClose: 3000 });
    }
  };

  return (
    <div className="d-flex justify-content-center align-items-center vh-100 bg-light">
      <div className="card shadow p-4" style={{ width: '100%', maxWidth: '400px' }}>
        <h3 className="text-center mb-4">Register</h3>
        <form onSubmit={handleRegister}>
          <div className="mb-3">
        
            <label htmlFor="username" className="form-label">Username</label>
            <input
              id="username"
              className="form-control"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
              autoFocus
            />
          </div>
          <div className="mb-3">
            <label htmlFor="password" className="form-label">Password</label>
            <input
              id="password"
              type="password"
              className="form-control"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </div>
          <div className="mb-3">
            <label htmlFor="role" className="form-label">Role</label>
            <select
              id="role"
              className="form-select"
              value={role}
              onChange={(e) => setRole(e.target.value)}
            >
              <option value="Driver">Driver</option>
              <option value="Manager">Manager</option>
            </select>
          </div>
          <button className="btn btn-primary w-100" type="submit">Register</button>
        </form>

        {/* Back to login */}
        <div className="text-center mt-3">
          <span>Already have an account? </span>
          <Link to="/login" className="text-decoration-none">Login here</Link>
        </div>
      </div>
      <ToastContainer position="top-right" />
    </div>
  );
}

export default Register;
