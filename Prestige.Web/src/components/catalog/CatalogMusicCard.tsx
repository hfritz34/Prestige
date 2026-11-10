import type { ReactNode } from 'react';
import type { MusicItem } from '@/hooks/useCatalogRatings';
import placeholderImage from '@/assets/music.jpg';

export default function CatalogMusicCard({ item, label, children }: {
  item: MusicItem | null; label: string; children?: ReactNode;
}) {
  return <article className="flex flex-wrap items-center gap-4 p-3 border border-gray-700 rounded-lg">
    {item && <img src={item.imageUrl || placeholderImage} alt="" className="w-16 h-16 object-cover rounded" onError={event => {
      if (event.currentTarget.getAttribute('src') !== placeholderImage) event.currentTarget.src = placeholderImage;
    }} />}
    <div className="flex-1 min-w-0">
      <h3 className="font-bold break-words">{item?.name ?? 'Catalog metadata unavailable'}</h3>
      {item && <p className="text-sm text-gray-400 break-words">{item.subtitle}</p>}
      {label && <p>{label}</p>}
    </div>
    {children}
  </article>;
}
