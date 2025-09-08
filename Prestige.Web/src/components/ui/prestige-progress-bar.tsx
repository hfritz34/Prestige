import React from 'react';
import { motion } from 'framer-motion';
import { useAuth0 } from '@auth0/auth0-react';
import axios from 'axios';

interface PrestigeTierInfo {
  tier: string;
  display_name: string;
  color: string;
  image_name: string;
  threshold: number;
}

interface ProgressStats {
  current_value: number;
  next_threshold?: number | null;
  percentage: number;
  is_max_level: boolean;
}

interface TimeEstimation {
  minutes_remaining: number;
  formatted_time: string;
  estimation_type: string;
}

interface PrestigeProgressData {
  item_id: string;
  item_type: string;
  item_name: string;
  current_level: PrestigeTierInfo;
  next_level?: PrestigeTierInfo | null;
  progress: ProgressStats;
  estimated_time_to_next?: TimeEstimation | null;
}

interface PrestigeProgressBarProps {
  itemId: string;
  itemType: 'tracks' | 'albums' | 'artists';
  itemName?: string;
  className?: string;
  showDetails?: boolean;
}

const PrestigeProgressBar: React.FC<PrestigeProgressBarProps> = ({ 
  itemId, 
  itemType, 
  itemName,
  className = "",
  showDetails = true 
}) => {
  const { getAccessTokenSilently } = useAuth0();
  const apiUri = import.meta.env.VITE_API_ADDRESS;
  
  const [progressData, setProgressData] = React.useState<PrestigeProgressData | null>(null);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    const fetchProgressData = async () => {
      if (!itemId) return;

      setLoading(true);
      setError(null);
      
      try {
        console.log(`🔵 PrestigeProgressBar: Fetching progress for ${itemType}/${itemId}`);
        
        // Get auth token using Auth0
        const token = await getAccessTokenSilently({
          authorizationParams: {
            audience: 'https://prestige-auth0-resource',
          },
          cacheMode: "on"
        });

        console.log(`🔵 PrestigeProgressBar: API URL: ${apiUri}`);
        console.log(`🔵 PrestigeProgressBar: Token received (${token?.length} chars)`);
        
        const response = await axios.get<PrestigeProgressData>(
          `${apiUri}/api/prestige-progress/${itemType}/${itemId}`, {
            headers: {
              'Authorization': `Bearer ${token}`,
              'Content-Type': 'application/json'
            }
          }
        );

        console.log(`✅ PrestigeProgressBar: Response status: ${response.status}`);
        console.log(`✅ PrestigeProgressBar: Received data for ${itemType}/${itemId}:`, response.data);
        
        setProgressData(response.data);
      } catch (err: any) {
        console.error(`❌ PrestigeProgressBar: Error fetching progress for ${itemType}/${itemId}:`, err);
        console.error(`❌ PrestigeProgressBar: Error details:`, {
          status: err.response?.status,
          statusText: err.response?.statusText,
          data: err.response?.data,
          message: err.message
        });
        
        setError(`API Error: ${err.response?.status || 'Network'} - ${err.message}`);
        
        // Fallback to mock data for development
        setProgressData(createMockProgressData(itemId, itemType, itemName || 'Unknown'));
      } finally {
        setLoading(false);
      }
    };

    fetchProgressData();
  }, [itemId, itemType, itemName, getAccessTokenSilently, apiUri]);


  // Create mock data for development/fallback
  const createMockProgressData = (id: string, type: string, name: string): PrestigeProgressData => {
    // Simple hash to get consistent mock data based on item ID
    const hash = id.split('').reduce((a, b) => a + b.charCodeAt(0), 0);
    const mockMinutes = (hash % 150) + 10; // Between 10-160 minutes
    
    return {
      item_id: id,
      item_type: type,
      item_name: name,
      current_level: {
        tier: 'silver',
        display_name: 'Silver',
        color: '#C0C0C0',
        image_name: 'silver_prestige',
        threshold: 20
      },
      next_level: {
        tier: 'gold',
        display_name: 'Gold',
        color: '#FFD700',
        image_name: 'gold_prestige',
        threshold: 35
      },
      progress: {
        current_value: mockMinutes,
        next_threshold: 35,
        percentage: ((mockMinutes - 20) / (35 - 20)) * 100,
        is_max_level: false
      },
      estimated_time_to_next: {
        minutes_remaining: 35 - mockMinutes,
        formatted_time: '2h',
        estimation_type: 'mock_data'
      }
    };
  };

  if (loading) {
    return (
      <div className={`prestige-progress-loading ${className}`}>
        <div className="animate-pulse">
          <div className="h-4 bg-gray-700 rounded-full mb-2"></div>
          <div className="h-2 bg-gray-600 rounded-full"></div>
        </div>
      </div>
    );
  }

  if (error && !progressData) {
    return (
      <div className={`prestige-progress-error ${className}`}>
        <div className="text-red-400 text-sm">
          <span className="text-xs">⚠️ API Error: {error}</span>
        </div>
      </div>
    );
  }

  if (!progressData) {
    return null;
  }

  const { current_level, next_level, progress, estimated_time_to_next } = progressData;

  return (
    <div className={`prestige-progress-bar ${className}`}>
      {/* Current Tier Badge */}
      <div className="flex items-center gap-2 mb-3">
        <div 
          className="w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold text-white"
          style={{ backgroundColor: current_level.color }}
        >
          {current_level.display_name.charAt(0)}
        </div>
        <span className="text-sm font-medium text-white">
          {current_level.display_name}
        </span>
        {progress.is_max_level && (
          <span className="text-xs text-yellow-400 font-medium">MAX</span>
        )}
      </div>

      {/* Progress Bar */}
      <div className="relative">
        {/* Background bar */}
        <div className="w-full h-3 bg-gray-700 rounded-full overflow-hidden">
          {/* Progress fill with gradient */}
          <motion.div
            className="h-full rounded-full"
            style={{
              background: next_level 
                ? `linear-gradient(90deg, ${current_level.color} 0%, ${next_level.color} 100%)`
                : current_level.color
            }}
            initial={{ width: 0 }}
            animate={{ width: `${Math.min(progress.percentage, 100)}%` }}
            transition={{ duration: 1.2, ease: "easeOut" }}
          />
        </div>
        
        {/* Progress text overlay */}
        <div className="absolute inset-0 flex items-center justify-center">
          <span className="text-xs font-bold text-white drop-shadow-lg">
            {progress.percentage.toFixed(1)}%
          </span>
        </div>
      </div>

      {/* Details */}
      {showDetails && (
        <div className="mt-3 space-y-1">
          {/* Current/Next thresholds */}
          <div className="flex justify-between text-xs text-gray-300">
            <span>{progress.current_value.toFixed(1)} min</span>
            {next_level && progress.next_threshold && (
              <span>{progress.next_threshold} min</span>
            )}
          </div>

          {/* Next tier info */}
          {next_level && !progress.is_max_level && (
            <div className="flex items-center justify-between">
              <span className="text-xs text-gray-400">
                Next: {next_level.display_name}
              </span>
              {estimated_time_to_next && (
                <span className="text-xs text-blue-400">
                  {estimated_time_to_next.formatted_time} remaining
                </span>
              )}
            </div>
          )}

          {/* Debug info (remove in production) */}
          {process.env.NODE_ENV === 'development' && (
            <div className="text-xs text-gray-500 mt-2 p-2 bg-gray-800 rounded">
              <div>API: {error ? '❌ Error' : '✅ Success'}</div>
              <div>Type: {estimated_time_to_next?.estimation_type || 'N/A'}</div>
              <div>Item: {itemType}/{itemId}</div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default PrestigeProgressBar;