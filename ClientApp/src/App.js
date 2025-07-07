import React, { useState, useEffect } from 'react';
import { Route, Routes } from 'react-router-dom';
import UrlShortener from './UrlShortener';
import AllUrlList from './AllUrlList';
import AnalyticsPage from './AnalyticsPage';
import LoginPage from './LoginPage';
import RegisterPage from './RegisterPage';
import PrivateRoute from './PrivateRoute';
import Navbar from './Navbar';
import UserDashboard from './UserDashboard';

function App() {
  const [shortenedUrls, setShortenedUrls] = useState([]);
  const [darkMode, setDarkMode] = useState(true); // Default to dark mode
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  // Check authentication status on app load
  useEffect(() => {
    const checkAuthStatus = () => {
      setIsLoading(true);
      const token = localStorage.getItem('token');
      if (token) {
        fetch('/api/account/validate-token', {
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
        }).then(data => {
          console.log('Token validation response:', data);
          if (data) {
            setIsAuthenticated(true);
          } else {
            setIsAuthenticated(false);
          }
        }).catch(() => {
          setIsAuthenticated(false);
        }).finally(() => {
          setIsLoading(false);
        });
      } else {
        setIsAuthenticated(false);
      }
      setIsLoading(false);
    };

    checkAuthStatus();

    // Listen for storage changes (login/logout in other tabs)
    const handleStorageChange = (e) => {
      if (e.key === 'token') {
        setIsAuthenticated(!!e.newValue);
      }
    };

    window.addEventListener('storage', handleStorageChange);
    return () => window.removeEventListener('storage', handleStorageChange);
  }, []);

  useEffect(() => {
    const root = window.document.documentElement;
    if (darkMode) {
      root.classList.add('dark');
    } else {
      root.classList.remove('dark');
    }
  }, [darkMode]);

  const handleShorten = (data) => {
    setShortenedUrls(prev => [data, ...prev]);
  };

  const handleLogin = () => {
    setIsAuthenticated(true);
  };

  const handleLogout = () => {
    setIsAuthenticated(false);
    localStorage.removeItem('token');
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-100 dark:bg-gray-900 flex items-center justify-center">
        <div className="text-gray-900 dark:text-white">Loading...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-100 dark:bg-gray-900 transition-colors text-gray-900 dark:text-white">
      <Navbar isAuthenticated={isAuthenticated} onLogout={handleLogout} />
      <div className="flex justify-between items-center p-4 max-w-md mx-auto">
        <h1 className="text-xl font-bold">URL Shortener</h1>
        <button
          onClick={() => setDarkMode(!darkMode)}
          className="text-sm px-3 py-1 bg-gray-300 dark:bg-gray-700 text-gray-900 dark:text-white rounded"
        >
          {darkMode ? '☀️ Light Mode' : '🌙 Dark Mode'}
        </button>
      </div>
      <Routes>
        <Route path="/" element={<UrlShortener onShorten={handleShorten} />} />
        <Route path="/urls" element={<PrivateRoute setIsAuthenticated={setIsAuthenticated}><AllUrlList /></PrivateRoute>} />
        <Route path="/analytics/:redirectCode" element={<PrivateRoute setIsAuthenticated={setIsAuthenticated}><AnalyticsPage /></PrivateRoute>} />
        <Route path="/login" element={<LoginPage onLogin={handleLogin} />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/account" element={<PrivateRoute setIsAuthenticated={setIsAuthenticated}><UserDashboard /></PrivateRoute>} />
      </Routes>
    </div>
  );
}

export default App;