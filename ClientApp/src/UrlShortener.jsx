import React, { useState } from 'react';
import DatePicker from 'react-datepicker';
import 'react-datepicker/dist/react-datepicker.css';
import SessionUrlList from './SessionUrlList';
import CsvUpload from './CsvUpload';


export default function UrlShortener({ onShorten }) {
  const [url, setUrl] = useState('');
  const [requestCode, setRequestCode] = useState('');
  const [shortenedUrls, setShortenedUrls] = useState([]);
  const [showCsvUpload, setShowCsvUpload] = useState(false);
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

  // Error handling states
  const [error, setError] = useState('');
  const [urlError, setUrlError] = useState('');
  const [codeError, setCodeError] = useState('');
  const [dateError, setDateError] = useState('');
  const [activeIndex, setActiveIndex] = useState(-1);



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

  

  // Validation functions
  const validateUrl = (urlString) => {
    if (!urlString.trim()) {
      return 'URL is required';
    }
    
    try {
      const url = new URL(urlString);
      if (!['http:', 'https:'].includes(url.protocol)) {
        return 'URL must start with http:// or https://';
      }
      return '';
    } catch {
      return 'Please enter a valid URL (e.g., https://example.com)';
    }
  };

  const validateCode = (code) => {
    if (code && !/^[a-zA-Z0-9]+$/.test(code)) {
      return 'Code can only contain letters and numbers';
    }
    if (code && code.length > 7) {
      return 'Code cannot be longer than 7 characters';
    }
    return '';
  };

  const validateDate = (date) => {
    if (!date) {
      return 'Expiration date is required';
    }
    
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const selectedDate = new Date(date);
    selectedDate.setHours(0, 0, 0, 0);
    
    if (selectedDate <= today) {
      return 'Expiration date must be in the future';
    }
    
    const maxDate = new Date();
    maxDate.setDate(maxDate.getDate() + 365);
    if (selectedDate > maxDate) {
      return 'Expiration date cannot be more than 365 days from today';
    }
    
    return '';
  };

  const clearErrors = () => {
    setError('');
    setUrlError('');
    setCodeError('');
    setDateError('');
  };

  const shortenUrl = async () => {
    clearErrors();
    
    // Validate inputs
    const urlValidation = validateUrl(url);
    const codeValidation = validateCode(requestCode);
    const dateValidation = validateDate(expirationDate);
    
    if (urlValidation) setUrlError(urlValidation);
    if (codeValidation) setCodeError(codeValidation);
    if (dateValidation) setDateError(dateValidation);
    
    if (urlValidation || codeValidation || dateValidation) {
      return;
    }

    setLoading(true);

    try {

      const response = await fetch('/api/url/create', {
        method: 'POST',
        headers: { 
          'Content-Type': 'application/json',
        },
        credentials: 'include',
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

  const handleUrlChange = (e) => {
    const value = e.target.value;
    setUrl(value);
    if (urlError) {
      setUrlError(validateUrl(value));
    }
  };

  const handleChange = (e) => {
    const value = e.target.value;
    setRequestCode(value);
    
    if (codeError) {
      setCodeError(validateCode(value));
    }
    
    fetchSuggestions();
    setShowSuggestions(true);
    setActiveIndex(-1);
  };

  const handleDateChange = (date) => {
    setExpirationDate(date);
    if (dateError) {
      setDateError(validateDate(date));
    }
  };


  // Debounced fetchSuggestions with timer
  let fetchSuggestionsTimeout = null;

  const fetchSuggestions = () => {
    if (!url && !requestCode) {
      setCodeSuggestions([]);
      setShowSuggestions(false);
      return;
    }

    if (fetchSuggestionsTimeout) {
      clearTimeout(fetchSuggestionsTimeout);
    }
    fetchSuggestionsTimeout = setTimeout(async () => {
      try {
        const response = await fetch('/api/url/suggest-codes?count=5',
          {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ 
              originalUrl: url, 
              requestedCode: requestCode || ""
            })
            
          }
        );
        const data = await response.json();
        if (!response.ok) {
          throw new Error(data.message || 'Failed to fetch suggestions');
        }
        setCodeSuggestions(data.suggestedCodes || []);
        setShowSuggestions(true);
      } catch (err) {
        console.error('Error fetching suggestions:', err);
        setCodeSuggestions([]);
        setShowSuggestions(false);
      }
    }, 800);
  };

  const handleKeyDown = (e) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      if (activeIndex >= 0 && activeIndex < codeSuggestions.length) {
        selectSuggestion(codeSuggestions[activeIndex]);
        setActiveIndex(-1);
      }
    }
    if (e.key === 'Escape') {
      setShowSuggestions(false);
    }
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setActiveIndex(prev => (prev < codeSuggestions.length - 1 ? prev + 1 : 0));
    }
    if (e.key === 'ArrowUp') {
      e.preventDefault();
      setActiveIndex(prev => (prev > 0 ? prev - 1 : codeSuggestions.length - 1));
    }
  };

  const handleCsvUploadComplete = (uploadedUrls) => {
    setShortenedUrls(prev => [...uploadedUrls, ...prev]);
    uploadedUrls.forEach(url => onShorten(url));
  };

  const handleCodeInputFocus = () => {
    // Only fetch suggestions from the server
    fetchSuggestions();
  };

  const handleCodeInputBlur = () => {
    // Delay hiding suggestions to allow clicking on them
    setTimeout(() => setShowSuggestions(false), 200);
  };

  const selectSuggestion = (suggestion) => {
    setRequestCode(suggestion);
    setShowSuggestions(false);
  };

  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-gray-100 dark:bg-gray-900 transition-colors">
      <div className="bg-white dark:bg-gray-800 shadow rounded p-8 w-full max-w-md space-y-4">
        <h1 className="text-2xl font-bold text-center text-gray-900 dark:text-white">
          URL Shortener
        </h1>
        
        {/* Global Error Message */}
        {error && (
          <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="ml-3">
                <p className="text-sm text-red-800 dark:text-red-200">{error}</p>
              </div>
              <div className="ml-auto pl-3">
                <button
                  onClick={() => setError('')}
                  className="text-red-400 hover:text-red-600 dark:hover:text-red-300"
                >
                  <span className="sr-only">Dismiss</span>
                  <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                    <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
                  </svg>
                </button>
              </div>
            </div>
          </div>
        )}

        <label htmlFor="url" className="block mb-1 text-sm font-medium text-gray-700 dark:text-gray-300">
          URL to shorten <span className="text-sm text-gray-500">(Required)</span>
        </label>
        <input
          type="text"
          placeholder="Paste your URL here..."
          value={url}
          onChange={handleUrlChange}
          className={`w-full p-3 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 ${
            urlError 
              ? 'border-red-300 dark:border-red-600 bg-red-50 dark:bg-red-900/20' 
              : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700'
          } text-gray-900 dark:text-white`}
        />
        {urlError && (
          <p className="text-sm text-red-600 dark:text-red-400 mt-1">{urlError}</p>
        )}

        <label htmlFor="requested-code" className="block mb-1 text-sm font-medium text-gray-700 dark:text-gray-300">
          Request a code <span className="text-sm text-gray-500">(optional)</span>
        </label>
        <div className="relative">
          <input
            type="text"
            placeholder="Request a code (optional)"
            maxLength={7}
            value={requestCode}
            onChange={handleChange}
            onFocus={handleCodeInputFocus}
            onBlur={handleCodeInputBlur}
            onKeyDown={handleKeyDown}
            className={`w-full p-3 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 ${
              codeError 
                ? 'border-red-300 dark:border-red-600 bg-red-50 dark:bg-red-900/20' 
                : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700'
            } text-gray-900 dark:text-white`}
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
                  className={`w-full text-left px-3 py-2 hover:bg-gray-100 dark:hover:bg-gray-600 text-gray-900 dark:text-white transition-colors ${
                    index === activeIndex ? 'bg-gray-100 dark:bg-gray-600' : ''
                  }`}
                >
                  {suggestion}
                </button>
              ))}
            </div>
          )}
        </div>
        {codeError && (
          <p className="text-sm text-red-600 dark:text-red-400 mt-1">{codeError}</p>
        )}

        <label htmlFor="expiration-date" className="block mb-1 text-sm font-medium text-gray-700 dark:text-gray-300">
          Expiration Date <span className="text-sm text-gray-500">(default is 30 days)</span>
        </label>
        <div className="mb-2">
          <DatePicker
            selected={expirationDate}
            onChange={handleDateChange}
            minDate={new Date().setDate(new Date().getDate() + 1)}
            maxDate={new Date(new Date().setDate(new Date().getDate() + 365))}
            className={`block w-full p-3 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 ${
              dateError 
                ? 'border-red-300 dark:border-red-600 bg-red-50 dark:bg-red-900/20' 
                : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700'
            } text-gray-900 dark:text-white`}
          />
        </div>
        {dateError && (
          <p className="text-sm text-red-600 dark:text-red-400 mt-1">{dateError}</p>
        )}
        <span id="expiration-help" className="text-sm text-gray-500">
          Please select a valid future date. (maximum 365 days from today)
        </span>

        <button
          onClick={shortenUrl}
          disabled={loading}
          className="w-full bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 disabled:cursor-not-allowed text-white font-semibold py-2 rounded-lg transition"
        >
          {loading ? (
            <span className="flex items-center justify-center">
              <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              Shortening...
            </span>
          ) : 'Shorten URL'}
        </button>

        {shortUrl && (
          <div className="mt-4 p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg">
            <p className="text-gray-600 dark:text-gray-300 text-sm mb-2">✅ Your shortened URL:</p>
            <a
              href={shortUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="text-blue-600 dark:text-blue-400 hover:underline break-all font-medium"
            >
              {shortUrl}
            </a>
            <button
              onClick={() => navigator.clipboard.writeText(shortUrl)}
              className="ml-2 text-sm text-gray-500 hover:text-gray-700 dark:hover:text-gray-300"
              title="Copy to clipboard"
            >
              📋
            </button>
          </div>
        )}
      </div>
      <SessionUrlList sessionUrls={shortenedUrls}/>
    </div>
  );
}
