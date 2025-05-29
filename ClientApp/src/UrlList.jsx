import React from 'react';

function UrlList({ urls }) {
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