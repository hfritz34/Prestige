import { useState, useEffect } from 'react';
import axios from 'axios';
import { useAuth0 } from '@auth0/auth0-react';

const SpotifyDataImportDevPage = () => {
  const [message, setMessage] = useState('');
  const [files, setFiles] = useState<FileList | null>(null);
  const [loading, setLoading] = useState(false);
  const [useLocal, setUseLocal] = useState(true);
  const [functionType, setFunctionType] = useState('streaming');
  const [batchId, setBatchId] = useState<string | null>(null);
  const [progress, setProgress] = useState<any>(null);
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
    setMessage('Starting import...');
    setProgress(null);
    setBatchId(null);

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
        formData.append('file', file);
      });

      // Select function URL based on settings
      let functionUrl: string;
      const baseUrl = useLocal 
        ? 'http://localhost:7071/api' 
        : 'https://prestigefunctions.azurewebsites.net/api';

      switch (functionType) {
        case 'streaming':
          functionUrl = `${baseUrl}/spotifydataimportstreamingfunction?userId=${encodeURIComponent(userId)}`;
          break;
        case 'orchestrator':
          functionUrl = `${baseUrl}/spotifydataimportorchestrator?userId=${encodeURIComponent(userId)}`;
          break;
        default:
          functionUrl = `${baseUrl}/spotifydataimportfunction?userId=${encodeURIComponent(userId)}`;
      }
      
      console.log('Uploading to:', functionUrl);
      
      // Start the import with a longer timeout
      const response = await axios.post(functionUrl, formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
        timeout: 300000, // 5 minute timeout
      });

      if (response.status === 200 || response.status === 202) {
        const data = response.data;
        setMessage('Import completed!');
        
        if (typeof data === 'object') {
          console.log('Import result:', data);
          setMessage(JSON.stringify(data, null, 2));
        }
        
        // Start monitoring processing status
        startProcessingMonitor(userId);
      }
    } catch (error: any) {
      console.error('Import error:', error);
      
      // Check if import might have succeeded despite timeout
      if (error.code === 'ECONNABORTED' || error.message.includes('timeout') || error.message.includes('Network Error')) {
        const userId = user?.sub?.split('|').pop();
        if (userId) {
          setMessage('Import may be processing in background. Checking status...');
          startProcessingMonitor(userId);
          return;
        }
      }
      
      if (error.response?.data) {
        setMessage(`Failed to import: ${JSON.stringify(error.response.data)}`);
      } else if (error.message) {
        setMessage(`Failed to import: ${error.message}`);
      } else {
        setMessage('Failed to import Spotify data. Please check the console for details.');
      }
    } finally {
      setLoading(false);
    }
  };

  const startProcessingMonitor = (userId: string) => {
    if (pollingInterval) {
      clearInterval(pollingInterval);
    }

    const baseUrl = useLocal 
      ? 'http://localhost:7071/api' 
      : 'https://prestigefunctions.azurewebsites.net/api';

    const interval = setInterval(async () => {
      try {
        const response = await axios.get(`${baseUrl}/processing-status/${userId}`);
        setProgress(response.data);
        
        if (response.data.UnprocessedTracks === 0) {
          clearInterval(interval);
          setPollingInterval(null);
          setMessage('All tracks processed successfully!');
        }
      } catch (error) {
        console.error('Error checking processing status:', error);
      }
    }, 2000); // Check every 2 seconds

    setPollingInterval(interval);

    // Stop polling after 5 minutes
    setTimeout(() => {
      if (interval) {
        clearInterval(interval);
        setPollingInterval(null);
      }
    }, 300000);
  };

  const testHealthCheck = async () => {
    try {
      const baseUrl = useLocal 
        ? 'http://localhost:7071/api' 
        : 'https://prestigefunctions.azurewebsites.net/api';
      
      const response = await axios.get(`${baseUrl}/healthcheck`);
      setMessage(JSON.stringify(response.data, null, 2));
    } catch (error: any) {
      setMessage(`Health check failed: ${error.message}`);
    }
  };

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (pollingInterval) {
        clearInterval(pollingInterval);
      }
    };
  }, [pollingInterval]);

  return (
    <div className="min-h-screen bg-gray-900 text-white">
      <div className="max-w-4xl mx-auto p-4 bg-gray-900 text-white rounded-lg shadow-md">
        <h2 className="text-center text-2xl font-bold mb-4">Import Spotify Data (Development)</h2>
        
        {/* Development Controls */}
        <div className="bg-gray-800 p-4 rounded mb-6">
          <h3 className="text-lg font-semibold mb-2">Development Settings</h3>
          
          <div className="flex items-center gap-4 mb-2">
            <label className="flex items-center">
              <input
                type="checkbox"
                checked={useLocal}
                onChange={(e) => setUseLocal(e.target.checked)}
                className="mr-2"
              />
              Use Local Functions (localhost:7071)
            </label>
          </div>
          
          <div className="mb-2">
            <label className="block mb-1">Function Type:</label>
            <select 
              value={functionType} 
              onChange={(e) => setFunctionType(e.target.value)}
              className="bg-gray-700 p-2 rounded"
            >
              <option value="original">Original (Synchronous)</option>
              <option value="streaming">Streaming (Blob Storage)</option>
              <option value="orchestrator">Orchestrator (Service Bus)</option>
            </select>
            {functionType === 'original' && (
              <p className="text-yellow-500 text-sm mt-1">
                ⚠️ Original function may timeout with large files. Use Streaming for better performance.
              </p>
            )}
          </div>
          
          <button
            onClick={testHealthCheck}
            className="bg-green-600 hover:bg-green-700 px-4 py-2 rounded text-white"
          >
            Test Health Check
          </button>
        </div>
       
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
            <li>Follow the instructions to request and download your data.</li>
            <li>After receiving your data from Spotify, you will have a folder named "Spotify Extended Data History".</li>
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

        {message && (
          <div className="mt-4 p-4 bg-gray-800 rounded">
            <pre className="whitespace-pre-wrap text-sm">{message}</pre>
          </div>
        )}

        {progress && (
          <div className="mt-6 p-4 bg-gray-800 rounded-lg">
            <h3 className="text-lg font-semibold mb-2">Processing Status</h3>
            <div className="space-y-2">
              <div className="flex justify-between">
                <span>Total Tracks:</span>
                <span className="text-blue-400">{progress.TotalTracks}</span>
              </div>
              <div className="flex justify-between">
                <span>Processed:</span>
                <span className="text-green-400">{progress.ProcessedTracks}</span>
              </div>
              <div className="flex justify-between">
                <span>Remaining:</span>
                <span className="text-yellow-400">{progress.UnprocessedTracks}</span>
              </div>
              <div className="flex justify-between">
                <span>Progress:</span>
                <span className="text-purple-400">{progress.ProcessingPercentage?.toFixed(1)}%</span>
              </div>
              
              {/* Progress Bar */}
              <div className="w-full bg-gray-700 rounded-full h-2.5 mt-3">
                <div 
                  className="bg-blue-600 h-2.5 rounded-full transition-all duration-300" 
                  style={{ width: `${progress.ProcessingPercentage || 0}%` }}
                ></div>
              </div>
              
              {progress.IsProcessing && (
                <div className="flex items-center mt-2">
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-500 mr-2"></div>
                  <span className="text-sm text-blue-400">Processing tracks...</span>
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default SpotifyDataImportDevPage;