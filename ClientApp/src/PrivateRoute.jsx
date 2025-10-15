import React, { useState, useEffect } from 'react';
import { Navigate } from 'react-router-dom';

export default function PrivateRoute({ children }) {
  const [loading, setLoading] = useState(true);
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    const validateToken = async () => {
      try {
        const res = await fetch('/api/account/validate-token', {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json'
          },
          credentials: 'include' // send cookies
        });

        if (res.ok) {
          setIsAuthenticated(true);
        } else {
          setIsAuthenticated(false);
        }
      } catch (error) {
        console.error('Validation error:', error);
        setIsAuthenticated(false);
      } finally {
        setLoading(false);
      }
    };

    validateToken();
  }, []);

  if (loading) {
    return <div>Loading...</div>; // or a spinner
  }

  return isAuthenticated ? children : <Navigate to="/login" />;
}