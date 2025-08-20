import * as React from "react";
import { Input } from "@/components/ui/input";
import useSpotify, { SearchResponse } from "@/hooks/useSpotify";
import TrackSearchCard from "@/components/search/TrackSearchCard";
import { useDebounce } from 'use-debounce';
import { useQuery } from "@tanstack/react-query";
import ArtistSearchCard from "@/components/search/ArtistSearchCard";
import AlbumSearchCard from "@/components/search/AlbumSearchCard";
import { ScrollArea } from "@/components/ui/scroll-area";
import { useNavigate } from "react-router-dom";
import usePrestige from "@/hooks/usePrestige";
import { useAuth0 } from "@auth0/auth0-react";

type HomeSearchBarProps = {
  contentType: "Tracks" | "Albums" | "Artists";
};

const HomeSearchBar: React.FC<HomeSearchBarProps> = ({ contentType }) => {
  const [searchTerm, setSearchTerm] = React.useState("");
  const [debouncedSearch] = useDebounce(searchTerm.trim(), 500);
  const spotify = useSpotify();
  const navigate = useNavigate();
  const prestige = usePrestige();
  const { user } = useAuth0();

  const searchQuery = useQuery({
    queryKey: ['search', 'all', debouncedSearch],
    queryFn: async () => {
      if (debouncedSearch === "") return null;
      // Search for all types
      return await spotify.search(debouncedSearch, "all");
    },
    enabled: debouncedSearch.length > 0
  });

  const handleTrackClick = async (trackId: string) => {
    const userId = user?.sub?.split("|").pop();
    if (userId) {
      // Try to add the track with 0 time if it doesn't exist
      try {
        await prestige.postUserTrack(userId, trackId, 0);
      } catch (error) {
        // Track might already exist, that's ok
      }
    }
    navigate(`/track/${trackId}`);
  };

  const handleAlbumClick = (albumId: string) => {
    navigate(`/album/${albumId}`);
  };

  const handleArtistClick = (artistId: string) => {
    navigate(`/artist/${artistId}`);
  };

  const data = searchQuery.data as SearchResponse;

  return (
    <div className="search-bar w-full flex flex-col items-center mb-4">
      <Input
        id="home-search-input"
        name="search"
        type="text"
        value={searchTerm}
        onChange={(e) => setSearchTerm(e.target.value)}
        placeholder={`Search for ${contentType.toLowerCase()}...`}
        className="mb-2 w-3/4 max-w-xl"
      />
      {debouncedSearch && data && (
        <ScrollArea className="w-3/4 max-w-xl max-h-96 p-2 bg-gray-900 rounded-md">
          {contentType === "Tracks" && data?.tracks?.slice(0, 10).map((track) => (
            <TrackSearchCard 
              key={track.id} 
              track={track} 
              onClick={() => handleTrackClick(track.id)}
            />
          ))}
          {contentType === "Albums" && data?.albums?.slice(0, 10).map((album) => (
            <AlbumSearchCard 
              key={album.id} 
              album={album} 
              onClick={() => handleAlbumClick(album.id)}
            />
          ))}
          {contentType === "Artists" && data?.artists?.slice(0, 10).map((artist) => (
            <ArtistSearchCard 
              key={artist.id} 
              artist={artist} 
              onClick={() => handleArtistClick(artist.id)}
            />
          ))}
        </ScrollArea>
      )}
    </div>
  );
};

export { HomeSearchBar };