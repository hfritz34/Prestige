import { SearchBar } from "./components/SearchBar";
import { Separator } from "@/components/ui/separator";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { SearchType } from "@/hooks/useSpotify";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import useProfile, { FavoritesResponse, UserAlbumResponse, UserArtistResponse, UserTrackResponse } from "@/hooks/useProfile";
import TrackSearchCard from "@/components/search/TrackSearchCard";
import AlbumSearchCard from "@/components/search/AlbumSearchCard";
import ArtistSearchCard from "@/components/search/ArtistSearchCard";
import { Button } from "@/components/ui/button";
import { useNavigate } from "react-router-dom";
import useUser from "@/hooks/useUsers";
import { ScrollArea } from "@/components/ui/scroll-area";


const AddFavoritesPage : React.FC = () => {
    const [favoriteType, setFavoriteType] = useState("track");
    const profileHook = useProfile();
    const userHook = useUser();
    const queryClient = useQueryClient();
    const nav = useNavigate();

    const FavoritesMutation = (type : string) => {
        type = type.concat("s");
        return useMutation({
        mutationKey: ['users', 'favorites', type],
        mutationFn: async (id : string) => {
            return await profileHook.patchFavorites(type, id);
        },
        onSuccess: () => {
            queryClient.invalidateQueries(
                {
                    queryKey: ['users', 'favorites', type]
                }
            );
            }
        });
    };

    const FavoritesQuery = (type : string) => {
        type = type.concat("s");
        return useQuery({
            queryKey: ['users', 'favorites', type],
            queryFn: async () => {
                return await profileHook.getFavorites(type);
            }
        });
    };

    const data = FavoritesQuery(favoriteType).data as FavoritesResponse;

    const onClickQuery = FavoritesMutation(favoriteType);

    const handleCompleteSetup = () => {
        userHook.patchIsSetup(true);
        nav("/home");
    }

    return (
        <div className="w-screen h-screen flex flex-col items-center overflow-hidden">
            <Tabs defaultValue="track" className="h-1/10 pt-8 grow-0">
                <TabsList className="px-2" >
                    <TabsTrigger value="track" onClick={() => setFavoriteType("track")}>Songs</TabsTrigger>
                    <Separator orientation="vertical" className="mx-2"/>
                    <TabsTrigger value="album" onClick={() => setFavoriteType("album")}>Albums</TabsTrigger>
                    <Separator orientation="vertical" className="mx-2"/>
                    <TabsTrigger value="artist" onClick={() => setFavoriteType("artist")}>Artists</TabsTrigger>
                </TabsList>
            </Tabs>
            <div className="w-screen p-4">
                <div className="flex justify-between items-center">
                        <p className="font-semibold">Current Favorites</p>
                        <Button className="self-end" onClick={() => handleCompleteSetup()}>Complete Setup</Button>
                </div>
                <ScrollArea className="w-screen h-48">
                    {
                        data ? (
                            favoriteType === "track" ? (
                                (data as unknown as UserTrackResponse[]).map(userTrack => <TrackSearchCard key={userTrack.track.id} track={userTrack.track} onClick={() => onClickQuery.mutate(userTrack.track.id)}/>)
                            ) : favoriteType === "album" ? (
                                (data as unknown as UserAlbumResponse[]).map(userAlbum => <AlbumSearchCard key={userAlbum.album.id} album={userAlbum.album} onClick={() => onClickQuery.mutate(userAlbum.album.id)}/>)
                            ) : favoriteType === "artist" ? (
                                (data as unknown as UserArtistResponse[]).map(userArtist => <ArtistSearchCard key={userArtist.artist.id} artist={userArtist.artist} onClick={() => onClickQuery.mutate(userArtist.artist.id)}/>)
                            ) : []
                        ) : []
                    }
                </ScrollArea>
            </div>
            <div className="grow min-h-0">
                <SearchBar searchType={favoriteType as SearchType} 
                onClickQuery={onClickQuery} />
            </div>
        </div>
    );
}

export default AddFavoritesPage;