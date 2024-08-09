import { useNavigate } from 'react-router-dom';

interface Track {
  trackId: string;
  trackName: string;
  albumName?: string;
  artistName: string;
  totalTime: number;
  imageUrl: string;
}

interface Album {
  albumId: string;
  albumName: string;
  artistName: string;
  totalTime: number;
  imageUrl: string;
}

interface Artist {
  artistId: string;
  artistName: string;
  totalTime: number;
  imageUrl: string;
}

const useRedirectToPrestigePages = () => {
  const navigate = useNavigate();

  const redirectToSongPage = (track: Track) => {
    navigate(`/prestige/song`, { state: track });
  };

  const redirectToAlbumPage = (album: Album) => {
    navigate(`/prestige/album`, { state: album });
  };

  const redirectToArtistPage = (artist: Artist) => {
    navigate(`/prestige/artist`, { state: artist });
  };

  return { redirectToSongPage, redirectToAlbumPage, redirectToArtistPage };
};

export default useRedirectToPrestigePages;
