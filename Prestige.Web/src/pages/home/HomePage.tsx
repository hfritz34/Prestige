import React, { useState } from "react";
import NavBar from "@/components/navigation/NavBar";
import useProfile, { UserTrackResponse, UserAlbumResponse, UserArtistResponse } from "@/hooks/useProfile";
import { useAuth0 } from "@auth0/auth0-react";
import TopTracks from "./components/TopTracks";
import TopAlbums from "./components/TopAlbums";
import TopArtists from "./components/TopArtists";
import RecentlyPlayed from "./components/RecentlyPlayed";
import Pinned from "./components/Pinned";
import { HomeSearchBar } from "./components/HomeSearchBar";
import { useQuery } from "@tanstack/react-query";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import CurrentlyPlaying from "@/components/CurrentlyPlaying/CurrentlyPlaying";
import useCurrentlyPlaying from "@/hooks/useCurrentlyPlaying";
import usePrestige from "@/hooks/usePrestige";
import useHttp from "@/hooks/useHttp";

const HomePage: React.FC = () => {
  const [viewType, setViewType] = useState<"TopTracks" | "TopAlbums" | "TopArtists" | "RecentlyPlayed" | "Pinned">("TopTracks");
  const { getTopTracks, getTopAlbums, getTopArtists } = useProfile();
  const { user } = useAuth0();
  const { currentlyPlaying, totalTime } = useCurrentlyPlaying();
  const { getPinnedItems } = usePrestige();
  const http = useHttp();
  const TOP_LIMIT = 60;

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

  const { data: recentItems, isLoading: recentLoading } = useQuery({
    queryKey: ["recentlyPlayed", user?.sub],
    queryFn: async () => {
      const userId = user?.sub?.split("|").pop();
      if (userId) {
        // Get items updated in the last hour
        const oneHourAgo = new Date(Date.now() - 60 * 60 * 1000).toISOString();
        try {
          const response = await http.getOne(`library/${userId}/recently-updated?since=${oneHourAgo}`);
          return response;
        } catch (error) {
          // If endpoint doesn't exist yet, return empty
          return { tracks: [], albums: [], artists: [] };
        }
      }
      return { tracks: [], albums: [], artists: [] };
    },
    enabled: !!user?.sub && viewType === "RecentlyPlayed",
  });

  const { data: pinnedItems, isLoading: pinnedLoading } = useQuery({
    queryKey: ["pinnedItems", user?.sub],
    queryFn: async () => {
      const userId = user?.sub?.split("|").pop();
      if (userId) {
        const items = await getPinnedItems(userId);
        return items;
      }
      return { tracks: [], albums: [], artists: [] };
    },
    enabled: !!user?.sub && viewType === "Pinned",
  });

  if (tracksLoading || albumsLoading || artistsLoading || recentLoading || pinnedLoading) {
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
      {currentlyPlaying && (
        <CurrentlyPlaying
          track={currentlyPlaying.track}
          isPlaying={currentlyPlaying.isPlaying}
          progressMs={currentlyPlaying.progressMs}
          totalTime={totalTime}
        />
      )}
      <h1 className="text-3xl font-bold text-center mt-4">Prestige</h1>
      
      <div className="mt-4">
        <HomeSearchBar />
      </div>

      <div className="flex justify-center mt-4">
        <DropdownMenu>
          <DropdownMenuTrigger className="text-white bg-gray-800 p-2 rounded-md">
            {viewType === "TopTracks" ? "Tracks" : 
             viewType === "TopAlbums" ? "Albums" : 
             viewType === "TopArtists" ? "Artists" :
             viewType === "RecentlyPlayed" ? "Recently Played" : "Pinned"}
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
            <DropdownMenuItem onSelect={() => setViewType("RecentlyPlayed")}>
              Recently Played
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={() => setViewType("Pinned")}>
              Pinned
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
      
      {viewType === "TopTracks" ? (
        <TopTracks topTracks={topTracks || []} />
      ) : viewType === "TopAlbums" ? (
        <TopAlbums topAlbums={topAlbums || []} />
      ) : viewType === "TopArtists" ? (
        <TopArtists topArtists={topArtists || []} />
      ) : viewType === "RecentlyPlayed" ? (
        <RecentlyPlayed recentItems={recentItems || { tracks: [], albums: [], artists: [] }} />
      ) : (
        <Pinned pinnedItems={pinnedItems || { tracks: [], albums: [], artists: [] }} />
      )}
      
      <div className="mt-10">
        <NavBar />
      </div>
    </div>
  );
};

export default HomePage;