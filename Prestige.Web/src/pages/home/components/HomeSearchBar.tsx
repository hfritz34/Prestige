import * as React from "react";
import { Input } from "@/components/ui/input";
import type { TrackResponse, AlbumResponse, ArtistResponse, SearchType } from "@/hooks/useSpotify";
import useMusicSearch from "@/hooks/useMusicSearch";
import { usesCatalogCollections } from "@/config/catalog";
import TrackSearchCard from "@/components/search/TrackSearchCard";
import { useDebounce } from 'use-debounce';
import { useQuery } from "@tanstack/react-query";
import ArtistSearchCard from "@/components/search/ArtistSearchCard";
import AlbumSearchCard from "@/components/search/AlbumSearchCard";
import { ScrollArea } from "@/components/ui/scroll-area";
import useRedirectToPrestigePages from "@/hooks/useRedirectToPrestigePages";
import usePrestige from "@/hooks/usePrestige";
import { useAccount } from '@/auth/AccountProvider';

type HomeSearchBarProps = {
  contentType: "Tracks" | "Albums" | "Artists";
};

const HomeSearchBar: React.FC<HomeSearchBarProps> = ({ contentType }) => {
  const [searchTerm, setSearchTerm] = React.useState("");
  const [debouncedSearch] = useDebounce(searchTerm.trim(), 500);
  const music = useMusicSearch();
  const { redirectToSongPage, redirectToAlbumPage, redirectToArtistPage } = useRedirectToPrestigePages();
  const prestige = usePrestige();
  const user = useAccount();
  const [openingTrack, setOpeningTrack] = React.useState(false);
  const [openError, setOpenError] = React.useState(false);
  const searchType: SearchType = contentType === 'Tracks' ? 'track' : contentType === 'Albums' ? 'album' : 'artist';
  const searchQuery = useQuery({
    queryKey: ['music-search', searchType, debouncedSearch],
    queryFn: () => music.search(debouncedSearch, searchType),
    enabled: debouncedSearch.length > 0,
    staleTime: 60_000,
    retry: false,
  });

  const handleTrackClick = async (track: TrackResponse) => {
    if (openingTrack) return;
    setOpeningTrack(true);
    setOpenError(false);
    try {
      // Legacy pinning still requires its listening join. Catalog browsing does not save anything.
      if (!usesCatalogCollections) await prestige.postUserTrack(user.id, track.id, 0);
      redirectToSongPage({
        trackId: track.id, trackName: track.name, albumId: track.album.id, albumName: track.album.name,
        artistName: track.artists.map(artist => artist.name).join(', '), imageUrl: track.album.images[0]?.url || '',
        totalTime: 0, hasListeningHistory: false,
      });
    } catch { setOpenError(true); }
    finally { setOpeningTrack(false); }
  };
  const handleAlbumClick = (album: AlbumResponse) => redirectToAlbumPage({
    albumId: album.id, albumName: album.name, artistName: album.artists.map(artist => artist.name).join(', '),
    imageUrl: album.images[0]?.url || '', totalTime: 0, hasListeningHistory: false,
  });
  const handleArtistClick = (artist: ArtistResponse) => redirectToArtistPage({
    artistId: artist.id, artistName: artist.name, imageUrl: artist.images[0]?.url || '',
    totalTime: 0, hasListeningHistory: false,
  });
  const data = searchQuery.data;

  return (
    <div className="search-bar w-full flex flex-col items-center mb-4">
      <Input
        id="home-search-input"
        name="search"
        maxLength={120}
        type="text"
        value={searchTerm}
        onChange={(e) => setSearchTerm(e.target.value)}
        placeholder={`Search for ${contentType.toLowerCase()}...`}
        className="mb-2 w-3/4 max-w-xl"
      />
      {searchQuery.isFetching && <p role="status">Searching...</p>}
      {searchQuery.isError && <p role="alert">Couldn’t search for music. <button onClick={() => void searchQuery.refetch()}>Try again</button></p>}
      {openError && <p role="alert">Couldn’t open this song. Please try again.</p>}
      {debouncedSearch && data && !searchQuery.isFetching && !data.tracks.length && !data.albums.length && !data.artists.length && <p>No matching music found.</p>}
      {debouncedSearch && data && (
        <ScrollArea className="w-3/4 max-w-xl max-h-96 p-2 bg-gray-900 rounded-md">
          {contentType === "Tracks" && data?.tracks?.slice(0, 10).map((track) => (
            <TrackSearchCard 
              key={track.id} 
              track={track} 
              onClick={() => handleTrackClick(track)}
            />
          ))}
          {contentType === "Albums" && data?.albums?.slice(0, 10).map((album) => (
            <AlbumSearchCard 
              key={album.id} 
              album={album} 
              onClick={() => handleAlbumClick(album)}
            />
          ))}
          {contentType === "Artists" && data?.artists?.slice(0, 10).map((artist) => (
            <ArtistSearchCard 
              key={artist.id} 
              artist={artist} 
              onClick={() => handleArtistClick(artist)}
            />
          ))}
        </ScrollArea>
      )}
    </div>
  );
};

export { HomeSearchBar };