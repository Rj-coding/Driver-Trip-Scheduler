import { Navigate } from 'react-router-dom';

// F2: Guards a route. Requires a token in localStorage; optionally requires a
// specific role. We only check token PRESENCE (no JWT decoding) per the milestone.
export default function ProtectedRoute({ children, role }) {
  const token = localStorage.getItem('token');
  const currentRole = localStorage.getItem('role');

  // Not logged in -> send to login.
  if (!token) {
    return <Navigate to="/login" replace />;
  }

  // Logged in but wrong role -> send back to the dashboard.
  if (role && currentRole !== role) {
    return <Navigate to="/dashboard" replace />;
  }

  return children;
}

// F2: Keeps already-logged-in users out of /login and /register.
export function PublicRoute({ children }) {
  const token = localStorage.getItem('token');
  if (token) {
    return <Navigate to="/dashboard" replace />;
  }
  return children;
}
