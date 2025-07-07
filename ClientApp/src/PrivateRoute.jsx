import React from 'react';
import { Navigate } from 'react-router-dom';

export default function PrivateRoute({ children, setIsAuthenticated }) {
  const token = localStorage.getItem('token');

  var isAuthenticated = false;
  // check token validity
  var auth = fetch('/api/account/validate-token', {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    }
  })
  .then(res => {
    if (res.ok) {
      console.log('Token is valid');
    }
    isAuthenticated = res.ok;
  }).then(data => {
    if (data) {
      setIsAuthenticated(true);
    } else {
      setIsAuthenticated(false);
    }
  }).catch(() => {
    setIsAuthenticated(false);
  });

  console.log('Auth status:', isAuthenticated);

  return isAuthenticated ? children : <Navigate to="/login" />;
}
