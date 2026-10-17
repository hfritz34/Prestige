import { usesCatalogCollections } from '@/config/catalog';
import useHttp from './useHttp';
import useSpotify, { type SearchResponse, type SearchType } from './useSpotify';

export default function useMusicSearch() {
  const http = useHttp();
  const spotify = useSpotify();
  const search = async (query: string, itemType: SearchType): Promise<SearchResponse> => {
    const term = query.trim();
    if (!term) return { tracks: [], albums: [], artists: [] };
    if (usesCatalogCollections) {
      const params = new URLSearchParams({ query: term, itemType, take: '20' });
      return http.getOne<SearchResponse>(`api/catalog/search?${params}`);
    }
    if (itemType !== 'all') return spotify.search(term, itemType);
    const [tracks, albums, artists] = await Promise.all([
      spotify.search(term, 'track'), spotify.search(term, 'album'), spotify.search(term, 'artist'),
    ]);
    return { tracks: tracks.tracks, albums: albums.albums, artists: artists.artists };
  };
  return { search };
}
