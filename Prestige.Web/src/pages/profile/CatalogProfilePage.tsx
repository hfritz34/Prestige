import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useAccount } from '@/auth/AccountProvider';
import { Button } from '@/components/ui/button';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import NavBar from '@/components/navigation/NavBar';
import CatalogMusicCard from '@/components/catalog/CatalogMusicCard';
import CatalogRatingsList from '@/components/catalog/CatalogRatingsList';
import useCollection, { type CollectionEntry, type ItemType } from '@/hooks/useCollection';
import useCatalogRatings, { musicItem } from '@/hooks/useCatalogRatings';
import useRating from '@/hooks/useRating';

export default function CatalogProfilePage() {
  const account = useAccount();
  const collection = useCollection();
  const catalogRatings = useCatalogRatings();
  const rating = useRating();
  const [view, setView] = useState('showcase');
  const [type, setType] = useState<ItemType>('track');
  const showcase = useQuery({ queryKey: ['collection', 'lists', 'showcase'], queryFn: collection.showcase,
    enabled: view === 'showcase', retry: false });
  const saved = useQuery({ queryKey: ['collection', 'lists', 'saved'], queryFn: () => collection.list({}),
    enabled: view === 'saved', retry: false });
  const ratings = useQuery({ queryKey: ['catalogRatings', type], queryFn: () => catalogRatings.list(type),
    enabled: view === 'ratings', retry: false });
  const categories = useQuery({ queryKey: ['ratingCategories'], queryFn: rating.getRatingCategories,
    enabled: view === 'ratings', retry: false });
  const change = useMutation({ mutationFn: ({ entry, flag }: { entry: CollectionEntry; flag: 'isFavorite' | 'isPinned' }) =>
    collection.update(entry.itemType, entry.itemId, { [flag]: !entry[flag] }) });
  const entries = view === 'showcase' ? showcase : saved;
  const failed = view === 'ratings' ? ratings.isError || categories.isError : entries.isError;
  const loading = view === 'ratings' ? ratings.isPending || categories.isPending : entries.isPending;
  const retry = () => { if (view === 'ratings') { void ratings.refetch(); void categories.refetch(); } else void entries.refetch(); };
  return <div className="min-h-screen bg-gray-900 text-white">
    <main className="max-w-4xl mx-auto p-4 pb-24 space-y-4">
      <div className="flex justify-between gap-4"><h1 className="text-2xl font-bold">Your music</h1><Link className="underline" to="/settings">Settings</Link></div>
      <header className="space-y-2">
        {account.profilePicURL && <img src={account.profilePicURL} alt="" className="w-24 h-24 rounded-full" />}
        <h2 className="text-xl font-bold">{account.nickName || account.name || 'Music fan'}</h2>
        {account.bio && <p>{account.bio}</p>}
        <p>Friends can see your showcase and ratings. Other saved music stays private.</p>
      </header>
      <div className="flex flex-wrap gap-4"><Link className="underline" to="/rating">Find and rate music</Link><Link className="underline" to="/friends">Find friends</Link></div>
      <Tabs value={view} onValueChange={setView}><TabsList>
        <TabsTrigger value="showcase">Showcase</TabsTrigger><TabsTrigger value="ratings">Ratings</TabsTrigger><TabsTrigger value="saved">Saved music</TabsTrigger>
      </TabsList></Tabs>
      {view === 'ratings' && <Tabs value={type} onValueChange={value => setType(value as ItemType)}><TabsList>
        <TabsTrigger value="track">Tracks</TabsTrigger><TabsTrigger value="album">Albums</TabsTrigger><TabsTrigger value="artist">Artists</TabsTrigger>
      </TabsList></Tabs>}
      {view === 'saved' && <p>Pin or favorite saved music to add it to your showcase.</p>}
      {change.isError && <p role="alert">Couldn’t update your showcase. Your last saved choices are still shown. Please try again.</p>}
      {failed ? <p role="alert">Couldn’t load your music. <Button onClick={retry}>Try again</Button></p>
        : loading ? <p role="status">Loading music...</p>
        : view === 'ratings' ? <section aria-label="Your ratings"><CatalogRatingsList ratings={ratings.data ?? []} categories={categories.data ?? []} type={type} /></section>
        : <section aria-label={view === 'showcase' ? 'Your showcase' : 'Your saved music'} className="space-y-3">
          {entries.data?.length === 0 && <p>{view === 'showcase'
            ? 'Your showcase is empty. Pin or favorite an item from Saved music to display it here.'
            : 'No saved music yet. Find music to rate or save, then curate your showcase here.'}</p>}
          {entries.data?.map(entry => <CatalogMusicCard key={`${entry.itemType}-${entry.itemId}`} item={musicItem(entry)}
            label={[entry.isFavorite && 'Favorite', entry.isPinned && 'Pinned'].filter(Boolean).join(' • ')}>
            <div className="flex flex-wrap gap-2">
              <Button variant="outline" aria-pressed={entry.isFavorite} disabled={change.isPending || entries.isFetching}
                onClick={() => change.mutate({ entry, flag: 'isFavorite' })}>{entry.isFavorite ? 'Unfavorite' : 'Favorite'}</Button>
              <Button variant="outline" aria-pressed={entry.isPinned} disabled={change.isPending || entries.isFetching}
                onClick={() => change.mutate({ entry, flag: 'isPinned' })}>{entry.isPinned ? 'Unpin' : 'Pin'}</Button>
            </div>
          </CatalogMusicCard>)}
        </section>}
    </main><NavBar />
  </div>;
}
