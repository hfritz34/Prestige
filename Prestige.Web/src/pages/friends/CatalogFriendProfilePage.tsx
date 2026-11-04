import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { isAxiosError } from 'axios';
import NavBar from '@/components/navigation/NavBar';
import { Button } from '@/components/ui/button';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import useCatalogFriends from '@/hooks/useCatalogFriends';
import { musicItem } from '@/hooks/useCatalogRatings';
import type { ItemType } from '@/hooks/useCollection';
import useRating from '@/hooks/useRating';
import placeholderImage from '@/assets/music.jpg';

export default function CatalogFriendProfilePage() {
  const { friendId = '' } = useParams<{ friendId: string }>();
  // Remount local tab selection when navigating between profiles; queries also include the owner.
  return <CatalogFriendProfile key={friendId} friendId={friendId} />;
}

function CatalogFriendProfile({ friendId }: { friendId: string }) {
  const service = useCatalogFriends();
  const rating = useRating();
  const [view, setView] = useState('showcase');
  const [type, setType] = useState<ItemType>('track');
  const profile = useQuery({ queryKey: ['catalogFriend', friendId, 'profile'], queryFn: () => service.get(friendId), enabled: !!friendId, retry: false });
  const showcase = useQuery({ queryKey: ['catalogFriend', friendId, 'showcase'], queryFn: () => service.showcase(friendId), enabled: profile.isSuccess && view === 'showcase', retry: false });
  const ratings = useQuery({ queryKey: ['catalogFriend', friendId, 'ratings', type], queryFn: () => service.ratings(friendId, type), enabled: profile.isSuccess && view === 'ratings', retry: false });
  const categories = useQuery({ queryKey: ['ratingCategories'], queryFn: rating.getRatingCategories, enabled: profile.isSuccess && view === 'ratings', retry: false });
  const unavailable = (error: unknown) => isAxiosError(error) && error.response?.status === 404;
  const denied = unavailable(profile.error) || unavailable(showcase.error) || unavailable(ratings.error);
  const failed = profile.isError || (view === 'showcase' ? showcase.isError : ratings.isError || categories.isError);
  const loading = profile.isPending || (view === 'showcase' ? showcase.isPending : ratings.isPending || categories.isPending);
  const retry = () => { void profile.refetch(); if (view === 'showcase') void showcase.refetch(); else { void ratings.refetch(); void categories.refetch(); } };
  const entries = view === 'showcase'
    ? showcase.data?.map(entry => ({ item: musicItem(entry), label: [entry.isFavorite && 'Favorite', entry.isPinned && 'Pinned'].filter(Boolean).join(' • ') }))
    : ratings.data?.map(entry => {
      const r = entry.rating;
      const category = categories.data?.find(category => category.id === r.categoryId)?.name ?? 'Rated';
      return { item: musicItem(entry), label: type === 'track'
        ? `${category} • Album rank #${r.rankWithinAlbum ?? '—'}`
        : `${category} • Score: ${r.personalScore?.toFixed(1) ?? '—'}` };
    });
  return <div className="min-h-screen bg-gray-900 text-white">
    <main className="max-w-4xl mx-auto p-4 pb-24 space-y-4">
      <Link className="underline" to="/friends">Back to friends</Link>
      {denied ? <p role="alert">This profile is unavailable. An accepted friendship is required.</p>
        : profile.isError ? <p role="alert">Couldn’t load this profile. <Button onClick={retry}>Try again</Button></p>
        : profile.isPending ? <p role="status">Loading profile...</p> : <>
          <header className="space-y-2">
            {profile.data.profilePicUrl && <img src={profile.data.profilePicUrl} alt="" className="w-24 h-24 rounded-full" />}
            <h1 className="text-2xl font-bold">{profile.data.nickname || profile.data.name || 'Music fan'}’s music</h1>
            {profile.data.bio && <p>{profile.data.bio}</p>}
          </header>
          <Tabs value={view} onValueChange={setView}><TabsList>
            <TabsTrigger value="showcase">Showcase</TabsTrigger><TabsTrigger value="ratings">Ratings</TabsTrigger>
          </TabsList></Tabs>
          {view === 'ratings' && <Tabs value={type} onValueChange={value => setType(value as ItemType)}><TabsList>
            <TabsTrigger value="track">Tracks</TabsTrigger><TabsTrigger value="album">Albums</TabsTrigger><TabsTrigger value="artist">Artists</TabsTrigger>
          </TabsList></Tabs>}
          {failed ? <p role="alert">Couldn’t load this music. <Button onClick={retry}>Try again</Button></p>
            : loading ? <p role="status">Loading music...</p>
            : <section aria-label="Friend music" className="space-y-3">
              {entries?.length === 0 && <p>{view === 'showcase' ? 'No favorites or pins yet.' : 'No ratings here yet.'}</p>}
              {entries?.map(({ item, label }, index) => item ? <article key={`${item.type}-${item.id}`} className="flex gap-4 p-3 border border-gray-700 rounded-lg">
                <img src={item.imageUrl || placeholderImage} alt="" className="w-16 h-16 object-cover rounded" onError={event => {
                  if (event.currentTarget.getAttribute('src') !== placeholderImage) event.currentTarget.src = placeholderImage;
                }} />
                <div><h2 className="font-bold">{item.name}</h2><p className="text-sm text-gray-400">{item.subtitle}</p><p>{label}</p></div>
              </article> : <p key={`missing-${index}`} role="status">Catalog metadata is unavailable for this rated item.</p>)}
            </section>}
        </>}
    </main><NavBar />
  </div>;
}
