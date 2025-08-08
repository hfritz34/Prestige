import React, { useState } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import useRating from '@/hooks/useRating';
import useProfile from '@/hooks/useProfile';
import BeliComparisonUI from './BeliComparisonUI';

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

// Beli-style binary search state
interface BinarySearchState {
  sortedList: ComparisonItem[];
  currentMid: number; // used as linear scan index
}

const RatingModal: React.FC<RatingModalProps> = ({ isOpen, onClose, item, onComplete }) => {
  const { getUserRatings, submitComparison } = useRating();
  const { getTopTracks, getTopAlbums, getTopArtists } = useProfile();
  
  const [step, setStep] = useState<RatingStep>('partition');
  const [selectedPartition, setSelectedPartition] = useState<'loved' | 'liked' | 'disliked' | null>(null);
  const [binarySearchState, setBinarySearchState] = useState<BinarySearchState | null>(null);
  const [currentComparison, setCurrentComparison] = useState<{ item: ComparisonItem; number: number; total: number } | null>(null);

  // Helper function to get full item details with images
  const getItemDetails = async (itemId: string, itemType: 'track' | 'album' | 'artist') => {
    try {
      let items = [];
      switch (itemType) {
        case 'track':
          items = await getTopTracks();
          break;
        case 'album':
          items = await getTopAlbums();
          break;
        case 'artist':
          items = await getTopArtists();
          break;
      }

      // Find the item in the user's collection
      const foundItem = items.find(item => {
        if (itemType === 'track') return item.track.id === itemId;
        if (itemType === 'album') return item.album.id === itemId;
        if (itemType === 'artist') return item.artist.id === itemId;
        return false;
      });

      if (foundItem) {
        if (itemType === 'track') {
          return {
            name: foundItem.track.name,
            subtitle: `${foundItem.track.artists.map((a: any) => a.name).join(', ')} • ${foundItem.track.album.name}`,
            imageUrl: foundItem.track.album.images[0]?.url
          };
        }
        if (itemType === 'album') {
          return {
            name: foundItem.album.name,
            subtitle: foundItem.album.artists.map((a: any) => a.name).join(', '),
            imageUrl: foundItem.album.images[0]?.url
          };
        }
        if (itemType === 'artist') {
          return {
            name: foundItem.artist.name,
            subtitle: 'Artist',
            imageUrl: foundItem.artist.images[0]?.url
          };
        }
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
        setStep('partition');
        const partitionMidpoint = {
          'loved': 8.5,
          'liked': 5.5,
          'disliked': 2.5
        };
        onComplete(item?.id || '', partition, partitionMidpoint[partition]);
        return;
      }
      
      // For tracks, only compare with tracks from the same album when possible
      let filteredRatings = existingRatings;
      if (item?.type === 'track' && item?.albumId) {
        const sameAlbum = existingRatings.filter((r: any) => r.albumId === item.albumId);
        filteredRatings = sameAlbum.length > 0 ? sameAlbum : existingRatings;
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
      
      // If no items in this partition, fall back to all available ratings for this type
      const candidateRatings = partitionItems.length > 0 ? partitionItems : filteredRatings;
      
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

        const sortedItems: ComparisonItem[] = itemsWithDetails
          .sort((a, b) => b.personalScore - a.personalScore);
        
        // Initialize binary search
        const linearState: BinarySearchState = {
          sortedList: sortedItems,
          currentMid: 0
        };

        setBinarySearchState(linearState);
        setCurrentComparison({
          item: sortedItems[0],
          number: 1,
          total: sortedItems.length
        });
        setStep('comparison');
      } else {
        // No items to compare against
        // If user loved it and there are no items in this section yet, give a perfect 10
        const defaultScore = partition === 'loved' ? 10 : (range.min + range.max) / 2;
        onComplete(item!.id, partition, Math.round(defaultScore * 10) / 10);
        handleClose();
      }
    } catch (error) {
      console.error('Error fetching ratings for comparison:', error);
      // Fall back to default scores (10-point scale)
      const defaultScore = partition === 'loved' ? 8.5 : partition === 'liked' ? 5.5 : 2.5;
      onComplete(item!.id, partition, defaultScore);
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

    // Linear scan through the ranked list to determine exact position
    const newState = { ...binarySearchState };
    const currentIndex = newState.currentMid;

    if (userPrefersNewItem) {
      // Insert before the current index
      const insertPosition = currentIndex;
      const totalItems = newState.sortedList.length + 1;
      const normalizedPosition = insertPosition / (totalItems - 1);
      const finalScore = Math.max(0, Math.min(10, 10 * (1 - normalizedPosition)));
      const roundedScore = Math.round(finalScore * 10) / 10;

      onComplete(item.id, selectedPartition!, roundedScore);
      handleClose();
      return;
    } else {
      // Keep scanning downward
      const nextIndex = currentIndex + 1;
      if (nextIndex >= newState.sortedList.length) {
        // New item is worse than all others; insert at end
        const insertPosition = newState.sortedList.length; // end position
        const totalItems = newState.sortedList.length + 1;
        const normalizedPosition = insertPosition / (totalItems - 1);
        const finalScore = Math.max(0, Math.min(10, 10 * (1 - normalizedPosition)));
        const roundedScore = Math.round(finalScore * 10) / 10;

        onComplete(item.id, selectedPartition!, roundedScore);
        handleClose();
        return;
      }

      newState.currentMid = nextIndex;
      setBinarySearchState(newState);
      setCurrentComparison({
        item: newState.sortedList[newState.currentMid],
        number: currentComparison.number + 1,
        total: newState.sortedList.length
      });
    }
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
    <Dialog open={isOpen} onOpenChange={handleClose}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>
            {step === 'partition' ? 'How did you feel about this?' : 'Compare with your other ratings'}
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-6">
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