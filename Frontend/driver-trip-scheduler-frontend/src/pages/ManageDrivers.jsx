import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { ToastContainer, toast } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';
import { Modal, Button, Form } from 'react-bootstrap';

export const ManageDrivers = () => {
  const [drivers, setDrivers] = useState([]);
  const [filteredDrivers, setFilteredDrivers] = useState([]);
  const [name, setName] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [driverId, setDriverId] = useState(null); // For edit mode
  const [searchTerm, setSearchTerm] = useState('');

  const [showModal, setShowModal] = useState(false);
  const handleClose = () => {
    setShowModal(false);
    setName('');
    setPhoneNumber('');
    setDriverId(null);
  };
  const handleShow = () => setShowModal(true);

  const apiUrl = 'http://localhost:5038/api/Driver';
  const token = localStorage.getItem('token');

  const fetchDrivers = async () => {
    try {
      const response = await axios.get(apiUrl, {
        headers: { Authorization: `Bearer ${token}` }
      });
      setDrivers(response.data);
      setFilteredDrivers(response.data);
    } catch (error) {
      toast.error('Failed to fetch drivers');
    }
  };

  useEffect(() => {
    fetchDrivers();
  }, []);

  const handleSaveDriver = async (e) => {
    e.preventDefault();

    if (!name.trim() || !phoneNumber.trim()) {
      toast.warning('Both Name and Phone Number are required');
      return;
    }

    try {
      if (driverId === null) {
        // Create
        await axios.post(apiUrl, { name, phoneNumber }, {
          headers: { Authorization: `Bearer ${token}` }
        });
        toast.success('Driver added successfully');
      } else {
        // Update
        await axios.put(`${apiUrl}/${driverId}`, { name, phoneNumber }, {
          headers: { Authorization: `Bearer ${token}` }
        });
        toast.success('Driver updated successfully');
      }

      handleClose();
      fetchDrivers();
    } catch (error) {
      toast.error('Failed to save driver');
    }
  };

  const handleDelete = async (id) => {
    try {
      await axios.delete(`${apiUrl}/${id}`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      toast.success('Driver deleted');
      fetchDrivers();
    } catch (error) {
      toast.error('Failed to delete driver');
    }
  };

  const handleEditClick = (driver) => {
    setDriverId(driver.driverId);
    setName(driver.name);
    setPhoneNumber(driver.phoneNumber);
    setShowModal(true);
  };

  const handleSearch = (e) => {
    const term = e.target.value.toLowerCase();
    setSearchTerm(term);
    const filtered = drivers.filter(
      (driver) =>
        driver.name.toLowerCase().includes(term) ||
        driver.phoneNumber.toLowerCase().includes(term)
    );
    setFilteredDrivers(filtered);
  };

  return (
    <div className="container mt-5">
      <ToastContainer />
      <h3 className="mb-4">Manage Drivers</h3>

      {/* Search */}
      <div className="mb-3">
        <input
          type="text"
          className="form-control"
          name="searchDriver"
          placeholder="Search by name or phone number"
          value={searchTerm}
          onChange={handleSearch}
        />
      </div>

      {/* Add Button */}
      <div className="mb-3 d-flex justify-content-end">
        <Button onClick={handleShow} name="addDriverButton">Add Driver</Button>
      </div>

      {/* Driver Table */}
      <div className="table-responsive">
        <table className="table table-bordered table-striped">
          <thead className="table-dark">
            <tr>
              <th>Driver ID</th>
              <th>Name</th>
              <th>Phone Number</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {filteredDrivers.length === 0 ? (
              <tr>
                <td colSpan="4" className="text-center">
                  No drivers found
                </td>
              </tr>
            ) : (
              filteredDrivers.map((driver) => (
                <tr key={driver.driverId}>
                  <td>{driver.driverId}</td>
                  <td>{driver.name}</td>
                  <td>{driver.phoneNumber}</td>
                  <td>
                    <button
                      className="btn btn-sm btn-warning me-2"
                      name={`editButton-${driver.driverId}`}
                      onClick={() => handleEditClick(driver)}
                    >
                      Edit
                    </button>
                    <button
                      className="btn btn-sm btn-danger"
                       name={`deleteButton-${driver.driverId}-${driver.name}`}
                      data-testid={`delete-button-${driver.driverId}`}
                      onClick={() => handleDelete(driver.driverId)}
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Modal */}
      <Modal show={showModal} onHide={handleClose}>
        <Form onSubmit={handleSaveDriver}>
          <Modal.Header closeButton>
            <Modal.Title>{driverId === null ? 'Add Driver' : 'Edit Driver'}</Modal.Title>
          </Modal.Header>
          <Modal.Body>
            <Form.Group className="mb-3">
              <Form.Label>Name</Form.Label>
              <Form.Control
                type="text"
                name="driverName"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
              />
            </Form.Group>
            <Form.Group>
              <Form.Label>Phone Number</Form.Label>
              <Form.Control
                type="text"
                name="phoneNumber"
                value={phoneNumber}
                onChange={(e) => setPhoneNumber(e.target.value)}
                required
              />
            </Form.Group>
          </Modal.Body>
          <Modal.Footer>
            <Button variant="secondary" onClick={handleClose} name="cancelButton">
              Cancel
            </Button>
            <Button variant="primary" type="submit" name="submitDriver">
              {driverId === null ? 'Add' : 'Update'}
            </Button>
          </Modal.Footer>
        </Form>
      </Modal>
    </div>
  );
};

export default ManageDrivers;
