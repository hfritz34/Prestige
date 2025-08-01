import React from "react";
import usePrestige from "@/hooks/usePrestige";

type PrestigeGridCardProps = {
  imageUrl: string;
  name: string;
  subtitle: string;
  totalTime: number;
  rank: number;
  onClick: () => void;
  type?: 'track' | 'album' | 'artist';
  ratingScore?: number;
};

const PrestigeGridCard: React.FC<PrestigeGridCardProps> = ({
  imageUrl,
  name,
  subtitle,
  totalTime,
  rank,
  onClick,
  type = 'track',
  ratingScore
}) => {
  const { getTrackPrestigeTier, getAlbumPrestigeTier, getArtistPrestigeTier } = usePrestige();
  
  const getPrestige = () => {
    switch (type) {
      case 'album':
        return getAlbumPrestigeTier(totalTime);
      case 'artist':
        return getArtistPrestigeTier(totalTime);
      default:
        return getTrackPrestigeTier(totalTime);
    }
  };
  
  const prestige = getPrestige();
  
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
              src={`src/assets/tiers/${prestige.toLowerCase()}.png`}
              alt={prestige}
              className="absolute inset-0 w-full h-full object-cover opacity-20 rounded-xl"
            />
          )}
          
          <div className="relative z-10 h-full flex flex-col">
            {/* Rank badge and Rating */}
            <div className="flex justify-between items-start mb-2">
              <div className="w-5 h-5 bg-black bg-opacity-70 rounded-full flex items-center justify-center">
                <span className="text-white text-xs font-bold">{rank}</span>
              </div>
              {ratingScore !== undefined && (
                <div className="bg-black bg-opacity-70 rounded-md px-1.5 py-0.5 flex items-center space-x-1">
                  <span className="text-xs">{getRatingIcon(ratingScore)}</span>
                  <span className={`text-xs font-bold ${getRatingColor(ratingScore)}`}>
                    {ratingScore.toFixed(1)}
                  </span>
                </div>
              )}
            </div>

            {/* Album artwork */}
            <div className="flex-1 flex items-center justify-center px-1">
              <div className="w-full max-w-[85%] aspect-square rounded-lg overflow-hidden shadow-lg">
                <img
                  src={imageUrl}
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
          {subtitle}
        </p>
        <p className="text-xs font-medium line-clamp-1 text-gray-300">
          {Math.floor(totalTime / 60)} min
        </p>
      </div>
    </div>
  );
};

export default PrestigeGridCard;