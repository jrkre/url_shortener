import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';

export default function AnalyticsPage() {
  const { redirectCode } = useParams();
  const [analytics, setAnalytics] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);


  console.log("Rendered AnalyticsPage with code:", redirectCode);
  useEffect(() => {
    if (!redirectCode) return;

    const fetchAnalytics = async () => {
      try {
        setLoading(true);
        const res = await fetch(`/api/url/analytics/${redirectCode}`);
        if (!res.ok) throw new Error('Failed to fetch analytics');
        const data = await res.json();
        setAnalytics(data);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    };

    fetchAnalytics();
  }, [redirectCode]);

  if (loading) return <p>Loading analytics...</p>;
  if (error) return <p>Error: {error}</p>;
  if (!analytics) return <p>No analytics found.</p>;

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-100 dark:bg-gray-900 transition-colors">
      <div className="bg-white dark:bg-gray-800 shadow rounded p-8 w-full max-w-md space-y-4">
        <h1 className="text-2xl font-bold text-center text-gray-900 dark:text-white">
          URL Analytics
        </h1>

        {loading && (
          <p className="text-center text-gray-700 dark:text-gray-300">Loading...</p>
        )}

        {error && (
          <p className="text-center text-red-500">{error}</p>
        )}

        {analytics && (
          <div className="space-y-3 text-gray-800 dark:text-gray-200">
            <p><strong>Original URL:</strong> <a href={analytics.originalUrl} target="_blank" rel="noopener noreferrer" className="text-blue-600 dark:text-blue-400 hover:underline break-words">{analytics.originalUrl}</a></p>
            <p><strong>Short URL:</strong> <a href={`/l/${analytics.code}`} rel="noopener noreferrer" className="text-blue-600 dark:text-blue-400 hover:underline break-words">{window.location.origin}/l/{analytics.code}</a></p>
            <p><strong>Code:</strong> {analytics.code}</p>
            <p><strong>Created At:</strong> {new Date(analytics.createdAt).toLocaleString()}</p>
            <p><strong>Expiration:</strong> {analytics.expirationDate ? new Date(analytics.expirationDate).toLocaleString() : 'None'}</p>
            <p><strong>Clicks:</strong> {analytics.clickCount}</p>
            <p><strong>Status:</strong> {analytics.isActive ? 'Active' : 'Inactive'}</p>
          </div>
        )}
      </div>
    </div>
  );
}
