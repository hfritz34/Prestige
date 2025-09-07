import React, { useState } from "react";
import useFriends, { ItemComparisonResponse, Friend } from "@/hooks/useFriends";
import { useQuery } from "@tanstack/react-query";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import CompareModal from "@/components/ui/compare-modal";

type PrestigeGridCardProps = {
  imageUrl?: string;
  images?: Array<{url: string}>;
  name: string;
  subtitle?: string;
  totalTime: number;
  rank?: number;
  onClick: () => void;
  type?: 'track' | 'album' | 'artist';
  ratingScore?: number;
  albumPosition?: number; // For tracks: position within album (1st, 2nd, 3rd best)
  id?: string;
  artists?: Array<{name: string}>;
  isPinned?: boolean;
  prestigeTier?: string;
};

const PrestigeGridCard: React.FC<PrestigeGridCardProps> = ({
  imageUrl,
  images,
  name,
  subtitle,
  totalTime,
  rank,
  onClick,
  type = 'track',
  ratingScore,
  albumPosition,
  id: _id,
  artists,
  isPinned: _isPinned,
  prestigeTier
}) => {
  // Removed deprecated functions - now using prestigeTier from props
  const { 
    compareTrackWithFriend, 
    compareAlbumWithFriend, 
    compareArtistWithFriend,
    getFriendsWhoListenedToTrack,
    getFriendsWhoListenedToAlbum,
    getFriendsWhoListenedToArtist
  } = useFriends();
  const [showCompareModal, setShowCompareModal] = useState(false);
  const [comparison, setComparison] = useState<ItemComparisonResponse | null>(null);
  const [isCompareLoading, setIsCompareLoading] = useState(false);

  const friendsQuery = useQuery<Friend[]>({
    queryKey: ['friends-who-listened', type, _id],
    queryFn: async (): Promise<Friend[]> => {
      if (!_id) return [];
      
      try {
        switch (type) {
          case 'track':
            return await getFriendsWhoListenedToTrack(_id);
          case 'album':
            return await getFriendsWhoListenedToAlbum(_id);
          case 'artist':
            return await getFriendsWhoListenedToArtist(_id);
          default:
            return [];
        }
      } catch (error) {
        console.error('Failed to fetch friends who listened:', error);
        return [];
      }
    },
    enabled: !!_id,
    staleTime: 10 * 60 * 1000, // Data stays fresh for 10 minutes
    gcTime: 30 * 60 * 1000, // Cached for 30 minutes
    refetchOnWindowFocus: false,
    retry: 2,
    retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 5000)
  });

  const handleCompareWithFriend = async (friendId: string) => {
    if (!_id) return;
    
    setIsCompareLoading(true);
    try {
      let comparisonResult: ItemComparisonResponse;
      
      switch (type) {
        case 'track':
          comparisonResult = await compareTrackWithFriend(_id, friendId);
          break;
        case 'album':
          comparisonResult = await compareAlbumWithFriend(_id, friendId);
          break;
        case 'artist':
          comparisonResult = await compareArtistWithFriend(_id, friendId);
          break;
        default:
          throw new Error('Invalid type');
      }
      
      setComparison(comparisonResult);
      setShowCompareModal(true);
    } catch (error) {
      console.error('Failed to compare with friend:', error);
    } finally {
      setIsCompareLoading(false);
    }
  };
  
  const prestige = prestigeTier;
  
  // Get rating color based on score (10-point scale)
  const getRatingColor = (score: number) => {
    if (score >= 7) return 'text-green-400'; // Loved
    if (score >= 4) return 'text-yellow-400'; // Liked  
    return 'text-red-400'; // Disliked
  };
  
  const getRatingIcon = (score: number) => {
    if (score >= 7) return '⭐'; // Loved
    if (score >= 4) return '👍'; // Liked
    return '👎'; // Disliked
  };

  return (
    <div 
      className="cursor-pointer group transition-transform hover:scale-105"
      onClick={onClick}
    >
      {/* Main card container */}
      <div className="relative aspect-square mb-2">
        <div className="w-full h-full rounded-xl p-2 relative overflow-hidden border-2 border-gray-600 bg-gray-800">
          {/* Prestige tier background image */}
          {prestige && (
            <img
              src={`src/assets/tiers/${prestige.toLowerCase().replace(' ', '')}.png`}
              alt={prestige}
              className="absolute inset-0 w-full h-full object-cover opacity-20 rounded-xl"
            />
          )}
          
          <div className="relative z-10 h-full flex flex-col">
            {/* Rank badge, Compare button, and Rating */}
            <div className="flex justify-between items-start mb-2">
              <div className="w-5 h-5 bg-black bg-opacity-70 rounded-full flex items-center justify-center">
                <span className="text-white text-xs font-bold">{rank}</span>
              </div>
              
              {/* Compare Button */}
              {_id && friendsQuery.data && friendsQuery.data.length > 0 && (
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button 
                      variant="ghost" 
                      size="sm" 
                      className="h-6 w-6 p-0 bg-black bg-opacity-70 hover:bg-opacity-90"
                      onClick={(e) => e.stopPropagation()}
                    >
                      <span className="text-xs">⚡</span>
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent className="bg-gray-800 border-gray-700">
                    <div className="px-2 py-1 text-xs text-gray-400">Compare with:</div>
                    {friendsQuery.data.map((friend) => (
                      <DropdownMenuItem
                        key={friend.id}
                        onClick={(e) => {
                          e.stopPropagation();
                          handleCompareWithFriend(friend.id);
                        }}
                        className="text-white hover:bg-gray-700 cursor-pointer"
                      >
                        <img 
                          src={friend.profilePicUrl} 
                          alt={friend.nickname}
                          className="w-4 h-4 rounded-full mr-2"
                        />
                        {friend.nickname}
                      </DropdownMenuItem>
                    ))}
                  </DropdownMenuContent>
                </DropdownMenu>
              )}
              {/* For tracks, show album position instead of rating score */}
              {type === 'track' && albumPosition !== undefined ? (
                <div className="bg-black bg-opacity-70 rounded-md px-1.5 py-0.5 flex items-center space-x-1">
                  <span className="text-xs">🏆</span>
                  <span className="text-xs font-bold text-yellow-400">
                    #{albumPosition}
                  </span>
                </div>
              ) : ratingScore !== undefined && type !== 'track' ? (
                <div className="bg-black bg-opacity-70 rounded-md px-1.5 py-0.5 flex items-center space-x-1">
                  <span className="text-xs">{getRatingIcon(ratingScore)}</span>
                  <span className={`text-xs font-bold ${getRatingColor(ratingScore)}`}>
                    {ratingScore.toFixed(1)}
                  </span>
                </div>
              ) : null}
            </div>

            {/* Album artwork */}
            <div className="flex-1 flex items-center justify-center px-1">
              <div className="w-full max-w-[85%] aspect-square rounded-lg overflow-hidden shadow-lg">
                <img
                  src={imageUrl || (images && images.length > 0 ? images[0].url : "/placeholder-album.png")}
                  alt={name}
                  className="w-full h-full object-cover"
                  onError={(e) => {
                    const target = e.target as HTMLImageElement;
                    target.src = "/placeholder-album.png";
                  }}
                />
              </div>
            </div>

            {/* Prestige badge */}
            <div className="flex justify-center mt-2">
              {prestige && (
                <div className="transform scale-75">
                  <div className="px-2 py-1 rounded-md text-xs font-bold text-white bg-black bg-opacity-70">
                    {prestige.toUpperCase()}
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Title and subtitle */}
      <div className="text-center space-y-1">
        <h3 className="text-xs font-semibold text-white line-clamp-1">
          {name}
        </h3>
        <p className="text-xs text-gray-400 line-clamp-1">
          {subtitle || (artists && artists.length > 0 ? artists.map(a => a.name).join(', ') : '')}
        </p>
        <p className="text-xs font-medium line-clamp-1 text-gray-300">
          {Math.floor(totalTime / 60)} min
        </p>
      </div>

      {/* Compare Modal */}
      <CompareModal
        isOpen={showCompareModal}
        onClose={() => setShowCompareModal(false)}
        comparison={comparison}
        isLoading={isCompareLoading}
      />
    </div>
  );
};

export default PrestigeGridCard;