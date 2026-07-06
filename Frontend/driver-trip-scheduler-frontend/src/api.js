import axios from 'axios';

export const api = axios.create({
  baseURL: 'http://localhost:5038/api', // Replace with your actual base URL
});
