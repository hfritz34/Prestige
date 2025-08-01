import React, { useState } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import useRating from '@/hooks/useRating';

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

const RatingModal: React.FC<RatingModalProps> = ({ isOpen, onClose, item, onComplete }) => {
  const { getUserRatings, submitComparison } = useRating();
  
  const [step, setStep] = useState<RatingStep>('partition');
  const [selectedPartition, setSelectedPartition] = useState<'loved' | 'liked' | 'disliked' | null>(null);
  const [comparisonItems, setComparisonItems] = useState<ComparisonItem[]>([]);
  const [currentComparisonIndex, setCurrentComparisonIndex] = useState(0);
  const [tentativeScore, setTentativeScore] = useState(0);

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
      
      // Sort by score for proper binary search insertion
      partitionItems.sort((a, b) => (b.personalScore || 0) - (a.personalScore || 0));
      
      if (partitionItems.length > 0) {
        // Use up to 5 items for comparison to avoid fatigue
        const itemsToCompare = partitionItems.slice(0, Math.min(5, partitionItems.length));
        
        const comparisonItems: ComparisonItem[] = itemsToCompare.map(rating => ({
          id: rating.itemId,
          name: `${item?.type} ${rating.itemId.substring(0, 8)}...`, // Abbreviated for now
          subtitle: `Score: ${rating.personalScore?.toFixed(1)}`,
          type: item?.type || 'track',
          personalScore: rating.personalScore || 0
        }));
        
        setComparisonItems(comparisonItems);
        setCurrentComparisonIndex(0);
        
        // Start with middle score of partition range
        setTentativeScore(Math.floor((range.min + range.max) / 2));
        setStep('comparison');
      } else {
        // No items to compare against, assign default score for partition
        const defaultScore = Math.floor((range.min + range.max) / 2);
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

  const handleComparison = async (liked: boolean) => {
    const currentItem = comparisonItems[currentComparisonIndex];
    
    try {
      // Submit comparison to backend
      const comparisonRequest = {
        itemId1: item!.id,
        itemId2: currentItem.id,
        itemType: item!.type,
        winnerId: liked ? item!.id : currentItem.id
      };
      
      const result = await submitComparison(comparisonRequest);
      
      if (result.updatedScore !== undefined) {
        setTentativeScore(result.updatedScore);
      } else {
        // Implement Beli-style binary search positioning (10-point scale)
        if (liked) {
          // New item is better than current item - should be ranked higher
          const increment = Math.max(0.1, (10 - currentItem.personalScore) / (comparisonItems.length + 1));
          setTentativeScore(Math.min(10, currentItem.personalScore + increment));
        } else {
          // Current item is better - new item should be ranked lower
          const decrement = Math.max(0.1, currentItem.personalScore / (comparisonItems.length + 1));
          setTentativeScore(Math.max(0, currentItem.personalScore - decrement));
        }
      }

      if (currentComparisonIndex < comparisonItems.length - 1) {
        setCurrentComparisonIndex(currentComparisonIndex + 1);
      } else {
        // Comparison complete - calculate final position-based score
        const finalScore = calculatePositionBasedScore();
        onComplete(item!.id, selectedPartition!, finalScore);
        handleClose();
      }
    } catch (error) {
      console.error('Error submitting comparison:', error);
      // Continue with local Beli-style calculation (10-point scale)
      if (liked) {
        const increment = Math.max(0.1, (10 - currentItem.personalScore) / (comparisonItems.length + 1));
        setTentativeScore(Math.min(10, currentItem.personalScore + increment));
      } else {
        const decrement = Math.max(0.1, currentItem.personalScore / (comparisonItems.length + 1));
        setTentativeScore(Math.max(0, currentItem.personalScore - decrement));
      }

      if (currentComparisonIndex < comparisonItems.length - 1) {
        setCurrentComparisonIndex(currentComparisonIndex + 1);
      } else {
        const finalScore = calculatePositionBasedScore();
        onComplete(item!.id, selectedPartition!, finalScore);
        handleClose();
      }
    }
  };

  const calculatePositionBasedScore = () => {
    // Beli-style position-based scoring
    // Create a virtual ranked list including the new item
    const allScores = [...comparisonItems.map(item => item.personalScore), tentativeScore];
    allScores.sort((a, b) => b - a); // Sort descending
    
    const position = allScores.indexOf(tentativeScore);
    const totalCount = allScores.length;
    
    // Convert position to score: score = 10 * (position from top) / (total count - 1)
    // But we want higher positions to have higher scores, so invert it
    const normalizedScore = totalCount === 1 ? 10 : 10 * (totalCount - 1 - position) / (totalCount - 1);
    
    return Math.max(0, Math.min(10, Math.round(normalizedScore * 10) / 10));
  };

  const handleClose = () => {
    setStep('partition');
    setSelectedPartition(null);
    setComparisonItems([]);
    setCurrentComparisonIndex(0);
    setTentativeScore(0);
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

          {step === 'comparison' && currentComparisonIndex < comparisonItems.length && (
            <div className="space-y-4">
              <p className="text-center text-gray-600">
                Did you like <strong>{item.name}</strong> more than this?
              </p>
              <p className="text-center text-sm text-gray-500">
                ({currentComparisonIndex + 1} of {comparisonItems.length})
              </p>
              
              <Card className="border-2 border-blue-200">
                <CardContent className="p-4">
                  <div className="flex items-center space-x-4">
                    <div className="w-12 h-12 rounded-md bg-gray-200 flex-shrink-0 flex items-center justify-center">
                      <svg className="w-4 h-4 text-gray-400" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M4 3a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V5a2 2 0 00-2-2H4zm12 12H4l4-8 3 6 2-4 3 6z" clipRule="evenodd" />
                      </svg>
                    </div>
                    <div className="flex-1">
                      <h4 className="font-medium">{comparisonItems[currentComparisonIndex].name}</h4>
                      <p className="text-sm text-gray-600">{comparisonItems[currentComparisonIndex].subtitle}</p>
                    </div>
                  </div>
                </CardContent>
              </Card>

              <div className="flex space-x-4">
                <Button 
                  variant="outline" 
                  className="flex-1 h-12 border-red-300 hover:bg-red-50"
                  onClick={() => handleComparison(false)}
                >
                  No, I liked the comparison more
                </Button>
                <Button 
                  variant="outline"
                  className="flex-1 h-12 border-green-300 hover:bg-green-50"
                  onClick={() => handleComparison(true)}
                >
                  Yes, I liked "{item.name}" more
                </Button>
              </div>
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
};

export default RatingModal;