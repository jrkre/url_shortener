import React, { useState } from 'react';
import UrlForm from './UrlForm';
import UrlList from './UrlList';

function App() {
  const [shortenedUrls, setShortenedUrls] = useState([]);

  const handleShorten = (data) => {
    setShortenedUrls(prev => [data, ...prev]);
  };

  return (
    <div>
      <h1>URL Shortener</h1>
      <UrlForm onShorten={handleShorten} />
      
      <UrlList urls={shortenedUrls} />
    </div>
  );
}

export default App;