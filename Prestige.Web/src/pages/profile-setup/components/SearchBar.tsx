import * as React from "react";
import { Input } from "@/components/ui/input";
import useSpotify, { SearchResponse, SearchType} from "@/hooks/useSpotify";
import TrackSearchCard from "@/components/search/TrackSearchCard";
import { useDebounce } from 'use-debounce';
import { UseMutationResult, useQuery } from "@tanstack/react-query";
import ArtistSearchCard from "@/components/search/ArtistSearchCard";
import AlbumSearchCard from "@/components/search/AlbumSearchCard";
import { FavoritesResponse } from "@/hooks/useProfile";
import { ScrollArea } from "@/components/ui/scroll-area";

type SearchBarProps = {
  onClickQuery: UseMutationResult<FavoritesResponse, Error, string, unknown>;
  searchType: SearchType;
}

const SearchBar: React.FC<SearchBarProps> = ({ searchType, onClickQuery }) => {
  const [searchTerm, setSearchTerm] = React.useState("");
  const [debouncedSearch] = useDebounce(searchTerm.trim(), 1000);
  const spotify = useSpotify();

  const Search = (query : string, type : SearchType) =>{
    return useQuery({
      queryKey : ['search', type, query],
      queryFn : async () => {
        if(query == "") return "";
        return await spotify.search(query, type);
      }
    });
  }

  const query = Search(debouncedSearch, searchType);
  const data = query.data as SearchResponse;

  return (
    <div className="search-bar w-screen h-full flex flex-col items-center">
      <Input
        id="search-input"
        name="search"
        type="text"
        value={searchTerm}
        onChange={(e) => setSearchTerm(e.target.value)}
        placeholder={`Search for ${searchType}...`}
        className="mb-2 w-3/4"
      />
      <ScrollArea id="SearchResults" className="w-screen p-4 min-h-0 grow">
        {
          data?.tracks?.map((track) => (
            <TrackSearchCard key={track.id} track={track} onClick={() => onClickQuery.mutate(track.id)}/>
          ))
        }
        {
          data?.albums?.map((album) => (
            <AlbumSearchCard key={album.id} album={album} onClick={() => onClickQuery.mutate(album.id)}/>
          ))
        }
        {
          data?.artists?.map((artist) => (
            <ArtistSearchCard key={artist.id} artist={artist} onClick={() => onClickQuery.mutate(artist.id)}/>
          ))
        }
      </ScrollArea>
    </div>
  );
};

export { SearchBar };
