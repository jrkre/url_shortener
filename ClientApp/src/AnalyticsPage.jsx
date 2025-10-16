import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { 
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
  PieChart, Pie, Cell
} from 'recharts';

export default function AnalyticsPage() {
  const { redirectCode } = useParams();
  const [analytics, setAnalytics] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884d8'];

  console.log("Rendered AnalyticsPage with code:", redirectCode);
  useEffect(() => {
    if (!redirectCode) return;

    const fetchAnalytics = async () => {
      try {
        setLoading(true);
        const res = await fetch(`/api/url/analytics/${redirectCode}`, {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json'
          },
          credentials: 'include'
        });
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
    <div className="min-h-screen bg-gray-100 dark:bg-gray-900 transition-colors py-8">
      <div className="container mx-auto px-4">
        <div className="bg-white dark:bg-gray-800 shadow rounded p-8 mb-8">
          <h1 className="text-2xl font-bold text-center text-gray-900 dark:text-white mb-6">
            URL Analytics
          </h1>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6 text-gray-800 dark:text-gray-200">
            <div>
              <p><strong>Original URL:</strong> <a href={analytics.originalUrl} target="_blank" rel="noopener noreferrer" className="text-blue-600 dark:text-blue-400 hover:underline break-words">{analytics.originalUrl}</a></p>
              <p><strong>Short URL:</strong> {analytics.shortUrl}</p>
              <p><strong>Code:</strong> {analytics.code}</p>
              <p><strong>Created At:</strong> {new Date(analytics.createdAt).toLocaleString()}</p>
            </div>
            <div>
              <p><strong>Expiration:</strong> {analytics.expirationDate ? new Date(analytics.expirationDate).toLocaleString() : 'None'}</p>
              <p><strong>Clicks:</strong> {analytics.clickCount}</p>
              <p><strong>Status:</strong> {analytics.isActive ? 'Active' : 'Inactive'}</p>
            </div>
          </div>
        </div>

        {/* Clicks Over Time Chart */}
        {analytics.clicksByDay && analytics.clicksByDay.length > 0 && (
          <div className="bg-white dark:bg-gray-800 shadow rounded p-8 mb-8">
            <h2 className="text-xl font-bold text-gray-900 dark:text-white mb-4">Clicks Over Time</h2>
            <div className="h-64">
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={analytics.clicksByDay}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="date" />
                  <YAxis />
                  <Tooltip />
                  <Legend />
                  <Line type="monotone" dataKey="count" stroke="#8884d8" name="Clicks" />
                </LineChart>
              </ResponsiveContainer>
            </div>
          </div>
        )}

        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          {/* Browser Stats */}
          {analytics.browserStats && analytics.browserStats.length > 0 && (
            <div className="bg-white dark:bg-gray-800 shadow rounded p-8">
              <h2 className="text-xl font-bold text-gray-900 dark:text-white mb-4">Browser Usage</h2>
              <div className="h-64">
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie
                      data={analytics.browserStats}
                      cx="50%"
                      cy="50%"
                      labelLine={false}
                      outerRadius={80}
                      fill="#8884d8"
                      dataKey="count"
                      nameKey="browser"
                      label={({ browser, percent }) => `${browser}: ${(percent * 100).toFixed(0)}%`}
                    >
                      {analytics.browserStats.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip />
                  </PieChart>
                </ResponsiveContainer>
              </div>
            </div>
          )}

          {/* Top Referrers */}
          {analytics.topReferrers && analytics.topReferrers.length > 0 && (
            <div className="bg-white dark:bg-gray-800 shadow rounded p-8">
              <h2 className="text-xl font-bold text-gray-900 dark:text-white mb-4">Top Referrers</h2>
              <ul className="space-y-2">
                {analytics.topReferrers.map((ref, index) => (
                  <li key={index} className="flex justify-between">
                    <span className="truncate max-w-xs">{ref.referrer || 'Direct'}</span>
                    <span className="font-semibold">{ref.count} clicks</span>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>

        {/* Recent Clicks */}
        {analytics.recentClicks && analytics.recentClicks.length > 0 && (
          <div className="bg-white dark:bg-gray-800 shadow rounded p-8 mt-8">
            <h2 className="text-xl font-bold text-gray-900 dark:text-white mb-4">Recent Clicks</h2>
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead>
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Time</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Browser</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Referrer</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">IP Address</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                  {analytics.recentClicks.map((click, index) => (
                    <tr key={index}>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800 dark:text-gray-200">{new Date(click.timestamp).toLocaleString()}</td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800 dark:text-gray-200">{click.userAgent ? click.userAgent.substring(0, 50) + '...' : 'Unknown'}</td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800 dark:text-gray-200">{click.referrer || 'Direct'}</td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800 dark:text-gray-200">{click.ipAddress}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
        </div>
      </div>
      );
}