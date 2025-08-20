import React, { useState } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import useRating from '@/hooks/useRating';
import BeliComparisonUI from './BeliComparisonUI';
import { UserAlbumResponse, UserArtistResponse, UserTrackResponse } from '@/hooks/useProfile';

interface RatingItem {
  id: string;
  name: string;
  subtitle: string;
  imageUrl?: string;
  type: 'track' | 'album' | 'artist';
  albumId?: string;
}

interface RatingModalProps {
  isOpen: boolean;
  onClose: () => void;
  item: RatingItem | null;
  onComplete: (itemId: string, partition: 'loved' | 'liked' | 'disliked', finalScore: number) => void;
  topTracks?: UserTrackResponse[];
  topAlbums?: UserAlbumResponse[];
  topArtists?: UserArtistResponse[];
}

interface PartitionChoice {
  key: 'loved' | 'liked' | 'disliked';
  label: string;
  color: string;
  bgColor: string;
  description: string;
}

const partitions: PartitionChoice[] = [
  {
    key: 'loved',
    label: 'I loved it',
    color: 'text-green-700',
    bgColor: 'bg-green-100 hover:bg-green-200 border-green-300',
    description: 'This is amazing!'
  },
  {
    key: 'liked',
    label: 'It was okay',
    color: 'text-yellow-700',
    bgColor: 'bg-yellow-100 hover:bg-yellow-200 border-yellow-300',
    description: 'Not bad, decent'
  },
  {
    key: 'disliked',
    label: "I didn't like it",
    color: 'text-red-700',
    bgColor: 'bg-red-100 hover:bg-red-200 border-red-300',
    description: 'Not for me'
  }
];

type RatingStep = 'partition' | 'comparison';

interface ComparisonItem extends RatingItem {
  personalScore: number;
}

// Binary search state for proper insertion logic
interface BinarySearchState {
  sortedList: ComparisonItem[];
  leftIndex: number;
  rightIndex: number;
  currentMid: number;
}

const RatingModal: React.FC<RatingModalProps> = ({ isOpen, onClose, item, onComplete, topTracks = [], topAlbums = [], topArtists = [] }) => {
  const { getUserRatings, submitComparison } = useRating();
  
  const [step, setStep] = useState<RatingStep>('partition');
  const [selectedPartition, setSelectedPartition] = useState<'loved' | 'liked' | 'disliked' | null>(null);
  const [binarySearchState, setBinarySearchState] = useState<BinarySearchState | null>(null);
  const [currentComparison, setCurrentComparison] = useState<{ item: ComparisonItem; number: number; total: number } | null>(null);

  // Helper function to get full item details with images
  const getItemDetails = async (itemId: string, itemType: 'track' | 'album' | 'artist') => {
    try {
      let items: Array<any> = [];
      if (itemType === 'track') items = topTracks;
      if (itemType === 'album') items = topAlbums;
      if (itemType === 'artist') items = topArtists;

      // Find the item in the user's collection first (for performance)
      const foundItem = items.find((it: any) => {
        if (itemType === 'track') return it?.track?.id === itemId || it?.trackId === itemId;
        if (itemType === 'album') return it?.album?.id === itemId || it?.albumId === itemId;
        if (itemType === 'artist') return it?.artist?.id === itemId || it?.artistId === itemId;
        return false;
      });

      if (foundItem) {
        if (itemType === 'track') {
          const track = (foundItem as any).track ?? foundItem;
          return {
            name: track.name ?? foundItem.trackName,
            subtitle: `${(track.artists ?? foundItem.artists ?? []).map((a: any) => a.name).join(', ') || foundItem.artistName} • ${(track.album?.name) ?? foundItem.albumName ?? ''}`,
            imageUrl: track.album?.images?.[0]?.url ?? foundItem.imageUrl
          };
        }
        if (itemType === 'album') {
          const album = (foundItem as any).album ?? foundItem;
          return {
            name: album.name ?? foundItem.albumName,
            subtitle: (album.artists ?? []).map((a: any) => a.name).join(', ') || foundItem.artistName,
            imageUrl: album.images?.[0]?.url ?? foundItem.imageUrl
          };
        }
        if (itemType === 'artist') {
          const artist = (foundItem as any).artist ?? foundItem;
          return {
            name: artist.name ?? foundItem.artistName,
            subtitle: 'Artist',
            imageUrl: artist.images?.[0]?.url ?? foundItem.imageUrl
          };
        }
      }

      // If not found in local arrays, fetch from API
      console.log(`Fetching missing item details from API for ${itemType} ${itemId}`);
      const response = await fetch(`/api/library/item/${itemType}/${itemId}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('access_token')}`,
          'Content-Type': 'application/json'
        }
      });

      if (response.ok) {
        const apiItem = await response.json();
        let subtitle = '';
        if (apiItem.artists && apiItem.artists.length > 0) {
          subtitle = apiItem.artists.join(', ');
          if (apiItem.albumName) {
            subtitle += ` • ${apiItem.albumName}`;
          }
        } else if (apiItem.albumName) {
          subtitle = apiItem.albumName;
        }

        return {
          name: apiItem.name || `${itemType} ${itemId.substring(0, 8)}...`,
          subtitle: subtitle || (itemType === 'artist' ? 'Artist' : 'Unknown'),
          imageUrl: apiItem.imageUrl
        };
      }
    } catch (error) {
      console.error('Error fetching item details:', error);
    }

    // Fallback if item not found
    return {
      name: `${itemType} ${itemId.substring(0, 8)}...`,
      subtitle: 'Unknown',
      imageUrl: undefined
    };
  };

  const handlePartitionSelect = async (partition: 'loved' | 'liked' | 'disliked') => {
    setSelectedPartition(partition);
    
    try {
      // Fetch existing ratings for this item type to compare against
      const existingRatings = await getUserRatings(item?.type || 'track');
      
      // Ensure existingRatings is an array
      if (!Array.isArray(existingRatings)) {
        console.warn('getUserRatings returned non-array:', existingRatings);
        // Skip comparison phase - no existing ratings to compare against
        // First item gets position 0
        onComplete(item?.id || '', partition, 0);
        return;
      }
      
      // For tracks, only compare with tracks from the same album
      let filteredRatings = existingRatings;
      if (item?.type === 'track' && item?.albumId) {
        filteredRatings = existingRatings.filter((r: any) => r.albumId === item.albumId);
        
        // If no tracks from same album exist, skip comparison phase
        if (filteredRatings.length === 0) {
          // First item in this album category gets position 0
          onComplete(item.id, partition, 0);
          handleClose();
          return;
        }
      }
      
      // Filter ratings based on partition score range (10-point scale)  
      const getPartitionRange = (partition: 'loved' | 'liked' | 'disliked') => {
        // Caps: loved 6.8-10, liked 3.4-6.7, disliked 0-3.3
        switch (partition) {
          case 'loved': return { min: 6.8, max: 10 };
          case 'liked': return { min: 3.4, max: 6.7 };
          case 'disliked': return { min: 0, max: 3.3 };
        }
      };
      
      const range = getPartitionRange(partition);
      const partitionItems = filteredRatings.filter((rating: any) => 
        rating.personalScore !== undefined && 
        rating.personalScore >= range.min && 
        rating.personalScore <= range.max
      );
      
      // If no items in this partition, assign first item position 0 (first in partition)
      if (partitionItems.length === 0) {
        // First item in this partition gets position 0
        onComplete(item?.id || '', partition, 0);
        handleClose();
        return;
      }

      const candidateRatings = partitionItems;
      
      if (candidateRatings.length > 0) {
        // Create sorted list for binary search (highest to lowest score)
        // First, fetch details for all items to get names and images
        const itemsWithDetails = await Promise.all(
          candidateRatings.map(async (rating: any) => {
            const details = await getItemDetails(rating.itemId, item?.type || 'track');
            return {
              id: rating.itemId,
              name: details.name,
              subtitle: details.subtitle,
              type: item?.type || 'track',
              personalScore: rating.personalScore || 0,
              imageUrl: details.imageUrl
            };
          })
        );

        // Filter out any items with missing data (empty names or IDs)
        const validItems = itemsWithDetails.filter(item => 
          item.id && item.id.length > 0 && 
          item.name && item.name !== `${item.type} ${item.id.substring(0, 8)}...`
        );

        const sortedItems: ComparisonItem[] = validItems
          .sort((a, b) => b.personalScore - a.personalScore);
        
        // Check if we have valid items after filtering
        if (sortedItems.length === 0) {
          // No valid items to compare against -> first item in category gets position 0
          onComplete(item!.id, partition, 0);
          handleClose();
          return;
        }
        
        // Initialize binary search - start from the MIDDLE
        // If even count, favor higher value (lower index)
        const leftIndex = 0;
        const rightIndex = sortedItems.length - 1;
        const startIndex = Math.floor((leftIndex + rightIndex) / 2);
        const insertionState: BinarySearchState = {
          sortedList: sortedItems,
          leftIndex: leftIndex,
          rightIndex: rightIndex,
          currentMid: startIndex
        };

        setBinarySearchState(insertionState);
        setCurrentComparison({
          item: sortedItems[startIndex],
          number: 1,
          total: sortedItems.length // Maximum comparisons needed
        });
        setStep('comparison');
      } else {
        // No items in this category yet -> first item gets position 0
        onComplete(item!.id, partition, 0);
        handleClose();
      }
    } catch (error) {
      console.error('Error fetching ratings for comparison:', error);
      // Fall back to position 0 for first item
      onComplete(item!.id, partition, 0);
      handleClose();
    }
  };

  const handleComparison = async (selectedItemId: string) => {
    if (!binarySearchState || !currentComparison || !item) return;
    
    const userPrefersNewItem = selectedItemId === item.id;

    try {
      // Submit comparison to backend
      const comparisonRequest = {
        itemId1: item.id,
        itemId2: currentComparison.item.id,
        itemType: item.type,
        winnerId: selectedItemId
      };
      
      await submitComparison(comparisonRequest);
    } catch (error) {
      console.error('Error submitting comparison:', error);
    }

    // Proper binary search logic
    let newLeftIndex = binarySearchState.leftIndex;
    let newRightIndex = binarySearchState.rightIndex;

    if (userPrefersNewItem) {
      // New item is BETTER than current comparison
      // Search in the upper half (items with higher scores, lower indices)
      newRightIndex = binarySearchState.currentMid - 1;
    } else {
      // Current comparison item is BETTER than new item
      // Search in the lower half (items with lower scores, higher indices)
      newLeftIndex = binarySearchState.currentMid + 1;
    }

    // Check if binary search is complete
    if (newLeftIndex > newRightIndex) {
      // Binary search complete - insert at newLeftIndex position
      onComplete(item.id, selectedPartition!, newLeftIndex);
      handleClose();
      return;
    }

    // Continue binary search - calculate new middle
    const newMidIndex = Math.floor((newLeftIndex + newRightIndex) / 2);
    const nextItem = binarySearchState.sortedList[newMidIndex];

    setBinarySearchState({
      ...binarySearchState,
      leftIndex: newLeftIndex,
      rightIndex: newRightIndex,
      currentMid: newMidIndex
    });

    setCurrentComparison({
      item: nextItem,
      number: currentComparison.number + 1,
      total: Math.ceil(Math.log2(binarySearchState.sortedList.length)) + 1 // More accurate total for binary search
    });
  };


  const handleClose = () => {
    setStep('partition');
    setSelectedPartition(null);
    setBinarySearchState(null);
    setCurrentComparison(null);
    onClose();
  };

  if (!item) return null;

  return (
    <Dialog open={isOpen} onOpenChange={(open) => { if (!open) handleClose(); }}>
      <DialogContent className="max-w-2xl" aria-describedby="rating-description">
        <DialogHeader>
          <DialogTitle>
            {step === 'partition' ? 'How did you feel about this?' : 'Compare with your other ratings'}
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-6" id="rating-modal-content">
          <p id="rating-description" className="sr-only">Rate and compare your selection to determine its position.</p>
          {/* Current Item Display */}
          <Card className="border-2">
            <CardContent className="p-4">
              <div className="flex items-center space-x-4">
                <div className="w-16 h-16 rounded-md overflow-hidden bg-gray-200 flex-shrink-0">
                  {item.imageUrl ? (
                    <img 
                      src={item.imageUrl} 
                      alt={item.name}
                      className="w-full h-full object-cover"
                    />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center">
                      <svg className="w-6 h-6 text-gray-400" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M4 3a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V5a2 2 0 00-2-2H4zm12 12H4l4-8 3 6 2-4 3 6z" clipRule="evenodd" />
                      </svg>
                    </div>
                  )}
                </div>
                <div className="flex-1">
                  <h3 className="font-semibold text-lg">{item.name}</h3>
                  <p className="text-gray-600">{item.subtitle}</p>
                  <p className="text-sm text-gray-500 capitalize">{item.type}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          {step === 'partition' && (
            <div className="space-y-3">
              <p className="text-center text-gray-600 mb-4">Choose how you feel about this {item.type}:</p>
              {partitions.map((partition) => (
                <Button
                  key={partition.key}
                  variant="outline"
                  className={`w-full h-16 ${partition.bgColor} ${partition.color} border-2 text-left justify-start`}
                  onClick={() => handlePartitionSelect(partition.key)}
                >
                  <div className="flex items-center space-x-4">
                    <div className={`w-4 h-4 rounded-full ${partition.key === 'loved' ? 'bg-green-500' : partition.key === 'liked' ? 'bg-yellow-500' : 'bg-red-500'}`} />
                    <div>
                      <div className="font-semibold">{partition.label}</div>
                      <div className="text-sm opacity-80">{partition.description}</div>
                    </div>
                  </div>
                </Button>
              ))}
            </div>
          )}

          {step === 'comparison' && currentComparison && (
            <BeliComparisonUI
              newItem={{
                id: item.id,
                name: item.name,
                subtitle: item.subtitle,
                imageUrl: item.imageUrl,
                type: item.type
              }}
              comparisonItem={currentComparison.item}
              onSelect={handleComparison}
              comparisonNumber={currentComparison.number}
              totalComparisons={currentComparison.total}
            />
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
};

export default RatingModal;