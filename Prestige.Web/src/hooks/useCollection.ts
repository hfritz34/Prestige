import { isAxiosError } from 'axios';
import { useQueryClient } from '@tanstack/react-query';
import { useAccount } from '@/auth/AccountProvider';
import useHttp from './useHttp';
import type { TrackResponse, AlbumResponse, ArtistResponse } from './useSpotify';
import type { UserTrackResponse, UserAlbumResponse, UserArtistResponse, CollectionRating } from './useProfile';

export type ItemType = 'track' | 'album' | 'artist';
export type CollectionEntry = {
  itemType: ItemType;
  itemId: string;
  isFavorite: boolean;
  isPinned: boolean;
  savedAt: string;
  updatedAt: string;
  rating: CollectionRating | null;
  track: TrackResponse | null;
  album: AlbumResponse | null;
  artist: ArtistResponse | null;
};

export const collectionKey = (type: ItemType, id: string) => ['collection', type, id] as const;
export function collectionType(type: string): ItemType {
  const normalized = type.replace(/s$/, '');
  if (normalized !== 'track' && normalized !== 'album' && normalized !== 'artist') throw new Error('Unknown music type');
  return normalized;
}

export default function useCollection() {
  const http = useHttp();
  const client = useQueryClient();
  const account = useAccount();
  const route = (type: ItemType, id: string) => `api/collection/${type}/${encodeURIComponent(id)}`;

  const get = async (type: ItemType, id: string): Promise<CollectionEntry | null> => {
    try { return await http.getOne<CollectionEntry>(route(type, id)); }
    catch (error) {
      if (isAxiosError(error) && error.response?.status === 404) return null;
      throw error;
    }
  };

  const update = async (type: ItemType, id: string, flags: { isFavorite?: boolean; isPinned?: boolean }) => {
    const entry = await http.patch<CollectionEntry, typeof flags>(flags, route(type, id));
    await client.cancelQueries({ queryKey: collectionKey(type, id) });
    client.setQueryData(collectionKey(type, id), entry);
    await Promise.all([
      client.invalidateQueries({ queryKey: ['favorite'] }),
      client.invalidateQueries({ queryKey: ['users', 'favorites'] }),
      client.invalidateQueries({ queryKey: ['pinnedItems'] }),
      client.invalidateQueries({ queryKey: ['albumTracks'] }),
      client.invalidateQueries({ queryKey: ['artistAlbums'] }),
    ]);
    return entry;
  };

  const list = async (options: { itemType?: ItemType; favoritesOnly?: boolean; pinnedOnly?: boolean }) => {
    const entries: CollectionEntry[] = [];
    // The API caps each page. Do not silently drop favorites/pins after the first 100 items.
    for (let skip = 0; ; skip += 100) {
      const params = new URLSearchParams({ skip: String(skip), take: '100' });
      Object.entries(options).forEach(([key, value]) => params.set(key, String(value)));
      const page = await http.getMany<CollectionEntry>(`api/collection?${params}`);
      entries.push(...page);
      if (page.length < 100) return entries;
    }
  };

  const asCards = (entries: CollectionEntry[]) => {
    const tracks: UserTrackResponse[] = [];
    const albums: UserAlbumResponse[] = [];
    const artists: UserArtistResponse[] = [];
    for (const entry of entries) {
      // Zero is only a compatibility value for older card props; history is explicitly absent.
      const common = { userId: account.id, totalTime: 0, hasListeningHistory: false,
        isFavorite: entry.isFavorite, isPinned: entry.isPinned, rating: entry.rating };
      if (entry.track) tracks.push({ ...common, track: entry.track });
      if (entry.album) albums.push({ ...common, album: entry.album });
      if (entry.artist) artists.push({ ...common, artist: entry.artist });
    }
    return { tracks, albums, artists };
  };
  return { get, update, list, asCards };
}
