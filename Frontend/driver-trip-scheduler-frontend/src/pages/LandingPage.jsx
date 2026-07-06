// src/pages/LandingPage.jsx
import { Link } from 'react-router-dom';
import './LandingPage.css'; 

const LandingPage = () => {
  return (
    <div className="landing-container">
      <div className="overlay">
        <nav className="navbar navbar-expand-lg navbar-dark px-4">
          <div className="container-fluid">
            <span className="navbar-brand fs-3 fw-bold">TravelMate</span>
            <div className="d-flex">
              <Link to="/login" className="btn btn-outline-light me-2">Login</Link>
              <Link to="/register" className="btn btn-light text-dark">Register</Link>
            </div>
          </div>
        </nav>

        <div className="landing-content text-center text-white">
          <h1 className="display-3 fw-bold">Explore. Dream. Discover.</h1>
          <p className="fs-4 mt-4 fst-italic">
            “Travel isn’t always pretty. It isn’t always comfortable. Sometimes it hurts, it even breaks you. But that’s okay. The journey changes you.”
          </p>
          <p className="fs-5 mt-3">
            “Jobs fill your pocket, but adventures fill your soul.”
          </p>
        </div>
      </div>
    </div>
  );
};

export default LandingPage;
