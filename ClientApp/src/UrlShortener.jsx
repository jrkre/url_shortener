import React, { useState } from 'react';
import DatePicker from 'react-datepicker';
import 'react-datepicker/dist/react-datepicker.css';
import SessionUrlList from './SessionUrlList'; 


export default function UrlShortener({ onShorten }) {
  const [url, setUrl] = useState('');
  const [requestCode, setRequestCode] = useState('');
  const [shortenedUrls, setShortenedUrls] = useState([]);
  const [expirationDate, setExpirationDate] = useState(() => {
    const today = new Date();
    today.setDate(today.getDate() + 30); // Default to 30 days from today
    return today;
  });
  const [shortUrl, setShortUrl] = useState('');
  const [loading, setLoading] = useState(false);
  const token = localStorage.getItem('token');
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [codeSuggestions, setCodeSuggestions] = useState([]);

  // Generate code suggestions based on the URL
  const generateCodeSuggestions = () => {
    const suggestions = [];
    
    if (url) {
      try {
        const urlObj = new URL(url);
        const domain = urlObj.hostname.replace('www.', '');
        const path = urlObj.pathname;
        
        // Domain-based suggestions
        const domainParts = domain.split('.');
        if (domainParts.length > 0) {
          const mainDomain = domainParts[0];
          suggestions.push(mainDomain.substring(0, 6).toLowerCase());
          if (mainDomain.length > 3) {
            suggestions.push(mainDomain.substring(0, 3).toLowerCase() + Math.floor(Math.random() * 100));
          }
        }
        
        // Path-based suggestions
        if (path && path !== '/') {
          const pathParts = path.split('/').filter(p => p);
          if (pathParts.length > 0) {
            const firstPath = pathParts[0].replace(/[^a-zA-Z0-9]/g, '');
            if (firstPath) {
              suggestions.push(firstPath.substring(0, 6).toLowerCase());
            }
          }
        }
      } catch (e) {
        // If URL parsing fails, generate generic suggestions
      }
    }
    
    // Add some random suggestions
    const chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    for (let i = 0; i < 3; i++) {
      let randomCode = '';
      for (let j = 0; j < 6; j++) {
        randomCode += chars.charAt(Math.floor(Math.random() * chars.length));
      }
      suggestions.push(randomCode);
    }
    
    // Remove duplicates and limit to 5 suggestions
    return [...new Set(suggestions)].slice(0, 5);
  };

  const handleCodeInputFocus = () => {
    const suggestions = generateCodeSuggestions();
    setCodeSuggestions(suggestions);
    setShowSuggestions(true);
  };

  const handleCodeInputBlur = () => {
    // Delay hiding suggestions to allow clicking on them
    setTimeout(() => setShowSuggestions(false), 200);
  };

  const selectSuggestion = (suggestion) => {
    setRequestCode(suggestion);
    setShowSuggestions(false);
  };

  console.log("TOKEN", token);

  const shortenUrl = async () => {
    if (!url) return;
    setLoading(true);

    try {
      const token = localStorage.getItem('token');

      if (!token) {
        throw new Error('No authentication token found. Please log in.');
      }

      const response = await fetch('/api/url/create', {
        method: 'POST',
        headers: { 
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({ 
          originalUrl: url, 
          requestedCode: requestCode, 
          expirationDate: expirationDate.toISOString()
        }),
      });

      // Check if response is ok before trying to parse JSON
      if (!response.ok) {
        // Try to get error message from response
        let errorMessage = 'Failed to shorten URL';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorMessage;
        } catch {
          // If response isn't JSON, get text
          // const errorText = await response.text();
          errorMessage = `HTTP ${response.status}: ${response.statusText}`;
        }
        throw new Error(errorMessage);
      }

      const data = await response.json();

      setShortUrl(data.shortUrl);
      setShortenedUrls(prev => [data, ...prev]);
      onShorten(data);
    } catch (err) {
      console.error('Error:', err);
      alert(`Error: ${err.message}`); // Show user-friendly error
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-gray-100 dark:bg-gray-900 transition-colors">
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
        <div className="relative">
          <input
            type="text"
            placeholder="Request a code (optional)"
            maxLength={7}
            value={requestCode}
            onChange={(e) => setRequestCode(e.target.value)}
            onFocus={handleCodeInputFocus}
            onBlur={handleCodeInputBlur}
            className="w-full p-3 border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          {showSuggestions && codeSuggestions.length > 0 && (
            <div className="absolute z-10 w-full mt-1 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg shadow-lg max-h-48 overflow-y-auto">
              <div className="p-2 text-xs text-gray-500 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600">
                Suggested codes:
              </div>
              {codeSuggestions.map((suggestion, index) => (
                <button
                  key={index}
                  type="button"
                  onClick={() => selectSuggestion(suggestion)}
                  className="w-full text-left px-3 py-2 hover:bg-gray-100 dark:hover:bg-gray-600 text-gray-900 dark:text-white transition-colors"
                >
                  {suggestion}
                </button>
              ))}
            </div>
          )}
        </div>
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
      <SessionUrlList sessionUrls={shortenedUrls}/>
    </div>
  );
}
