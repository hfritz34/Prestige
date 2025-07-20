import { useState } from 'react';
import axios from 'axios';
import { useAuth0 } from '@auth0/auth0-react';

const SpotifyDataImportPage = () => {
  const [message, setMessage] = useState('');
  const [files, setFiles] = useState<FileList | null>(null);
  const [loading, setLoading] = useState(false);
  const { user } = useAuth0();

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setFiles(event.target.files);
  };

  const handleImportData = async () => {
    if (!files || files.length === 0) {
      setMessage('Please select files to upload.');
      return;
    }

    if (!user?.sub) {
      setMessage('User not authenticated. Please log in again.');
      return;
    }

    setLoading(true);
    setMessage('');

    try {
      // Extract userId from Auth0 user.sub (format: oauth2|Spotify|userId)
      const userId = user.sub.split('|').pop();
      
      if (!userId) {
        setMessage('Unable to determine user ID.');
        setLoading(false);
        return;
      }

      const formData = new FormData();
      Array.from(files).forEach((file) => {
        formData.append('file', file); // Use 'file' as key name for each file
      });

      // Use production Azure Functions URL
      const functionUrl = `https://prestigefunctions.azurewebsites.net/api/spotifydataimportfunction?userId=${encodeURIComponent(userId)}`;
      
      const response = await axios.post(functionUrl, formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });

      if (response.status === 200) {
        setMessage(`Import completed! ${response.data}`);
      }
    } catch (error: any) {
      console.error('Import error:', error);
      
      if (error.response?.data) {
        setMessage(`Failed to import: ${error.response.data}`);
      } else if (error.message) {
        setMessage(`Failed to import: ${error.message}`);
      } else {
        setMessage('Failed to import Spotify data. Please check the console for details.');
      }
    } finally {
      setLoading(false);
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
            className={`py-2 px-4 rounded text-white ${
              loading 
                ? 'bg-gray-500 cursor-not-allowed' 
                : 'bg-blue-600 hover:bg-blue-700 cursor-pointer'
            }`}
            onClick={handleImportData}
            disabled={loading}
          >
            {loading ? 'Importing...' : 'Import Spotify Data'}
          </button>
        </div>

        {message && <p className="mt-4 text-center">{message}</p>}
      </div>
    </div>
  );
};

export default SpotifyDataImportPage;
