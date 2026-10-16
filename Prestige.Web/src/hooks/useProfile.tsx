import { useAccount } from '@/auth/AccountProvider';
import useCollection, { collectionType } from './useCollection';
import { usesCatalogCollections } from '@/config/catalog';
import useHttp from "./useHttp";
import { AlbumResponse, ArtistResponse, TrackResponse } from "./useSpotify";

export type TopTrackResponse = {
    trackId: string;    
    trackName: string;
    albumName: string;
    artistName: string;
    totalTime: number;
    imageUrl: string;
    prestigeTier?: string;
};

export type TopAlbumResponse = {
    albumId: string;
    albumName: string;
    artistName: string;
    totalTime: number;
    imageUrl: string;
    prestigeTier?: string;
};

export type TopArtistResponse = {
    artistId: string;
    artistName: string;
    totalTime: number;
    imageUrl: string;
    prestigeTier?: string;
};

export type RecentlyPlayedResponse = {
    trackName: string;
    artistName: string;
    imageUrl: string;
    id: string;
};

export type RecentlyPlayedAlbumResponse = {
    albumName: string;
    artistName: string;
    imageUrl: string;
    id: string;
};

export type RecentlyPlayedArtistResponse = {
    artistName: string;
    imageUrl: string;
    id: string;
};

export type CollectionRating = {
    score: number | null;
    rankWithinAlbum: number | null;
};

export type UserTrackResponse = {
    track: TrackResponse,
    userId: string,
    totalTime: number,
    hasListeningHistory?: boolean,
    rating?: CollectionRating | null,
    isFavorite?: boolean,
    isPinned?: boolean,
    prestigeTier?: string
}
  
export type UserAlbumResponse = {
    album: AlbumResponse,
    userId: string,
    totalTime: number,
    hasListeningHistory?: boolean,
    rating?: CollectionRating | null,
    isFavorite?: boolean,
    isPinned?: boolean,
    prestigeTier?: string
}
  
export type UserArtistResponse = {
    artist: ArtistResponse,
    userId: string,
    totalTime: number,
    hasListeningHistory?: boolean,
    rating?: CollectionRating | null,
    isFavorite?: boolean,
    isPinned?: boolean,
    prestigeTier?: string
}
  
export type FavoritesResponse = UserTrackResponse[] | UserAlbumResponse[] | UserArtistResponse[];

const useProfile = () => {
    const collection = useCollection();
    const { getMany, patch, getOne } = useHttp();
    const baseEndpoint = "profiles";

    const user = useAccount();
    const userId = user.id;

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

    const getRecentlyPlayedAlbums = async (): Promise<RecentlyPlayedAlbumResponse[]> => {
        return await getMany<RecentlyPlayedAlbumResponse>(`${baseEndpoint}/${userId}/recently-played-albums`);
    };

    const getRecentlyPlayedArtists = async (): Promise<RecentlyPlayedArtistResponse[]> => {
        return await getMany<RecentlyPlayedArtistResponse>(`${baseEndpoint}/${userId}/recently-played-artists`);
    };

    const patchFavorites = async (type: string, itemId: string): Promise<void> => {
        if (!usesCatalogCollections) {
            await patch<FavoritesResponse, Record<string, never>>({}, `profiles/${userId}/favorites/${type}/${itemId}`);
            return;
        }
        const itemType = collectionType(type);
        const current = await collection.get(itemType, itemId);
        await collection.update(itemType, itemId, { isFavorite: !current?.isFavorite });
    };

    const getFavorites = async (type: string): Promise<FavoritesResponse> => {
        if (!usesCatalogCollections) return getOne<FavoritesResponse>(`profiles/${userId}/favorites/${type}`);
        const cards = collection.asCards(await collection.list({ itemType: collectionType(type), favoritesOnly: true }));
        return cards[`${collectionType(type)}s`];
    };

    return { getTopTracks, getTopAlbums, getTopArtists, patchFavorites, getFavorites , getRecentlyPlayed, getRecentlyPlayedAlbums, getRecentlyPlayedArtists };
};


export default useProfile;