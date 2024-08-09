import { useAuth0 } from "@auth0/auth0-react";
import useHttp from "./useHttp";
import { AlbumResponse, ArtistResponse, TrackResponse } from "./useSpotify";

export type TopTrackResponse = {
    trackId: string;    
    trackName: string;
    albumName: string;
    artistName: string;
    totalTime: number;
    imageUrl: string;
};

export type TopAlbumResponse = {
    albumId: string;
    albumName: string;
    artistName: string;
    totalTime: number;
    imageUrl: string;
};

export type TopArtistResponse = {
    artistId: string;
    artistName: string;
    totalTime: number;
    imageUrl: string;
};

export type RecentlyPlayedResponse = {
    trackName: string;
    artistName: string;
    imageUrl: string;
    id: string;
};

export type UserTrackResponse = {
    track: TrackResponse,
    userId: string,
    totalTime: number
}
  
export type UserAlbumResponse = {
    album: AlbumResponse,
    userId: string,
    totalTime: number
}
  
export type UserArtistResponse = {
    artist: ArtistResponse,
    userId: string,
    totalTime: number
}
  
export type FavoritesResponse = {
    Tracks: UserTrackResponse[], Albums: never, Artists: never
} | {
    Tracks: never, Albums: UserAlbumResponse[], Artists: never
} | {
    Tracks: never, Albums: never, Artists: UserArtistResponse[]
};

const useProfile = () => {
    const { getMany, patch, getOne } = useHttp();
    const baseEndpoint = "profiles";

    const { user } = useAuth0();
    const userId = user?.sub?.split("|").pop();

    const getTopTracks = async (): Promise<UserTrackResponse[]> => {
        return await getMany<UserTrackResponse>(`${baseEndpoint}/${userId}/top/tracks`);
    };

    const getTopAlbums = async (): Promise<UserAlbumResponse[]> => {
        return await getMany<UserAlbumResponse>(`${baseEndpoint}/${userId}/top/albums`);
    };

    const getTopArtists = async (): Promise<UserArtistResponse[]> => {
        return await getMany<UserArtistResponse>(`${baseEndpoint}/${userId}/top/artists`);

    };
    
    const getRecentlyPlayed = async (): Promise<RecentlyPlayedResponse[]> => {
        return await getMany<RecentlyPlayedResponse>(`${baseEndpoint}/${userId}/recently-played`);
    };

    const patchFavorites = async (type : string, trackId : string) : Promise<FavoritesResponse> => patch<FavoritesResponse, Record<string, never>>({}, `profiles/${userId}/favorites/${type}/${trackId}`);

    const getFavorites = async (type : string) : Promise<FavoritesResponse> => getOne<FavoritesResponse>(`profiles/${userId}/favorites/${type}`);

    return { getTopTracks, getTopAlbums, getTopArtists, patchFavorites, getFavorites , getRecentlyPlayed };
};


export default useProfile;