import { useQueryClient } from '@tanstack/react-query';
import useHttp from './useHttp';
import useRating, { type RatingResponse } from './useRating';
import type { ItemType } from './useCollection';
import type { TrackResponse, AlbumResponse, ArtistResponse } from './useSpotify';

export type CatalogRating = {
  rating: Omit<RatingResponse, 'personalScore'> & { personalScore?: number | null };
  track: TrackResponse | null;
  album: AlbumResponse | null;
  artist: ArtistResponse | null;
};
export type MusicItem = { id: string; name: string; subtitle: string; imageUrl?: string; type: ItemType; albumId?: string };
export function musicItem(metadata: { track?: TrackResponse | null; album?: AlbumResponse | null; artist?: ArtistResponse | null }): MusicItem | null {
  if (metadata.track) {
    const t = metadata.track;
    return { id: t.id, name: t.name, subtitle: `${t.artists.map(a => a.name).join(', ')} • ${t.album.name}`,
      imageUrl: t.album.images[0]?.url, type: 'track', albumId: t.album.id };
  }
  if (metadata.album) {
    const a = metadata.album;
    return { id: a.id, name: a.name, subtitle: a.artists.map(a => a.name).join(', '), imageUrl: a.images[0]?.url, type: 'album' };
  }
  if (metadata.artist) {
    const a = metadata.artist;
    return { id: a.id, name: a.name, subtitle: 'Artist', imageUrl: a.images[0]?.url, type: 'artist' };
  }
  return null;
}

export default function useCatalogRatings() {
  const http = useHttp();
  const ratings = useRating();
  const client = useQueryClient();
  const list = async (type: ItemType) => {
    const result: CatalogRating[] = [];
    for (let skip = 0; ; skip += 100) {
      const page = await http.getMany<CatalogRating>(`api/catalog/ratings/${type}?skip=${skip}&take=100`);
      result.push(...page);
      if (page.length < 100) return result;
    }
  };
  const refresh = () => Promise.all(['catalogRatings', 'ratingCollection', 'ratings', 'collection', 'favorite',
    'users', 'pinnedItems', 'albumTracks', 'artistAlbums'].map(key => client.invalidateQueries({ queryKey: [key] })));
  const save = async (item: MusicItem, categoryId: number, insertionIndex: number) => {
    // Song positions are one-based within an album category; album/artist insertion indexes are zero-based.
    await ratings.saveRating(item.type, item.id, insertionIndex + (item.type === 'track' ? 1 : 0), categoryId);
    await refresh();
  };
  const remove = async (item: MusicItem) => {
    await ratings.deleteRating(item.type, item.id);
    await refresh();
  };
  return { list, save, remove };
}
