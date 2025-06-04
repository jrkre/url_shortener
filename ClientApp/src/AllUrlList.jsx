import React from 'react';



export default function AllUrlList() {
  const [loading, setLoading] = React.useState(true);
  const [allUrls, setAllUrls] = React.useState([]);
  const fetchUrls = async () => {
    try {
      const response = await fetch('/api/url');
      const data = await response.json();
      setAllUrls(data);
      console.log('Fetched URLs:', data);
    } catch (error) {
      console.error('Error fetching URLs:', error);
    } finally {
      setLoading(false);
    }
  };
  React.useEffect(() => {
    fetchUrls();
    setLoading(false);
  }, []);
  
  if (loading) {
    return <p className="text-center text-gray-500">Loading URLs...</p>;
  }

  const UrlCard = ({ item }) => (
    <div className="bg-white dark:bg-gray-800 shadow rounded-lg p-4 border border-gray-200 dark:border-gray-700 mb-4">
      <p className="text-sm text-gray-600 dark:text-gray-400 mb-1">
        <span className="font-medium text-gray-900 dark:text-white">Original URL:</span><br />
        <a href={item.originalUrl} target="_blank" rel="noopener noreferrer" className="text-blue-600 dark:text-blue-400 hover:underline break-words">
          {item.originalUrl}
        </a>
      </p>
      <p className="text-sm text-gray-600 dark:text-gray-400">
        <span className="font-medium text-gray-900 dark:text-white">Shortened URL:</span><br />
        <a href={`/l/${item.code}`} target="_blank" rel="noopener noreferrer" className="text-blue-600 dark:text-blue-400 hover:underline break-words">
          {window.location.origin}/l/{item.code}
        </a>
      </p>
    </div>
  );

  
  return (
    <div className="max-w-3xl mx-auto px-4 py-8">
      {/* <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">Session URLs</h2>
      {(!sessionUrls || sessionUrls.length === 0) ? (
        <p className="text-gray-500">No session URLs found.</p>
      ) : (
        <div>
          {sessionUrls.map((item, index) => (
            <UrlCard key={index} item={item} />
          ))}
        </div>
      )} */}

      <h2 className="text-2xl font-bold text-gray-900 dark:text-white mt-10 mb-4">All valid URLs from database</h2>
      {allUrls.length === 0 ? (
        <p className="text-gray-500">No URLs in the database.</p>
      ) : (
        <div>
          {allUrls.map((item, index) => (
            <UrlCard key={index} item={item} />
          ))}
        </div>
      )}
    </div>
  );

}