import React, { useState } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import useRating from '@/hooks/useRating';
import BeliComparisonUI from './BeliComparisonUI';

interface RatingItem {
  id: string;
  name: string;
  subtitle: string;
  imageUrl?: string;
  type: 'track' | 'album' | 'artist';
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
  low: number;
  high: number;
  currentMid: number;
}

const RatingModal: React.FC<RatingModalProps> = ({ isOpen, onClose, item, onComplete }) => {
  const { getUserRatings, submitComparison } = useRating();
  
  const [step, setStep] = useState<RatingStep>('partition');
  const [selectedPartition, setSelectedPartition] = useState<'loved' | 'liked' | 'disliked' | null>(null);
  const [binarySearchState, setBinarySearchState] = useState<BinarySearchState | null>(null);
  const [currentComparison, setCurrentComparison] = useState<{ item: ComparisonItem; number: number; total: number } | null>(null);

  const handlePartitionSelect = async (partition: 'loved' | 'liked' | 'disliked') => {
    setSelectedPartition(partition);
    
    try {
      // Fetch existing ratings for this item type to compare against
      const existingRatings = await getUserRatings(item?.type || 'track');
      
      // Filter ratings based on partition score range (10-point scale)
      const getPartitionRange = (partition: 'loved' | 'liked' | 'disliked') => {
        switch (partition) {
          case 'loved': return { min: 7, max: 10 };
          case 'liked': return { min: 4, max: 6.9 };
          case 'disliked': return { min: 0, max: 3.9 };
        }
      };
      
      const range = getPartitionRange(partition);
      const partitionItems = existingRatings.filter(rating => 
        rating.personalScore !== undefined && 
        rating.personalScore >= range.min && 
        rating.personalScore <= range.max
      );
      
      if (partitionItems.length > 0) {
        // Create sorted list for binary search (highest to lowest score)
        const sortedItems: ComparisonItem[] = partitionItems
          .map(rating => ({
            id: rating.itemId,
            name: `${item?.type} ${rating.itemId.substring(0, 8)}...`, // Abbreviated for now
            subtitle: `Score: ${rating.personalScore?.toFixed(1)}`,
            type: item?.type || 'track',
            personalScore: rating.personalScore || 0,
            imageUrl: undefined // We'd need to fetch this from Spotify
          }))
          .sort((a, b) => b.personalScore - a.personalScore);
        
        // Initialize binary search
        const binaryState: BinarySearchState = {
          sortedList: sortedItems,
          low: 0,
          high: sortedItems.length - 1,
          currentMid: Math.floor(sortedItems.length / 2)
        };
        
        setBinarySearchState(binaryState);
        setCurrentComparison({
          item: sortedItems[binaryState.currentMid],
          number: 1,
          total: Math.ceil(Math.log2(sortedItems.length + 1)) // Maximum comparisons needed
        });
        setStep('comparison');
      } else {
        // No items to compare against, assign default score for partition
        const defaultScore = (range.min + range.max) / 2;
        onComplete(item!.id, partition, defaultScore);
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
    
    // Perform binary search step
    const newState = { ...binarySearchState };
    
    if (userPrefersNewItem) {
      // New item is better - search in upper half
      newState.high = newState.currentMid - 1;
    } else {
      // Comparison item is better - search in lower half  
      newState.low = newState.currentMid + 1;
    }
    
    // Check if binary search is complete
    if (newState.low > newState.high) {
      // Binary search complete - calculate final position and score
      let insertPosition;
      
      if (userPrefersNewItem) {
        insertPosition = newState.currentMid;
      } else {
        insertPosition = newState.currentMid + 1;
      }
      
      // Calculate score based on position in sorted list
      const totalItems = newState.sortedList.length + 1; // +1 for new item
      const normalizedPosition = insertPosition / (totalItems - 1);
      
      // Convert to 10-point scale, with higher positions getting higher scores
      const finalScore = Math.max(0, Math.min(10, 10 * (1 - normalizedPosition)));
      const roundedScore = Math.round(finalScore * 10) / 10;
      
      onComplete(item.id, selectedPartition!, roundedScore);
      handleClose();
      return;
    }
    
    // Continue binary search
    newState.currentMid = Math.floor((newState.low + newState.high) / 2);
    setBinarySearchState(newState);
    
    setCurrentComparison({
      item: newState.sortedList[newState.currentMid],
      number: currentComparison.number + 1,
      total: currentComparison.total
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