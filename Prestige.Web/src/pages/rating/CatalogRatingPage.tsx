import { useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useDebounce } from 'use-debounce';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import NavBar from '@/components/navigation/NavBar';
import useCollection, { type ItemType } from '@/hooks/useCollection';
import useCatalogRatings, { musicItem, type MusicItem } from '@/hooks/useCatalogRatings';
import useMusicSearch from '@/hooks/useMusicSearch';
import useRating from '@/hooks/useRating';
import RatingItemCard from './components/RatingItemCard';
import CatalogRatingModal from './components/CatalogRatingModal';

export default function CatalogRatingPage() {
  const [type, setType] = useState<ItemType>('track');
  const [view, setView] = useState('saved');
  const [term, setTerm] = useState('');
  const [query] = useDebounce(term.trim(), 400);
  const [selected, setSelected] = useState<MusicItem | null>(null);
  const collection = useCollection();
  const catalogRatings = useCatalogRatings();
  const music = useMusicSearch();
  const rating = useRating();
  const categories = useQuery({ queryKey: ['ratingCategories'], queryFn: rating.getRatingCategories });
  const rated = useQuery({ queryKey: ['catalogRatings', type], queryFn: () => catalogRatings.list(type), retry: false });
  const saved = useQuery({ queryKey: ['ratingCollection', type], queryFn: () => collection.list({ itemType: type }), retry: false });
  const search = useQuery({ queryKey: ['music-search', type, query], queryFn: () => music.search(query, type),
    enabled: query.length > 0, staleTime: 60_000, retry: false });
  const removal = useMutation({ mutationFn: catalogRatings.remove });
  const searchItems = search.data ? [...search.data.tracks.map(track => musicItem({ track })),
    ...search.data.albums.map(album => musicItem({ album })), ...search.data.artists.map(artist => musicItem({ artist }))] : [];
  const items = (query ? searchItems : view === 'rated' ? rated.data?.map(musicItem) : saved.data?.map(musicItem))
    ?.filter((item): item is MusicItem => !!item) ?? [];
  const failed = rated.isError || categories.isError || (query ? search.isError : view === 'saved' && saved.isError);
  const loading = rated.isPending || categories.isPending || (query ? search.isFetching : view === 'saved' && saved.isPending);
  const retry = () => { void rated.refetch(); void categories.refetch(); if (query) void search.refetch(); else void saved.refetch(); };
  const showRating = (id: string) => {
    const r = rated.data?.find(entry => entry.rating.itemId === id)?.rating;
    if (!r) return '';
    const category = categories.data?.find(category => category.id === r.categoryId)?.name ?? 'Rated';
    return type === 'track' ? `${category} • Album rank #${r.rankWithinAlbum ?? '—'}` : `${category} • Score: ${r.personalScore?.toFixed(1) ?? '—'}`;
  };
  return <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main className="max-w-7xl mx-auto px-4 pb-20">
      <h1 className="text-3xl font-bold py-6">Rate Your Music</h1>
      <Tabs value={type} onValueChange={value => setType(value as ItemType)}>
        <TabsList className="grid grid-cols-3">{(['track', 'album', 'artist'] as const).map(type =>
          <TabsTrigger key={type} value={type}>{type === 'track' ? 'Tracks' : type === 'album' ? 'Albums' : 'Artists'}</TabsTrigger>)}</TabsList>
      </Tabs>
      <Input className="my-4" aria-label="Search music to rate" placeholder={`Search ${type}s to rate...`} maxLength={120}
        value={term} onChange={event => setTerm(event.target.value)} />
      {!query && <Tabs value={view} onValueChange={setView}><TabsList>
        <TabsTrigger value="saved">Saved music</TabsTrigger><TabsTrigger value="rated">Your ratings</TabsTrigger>
      </TabsList></Tabs>}
      {failed ? <p role="alert">Couldn’t load your music or ratings. <Button onClick={retry}>Try again</Button></p>
        : loading ? <p role="status">Loading music...</p>
        : <section className="space-y-3 mt-4" aria-label="Music to rate">
          {items.length === 0 && <p>{query ? 'No matching music found.' : 'Search for music to start rating.'}</p>}
          {view === 'rated' && !query && rated.data?.some(item => !musicItem(item)) && <p role="alert">Some rated music has unavailable catalog metadata.</p>}
          {items.map(item => <div key={`${item.type}-${item.id}`} className="flex items-center gap-4">
            <div className="flex-1"><RatingItemCard {...item} subtitle={[item.subtitle, showRating(item.id)].filter(Boolean).join(' • ')}
              onRate={() => { if (!removal.isPending) setSelected(item); }} /></div>
            {rated.data?.some(r => r.rating.itemId === item.id) && <Button variant="destructive" disabled={removal.isPending}
              aria-label={`Remove rating for ${item.name}`} onClick={() => removal.mutate(item)}>Remove</Button>}
          </div>)}
        </section>}
      {removal.isError && <p role="alert">Couldn’t remove this rating. Please try again.</p>}
    </main>
    {selected && categories.data && <CatalogRatingModal key={`${selected.type}-${selected.id}`} item={selected}
      categories={[...categories.data].sort((a, b) => a.displayOrder - b.displayOrder)} onClose={() => setSelected(null)} />}
    <NavBar />
  </div>;
}
