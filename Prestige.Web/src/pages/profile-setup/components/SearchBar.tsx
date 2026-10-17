import * as React from "react";
import { Input } from "@/components/ui/input";
import type { SearchType } from "@/hooks/useSpotify";
import useMusicSearch from "@/hooks/useMusicSearch";
import TrackSearchCard from "@/components/search/TrackSearchCard";
import { useDebounce } from 'use-debounce';
import { UseMutationResult, useQuery } from "@tanstack/react-query";
import ArtistSearchCard from "@/components/search/ArtistSearchCard";
import AlbumSearchCard from "@/components/search/AlbumSearchCard";
import { ScrollArea } from "@/components/ui/scroll-area";

type SearchBarProps = {
  onClickQuery: UseMutationResult<void, Error, string, unknown>;
  searchType: SearchType;
  disabled?: boolean;
}

const SearchBar: React.FC<SearchBarProps> = ({ searchType, onClickQuery, disabled }) => {
  const [searchTerm, setSearchTerm] = React.useState("");
  const [debouncedSearch] = useDebounce(searchTerm.trim(), 1000);
  const music = useMusicSearch();
  const query = useQuery({
    queryKey: ['music-search', searchType, debouncedSearch],
    queryFn: () => music.search(debouncedSearch, searchType),
    enabled: debouncedSearch.length > 0,
    staleTime: 60_000,
    retry: false,
  });
  const data = query.data;

  return (
    <div className="search-bar w-screen h-full flex flex-col items-center">
      <Input
        id="search-input"
        name="search"
        maxLength={120}
        type="text"
        value={searchTerm}
        onChange={(e) => setSearchTerm(e.target.value)}
        placeholder={`Search for ${searchType}...`}
        className="mb-2 w-3/4"
      />
      {query.isFetching && <p role="status">Searching...</p>}
      {debouncedSearch && data && !query.isFetching && !data.tracks.length && !data.albums.length && !data.artists.length && <p>No matching music found.</p>}
      {query.isError && <p role="alert">Couldn’t search for music. <button onClick={() => void query.refetch()}>Try again</button></p>}
      <ScrollArea id="SearchResults" className="w-screen p-4 min-h-0 grow">
        {
          data?.tracks?.map((track) => (
            <TrackSearchCard key={track.id} track={track} onClick={() => !disabled && onClickQuery.mutate(track.id)}/>
          ))
        }
        {
          data?.albums?.map((album) => (
            <AlbumSearchCard key={album.id} album={album} onClick={() => !disabled && onClickQuery.mutate(album.id)}/>
          ))
        }
        {
          data?.artists?.map((artist) => (
            <ArtistSearchCard key={artist.id} artist={artist} onClick={() => !disabled && onClickQuery.mutate(artist.id)}/>
          ))
        }
      </ScrollArea>
    </div>
  );
};

export { SearchBar };
