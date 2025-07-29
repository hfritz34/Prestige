import { useQuery } from "@tanstack/react-query";
import useHttp from "./useHttp";
import { useAuth0 } from "@auth0/auth0-react";
import { useEffect } from "react";

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
      const response = await getOne<CurrentlyPlayingResponse>("spotify/currently-playing");
      return response;
    } catch (error) {
      console.error("Error fetching currently playing track:", error);
      return null;
    }
  };

  const { data, error, isLoading, refetch } = useQuery({
    queryKey: ["currentlyPlaying", user?.sub],
    queryFn: getCurrentlyPlaying,
    enabled: !!user?.sub,
    refetchInterval: (data) => {
      // Refetch every 5 seconds if playing, every 30 seconds if not
      return data?.isPlaying ? 5000 : 30000;
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

export default useCurrentlyPlaying;