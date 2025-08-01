import React from "react";
import { useQuery } from "@tanstack/react-query";
import { UserArtistResponse } from "@/hooks/useProfile";
import useRedirectToPrestigePages from "@/hooks/useRedirectToPrestigePages";
import useRating from "@/hooks/useRating";
import PrestigeGridCard from "./PrestigeGridCard";

type TopArtistsProps = {
  topArtists: UserArtistResponse[];
};

const TopArtists: React.FC<TopArtistsProps> = ({ topArtists }) => {
  const { redirectToArtistPage } = useRedirectToPrestigePages();
  const { getUserRatings } = useRating();

  // Fetch artist ratings
  const { data: artistRatings } = useQuery({
    queryKey: ['ratings', 'artist'],
    queryFn: () => getUserRatings('artist')
  });

  // Helper to get rating for an artist
  const getArtistRating = (artistId: string) => {
    return artistRatings?.find(rating => rating.itemId === artistId)?.personalScore;
  };

  const handleArtistClick = (artist: UserArtistResponse) => {
    console.log(`TopArtists - Artist Total Time (minutes): ${Math.floor(artist.totalTime / 60)}`);
    redirectToArtistPage({
      artistId: artist.artist.id,
      artistName: artist.artist.name,
      totalTime: artist.totalTime,
      imageUrl: artist.artist.images[0]?.url,
    });
  };

  return (
    <div className="p-4">
      <div className="mb-6">
        <h2 className="text-xl font-bold text-white mb-4 px-2">Your Top Prestiges</h2>
        
        {/* Grid layout - 3 columns */}
        <div className="grid grid-cols-3 gap-4 px-2">
          {topArtists.map((artist, index) => (
            <PrestigeGridCard
              key={`${artist.artist.id}-${index}`}
              imageUrl={artist.artist.images[0]?.url || "/placeholder-album.png"}
              name={artist.artist.name}
              subtitle="Artist"
              totalTime={artist.totalTime}
              rank={index + 1}
              type="artist"
              ratingScore={getArtistRating(artist.artist.id)}
              onClick={() => handleArtistClick(artist)}
            />
          ))}
        </div>

        {/* Empty state */}
        {topArtists.length === 0 && (
          <div className="text-center py-12">
            <div className="text-gray-400 mb-4">
              <svg className="w-16 h-16 mx-auto" fill="currentColor" viewBox="0 0 24 24">
                <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 13h-2v-6h2v6zm0-8h-2V5h2v2z"/>
              </svg>
            </div>
            <h3 className="text-lg font-semibold text-white mb-2">No Top Artists</h3>
            <p className="text-gray-400">Explore artists to build prestige</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default TopArtists;
