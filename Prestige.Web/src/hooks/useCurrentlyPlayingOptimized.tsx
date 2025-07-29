import { useQuery } from "@tanstack/react-query";
import useHttp from "./useHttp";
import { useAuth0 } from "@auth0/auth0-react";

// Optimized version that reduces API calls
const useCurrentlyPlayingOptimized = () => {
  const { getOne } = useHttp();
  const { user } = useAuth0();

  // Only check during "active hours" when users are likely listening
  const isActiveHours = () => {
    const hour = new Date().getHours();
    return hour >= 6 && hour <= 23; // 6 AM to 11 PM
  };

  // Dynamic intervals based on time and user activity
  const getRefreshInterval = (data: any) => {
    if (!isActiveHours()) return 300000; // 5 minutes during off-hours
    if (data?.isPlaying) return 60000; // 1 minute if playing
    return 180000; // 3 minutes if not playing
  };

  return useQuery({
    queryKey: ["currentlyPlaying", user?.sub],
    queryFn: async () => {
      try {
        const response = await getOne("spotify/currently-playing");
        return response;
      } catch (error) {
        return null;
      }
    },
    enabled: !!user?.sub && isActiveHours(),
    refetchInterval: (query) => getRefreshInterval(query.state.data),
    staleTime: 30000, // Consider data stale after 30 seconds
    gcTime: 300000, // Keep in cache for 5 minutes
    retry: false,
  });
};

export default useCurrentlyPlayingOptimized;