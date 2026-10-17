import { useRef, useState } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import useCatalogRatings, { musicItem, type MusicItem } from '@/hooks/useCatalogRatings';
import type { RatingCategoryResponse } from '@/hooks/useRating';

type Placement = { peers: MusicItem[]; left: number; right: number; categoryId: number };
export default function CatalogRatingModal({ item, categories, onClose }: {
  item: MusicItem; categories: RatingCategoryResponse[]; onClose: () => void;
}) {
  const ratings = useCatalogRatings();
  const [placement, setPlacement] = useState<Placement | null>(null);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [retrySave, setRetrySave] = useState<{ categoryId: number; index: number } | null>(null);
  const lock = useRef(false);
  const run = async (action: () => Promise<void>) => {
    if (lock.current) return;
    lock.current = true;
    setPending(true);
    setError(null);
    try { await action(); }
    catch { setError('Couldn’t finish this rating. Please try again.'); }
    finally { lock.current = false; setPending(false); }
  };
  const save = async (categoryId: number, index: number) => {
    setRetrySave({ categoryId, index });
    await ratings.save(item, categoryId, index);
    onClose();
  };
  const chooseCategory = (categoryId: number) => run(async () => {
    if (item.type === 'track' && !item.albumId) throw new Error('Album context is required');
    const existing = await ratings.list(item.type);
    const peers = existing.filter(r => r.rating.itemId !== item.id && r.rating.categoryId === categoryId
      && (item.type !== 'track' || r.rating.albumId === item.albumId))
      .sort((a, b) => (a.rating.position ?? 0) - (b.rating.position ?? 0))
      .map(r => {
        const peer = musicItem(r);
        if (!peer) throw new Error('Comparison metadata is unavailable');
        return peer;
      });
    if (peers.length === 0) await save(categoryId, 0);
    else setPlacement({ peers, left: 0, right: peers.length - 1, categoryId });
  });
  const chooseItem = (preferNew: boolean) => run(async () => {
    if (!placement) return;
    const mid = Math.floor((placement.left + placement.right) / 2);
    const left = preferNew ? placement.left : mid + 1;
    const right = preferNew ? mid - 1 : placement.right;
    if (left > right) await save(placement.categoryId, left);
    else setPlacement({ ...placement, left, right });
  });
  const peer = placement?.peers[Math.floor((placement.left + placement.right) / 2)];
  return <Dialog open onOpenChange={open => { if (!open && !pending) onClose(); }}>
    <DialogContent className="max-w-2xl" aria-describedby="catalog-rating-description">
      <DialogHeader><DialogTitle>{peer ? 'Which do you prefer?' : 'How did you feel about this?'}</DialogTitle></DialogHeader>
      <p id="catalog-rating-description">{item.name}{item.type === 'track' ? ' — ranked within its album' : ''}</p>
      {error && <p role="alert">{error}</p>}
      {pending && <p role="status">{retrySave ? 'Saving your rating...' : 'Loading comparisons...'}</p>}
      {retrySave ? <Button disabled={pending} onClick={() => void run(() => save(retrySave.categoryId, retrySave.index))}>Try saving again</Button>
        : peer ? <div className="grid grid-cols-2 gap-4">
          {[item, peer].map((choice, index) => <Button key={choice.id} variant="outline" className="h-auto flex-col p-4 whitespace-normal"
            disabled={pending} onClick={() => void chooseItem(index === 0)}>
            {choice.imageUrl && <img src={choice.imageUrl} alt="" className="w-24 h-24 object-cover mb-2" />}
            <span>{choice.name}</span><span className="text-sm">{choice.subtitle}</span>
          </Button>)}
        </div> : <div className="space-y-3">{categories.map(category =>
          <Button key={category.id} variant="outline" className="w-full" disabled={pending}
            onClick={() => void chooseCategory(category.id)}>{category.name}</Button>)}</div>}
      <Button variant="ghost" disabled={pending} onClick={onClose}>Cancel</Button>
    </DialogContent>
  </Dialog>;
}
