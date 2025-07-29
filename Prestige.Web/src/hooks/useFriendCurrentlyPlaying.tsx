import { useQuery } from "@tanstack/react-query";
import useHttp from "./useHttp";

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

const useFriendCurrentlyPlaying = (friendId: string | undefined) => {
  const { getOne } = useHttp();

  const getFriendCurrentlyPlaying = async (): Promise<CurrentlyPlayingResponse | null> => {
    if (!friendId) return null;
    
    try {
      const response = await getOne<CurrentlyPlayingResponse>(`spotify/currently-playing/${friendId}`);
      return response;
    } catch (error) {
      console.error("Error fetching friend's currently playing track:", error);
      return null;
    }
  };

  const { data, error, isLoading, refetch } = useQuery({
    queryKey: ["friendCurrentlyPlaying", friendId],
    queryFn: getFriendCurrentlyPlaying,
    enabled: !!friendId,
    refetchInterval: (query) => {
      // Refetch every 10 seconds if playing, every 60 seconds if not
      return query.state.data?.isPlaying ? 10000 : 60000;
    },
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

export default useFriendCurrentlyPlaying;