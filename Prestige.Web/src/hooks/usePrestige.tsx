const usePrestige = () => {
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
  
    return { getTrackPrestigeTier, getArtistPrestigeTier, getAlbumPrestigeTier };
  };
  
  export default usePrestige;
  