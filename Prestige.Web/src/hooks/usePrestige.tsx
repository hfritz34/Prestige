import useHttp from "./useHttp";

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

    const getPinnedItems = async (userId: string) => {
      const result = await http.getOne(`prestige/${userId}/pinned`);
      console.log("Pinned items from API:", result);
      return result;
    };

    const getTrackPrestigeTier = (totalTime: number): string => {
      if (totalTime >= 15000 * 60) return "DarkMatter";
      else if (totalTime >= 6000 * 60) return "Opal";
      else if (totalTime >= 3000 * 60) return "Diamond";
      else if (totalTime >= 2200 * 60) return "Jet";
      else if (totalTime >= 1600 * 60) return "Garnet";
      else if (totalTime >= 1200 * 60) return "Sapphire";
      else if (totalTime >= 800 * 60) return "Emerald";
      else if (totalTime >= 500 * 60) return "Gold";
      else if (totalTime >= 300 * 60) return "Peridot";
      else if (totalTime >= 150 * 60) return "Silver";
      else if (totalTime >= 60 * 60) return "Bronze";
      return "";
    };
  
    const getArtistPrestigeTier = (totalTime: number): string => {
      if (totalTime >= 100000 * 60) return "DarkMatter";
      else if (totalTime >= 50000 * 60) return "Opal";
      else if (totalTime >= 25000 * 60) return "Diamond";
      else if (totalTime >= 15000 * 60) return "Jet";
      else if (totalTime >= 10000 * 60) return "Garnet";
      else if (totalTime >= 6000 * 60) return "Sapphire";
      else if (totalTime >= 3000 * 60) return "Emerald";
      else if (totalTime >= 2000 * 60) return "Gold";
      else if (totalTime >= 1200 * 60) return "Peridot";
      else if (totalTime >= 750 * 60) return "Silver";
      else if (totalTime >= 400 * 60) return "Bronze";
      return "";
    };
  
    const getAlbumPrestigeTier = (totalTime: number): string => {
      if (totalTime >= 50000 * 60) return "DarkMatter";
      else if (totalTime >= 30000 * 60) return "Opal";
      else if (totalTime >= 15000 * 60) return "Diamond";
      else if (totalTime >= 10000 * 60) return "Jet";
      else if (totalTime >= 6000 * 60) return "Garnet";
      else if (totalTime >= 4000 * 60) return "Sapphire";
      else if (totalTime >= 2000 * 60) return "Emerald";
      else if (totalTime >= 1000 * 60) return "Gold";
      else if (totalTime >= 500 * 60) return "Peridot";
      else if (totalTime >= 350 * 60) return "Silver";
      else if (totalTime >= 200 * 60) return "Bronze";
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
      getPinnedItems
    };
  };
  
  export default usePrestige;
  