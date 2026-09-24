import axios from 'axios';
import { toast } from 'react-toastify';

export const api = axios.create({
  baseURL: 'http://localhost:5038/api', // Replace with your actual base URL
});

// F3: All pages now use this shared instance, so the 401 auto-logout lives here.
// Guarded by a token check so parallel 401s fire the toast/redirect only once.
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && localStorage.getItem('token')) {
      localStorage.removeItem('token');
      localStorage.removeItem('role');
      toast.error('Session expired. Please login again.');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);
