import { useState } from 'react';
import axios from 'axios';

const SpotifyDataImportPage = () => {
  const [message, setMessage] = useState('');
  const [files, setFiles] = useState<FileList | null>(null);

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setFiles(event.target.files);
  };

  const handleImportData = async () => {
    if (!files || files.length === 0) {
      setMessage('Please select files to upload.');
      return;
    }

    const formData = new FormData();
    Array.from(files).forEach((file) => {
      formData.append('files', file);
    });

    try {
      const response = await axios.post('https://<Prestige>.azurewebsites.net/api/SpotifyDataImportFunction', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });

      if (response.status === 200) {
        setMessage('Data imported successfully.');
      }
    } catch (error) {
      setMessage('Failed to import Spotify data.');
    }
  };

  return (
    <div className="min-h-screen bg-gray-900 text-white">
      <div className="max-w-md mx-auto p-4 bg-gray-900 text-white rounded-lg shadow-md">
        <h2 className="text-center text-2xl font-bold mb-4">Import Spotify Data</h2>
       
        <div className="text-lg mb-10">
          <p>To import your Spotify data, follow these steps:</p>
          <ol className="list-decimal list-inside my-2">
            <li>
              Visit the 
              <a href="https://www.spotify.com/account/privacy/" className="text-blue-500 hover:underline ml-1" target="_blank" rel="noopener noreferrer">
                Spotify Privacy Page
              </a>.
            </li>
            <li>Scroll down to the "Download your data" section.</li>
            <li>Follow the instructions to request and download your data. </li>
            <li>After receiving your data from Spotify, you will have a folder named "Spotify Extended Data History". This folder is located in your local files on your device and contains JSON files.</li>
          </ol>
          <p className="mt-2 mb-10">
            Upload all the JSON files in the folder below to import them.
          </p>
        </div>

        <div className="text-center">
          <input
            type="file"
            accept=".json"
            onChange={handleFileChange}
            multiple
            className="mb-4"
          />
          <button
            className="bg-blue-600 text-white py-2 px-4 rounded hover:bg-blue-700 cursor-pointer"
            onClick={handleImportData}
          >
            Import Spotify Data
          </button>
        </div>

        {message && <p className="mt-4 text-center">{message}</p>}
      </div>
    </div>
  );
};

export default SpotifyDataImportPage;
