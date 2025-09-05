import useHttp from "./useHttp";

export type AlbumTrackResponse = {
  trackId: string
  trackName: string
  artists: Array<{id: string, name: string}>
  durationMs: number
  trackNumber: number
  userListeningTime: number
  userRating?: number
  hasUserRating: boolean
  albumRanking?: number
  isPinned: boolean
  isFavorite: boolean
  isFromDatabase: boolean
}

export type AlbumTracksWithRankingsResponse = {
  albumId: string
  totalTracks: number
  ratedTracks: number
  allTracksRated: boolean
  tracks: AlbumTrackResponse[]
}

export type ArtistAlbumResponse = {
  albumId: string
  albumName: string
  artistName: string
  albumImage: string
  albumRatingScore?: number
  totalTime: number
  isPinned: boolean
  isFavorite: boolean
}

export type ArtistAlbumsWithRankingsResponse = {
  artistId: string
  totalAlbums: number
  albums: ArtistAlbumResponse[]
}

const usePrestige = () => {
    const http = useHttp();

    const postUserTrack = async (userId: string, trackId: string, totalTime: number) => {
      return await http.post(
        { trackId, totalTime },
        `prestige/${userId}/tracks`
      );
    };

    const togglePinTrack = async (userId: string, trackId: string) => {
      console.log(`Toggling pin for track ${trackId} for user ${userId}`);
      const result = await http.post({}, `prestige/${userId}/tracks/${trackId}/pin`);
      console.log("Pin track result:", result);
      return result;
    };

    const togglePinAlbum = async (userId: string, albumId: string) => {
      return await http.post({}, `prestige/${userId}/albums/${albumId}/pin`);
    };

    const togglePinArtist = async (userId: string, artistId: string) => {
      return await http.post({}, `prestige/${userId}/artists/${artistId}/pin`);
    };

    const getPinnedItems = async (userId: string): Promise<{tracks: any[], albums: any[], artists: any[]}> => {
      const result = await http.getOne<{tracks: any[], albums: any[], artists: any[]}>(`prestige/${userId}/pinned`);
      console.log("Pinned items from API:", result);
      return result;
    };

    const getAlbumTracksWithRankings = async (userId: string, albumId: string): Promise<AlbumTracksWithRankingsResponse> => {
      return await http.getOne<AlbumTracksWithRankingsResponse>(`prestige/${userId}/albums/${albumId}/tracks`);
    };

    const getArtistAlbumsWithUserActivity = async (userId: string, artistId: string): Promise<ArtistAlbumsWithRankingsResponse> => {
      return await http.getOne<ArtistAlbumsWithRankingsResponse>(`prestige/${userId}/artists/${artistId}/albums`);
    };

    // Deprecated: These functions are kept for backwards compatibility but should not be used
    // The backend now calculates and returns the correct prestige tier based on environment settings
    const getTrackPrestigeTier = (_totalTime: number): string => {
      console.warn('getTrackPrestigeTier is deprecated. Use prestigeTier from API response instead.');
      return "";
    };
  
    const getArtistPrestigeTier = (_totalTime: number): string => {
      console.warn('getArtistPrestigeTier is deprecated. Use prestigeTier from API response instead.');
      return "";
    };
  
    const getAlbumPrestigeTier = (_totalTime: number): string => {
      console.warn('getAlbumPrestigeTier is deprecated. Use prestigeTier from API response instead.');
      return "";
    };
  
    return { 
      getTrackPrestigeTier, 
      getArtistPrestigeTier, 
      getAlbumPrestigeTier,
      postUserTrack,
      togglePinTrack,
      togglePinAlbum,
      togglePinArtist,
      getPinnedItems,
      getAlbumTracksWithRankings,
      getArtistAlbumsWithUserActivity
    };
  };
  
  export default usePrestige;
  