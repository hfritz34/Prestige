import { useQuery } from "@tanstack/react-query";
import useHttp from "./useHttp";
import { useAuth0 } from "@auth0/auth0-react";

interface CurrentlyPlayingResponse {
  track: {
    id: string;
    name: string;
    durationMs: number;
    album: {
      id: string;
      name: string;
      images: {
        url: string;
        height: number;
        width: number;
      }[];
      artists: {
        id: string;
        name: string;
        images: {
          url: string;
          height: number;
          width: number;
        }[];
      }[];
    };
    artists: {
      id: string;
      name: string;
      images: {
        url: string;
        height: number;
        width: number;
      }[];
    }[];
  };
  isPlaying: boolean;
  progressMs: number;
  durationMs: number;
}

const useCurrentlyPlaying = () => {
  const { getOne } = useHttp();
  const { user } = useAuth0();

  const getCurrentlyPlaying = async (): Promise<CurrentlyPlayingResponse | null> => {
    try {
      const response = await getOne<CurrentlyPlayingResponse | any>("spotify/currently-playing");
      
      // Handle the case where backend returns a message object instead of track data
      if (response && typeof response === 'object' && 'message' in response) {
        return null;
      }
      
      // Only return response if it has the expected track structure
      if (response && response.track) {
        return response as CurrentlyPlayingResponse;
      }
      
      return null;
    } catch (error) {
      return null;
    }
  };

  const { data, error, isLoading, refetch } = useQuery({
    queryKey: ["currentlyPlaying", user?.sub],
    queryFn: getCurrentlyPlaying,
    enabled: !!user?.sub,
    // Conservative refresh to manage token usage
    refetchInterval: 120000, // 2 minutes
    retry: false,
  });

  // Calculate total time listened for prestige
  const calculateTotalTime = (progressMs: number): number => {
    // Convert milliseconds to seconds for consistency with prestige calculations
    return Math.floor(progressMs / 1000);
  };

  return {
    currentlyPlaying: data,
    isLoading,
    error,
    refetch,
    totalTime: data ? calculateTotalTime(data.progressMs) : 0,
  };
};

export default useCurrentlyPlaying;