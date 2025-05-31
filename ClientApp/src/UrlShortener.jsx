import React, { useState } from 'react';
import DatePicker from 'react-datepicker';
import 'react-datepicker/dist/react-datepicker.css';

export default function UrlShortener({ onShorten }) {
  const [url, setUrl] = useState('');
  const [requestCode, setRequestCode] = useState('');
  const [expirationDate, setExpirationDate] = useState(() => {
    const today = new Date();
    today.setDate(today.getDate() + 30); // Default to 30 days from today
    return today;
  });
  const [shortUrl, setShortUrl] = useState('');
  const [loading, setLoading] = useState(false);

  const shortenUrl = async () => {
    if (!url) return;
    setLoading(true);

    try {
      const response = await fetch('/api/url/create', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ 
          originalUrl: url, 
          requestedCode: requestCode, 
          expirationDate: expirationDate.toISOString()
        }),
      });
      const data = await response.json();
      setShortUrl(data.shortUrl);
      onShorten(data)
    } catch (err) {
      console.error('Error:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-100 dark:bg-gray-900 transition-colors">
      <div className="bg-white dark:bg-gray-800 shadow rounded p-8 w-full max-w-md space-y-4">
        <h1 className="text-2xl font-bold text-center text-gray-900 dark:text-white">
          URL Shortener
        </h1>
        <label htmlFor="url" className="block mb-1 text-sm font-medium text-gray-700 dark:text-gray-300">
          URL to shorten <span className="text-sm text-gray-500">(Required)</span>
        </label>
        <input
          type="text"
          placeholder="Paste your URL here..."
          value={url}
          onChange={(e) => setUrl(e.target.value)}
          className="w-full p-3 border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        <label htmlFor="requested-code" className="block mb-1 text-sm font-medium text-gray-700 dark:text-gray-300">
          Request a code <span className="text-sm text-gray-500">(optional)</span>
        </label>
        <input
          type="text"
          placeholder="Request a code (optional)"
          maxLength={7}
          value={requestCode}
          onChange={(e) => setRequestCode(e.target.value)}
          className="w-full p-3 border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        <label htmlFor="expiration-date" className="block mb-1 text-sm font-medium text-gray-700 dark:text-gray-300">
          Expiration Date <span className="text-sm text-gray-500">(default is 30 days)</span>
        </label>
        <div className="mb-2">
          <DatePicker
          selected={expirationDate}
          onChange={(date) => setExpirationDate(date)}
          minDate={new Date().setDate(new Date().getDate() + 1)} // Minimum date is tomorrow
          maxDate={new Date(new Date().setDate(new Date().getDate() + 365))}
          className="block w-full p-3 border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        <span id="expiration-help" className="text-sm text-gray-500">
          Please select a valid future date. (maximum 365 days from today)
        </span>
        {expirationDate && new Date(expirationDate) < new Date() && (
          <p className="text-sm text-red-600 mt-1">Expiration date cannot be in the past.</p>
        )}

        <button
          onClick={shortenUrl}
          disabled={loading}
          className="w-full bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2 rounded-lg transition"
        >
          {loading ? 'Shortening...' : 'Shorten URL'}
        </button>

        {shortUrl && (
          <div className="mt-4 text-center">
            <p className="text-gray-600 dark:text-gray-300">Your shortened URL:</p>
            <a
              href={shortUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="text-blue-600 dark:text-blue-400 hover:underline break-all"
            >
              {shortUrl}
            </a>
          </div>
        )}
      </div>
    </div>
  );
}
