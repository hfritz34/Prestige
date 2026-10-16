import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { usesCatalogCollections } from '@/config/catalog';
import useCollection, { collectionKey, type ItemType } from './useCollection';

export default function useCollectionPin(type: ItemType, id: string | undefined,
  initialPinned: boolean, legacyToggle: () => Promise<unknown>) {
  const collection = useCollection();
  const client = useQueryClient();
  const [legacyPinned, setLegacyPinned] = useState(initialPinned);
  const entry = useQuery({
    queryKey: collectionKey(type, id ?? ''),
    queryFn: () => collection.get(type, id!),
    enabled: usesCatalogCollections && !!id,
    retry: false,
  });
  const isPinned = usesCatalogCollections ? entry.data?.isPinned ?? false : legacyPinned;
  const mutation = useMutation({
    mutationFn: () => usesCatalogCollections
      ? collection.update(type, id!, { isPinned: !isPinned }) : legacyToggle(),
    onSuccess: () => {
      if (!usesCatalogCollections) {
        setLegacyPinned(!legacyPinned);
        for (const key of ['pinnedItems', 'topTracks', 'topAlbums', 'topArtists']) {
          void client.invalidateQueries({ queryKey: [key] });
        }
      }
    },
  });
  return {
    isPinned,
    pending: mutation.isPending || (usesCatalogCollections && (entry.isPending || entry.isError)),
    error: mutation.isError || (usesCatalogCollections && entry.isError),
    toggle: () => mutation.mutate(),
    retry: () => {
      mutation.reset();
      if (usesCatalogCollections && entry.isError) void entry.refetch();
      else mutation.mutate();
    },
  };
}
