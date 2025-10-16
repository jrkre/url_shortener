// ClientApp/src/CsvUpload.jsx
import React, { useState } from 'react';

export default function CsvUpload({ onUploadComplete }) {
  const [file, setFile] = useState(null);
  const [uploading, setUploading] = useState(false);
  const [results, setResults] = useState(null);
  const [showResults, setShowResults] = useState(false);

  const handleFileChange = (e) => {
    const selectedFile = e.target.files[0];
    if (selectedFile && selectedFile.type === 'text/csv') {
      setFile(selectedFile);
      setResults(null);
    } else {
      alert('Please select a valid CSV file.');
      e.target.value = '';
    }
  };

  const uploadCsv = async () => {
    if (!file) return;

    setUploading(true);
    const formData = new FormData();
    formData.append('file', file);

    try {
      const response = await fetch('/api/url/upload-csv', {
        method: 'POST',
        body: formData,
      });

      const data = await response.json();
      
      if (response.ok) {
        setResults(data);
        setShowResults(true);
        if (onUploadComplete) {
          onUploadComplete(data.Results.filter(r => r.Status === 'Success'));
        }
      } else {
        alert(`Upload failed: ${data.message || 'Unknown error'}`);
      }
    } catch (err) {
      console.error('Upload error:', err);
      alert('An error occurred during upload.');
    } finally {
      setUploading(false);
    }
  };

  const downloadTemplate = () => {
    const csvContent = 'OriginalUrl,RequestedCode,ExpirationDate\n' +
                      'https://example.com,example1,2024-12-31\n' +
                      'https://google.com,,2024-11-30\n' +
                      'https://github.com,github,';
    
    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'url_template.csv';
    a.click();
    window.URL.revokeObjectURL(url);
  };

  const downloadResults = () => {
    if (!results) return;

    const csvContent = 'Row,OriginalUrl,ShortUrl,Code,Status,Error\n' +
      results.Results.map(r => 
        `${r.Row},"${r.OriginalUrl}","${r.ShortUrl || ''}","${r.Code || ''}","${r.Status}","${r.Error || ''}"`
      ).join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'upload_results.csv';
    a.click();
    window.URL.revokeObjectURL(url);
  };

  return (
    <div className="bg-white dark:bg-gray-800 shadow rounded p-6 w-full max-w-2xl space-y-4">
      <h2 className="text-xl font-bold text-gray-900 dark:text-white">
        Bulk URL Upload
      </h2>
      
      <div className="space-y-4">
        <div>
          <button
            onClick={downloadTemplate}
            className="text-sm text-blue-600 dark:text-blue-400 hover:underline"
          >
            Download CSV Template
          </button>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            CSV should have columns: OriginalUrl (required), RequestedCode (optional), ExpirationDate (optional)
          </p>
        </div>

        <div>
          <input
            type="file"
            accept=".csv"
            onChange={handleFileChange}
            className="block w-full text-sm text-gray-500 dark:text-gray-400
                     file:mr-4 file:py-2 file:px-4
                     file:rounded-lg file:border-0
                     file:text-sm file:font-semibold
                     file:bg-blue-50 file:text-blue-700
                     hover:file:bg-blue-100
                     dark:file:bg-blue-900 dark:file:text-blue-300"
          />
        </div>

        <button
          onClick={uploadCsv}
          disabled={!file || uploading}
          className="w-full bg-green-600 hover:bg-green-700 disabled:bg-gray-400 
                   text-white font-semibold py-2 rounded-lg transition"
        >
          {uploading ? 'Uploading...' : 'Upload CSV'}
        </button>
      </div>

      {results && showResults && (
        <div className="mt-6 space-y-4">
          <div className="flex justify-between items-center">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
              Upload Results
            </h3>
            <button
              onClick={downloadResults}
              className="text-sm bg-blue-600 hover:bg-blue-700 text-white px-3 py-1 rounded"
            >
              Download Results
            </button>
          </div>
          
          <div className="grid grid-cols-3 gap-4 text-center">
            <div className="bg-gray-100 dark:bg-gray-700 p-3 rounded">
              <div className="text-2xl font-bold text-gray-900 dark:text-white">
                {results.TotalProcessed}
              </div>
              <div className="text-sm text-gray-600 dark:text-gray-400">Total</div>
            </div>
            <div className="bg-green-100 dark:bg-green-900 p-3 rounded">
              <div className="text-2xl font-bold text-green-800 dark:text-green-200">
                {results.SuccessCount}
              </div>
              <div className="text-sm text-green-600 dark:text-green-400">Success</div>
            </div>
            <div className="bg-red-100 dark:bg-red-900 p-3 rounded">
              <div className="text-2xl font-bold text-red-800 dark:text-red-200">
                {results.ErrorCount}
              </div>
              <div className="text-sm text-red-600 dark:text-red-400">Errors</div>
            </div>
          </div>

          {results.Errors.length > 0 && (
            <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded p-4">
              <h4 className="font-semibold text-red-800 dark:text-red-200 mb-2">Errors:</h4>
              <ul className="text-sm text-red-700 dark:text-red-300 space-y-1">
                {results.Errors.map((error, index) => (
                  <li key={index}>• {error}</li>
                ))}
              </ul>
            </div>
          )}

          <button
            onClick={() => setShowResults(false)}
            className="text-sm text-gray-600 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200"
          >
            Hide Results
          </button>
        </div>
      )}
    </div>
  );
}