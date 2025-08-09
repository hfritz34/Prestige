import React, { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Star } from 'lucide-react';
import RatingModal from '@/pages/rating/components/RatingModal';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import useProfile from '@/hooks/useProfile';
import useRating from '@/hooks/useRating';

interface RateButtonProps {
  itemId: string;
  itemName: string;
  itemSubtitle: string;
  itemType: 'track' | 'album' | 'artist';
  itemImageUrl?: string;
  albumId?: string;
  className?: string;
  size?: 'sm' | 'default' | 'lg';
  variant?: 'default' | 'outline' | 'ghost';
}

const RateButton: React.FC<RateButtonProps> = ({
  itemId,
  itemName,
  itemSubtitle,
  itemType,
  itemImageUrl,
  albumId,
  className = '',
  size = 'default',
  variant = 'default'
}) => {
  const [isRatingModalOpen, setIsRatingModalOpen] = useState(false);
  const { getTopTracks, getTopAlbums, getTopArtists } = useProfile();
  const { saveRating } = useRating();
  const queryClient = useQueryClient();

  // Fetch data for rating comparisons
  const { data: topTracks } = useQuery({
    queryKey: ['topTracks'],
    queryFn: getTopTracks,
    enabled: isRatingModalOpen && itemType === 'track'
  });

  const { data: topAlbums } = useQuery({
    queryKey: ['topAlbums'],
    queryFn: getTopAlbums,
    enabled: isRatingModalOpen && itemType === 'album'
  });

  const { data: topArtists } = useQuery({
    queryKey: ['topArtists'],
    queryFn: getTopArtists,
    enabled: isRatingModalOpen && itemType === 'artist'
  });

  const handleRatingComplete = async (itemId: string, _partition: 'loved' | 'liked' | 'disliked', finalScore: number) => {
    try {
      const getCategoryId = (_partition: 'loved' | 'liked' | 'disliked', score: number) => {
        if (score >= 7) return 1;
        if (score >= 4) return 2;
        return 3;
      };

      const categoryId = getCategoryId(_partition, finalScore);
      await saveRating(itemType, itemId, finalScore, categoryId);
      
      setIsRatingModalOpen(false);
      
      // Invalidate rating queries
      await queryClient.invalidateQueries({ queryKey: ['ratings'] });
    } catch (error) {
      console.error('Error completing rating:', error);
    }
  };

  return (
    <>
      <Button
        variant={variant}
        size={size}
        onClick={() => setIsRatingModalOpen(true)}
        className={`flex items-center gap-2 ${className}`}
      >
        <Star className="w-4 h-4" />
        Rate
      </Button>

      <RatingModal
        isOpen={isRatingModalOpen}
        onClose={() => setIsRatingModalOpen(false)}
        item={{
          id: itemId,
          name: itemName,
          subtitle: itemSubtitle,
          type: itemType,
          imageUrl: itemImageUrl,
          albumId
        }}
        onComplete={handleRatingComplete}
        topTracks={topTracks || []}
        topAlbums={topAlbums || []}
        topArtists={topArtists || []}
      />
    </>
  );
};

export default RateButton;