import React from 'react';



function UrlList({ urls }) {
  const [loading, setLoading] = React.useState(true);
  const [allUrls, setAllUrls] = React.useState([]);
  React.useEffect(() => {
    fetchUrls();
    setLoading(false);
  }, []);
  if (loading) {
    return <p>Loading URLs...</p>;
  }
  if (!urls || urls.length === 0) {
    return <p>No URLs found.</p>;
  }


  function fetchUrls() {
    fetch('/api/url')
      .then(response => response.json())
      .then(data => {
        setAllUrls(data);
        console.log('Fetched URLs:', data);
        // Assuming you want to set this data in state, you can use a state hook here
      })
      .catch(error => console.error('Error fetching URLs:', error));
      
  }
  return (
    <ul>
      {urls.map((item, index) => (
        <li key={index}>
          <div>
            Original URL: <a href={item.originalUrl}>{item.originalUrl}</a><br />
            Shortened URL: <a href={`/${item.code}`}>
              {window.location.origin}/{item.code}</a>
          </div>
        </li>
      ))}
    </ul>
  );
}

export default UrlList;