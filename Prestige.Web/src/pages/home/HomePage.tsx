import React, { useState } from "react";
import NavBar from "@/components/navigation/NavBar";
import useProfile, { UserTrackResponse, UserAlbumResponse, UserArtistResponse } from "@/hooks/useProfile";
import { useAuth0 } from "@auth0/auth0-react";
import TopTracks from "./components/TopTracks";
import TopAlbums from "./components/TopAlbums";
import TopArtists from "./components/TopArtists";
import { useQuery } from "@tanstack/react-query";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

const HomePage: React.FC = () => {
  const [viewType, setViewType] = useState<"TopTracks" | "TopAlbums" | "TopArtists">("TopTracks");
  const { getTopTracks, getTopAlbums, getTopArtists } = useProfile();
  const { user } = useAuth0();
  const TOP_LIMIT = 25;

  const { data: topTracks, error: tracksError, isLoading: tracksLoading } = useQuery<UserTrackResponse[]>({
    queryKey: ["topTracks", user?.sub],
    queryFn: async () => {
      const userId = user?.sub?.split("|").pop();
      if (userId) {
        const tracks = await getTopTracks();
        return tracks.slice(0, TOP_LIMIT);
      }
      return [];
    },
    enabled: !!user?.sub && viewType === "TopTracks",
  });

  const { data: topAlbums, error: albumsError, isLoading: albumsLoading } = useQuery<UserAlbumResponse[]>({
    queryKey: ["topAlbums", user?.sub],
    queryFn: async () => {
      const userId = user?.sub?.split("|").pop();
      if (userId) {
        const albums = await getTopAlbums();
        return albums.slice(0, TOP_LIMIT);
      }
      return [];
    },
    enabled: !!user?.sub && viewType === "TopAlbums",
  });

  const { data: topArtists, error: artistsError, isLoading: artistsLoading } = useQuery<UserArtistResponse[]>({
    queryKey: ["topArtists", user?.sub],
    queryFn: async () => {
      const userId = user?.sub?.split("|").pop();
      if (userId) {
        const artists = await getTopArtists();
        return artists.slice(0, TOP_LIMIT);
      }
      return [];
    },
    enabled: !!user?.sub && viewType === "TopArtists",
  });

  if (tracksLoading || albumsLoading || artistsLoading) {
    return <div>Loading...</div>;
  }

  if (tracksError) {
    return <div>Error fetching top tracks: {tracksError.toString()}</div>;
  }

  if (albumsError) {
    return <div>Error fetching top albums: {albumsError.toString()}</div>;
  }

  if (artistsError) {
    return <div>Error fetching top artists: {artistsError.toString()}</div>;
  }

  return (
    <div className="bg-gray-800 text-white min-h-screen overflow-visible">
      <h1 className="text-3xl font-bold text-center mt-4">Prestige</h1>
      <div className="flex justify-center mt-4">
        <DropdownMenu>
          <DropdownMenuTrigger className="text-white bg-gray-800 p-2 rounded-md">
            {viewType === "TopTracks" ? "Tracks" : viewType === "TopAlbums" ? "Albums" : "Artists"}
          </DropdownMenuTrigger>
          <DropdownMenuContent className="bg-gray-800 text-white">
            <DropdownMenuItem onSelect={() => setViewType("TopTracks")}>
              Tracks
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={() => setViewType("TopAlbums")}>
              Albums
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={() => setViewType("TopArtists")}>
              Artists
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
      {viewType === "TopTracks" ? (
        <TopTracks topTracks={topTracks || []} />
      ) : viewType === "TopAlbums" ? (
        <TopAlbums topAlbums={topAlbums || []} />
      ) : (
        <TopArtists topArtists={topArtists || []} />
      )}
      <div className="mt-10">
        <NavBar />
      </div>
    </div>
  );
};

export default HomePage;
