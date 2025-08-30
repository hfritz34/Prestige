import React from 'react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Badge } from '@/components/ui/badge';
import { UserTrackResponse, UserAlbumResponse, UserArtistResponse } from '@/hooks/useProfile';

interface FriendItemModalProps {
  isOpen: boolean;
  onClose: () => void;
  item: UserTrackResponse | UserAlbumResponse | UserArtistResponse | null;
  itemType: 'track' | 'album' | 'artist';
  friendNickname: string;
}

const FriendItemModal: React.FC<FriendItemModalProps> = ({
  isOpen,
  onClose,
  item,
  itemType,
  friendNickname,
}) => {
  const formatTime = (minutes?: number) => {
    if (!minutes) return 'No listening time';
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    if (hours === 0) return `${mins}m`;
    return `${hours}h ${mins}m`;
  };

  const getPrestigeTier = (totalTime: number, type: string) => {
    const timeInMinutes = totalTime;
    
    switch (type) {
      case 'track':
        if (timeInMinutes >= 500) return 'Obsessed';
        if (timeInMinutes >= 200) return 'Devoted';
        if (timeInMinutes >= 100) return 'Fan';
        if (timeInMinutes >= 50) return 'Casual';
        return 'Listener';
      case 'album':
        if (timeInMinutes >= 2000) return 'Obsessed';
        if (timeInMinutes >= 1000) return 'Devoted';
        if (timeInMinutes >= 500) return 'Fan';
        if (timeInMinutes >= 200) return 'Casual';
        return 'Listener';
      case 'artist':
        if (timeInMinutes >= 5000) return 'Obsessed';
        if (timeInMinutes >= 2500) return 'Devoted';
        if (timeInMinutes >= 1000) return 'Fan';
        if (timeInMinutes >= 500) return 'Casual';
        return 'Listener';
      default:
        return 'Unknown';
    }
  };

  const getPrestigeColor = (tier: string) => {
    switch (tier.toLowerCase()) {
      case 'obsessed': return 'bg-red-600';
      case 'devoted': return 'bg-orange-500';
      case 'fan': return 'bg-yellow-500';
      case 'casual': return 'bg-green-500';
      case 'listener': return 'bg-blue-500';
      default: return 'bg-gray-500';
    }
  };

  if (!item) return null;

  const getItemName = () => {
    if ('track' in item) return item.track.name;
    if ('album' in item) return item.album.name;
    if ('artist' in item) return item.artist.name;
    return '';
  };

  const getItemImage = () => {
    if ('track' in item) return item.track.album.images?.[0]?.url;
    if ('album' in item) return item.album.images?.[0]?.url;
    if ('artist' in item) return item.artist.images?.[0]?.url;
    return '';
  };

  const getSubtitle = () => {
    if ('track' in item) return item.track.artists.map(a => a.name).join(', ');
    if ('album' in item) return item.album.artists.map(a => a.name).join(', ');
    return '';
  };

  const prestigeTier = getPrestigeTier(item.totalTime, itemType);

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-md w-full bg-gray-900 text-white border-gray-700">
        <DialogHeader>
          <DialogTitle className="text-xl font-bold">{friendNickname}'s Stats</DialogTitle>
        </DialogHeader>

        <div className="space-y-4">
          {/* Item Info */}
          <div className="flex items-center space-x-4 p-4 bg-gray-800 rounded-lg">
            {getItemImage() && (
              <img 
                src={getItemImage()} 
                alt={getItemName()}
                className="w-16 h-16 rounded-lg object-cover"
              />
            )}
            <div className="flex-1">
              <h2 className="text-lg font-bold line-clamp-2">{getItemName()}</h2>
              {getSubtitle() && (
                <p className="text-gray-400 text-sm line-clamp-1">{getSubtitle()}</p>
              )}
              <p className="text-gray-500 text-xs capitalize">{itemType}</p>
            </div>
          </div>

          {/* Stats */}
          <div className="space-y-4 p-4 bg-gray-800 rounded-lg">
            <div>
              <p className="text-sm text-gray-400 mb-1">Total Listening Time</p>
              <p className="text-xl font-bold">{formatTime(item.totalTime)}</p>
            </div>

            <div>
              <p className="text-sm text-gray-400 mb-2">Prestige Tier</p>
              <Badge className={`${getPrestigeColor(prestigeTier)} text-white`}>
                {prestigeTier.toUpperCase()}
              </Badge>
            </div>

            {/* Show if it's a favorite */}
            {'isFavorite' in item && item.isFavorite && (
              <div>
                <Badge className="bg-pink-600 text-white">
                  ❤️ Favorite
                </Badge>
              </div>
            )}
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
};

export default FriendItemModal;