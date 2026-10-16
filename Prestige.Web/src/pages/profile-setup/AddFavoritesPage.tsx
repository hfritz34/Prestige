import { SearchBar } from "./components/SearchBar";
import { Separator } from "@/components/ui/separator";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { SearchType } from "@/hooks/useSpotify";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import useProfile, { UserAlbumResponse, UserArtistResponse, UserTrackResponse } from "@/hooks/useProfile";
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

    const type = `${favoriteType}s`;
    const favorites = useQuery({
        queryKey: ['users', 'favorites', type],
        queryFn: () => profileHook.getFavorites(type),
    });
    const data = favorites.data;
    const onClickQuery = useMutation({
        mutationFn: (id: string) => profileHook.patchFavorites(type, id),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['users', 'favorites'] }),
    });
    const completeSetup = useMutation({
        mutationFn: () => userHook.patchIsSetup(true),
        onSuccess: () => nav('/home'),
    });
    const busy = onClickQuery.isPending || completeSetup.isPending;
    const toggleFavorite = (id: string) => { if (!busy) onClickQuery.mutate(id); };

    return (
        <div className="w-screen h-screen flex flex-col items-center overflow-hidden">
            <Tabs defaultValue="track" className="h-1/10 pt-8 grow-0">
                <TabsList className="px-2" >
                    <TabsTrigger disabled={busy} value="track" onClick={() => setFavoriteType("track")}>Songs</TabsTrigger>
                    <Separator orientation="vertical" className="mx-2"/>
                    <TabsTrigger disabled={busy} value="album" onClick={() => setFavoriteType("album")}>Albums</TabsTrigger>
                    <Separator orientation="vertical" className="mx-2"/>
                    <TabsTrigger disabled={busy} value="artist" onClick={() => setFavoriteType("artist")}>Artists</TabsTrigger>
                </TabsList>
            </Tabs>
            <div className="w-screen p-4">
                <div className="flex justify-between items-center">
                        <p className="font-semibold">Current Favorites</p>
                        <Button className="self-end" disabled={busy} onClick={() => completeSetup.mutate()}>Complete Setup</Button>
                </div>
                {favorites.isPending && <p role="status">Loading favorites...</p>}
                {favorites.isError && <p role="alert">Couldn’t load favorites. <button onClick={() => void favorites.refetch()}>Try again</button></p>}
                {onClickQuery.isError && <p role="alert">Couldn’t save your favorite. Please try again.</p>}
                {completeSetup.isError && <p role="alert">Couldn’t finish setup. Please try again.</p>}
                <ScrollArea className="w-screen h-48">
                    {
                        data ? (
                            favoriteType === "track" ? (
                                (data as UserTrackResponse[]).map(userTrack => <TrackSearchCard key={userTrack.track.id} track={userTrack.track} onClick={() => toggleFavorite(userTrack.track.id)}/>)
                            ) : favoriteType === "album" ? (
                                (data as UserAlbumResponse[]).map(userAlbum => <AlbumSearchCard key={userAlbum.album.id} album={userAlbum.album} onClick={() => toggleFavorite(userAlbum.album.id)}/>)
                            ) : favoriteType === "artist" ? (
                                (data as UserArtistResponse[]).map(userArtist => <ArtistSearchCard key={userArtist.artist.id} artist={userArtist.artist} onClick={() => toggleFavorite(userArtist.artist.id)}/>)
                            ) : []
                        ) : []
                    }
                </ScrollArea>
            </div>
            <div className="grow min-h-0">
                <SearchBar searchType={favoriteType as SearchType} 
                onClickQuery={onClickQuery} disabled={busy} />
            </div>
        </div>
    );
}

export default AddFavoritesPage;