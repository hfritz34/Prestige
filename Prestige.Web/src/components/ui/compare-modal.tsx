import React from 'react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { ItemComparisonResponse, UserStats } from '@/hooks/useFriends';
import { Badge } from '@/components/ui/badge';

interface CompareModalProps {
  isOpen: boolean;
  onClose: () => void;
  comparison: ItemComparisonResponse | null;
  isLoading?: boolean;
}

const CompareModal: React.FC<CompareModalProps> = ({
  isOpen,
  onClose,
  comparison,
  isLoading = false,
}) => {
  const formatTime = (minutes?: number) => {
    if (!minutes) return 'No listening time';
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    if (hours === 0) return `${mins}m`;
    return `${hours}h ${mins}m`;
  };

  const getPrestigeColor = (tier?: string) => {
    switch (tier?.toLowerCase()) {
      case 'obsessed': return 'bg-red-600';
      case 'devoted': return 'bg-orange-500';
      case 'fan': return 'bg-yellow-500';
      case 'casual': return 'bg-green-500';
      case 'listener': return 'bg-blue-500';
      default: return 'bg-gray-500';
    }
  };

  const getRatingColor = (score?: number) => {
    if (!score) return 'text-gray-400';
    if (score >= 7) return 'text-green-400';
    if (score >= 4) return 'text-yellow-400';
    return 'text-red-400';
  };

  const StatsCard: React.FC<{ 
    title: string; 
    stats: UserStats; 
    isUser?: boolean 
  }> = ({ title, stats, isUser = false }) => (
    <div className={`p-4 rounded-lg ${isUser ? 'bg-blue-900/30 border-blue-500/50' : 'bg-purple-900/30 border-purple-500/50'} border`}>
      <h3 className="text-lg font-semibold mb-3 text-center">{title}</h3>
      
      <div className="space-y-3">
        <div>
          <p className="text-sm text-gray-400 mb-1">Listening Time</p>
          <p className="text-lg font-bold">{formatTime(stats.listeningTime)}</p>
        </div>

        <div>
          <p className="text-sm text-gray-400 mb-1">Prestige Tier</p>
          {stats.prestigeTier ? (
            <Badge className={`${getPrestigeColor(stats.prestigeTier)} text-white`}>
              {stats.prestigeTier.toUpperCase()}
            </Badge>
          ) : (
            <p className="text-gray-500">None</p>
          )}
        </div>

        {stats.ratingScore && (
          <div>
            <p className="text-sm text-gray-400 mb-1">Rating</p>
            <p className={`text-lg font-bold ${getRatingColor(stats.ratingScore)}`}>
              {stats.ratingScore.toFixed(1)}/10
            </p>
          </div>
        )}

        {stats.position && (
          <div>
            <p className="text-sm text-gray-400 mb-1">Rank</p>
            <p className="text-lg font-bold text-yellow-400">#{stats.position}</p>
          </div>
        )}
      </div>
    </div>
  );

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-4xl w-full max-h-[90vh] overflow-y-auto bg-gray-900 text-white border-gray-700">
        <DialogHeader>
          <DialogTitle className="text-xl font-bold">Compare Stats</DialogTitle>
        </DialogHeader>

        {isLoading ? (
          <div className="flex justify-center items-center py-8">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-white"></div>
          </div>
        ) : comparison ? (
          <div className="space-y-6">
            {/* Item Info */}
            <div className="flex items-center space-x-4 p-4 bg-gray-800 rounded-lg">
              {comparison.itemImageUrl && (
                <img 
                  src={comparison.itemImageUrl} 
                  alt={comparison.itemName}
                  className="w-16 h-16 rounded-lg object-cover"
                />
              )}
              <div>
                <h2 className="text-xl font-bold">{comparison.itemName}</h2>
                <p className="text-gray-400 capitalize">{comparison.itemType}</p>
              </div>
            </div>

            {/* Comparison Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <StatsCard 
                title="Your Stats" 
                stats={comparison.userStats} 
                isUser={true}
              />
              <StatsCard 
                title={`${comparison.friendNickname}'s Stats`} 
                stats={comparison.friendStats} 
              />
            </div>

            {/* Winner Badge */}
            {comparison.userStats.listeningTime && comparison.friendStats.listeningTime && (
              <div className="text-center py-4">
                {comparison.userStats.listeningTime > comparison.friendStats.listeningTime ? (
                  <Badge className="bg-green-600 text-white px-4 py-2 text-lg">
                    🏆 You listened more!
                  </Badge>
                ) : comparison.userStats.listeningTime < comparison.friendStats.listeningTime ? (
                  <Badge className="bg-blue-600 text-white px-4 py-2 text-lg">
                    🏆 {comparison.friendNickname} listened more!
                  </Badge>
                ) : (
                  <Badge className="bg-gray-600 text-white px-4 py-2 text-lg">
                    🤝 It's a tie!
                  </Badge>
                )}
              </div>
            )}
          </div>
        ) : (
          <div className="text-center py-8 text-gray-400">
            No comparison data available
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
};

export default CompareModal;