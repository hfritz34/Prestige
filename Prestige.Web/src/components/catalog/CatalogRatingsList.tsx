import { musicItem, type CatalogRating } from '@/hooks/useCatalogRatings';
import type { RatingCategoryResponse } from '@/hooks/useRating';
import type { ItemType } from '@/hooks/useCollection';
import CatalogMusicCard from './CatalogMusicCard';

export default function CatalogRatingsList({ ratings, categories, type }: {
  ratings: CatalogRating[]; categories: RatingCategoryResponse[]; type: ItemType;
}) {
  const card = (entry: CatalogRating) => {
    const r = entry.rating;
    const category = categories.find(category => category.id === r.categoryId)?.name ?? 'Rated';
    const label = type === 'track' ? `${category} • Album rank #${r.rankWithinAlbum ?? '—'}`
      : `${category} • Score: ${r.personalScore?.toFixed(1) ?? '—'}`;
    return <CatalogMusicCard key={`${r.itemType}-${r.itemId}`} item={musicItem(entry)} label={label} />;
  };
  if (!ratings.length) return <p>No ratings here yet.</p>;
  if (type !== 'track') return <div className="space-y-3">{ratings.map(card)}</div>;

  // Group by release ID, not title: distinct releases can share a name. Rank crosses categories within an album.
  const albums = new Map<string, { id: string; title: string; artists: string; entries: CatalogRating[] }>();
  for (const entry of ratings) {
    const album = entry.track?.album;
    const id = entry.rating.albumId || album?.id || `unknown-${entry.rating.itemId}`;
    const group = albums.get(id) ?? { id, title: album?.name ?? 'Album metadata unavailable',
      artists: album?.artists.map(artist => artist.name).join(', ') ?? '', entries: [] };
    group.entries.push(entry);
    albums.set(id, group);
  }
  return <div className="space-y-6">
    <p className="text-sm text-gray-400">Rankings include rated tracks only.</p>
    {[...albums.values()].sort((a, b) => a.title.localeCompare(b.title) || a.id.localeCompare(b.id)).map(album =>
      <section key={album.id} aria-label={`Track rankings for ${album.title}`} className="space-y-3">
        <h2 className="text-lg font-bold">{album.title}</h2>
        {album.artists && <p className="text-sm text-gray-400">{album.artists}</p>}
        {album.entries.sort((a, b) => (a.rating.rankWithinAlbum ?? Infinity) - (b.rating.rankWithinAlbum ?? Infinity)
          || a.rating.itemId.localeCompare(b.rating.itemId)).map(card)}
      </section>)}
  </div>;
}
