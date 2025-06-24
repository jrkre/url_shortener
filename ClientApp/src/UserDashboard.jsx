// src/UserDashboard.jsx
import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';

export default function UserDashboard() {
  const [userUrls, setUserUrls] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchUserUrls = async () => {
      try {
        setLoading(true);
        const res = await fetch('/api/url/'); // Adjust backend endpoint if needed
        if (!res.ok) throw new Error('Failed to load user URLs');
        const data = await res.json();
        setUserUrls(data);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    };

    fetchUserUrls();
  }, []);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-100 dark:bg-gray-900 transition-colors">
      <div className="bg-white dark:bg-gray-800 shadow rounded p-8 w-full max-w-2xl space-y-6">
        <h1 className="text-2xl font-bold text-center text-gray-900 dark:text-white">User Dashboard</h1>

        {loading && <p className="text-center text-gray-700 dark:text-gray-300">Loading your URLs...</p>}
        {error && <p className="text-center text-red-500">{error}</p>}

        {!loading && userUrls.length === 0 && (
          <p className="text-center text-gray-600 dark:text-gray-400">You haven't shortened any URLs yet.</p>
        )}

        {!loading && userUrls.length > 0 && (
          <div className="space-y-4">
            {userUrls.map(url => (
              <div key={url.code} className="p-4 bg-gray-50 dark:bg-gray-700 rounded shadow-sm space-y-2">
                <p>
                  <strong>Original:</strong>{' '}
                  <a href={url.originalUrl} target="_blank" rel="noopener noreferrer" className="text-blue-600 dark:text-blue-400 hover:underline break-words">
                    {url.originalUrl}
                  </a>
                </p>
                <p><strong>Short URL:</strong> {url.shortUrl}</p>
                <p><strong>Clicks:</strong> {url.clickCount}</p>
                <Link
                  to={`/analytics/${url.code}`}
                  className="inline-block mt-2 px-4 py-1 bg-blue-600 text-white text-sm rounded hover:bg-blue-700"
                >
                  View Analytics
                </Link>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
