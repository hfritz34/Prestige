import React from "react";
import { UserAlbumResponse } from "@/hooks/useProfile";
import useRedirectToPrestigePages from "@/hooks/useRedirectToPrestigePages";
import PrestigeGridCard from "./PrestigeGridCard";

type TopAlbumsProps = {
  topAlbums: UserAlbumResponse[];
};

const TopAlbums: React.FC<TopAlbumsProps> = ({ topAlbums }) => {
  const { redirectToAlbumPage } = useRedirectToPrestigePages();

  const handleAlbumClick = (album: UserAlbumResponse) => {
    console.log(`TopAlbums - Album Total Time (minutes): ${Math.floor(album.totalTime / 60)}`);
    redirectToAlbumPage({
      albumId: album.album.id,
      albumName: album.album.name,
      artistName: album.album.artists.map(artist => artist.name).join(", "),
      totalTime: album.totalTime,
      imageUrl: album.album.images[0].url,
    });
  };

  return (
    <div className="p-4">
      <div className="mb-6">
        <h2 className="text-xl font-bold text-white mb-4 px-2">Your Top Prestiges</h2>
        
        {/* Grid layout - 3 columns */}
        <div className="grid grid-cols-3 gap-4 px-2">
          {topAlbums.map((album, index) => (
            <PrestigeGridCard
              key={`${album.album.id}-${index}`}
              imageUrl={album.album.images[0]?.url || "/placeholder-album.png"}
              name={album.album.name}
              subtitle={album.album.artists.map(artist => artist.name).join(", ")}
              totalTime={album.totalTime}
              rank={index + 1}
              type="album"
              onClick={() => handleAlbumClick(album)}
            />
          ))}
        </div>

        {/* Empty state */}
        {topAlbums.length === 0 && (
          <div className="text-center py-12">
            <div className="text-gray-400 mb-4">
              <svg className="w-16 h-16 mx-auto" fill="currentColor" viewBox="0 0 24 24">
                <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-5 14H7v-2h7v2zm3-4H7v-2h10v2zm0-4H7V7h10v2z"/>
              </svg>
            </div>
            <h3 className="text-lg font-semibold text-white mb-2">No Top Albums</h3>
            <p className="text-gray-400">Listen to complete albums to build prestige</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default TopAlbums;
