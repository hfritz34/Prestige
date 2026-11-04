import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useDebounce } from 'use-debounce';
import { useAccount } from '@/auth/AccountProvider';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import NavBar from '@/components/navigation/NavBar';
import useCatalogFriends, { type CatalogFriend, type FriendAction } from '@/hooks/useCatalogFriends';
import useUsers from '@/hooks/useUsers';

export default function CatalogFriendsPage() {
  const account = useAccount();
  const service = useCatalogFriends();
  const users = useUsers();
  const [view, setView] = useState('friends');
  const [term, setTerm] = useState('');
  const [query] = useDebounce(term.trim(), 400);
  const friends = useQuery({ queryKey: ['catalogFriends', 'accepted'], queryFn: service.list, retry: false });
  const incoming = useQuery({ queryKey: ['catalogFriends', 'incoming'], queryFn: service.incoming, retry: false });
  const outgoing = useQuery({ queryKey: ['catalogFriends', 'outgoing'], queryFn: service.outgoing, retry: false });
  const search = useQuery({ queryKey: ['search', 'users', query], queryFn: () => users.searchUsers(query), enabled: !!query, retry: false });
  const mutation = useMutation({ mutationFn: service.change });
  const change = (id: string, action: FriendAction) => mutation.mutate({ id, action });
  const listsReady = friends.isSuccess && incoming.isSuccess && outgoing.isSuccess;
  const current = view === 'friends' ? friends : view === 'incoming' ? incoming : outgoing;
  const person = (friend: CatalogFriend) => <div className="flex items-center gap-3 min-w-0">
    {friend.profilePicUrl && <img src={friend.profilePicUrl} alt="" className="w-12 h-12 rounded-full" />}
    <div><p className="font-bold">{friend.nickname || friend.name || 'Music fan'}</p><p className="text-sm text-gray-400">{friend.name}</p></div>
  </div>;
  return <div className="min-h-screen bg-gray-900 text-white">
    <main className="max-w-4xl mx-auto p-4 pb-24 space-y-4">
      <h1 className="text-2xl font-bold">Friends</h1>
      <p>Friends can see your ratings, album track rankings, favorites, and pins.</p>
      <label className="block" htmlFor="catalog-user-search">Find friends</label>
      <Input id="catalog-user-search" value={term} maxLength={120} onChange={event => setTerm(event.target.value)} placeholder="Search by name or nickname" />
      {query && <section aria-label="People search results" className="space-y-3">
        {search.isError ? <p role="alert">Couldn’t search for people. <Button onClick={() => void search.refetch()}>Try search again</Button></p>
          : search.isFetching ? <p role="status">Searching...</p>
          : search.data?.filter(user => user.id !== account.id).length === 0 ? <p>No people found.</p>
          : search.data?.filter(user => user.id !== account.id).map(user => {
            const accepted = friends.data?.some(friend => friend.id === user.id);
            const requested = outgoing.data?.some(friend => friend.id === user.id);
            const received = incoming.data?.some(friend => friend.id === user.id);
            return <div key={user.id} className="flex items-center justify-between gap-3 p-3 border border-gray-700 rounded-lg">
              <span>{user.nickName || user.name || 'Music fan'}</span>
              {accepted ? <Link to={`/friends/${encodeURIComponent(user.id)}`} className="underline">View profile</Link>
                : received ? <Button onClick={() => setView('incoming')}>View request</Button>
                : <Button disabled={!listsReady || mutation.isPending || requested} onClick={() => change(user.id, 'send')}>
                  {requested ? 'Request sent' : 'Send request'}
                </Button>}
            </div>;
          })}
      </section>}
      {!listsReady && (friends.isError || incoming.isError || outgoing.isError) && <p role="alert">
        Couldn’t load friendship status. <Button onClick={() => { void friends.refetch(); void incoming.refetch(); void outgoing.refetch(); }}>Retry friendship status</Button>
      </p>}
      {mutation.isError && <p role="alert">Couldn’t update this friendship. Please try again.</p>}
      {mutation.isSuccess && <p role="status">{mutation.variables.action === 'send' ? 'Friend request sent.' : 'Friendship updated.'}</p>}
      <Tabs value={view} onValueChange={setView}><TabsList className="grid grid-cols-3">
        <TabsTrigger value="friends">My friends</TabsTrigger>
        <TabsTrigger value="incoming">Incoming{incoming.data?.length ? ` (${incoming.data.length})` : ''}</TabsTrigger>
        <TabsTrigger value="outgoing">Sent{outgoing.data?.length ? ` (${outgoing.data.length})` : ''}</TabsTrigger>
      </TabsList></Tabs>
      {current.isError ? <p role="alert">Couldn’t load this list. <Button onClick={() => void current.refetch()}>Retry list</Button></p>
        : current.isPending ? <p role="status">Loading friends...</p>
        : <section aria-label={view === 'friends' ? 'My friends' : view === 'incoming' ? 'Incoming requests' : 'Sent requests'} className="space-y-3">
          {current.data.length === 0 && <p>{view === 'friends' ? 'No friends yet. Search for someone to send a request.' : 'No pending requests.'}</p>}
          {current.data.map(friend => <div key={friend.id} className="flex flex-wrap items-center justify-between gap-3 p-3 border border-gray-700 rounded-lg">
            {person(friend)}
            <div className="flex gap-2">
              {view === 'friends' ? <>
                <Link className="underline p-2" to={`/friends/${encodeURIComponent(friend.id)}`}>View profile</Link>
                <Button variant="outline" disabled={mutation.isPending} onClick={() => change(friend.id, 'remove')}>Remove friend</Button>
              </> : view === 'incoming' ? <>
                <Button disabled={mutation.isPending} onClick={() => change(friend.id, 'accept')}>Accept</Button>
                <Button variant="outline" disabled={mutation.isPending} onClick={() => change(friend.id, 'decline')}>Decline</Button>
              </> : <Button variant="outline" disabled={mutation.isPending} onClick={() => change(friend.id, 'cancel')}>Cancel request</Button>}
            </div>
          </div>)}
        </section>}
    </main><NavBar />
  </div>;
}
