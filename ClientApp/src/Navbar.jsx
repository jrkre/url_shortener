import React from 'react';
import { Link, useNavigate } from 'react-router-dom';

export default function Navbar() {
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

  const handleLogout = () => {
    localStorage.removeItem('token');
    navigate('/login');
  };

  return (
    <nav className="bg-white dark:bg-gray-800 shadow mb-4">
      <div className="max-w-4xl mx-auto px-4 py-3 flex justify-between items-center">
        <Link to="/" className="text-lg font-bold text-gray-900 dark:text-white">
          URL Shortener
        </Link>
        <div className="space-x-4 text-sm">
          {!token ? (
            <>
              <Link
                to="/login"
                className="text-blue-600 dark:text-blue-400 hover:underline"
              >
                Login
              </Link>
              <Link
                to="/register"
                className="text-blue-600 dark:text-blue-400 hover:underline"
              >
                Register
              </Link>
            </>
          ) : (
            <>
              <Link
                to="/dashboard"
                className="text-green-600 dark:text-green-400 hover:underline"
              >
                Dashboard
              </Link>
              <button
                onClick={handleLogout}
                className="text-red-600 dark:text-red-400 hover:underline"
              >
                Logout
              </button>
            </>
          )}
        </div>
      </div>
    </nav>
  );
}
