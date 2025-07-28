import { useState, useEffect } from 'react';
import axios from 'axios';
import { useAuth0 } from '@auth0/auth0-react';

interface ImportStatus {
  batchId?: string;
  files?: Array<{
    fileName: string;
    status: string;
    recordCount: number;
  }>;
  summary?: {
    totalFiles: number;
    completedFiles: number;
    failedFiles: number;
    processingFiles: number;
    queuedFiles: number;
    totalRecords: number;
    allCompleted: boolean;
  };
}

const SpotifyDataImportPage = () => {
  const [message, setMessage] = useState('');
  const [files, setFiles] = useState<FileList | null>(null);
  const [loading, setLoading] = useState(false);
  const [importStatus, setImportStatus] = useState<ImportStatus | null>(null);
  const [pollingInterval, setPollingInterval] = useState<NodeJS.Timeout | null>(null);
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

      // Use the streaming import function for better performance
      const functionUrl = `https://prestigefunctions.azurewebsites.net/api/spotifydataimportstreamingfunction?userId=${encodeURIComponent(userId)}`;
      
      const response = await axios.post(functionUrl, formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });

      if (response.status === 200 || response.status === 202) {
        const data = response.data;
        setMessage(data.summary || `Import started! ${data.message}`);
        
        // If we get a batch ID, start polling for status
        if (data.batchId) {
          startPollingStatus(data.batchId);
        }
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

  const startPollingStatus = (batchId: string) => {
    // Clear any existing polling
    if (pollingInterval) {
      clearInterval(pollingInterval);
    }

    // Poll every 2 seconds
    const interval = setInterval(async () => {
      try {
        const statusUrl = `https://prestigefunctions.azurewebsites.net/api/import-status/${batchId}`;
        const response = await axios.get(statusUrl);
        
        if (response.status === 200) {
          setImportStatus(response.data);
          
          // Stop polling if all files are completed
          if (response.data.summary?.allCompleted) {
            clearInterval(interval);
            setPollingInterval(null);
            setMessage('Import completed!');
          }
        }
      } catch (error) {
        console.error('Error polling status:', error);
      }
    }, 2000);

    setPollingInterval(interval);
  };

  // Clean up polling on unmount
  useEffect(() => {
    return () => {
      if (pollingInterval) {
        clearInterval(pollingInterval);
      }
    };
  }, [pollingInterval]);

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
        
        {importStatus && (
          <div className="mt-6 p-4 bg-gray-800 rounded-lg">
            <h3 className="text-lg font-semibold mb-2">Import Progress</h3>
            {importStatus.summary && (
              <div className="mb-4 text-sm">
                <p>Total Files: {importStatus.summary.totalFiles}</p>
                <p>Completed: {importStatus.summary.completedFiles}</p>
                <p>Processing: {importStatus.summary.processingFiles}</p>
                <p>Queued: {importStatus.summary.queuedFiles}</p>
                <p>Failed: {importStatus.summary.failedFiles}</p>
                <p>Total Records: {importStatus.summary.totalRecords}</p>
              </div>
            )}
            {importStatus.files && (
              <div className="space-y-2">
                {importStatus.files.map((file, index) => (
                  <div key={index} className="p-2 bg-gray-700 rounded">
                    <p className="font-medium">{file.fileName}</p>
                    <p className="text-sm">Status: {file.status}</p>
                    <p className="text-sm">Records: {file.recordCount}</p>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default SpotifyDataImportPage;
