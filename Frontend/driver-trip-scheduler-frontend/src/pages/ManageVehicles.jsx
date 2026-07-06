
import React, { useEffect, useState } from "react";
import { ToastContainer, toast } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";
import { Modal, Button, Form } from "react-bootstrap";

export const ManageVehicles = () => {
  const [vehicles, setVehicles] = useState([]);
  const [vehicleId, setVehicleId] = useState(0);
  const [vehicleNumber, setVehicleNumber] = useState("");
  const [vehicleType, setVehicleType] = useState("");
  const [showModal, setShowModal] = useState(false);
  const [isEdit, setIsEdit] = useState(false);
  const [search, setSearch] = useState("");

  const token = localStorage.getItem("token");

  useEffect(() => {
    fetchVehicles();
  }, []);

  const fetchVehicles = async () => {
    try {
      const response = await fetch("http://localhost:5038/api/Vehicle", {
        headers: { Authorization: `Bearer ${token}` },
      });
      const data = await response.json();
      setVehicles(data);
    } catch (error) {
      toast.error("Failed to load vehicles");
    }
  };

  const handleSubmit = async (e) => {
  e.preventDefault();

  const vehicleData = {
    vehicleId, // include vehicleId even for PUT
    vehicleNumber,
    type: vehicleType, // must be 'type', not 'vehicleType'
  };

  const url = isEdit
    ? `http://localhost:5038/api/Vehicle/${vehicleId}`
    : "http://localhost:5038/api/Vehicle";
  const method = isEdit ? "PUT" : "POST";

  try {
    const response = await fetch(url, {
      method,
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(vehicleData),
    });

    if (response.ok) {
      toast.success(`Vehicle ${isEdit ? "updated" : "added"} successfully`);
      fetchVehicles();
      handleClose();
    } else {
      toast.error("Failed to save vehicle");
    }
  } catch (error) {
    toast.error("Error occurred while saving vehicle");
  }
};


  const handleEdit = (vehicle) => {
    setVehicleId(vehicle.vehicleId);
    setVehicleNumber(vehicle.vehicleNumber);
    setVehicleType(vehicle.type);
    setIsEdit(true);
    setShowModal(true);
  };

  const handleDelete = async (id) => {
    if (!window.confirm("Are you sure you want to delete this vehicle?")) return;

    try {
      const response = await fetch(`http://localhost:5038/api/Vehicle/${id}`, {
        method: "DELETE",
        headers: { Authorization: `Bearer ${token}` },
      });

      if (response.ok) {
        toast.success("Vehicle deleted");
        fetchVehicles();
      } else {
        toast.error("Failed to delete vehicle");
      }
    } catch (error) {
      toast.error("Error occurred while deleting");
    }
  };

  const handleClose = () => {
    setShowModal(false);
    setVehicleId(0);
    setVehicleNumber("");
    setVehicleType("");
    setIsEdit(false);
  };

const filteredVehicles = vehicles.filter(
  (v) =>
    v.vehicleNumber.toLowerCase().includes(search.toLowerCase()) ||
    v.type.toLowerCase().includes(search.toLowerCase())
);


  return (
    <div className="container mt-4">
      <ToastContainer />
      <h2>Manage Vehicles</h2>

      <div className="d-flex justify-content-between mb-3">
        <input
          type="text"
          className="form-control w-50"
          placeholder="Search by vehicle number or type"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <Button variant="primary" onClick={() => setShowModal(true)} data-testid="add-vehicle-button"
>
          Add Vehicle
        </Button>
      </div>

      <table className="table table-bordered table-hover">
        <thead className="table-dark">
          <tr>
            <th>Vehicle ID</th>
            <th>Vehicle Number</th>
            <th>Vehicle Type</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {filteredVehicles.map((v) => (
            <tr key={v.vehicleId}>
              <td>{v.vehicleId}</td>
              <td>{v.vehicleNumber}</td>
              <td>{v.type}</td>
              <td>
                <button className="btn btn-warning btn-sm me-2" onClick={() => handleEdit(v)} data-testid={`edit-button-${v.vehicleId}`}>
                  Edit
                </button>
                <button className="btn btn-danger btn-sm" onClick={() => handleDelete(v.vehicleId)}   data-testid={`delete-button-${v.vehicleId}`}
>
                  Delete
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      <Modal show={showModal} onHide={handleClose}>
        <Modal.Header closeButton>
          <Modal.Title>{isEdit ? "Edit Vehicle" : "Add Vehicle"}</Modal.Title>
        </Modal.Header>
        <Form onSubmit={handleSubmit}>
          <Modal.Body>
            <Form.Group className="mb-3">
              <Form.Label>Vehicle Number</Form.Label>
              <Form.Control
                type="text"
                name="vehicleNumber"
                value={vehicleNumber}
                onChange={(e) => setVehicleNumber(e.target.value)}
                required
              />
            </Form.Group>
            <Form.Group>
              <Form.Label>Vehicle Type</Form.Label>
              <Form.Control
                type="text"
                name="vehicleType"
                value={vehicleType}
                onChange={(e) => setVehicleType(e.target.value)}
                required
              />
            </Form.Group>
          </Modal.Body>
          <Modal.Footer>
            <Button variant="secondary" onClick={handleClose}>
              Cancel
            </Button>
            <Button variant="primary" type="submit" data-testid="submit-vehicle-button">
              {isEdit ? "Update" : "Add"}
            </Button>
          </Modal.Footer>
        </Form>
      </Modal>
    </div>
  );
};
