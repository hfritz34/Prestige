import React, { useState } from "react";
import NavBar from "@/components/navigation/NavBar";
import useProfile, { UserTrackResponse, UserAlbumResponse, UserArtistResponse } from "@/hooks/useProfile";

type RecentlyUpdatedResponse = {
  tracks: UserTrackResponse[];
  albums: UserAlbumResponse[];
  artists: UserArtistResponse[];
};
import { useAuth0 } from "@auth0/auth0-react";
import TopTracks from "./components/TopTracks";
import TopAlbums from "./components/TopAlbums";
import TopArtists from "./components/TopArtists";
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
  const [contentType, setContentType] = useState<"Tracks" | "Albums" | "Artists">("Tracks");
  const [timeFilter, setTimeFilter] = useState<"AllTime" | "Recent" | "Pinned">("AllTime");
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
    enabled: !!user?.sub && contentType === "Tracks" && timeFilter === "AllTime",
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
    enabled: !!user?.sub && contentType === "Albums" && timeFilter === "AllTime",
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
    enabled: !!user?.sub && contentType === "Artists" && timeFilter === "AllTime",
  });

  const { data: recentItems, isLoading: recentLoading } = useQuery<RecentlyUpdatedResponse>({
    queryKey: ["recentlyPlayed", user?.sub],
    queryFn: async (): Promise<RecentlyUpdatedResponse> => {
      const userId = user?.sub?.split("|").pop();
      if (userId) {
        // Get items updated in the last 24 hours
        const twentyFourHoursAgo = new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString();
        console.log("Fetching recently updated items for user:", userId);
        console.log("Since:", twentyFourHoursAgo);
        try {
          const response = await http.getOne<RecentlyUpdatedResponse>(`api/library/${userId}/recently-updated?since=${twentyFourHoursAgo}`);
          console.log("Recently updated response:", response);
          return response;
        } catch (error) {
          console.error("Error fetching recently updated items:", error);
          return { tracks: [], albums: [], artists: [] };
        }
      }
      return { tracks: [], albums: [], artists: [] };
    },
    enabled: !!user?.sub && timeFilter === "Recent",
    staleTime: 60 * 60 * 1000, // Cache for 1 hour
    gcTime: 60 * 60 * 1000, // Keep cached data for 1 hour
  });

  const { data: pinnedItems, isLoading: pinnedLoading, error: pinnedError } = useQuery<RecentlyUpdatedResponse>({
    queryKey: ["pinnedItems", user?.sub],
    queryFn: async (): Promise<RecentlyUpdatedResponse> => {
      const userId = user?.sub?.split("|").pop();
      if (userId) {
        try {
          const items = await getPinnedItems(userId);
          return items;
        } catch (error) {
          console.error("Error fetching pinned items:", error);
          return { tracks: [], albums: [], artists: [] };
        }
      }
      return { tracks: [], albums: [], artists: [] };
    },
    enabled: !!user?.sub && timeFilter === "Pinned",
    retry: false,
    refetchOnWindowFocus: false,
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

  if (pinnedError) {
    console.error("Pinned items error:", pinnedError);
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
        <HomeSearchBar contentType={contentType} />
      </div>

      {/* Content Type Buttons */}
      <div className="flex justify-center mt-4 gap-2">
        <button
          onClick={() => setContentType("Tracks")}
          className={`px-4 py-2 rounded-md font-medium ${
            contentType === "Tracks"
              ? "bg-purple-600 text-white"
              : "bg-gray-700 text-gray-300 hover:bg-gray-600"
          }`}
        >
          Tracks
        </button>
        <button
          onClick={() => setContentType("Albums")}
          className={`px-4 py-2 rounded-md font-medium ${
            contentType === "Albums"
              ? "bg-purple-600 text-white"
              : "bg-gray-700 text-gray-300 hover:bg-gray-600"
          }`}
        >
          Albums
        </button>
        <button
          onClick={() => setContentType("Artists")}
          className={`px-4 py-2 rounded-md font-medium ${
            contentType === "Artists"
              ? "bg-purple-600 text-white"
              : "bg-gray-700 text-gray-300 hover:bg-gray-600"
          }`}
        >
          Artists
        </button>
      </div>

      {/* Time Filter Dropdown */}
      <div className="flex justify-center mt-4">
        <DropdownMenu>
          <DropdownMenuTrigger className="text-white bg-gray-800 p-2 rounded-md border border-gray-600">
            {timeFilter === "AllTime" ? "All Time" : 
             timeFilter === "Recent" ? "Recently Updated" : "Pinned Items"}
          </DropdownMenuTrigger>
          <DropdownMenuContent className="bg-gray-800 text-white">
            <DropdownMenuItem onSelect={() => {
              console.log("Switching to AllTime");
              setTimeFilter("AllTime");
            }}>
              All Time
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={() => {
              console.log("Switching to Recent");
              setTimeFilter("Recent");
            }}>
              Recently Updated
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={() => {
              console.log("Switching to Pinned");
              setTimeFilter("Pinned");
            }}>
              Pinned Items
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
      
      {/* Render based on content type and time filter */}
      {timeFilter === "AllTime" && (
        <>
          {contentType === "Tracks" && <TopTracks topTracks={topTracks || []} />}
          {contentType === "Albums" && <TopAlbums topAlbums={topAlbums || []} />}
          {contentType === "Artists" && <TopArtists topArtists={topArtists || []} />}
        </>
      )}
      
      {timeFilter === "Recent" && (
        <>
          {contentType === "Tracks" && (
            <TopTracks topTracks={recentItems?.tracks || []} />
          )}
          {contentType === "Albums" && (
            <TopAlbums topAlbums={recentItems?.albums || []} />
          )}
          {contentType === "Artists" && (
            <TopArtists topArtists={recentItems?.artists || []} />
          )}
        </>
      )}
      
      {timeFilter === "Pinned" && (
        <>
          {pinnedError ? (
            <div className="text-center text-red-400 p-8">
              <p>Error loading pinned items. Please try again.</p>
            </div>
          ) : (
            <>
              {contentType === "Tracks" && (
                <TopTracks topTracks={pinnedItems?.tracks || []} />
              )}
              {contentType === "Albums" && (
                <TopAlbums topAlbums={pinnedItems?.albums || []} />
              )}
              {contentType === "Artists" && (
                <TopArtists topArtists={pinnedItems?.artists || []} />
              )}
            </>
          )}
        </>
      )}
      
      <div className="mt-10">
        <NavBar />
      </div>
    </div>
  );
};

export default HomePage;