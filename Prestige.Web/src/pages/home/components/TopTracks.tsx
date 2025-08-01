import React from "react";
import { useQuery } from "@tanstack/react-query";
import { UserTrackResponse } from "@/hooks/useProfile";
import useRedirectToPrestigePages from "@/hooks/useRedirectToPrestigePages";
import useRating from "@/hooks/useRating";
import PrestigeGridCard from "./PrestigeGridCard";

type TopTracksProps = {
  topTracks: UserTrackResponse[];
};

const TopTracks: React.FC<TopTracksProps> = ({ topTracks }) => {
  const { redirectToSongPage } = useRedirectToPrestigePages();
  const { getUserRatings } = useRating();

  // Fetch track ratings
  const { data: trackRatings } = useQuery({
    queryKey: ['ratings', 'track'],
    queryFn: () => getUserRatings('track'),
    retry: false,
    throwOnError: false
  });

  // Helper to get rating for a track
  const getTrackRating = (trackId: string) => {
    return Array.isArray(trackRatings) ? trackRatings.find(rating => rating.itemId === trackId)?.personalScore : undefined;
  };

  const handleTrackClick = (track: UserTrackResponse) => {
    redirectToSongPage({
      trackId: track.track.id,
      trackName: track.track.name,
      albumName: track.track.album.name,
      artistName: track.track.artists.map((artist) => artist.name).join(", "),
      totalTime: track.totalTime,
      imageUrl: track.track.album.images[0]?.url,
    });
  };

  return (
    <div className="p-4">
      <div className="mb-6">
        <h2 className="text-xl font-bold text-white mb-4 px-2">Your Top Prestiges</h2>
        
        {/* Grid layout - 3 columns */}
        <div className="grid grid-cols-3 gap-4 px-2">
          {topTracks.map((track, index) => (
            <PrestigeGridCard
              key={`${track.track.id}-${index}`}
              imageUrl={track.track.album.images[0]?.url || "/placeholder-album.png"}
              name={track.track.name}
              subtitle={track.track.artists.map(artist => artist.name).join(", ")}
              totalTime={track.totalTime}
              rank={index + 1}
              type="track"
              ratingScore={getTrackRating(track.track.id)}
              onClick={() => handleTrackClick(track)}
            />
          ))}
        </div>

        {/* Empty state */}
        {topTracks.length === 0 && (
          <div className="text-center py-12">
            <div className="text-gray-400 mb-4">
              <svg className="w-16 h-16 mx-auto" fill="currentColor" viewBox="0 0 24 24">
                <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/>
              </svg>
            </div>
            <h3 className="text-lg font-semibold text-white mb-2">No Top Tracks</h3>
            <p className="text-gray-400">Start listening to build your prestige</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default TopTracks;
