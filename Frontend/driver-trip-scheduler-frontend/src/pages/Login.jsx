// src/pages/Login.jsx
import { useState,useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { api } from '../api';
import { toast, ToastContainer } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';

function Login() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const navigate = useNavigate();
  useEffect(() => {
    const isTestParam = window.location.search.includes("test=true");
    if (isTestParam) {
      localStorage.setItem("testEnv", "true");
    }
  }, []);


  const handleLogin = async (e) => {
    e.preventDefault();
    try {
      const res = await api.post('/Auth/login', {
        username,
        password
      });

      const { token, role } = res.data;

      localStorage.setItem('token', token);
      localStorage.setItem('role', role);

      api.defaults.headers.common['Authorization'] = `Bearer ${token}`;

      toast.success('Login successful!', { autoClose: 2000 });
      setTimeout(() => navigate('/dashboard'), 2000);
    } catch (error) {
      const errMsg = error.response?.data?.message || 'Please check your credentials';
      toast.error(`Login failed: ${errMsg}`, { autoClose: 3000 });
    }
  };

  return (
    <div className="d-flex justify-content-center align-items-center vh-100 bg-light">
      <div className="card shadow p-4" style={{ width: '100%', maxWidth: '400px' }}>
        <h3 className="text-center mb-4">Login</h3>
        <form onSubmit={handleLogin}>
          <div className="mb-3">
            <label htmlFor="username" className="form-label">Username</label>
            <input
              id="username"
              name="username"
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
              name="password"
              type="password"
              className="form-control"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            /></div>
          <button className="btn btn-primary w-100" type="submit">Login</button>
        </form>

        {/* Register Button */}
        <div className="text-center mt-3">
          <p className="mb-2">Don't have an account?</p>
          <Link to="/register" className="btn btn-outline-secondary w-100">
            Register
          </Link>
        </div>
      </div>
      <ToastContainer position="top-right" />
    </div>
  );
}

export default Login;
