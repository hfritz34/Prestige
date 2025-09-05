import React from "react";
import { useNavigate } from "react-router-dom";
import PrestigeGridCard from "./PrestigeGridCard";
import { UserTrackResponse, UserAlbumResponse, UserArtistResponse } from "@/hooks/useProfile";

type RecentlyPlayedProps = {
  recentItems: {
    tracks: UserTrackResponse[];
    albums: UserAlbumResponse[];
    artists: UserArtistResponse[];
  };
};

const RecentlyPlayed: React.FC<RecentlyPlayedProps> = ({ recentItems }) => {
  const navigate = useNavigate();

  const handleCardClick = (id: string, type: "track" | "album" | "artist") => {
    navigate(`/${type}/${id}`);
  };

  const allItems = [
    ...recentItems.tracks.map(track => ({ ...track, type: "track" as const })),
    ...recentItems.albums.map(album => ({ ...album, type: "album" as const })),
    ...recentItems.artists.map(artist => ({ ...artist, type: "artist" as const }))
  ].sort((a, b) => b.totalTime - a.totalTime);

  return (
    <div>
      <h2 className="text-2xl font-bold text-center mt-4 mb-4">Recently Updated</h2>
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4 p-4">
        {allItems.map((item) => {
          if (item.type === "track" && "track" in item) {
            return (
              <PrestigeGridCard
                key={`track-${item.track.id}`}
                id={item.track.id}
                totalTime={item.totalTime}
                name={item.track.name}
                images={item.track.album.images}
                artists={item.track.artists}
                prestigeTier={item.prestigeTier}
                onClick={() => handleCardClick(item.track.id, "track")}
              />
            );
          } else if (item.type === "album" && "album" in item) {
            return (
              <PrestigeGridCard
                key={`album-${item.album.id}`}
                id={item.album.id}
                totalTime={item.totalTime}
                name={item.album.name}
                images={item.album.images}
                artists={item.album.artists}
                prestigeTier={item.prestigeTier}
                onClick={() => handleCardClick(item.album.id, "album")}
              />
            );
          } else if (item.type === "artist" && "artist" in item) {
            return (
              <PrestigeGridCard
                key={`artist-${item.artist.id}`}
                id={item.artist.id}
                totalTime={item.totalTime}
                name={item.artist.name}
                images={item.artist.images}
                prestigeTier={item.prestigeTier}
                onClick={() => handleCardClick(item.artist.id, "artist")}
              />
            );
          }
          return null;
        })}
      </div>
    </div>
  );
};

export default RecentlyPlayed;