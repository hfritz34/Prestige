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

    const getTrackPrestigeTier = (totalTime: number): string => {
      if (totalTime >= 200 * 60) return "DarkMatter";
      else if (totalTime >= 120 * 60) return "Opal";
      else if (totalTime >= 60 * 60) return "Diamond";
      else if (totalTime >= 40 * 60) return "Jet";
      else if (totalTime >= 30 * 60) return "Garnet";
      else if (totalTime >= 20 * 60) return "Sapphire";
      else if (totalTime >= 10 * 60) return "Emerald";
      else if (totalTime >= 6 * 60) return "Gold";
      else if (totalTime >= 3 * 60) return "Peridot";
      else if (totalTime >= 2 * 60) return "Silver";
      else if (totalTime >= 1 * 60) return "Bronze";
      return "";
    };
  
    const getArtistPrestigeTier = (totalTime: number): string => {
      if (totalTime >= 1500 * 60) return "DarkMatter";
      else if (totalTime >= 1000 * 60) return "Opal";
      else if (totalTime >= 600 * 60) return "Diamond";
      else if (totalTime >= 400 * 60) return "Jet";
      else if (totalTime >= 250 * 60) return "Garnet";
      else if (totalTime >= 150 * 60) return "Sapphire";
      else if (totalTime >= 75 * 60) return "Emerald";
      else if (totalTime >= 40 * 60) return "Gold";
      else if (totalTime >= 20 * 60) return "Peridot";
      else if (totalTime >= 10 * 60) return "Silver";
      else if (totalTime >= 5 * 60) return "Bronze";
      return "";
    };
  
    const getAlbumPrestigeTier = (totalTime: number): string => {
      if (totalTime >= 750 * 60) return "DarkMatter";
      else if (totalTime >= 500 * 60) return "Opal";
      else if (totalTime >= 250 * 60) return "Diamond";
      else if (totalTime >= 150 * 60) return "Jet";
      else if (totalTime >= 100 * 60) return "Garnet";
      else if (totalTime >= 60 * 60) return "Sapphire";
      else if (totalTime >= 30 * 60) return "Emerald";
      else if (totalTime >= 15 * 60) return "Gold";
      else if (totalTime >= 8 * 60) return "Peridot";
      else if (totalTime >= 4 * 60) return "Silver";
      else if (totalTime >= 2 * 60) return "Bronze";
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
  