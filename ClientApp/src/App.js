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
  const [darkMode, setDarkMode] = useState(true);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  // ✅ Check authentication on mount
  useEffect(() => {
    const checkAuthStatus = async () => {
      try {
        setIsLoading(true);
        const res = await fetch('/api/account/auth-status', {
          method: 'GET',
          credentials: 'include', // send cookies
        });

        if (res.ok) {
          const data = await res.json();
          setIsAuthenticated(data.isAuthenticated === true);
        } else {
          setIsAuthenticated(false);
        }
      } catch (err) {
        console.error('Auth check failed:', err);
        setIsAuthenticated(false);
      } finally {
        setIsLoading(false);
      }
    };

    checkAuthStatus();
  }, []);

  // ✅ Handle dark mode toggling
  useEffect(() => {
    const root = document.documentElement;
    if (darkMode) root.classList.add('dark');
    else root.classList.remove('dark');
  }, [darkMode]);

  // ✅ Simple state handlers
  const handleShorten = (data) => setShortenedUrls(prev => [data, ...prev]);
  const handleLogin = () => setIsAuthenticated(true);
  const handleLogout = async () => {
    try {
      await fetch('/api/account/logout', {
        method: 'POST',
        credentials: 'include',
      });
    } catch (err) {
      console.error('Logout failed:', err);
    } finally {
      setIsAuthenticated(false);
    }
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

      <Routes>
        <Route path="/" element={<UrlShortener onShorten={handleShorten} />} />
        <Route
          path="/urls"
          element={
            <PrivateRoute isAuthenticated={isAuthenticated} isLoading={isLoading}>
              <AllUrlList />
            </PrivateRoute >
          }
        />
        <Route
          path="/analytics/:redirectCode"
          element={
            <PrivateRoute isAuthenticated={isAuthenticated} isLoading={isLoading}>
              <AnalyticsPage />
            </PrivateRoute>
          }
        />
        <Route path="/login" element={<LoginPage onLogin={handleLogin} />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route
          path="/account"
          element={
            <PrivateRoute isAuthenticated={isAuthenticated} isLoading={isLoading}>
              <UserDashboard />
            </PrivateRoute>
          }
        />
      </Routes>
    </div>
  );
}

export default App;
