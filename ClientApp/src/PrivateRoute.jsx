import React from 'react';
import { Navigate } from 'react-router-dom';

// Consume auth state from parent (App) as a single source of truth.
// Props:
// - children: the protected route element(s)
// - isAuthenticated: boolean from App
// - isLoading: optional boolean indicating whether App is still checking auth
export default function PrivateRoute({ children, isAuthenticated, isLoading }) {
  // If parent is still checking auth, show a loading placeholder
  if (isLoading) return <div>Loading...</div>;

  return isAuthenticated ? children : <Navigate to="/login" />;
}