import React, { useState, useEffect } from 'react';
import { Route, Routes } from 'react-router-dom';
import UrlShortener from './UrlShortener';
import AllUrlList from './AllUrlList';
import AnalyticsPage from './AnalyticsPage';

function App() {
  const [shortenedUrls, setShortenedUrls] = useState([]);
  const [darkMode, setDarkMode] = useState(true); // Default to dark mode

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

  return (
    
    <div className="min-h-screen bg-gray-100 dark:bg-gray-900 transition-colors text-gray-900 dark:text-white">
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
        {/* other routes */}
        <Route path="/" element={<UrlShortener onShorten={handleShorten} /> } />
        <Route path="/urls" element={<AllUrlList  />} />
        <Route path="/analytics/:redirectCode" element={<AnalyticsPage />} />
      </Routes>

      
      {/* <UrlShortener onShorten={handleShorten} />
      <UrlList urls={shortenedUrls} /> */}
    </div>
  );
}

export default App;