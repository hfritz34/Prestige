import { useQueryClient } from '@tanstack/react-query';
import useHttp from './useHttp';
import type { CatalogRating } from './useCatalogRatings';
import type { CollectionEntry, ItemType } from './useCollection';

export type CatalogFriend = {
  id: string;
  name: string;
  nickname: string;
  profilePicUrl: string;
  bio: string | null;
  status: 'Pending' | 'Accepted' | 'Declined';
  requestDate: string;
  acceptedDate: string | null;
};
export type FriendAction = 'send' | 'accept' | 'decline' | 'cancel' | 'remove';

export default function useCatalogFriends() {
  const http = useHttp();
  const client = useQueryClient();
  const base = 'api/catalog/friends';
  const profile = (id: string) => `${base}/${encodeURIComponent(id)}`;
  const all = async <T,>(path: string): Promise<T[]> => {
    const result: T[] = [];
    for (let skip = 0; ; skip += 100) {
      const page = await http.getMany<T>(`${path}${path.includes('?') ? '&' : '?'}skip=${skip}&take=100`);
      result.push(...page);
      if (page.length < 100) return result;
    }
  };
  const change = async ({ id, action }: { id: string; action: FriendAction }) => {
    const request = `${base}/requests/${encodeURIComponent(id)}`;
    if (action === 'remove') await http.deleteOne<void>(profile(id));
    else if (action === 'cancel') await http.deleteOne<void>(request);
    else await http.post<CatalogFriend, object>({}, `${request}${action === 'send' ? '' : `/${action}`}`);
    // Clear any cached friend music after access changes, then refresh relationship lists.
    if (action === 'remove') client.removeQueries({ queryKey: ['catalogFriend', id] });
    await client.invalidateQueries({ queryKey: ['catalogFriends'] });
  };
  return {
    list: () => all<CatalogFriend>(base),
    incoming: () => all<CatalogFriend>(`${base}/requests?direction=incoming`),
    outgoing: () => all<CatalogFriend>(`${base}/requests?direction=outgoing`),
    get: (id: string) => http.getOne<CatalogFriend>(profile(id)),
    showcase: (id: string) => all<CollectionEntry>(`${profile(id)}/showcase`),
    ratings: (id: string, type: ItemType) => all<CatalogRating>(`${profile(id)}/ratings/${type}`),
    change,
  };
}
