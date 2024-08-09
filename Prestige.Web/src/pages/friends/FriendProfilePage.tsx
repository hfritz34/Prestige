import React, { useState, useEffect } from "react";
import { useParams } from "react-router-dom";
import NavBar from "@/components/navigation/NavBar";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import useFriends from "@/hooks/useFriends";
import { useQuery } from "@tanstack/react-query";
import FriendFavoritesCarousel from "./components/FriendFavoritesCarousel";
import FriendTopCarousel from "./components/FriendTopCarousel";

const FriendProfilePage: React.FC = () => {
  const { friendId } = useParams<{ friendId: string }>();
  const [viewTypeTop, setViewTypeTop] = useState<"TopTracks" | "TopAlbums" | "TopArtists">("TopTracks");
  const [viewTypeFavorite, setViewTypeFavorite] = useState<"FavoriteTracks" | "FavoriteAlbums" | "FavoriteArtists">("FavoriteTracks");

  const { getFriend } = useFriends();

  useEffect(() => {
  }, [friendId]);

  const { data } = useQuery({
    queryKey: ["friends", friendId],
    queryFn: async () => {
      return await getFriend(friendId as string);
    },
    enabled: !!friendId,
  });

  return (
    <div className="min-h-screen bg-gray-900 text-white">
      <div className="flex justify-between items-center p-4">
      </div>
      <div className="flex flex-col items-center p-4">
        <Avatar className="w-24 h-24">
          <AvatarImage
            src={data?.profilePicUrl || "https://github.com/shadcn.png"}
          />
          <AvatarFallback>Friend</AvatarFallback>
        </Avatar>
        <h2 className="text-xl font-bold mt-6">
        {data?.nickname}'s Profile
        </h2>
      </div>

      <div id="Top Tracks/Artists/Albums" className="px-4">
        <h2 className="text-lg font-bold mt-6 mb-2">Top</h2>
        <DropdownMenu>
          <DropdownMenuTrigger className="text-white bg-gray-800 p-2 rounded-md">
            {viewTypeTop === "TopTracks" ? "Tracks" : viewTypeTop === "TopAlbums" ? "Albums" : "Artists"}
          </DropdownMenuTrigger>
          <DropdownMenuContent className="bg-gray-800 text-white">
            <DropdownMenuItem onSelect={() => setViewTypeTop("TopTracks")}>
              Tracks
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={() => setViewTypeTop("TopAlbums")}>
              Albums
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={() => setViewTypeTop("TopArtists")}>
              Artists
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
        {data ? <FriendTopCarousel type={viewTypeTop} friend={data} /> : []}
      </div>
      
      <div id="Favorite Tracks/Artists/Albums" className="px-4">
        <h2 className="text-lg font-bold mt-6 mb-2">Favorites</h2>
        <DropdownMenu>
          <DropdownMenuTrigger className="text-white bg-gray-800 p-2 rounded-md">
            {viewTypeFavorite === "FavoriteTracks" ? "Tracks" : viewTypeFavorite === "FavoriteAlbums" ? "Albums" : "Artists"}
          </DropdownMenuTrigger>
          <DropdownMenuContent className="bg-gray-800 text-white">
            <DropdownMenuItem onSelect={() => setViewTypeFavorite("FavoriteTracks")}>
              Tracks
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={() => setViewTypeFavorite("FavoriteAlbums")}>
              Albums
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={() => setViewTypeFavorite("FavoriteArtists")}>
              Artists
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
        {data ? <FriendFavoritesCarousel type={viewTypeFavorite} friend={data} /> : []}
      </div>
      <NavBar />
    </div>
  );
};

export default FriendProfilePage;
