import React, { useState, useEffect } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';

interface Album {
  id: string;
  name: string;
  artist: string;
  imageUrl: string;
  score?: number;
}

const InteractiveRankingDemo: React.FC = () => {
  // All albums for the comparison demo
  const allAlbums: Album[] = [
    {
      id: "5zi7WsKlIiUXv09tbGLKsE",
      name: "IGOR",
      artist: "Tyler, The Creator",
      imageUrl: "https://i.scdn.co/image/ab67616d0000b2737005885df706891a3c182a57"
    },
    {
      id: "wild-one",
      name: "Wild One", 
      artist: "fakemink",
      imageUrl: "https://i.scdn.co/image/ab67616d0000b2736bea1667b951efcc4eeda596"
    },
    {
      id: "what-the-feng",
      name: "What The Feng",
      artist: "Feng",
      imageUrl: "https://i.scdn.co/image/ab67616d0000b273e9b4b33ac93a78e8e504c641"
    },
    {
      id: "5uRdvUR7xCnHmUW8n64n9y",
      name: "Homework",
      artist: "Daft Punk",
      imageUrl: "https://i.scdn.co/image/ab67616d0000b2738ac778cc7d88779f74d33311"
    },
    {
      id: "3mH6qwIy9crq0I9YQbOuDf",
      name: "Blonde",
      artist: "Frank Ocean",
      imageUrl: "https://i.scdn.co/image/ab67616d0000b273c5649add07ed3720be9d5526"
    },
    {
      id: "5GuWww4OaildzkmTTlfMN3",
      name: "Lahai",
      artist: "Sampha",
      imageUrl: "https://i.scdn.co/image/ab67616d0000b27365fa072296480cea19a38cd0"
    },
    {
      id: "3STQHyw2nOlIbb1FSgPse8",
      name: "OFFLINE!",
      artist: "JPEGMAFIA",
      imageUrl: "https://i.scdn.co/image/ab67616d0000b273e6ff353e9e91669eb9af5e2f"
    },
    {
      id: "5vkqYmiPBYLaalcmjujWxK",
      name: "In Rainbows",
      artist: "Radiohead",
      imageUrl: "https://i.scdn.co/image/ab67616d0000b273de3c04b5fc750b68899b20a9"
    },
    {
      id: "2oJo7cB45gMVuRsaWNwDq2",
      name: "Drukqs",
      artist: "Aphex Twin",
      imageUrl: "https://i.scdn.co/image/ab67616d0000b2732e261a0b1b19d0ff95e346b3"
    }
  ];

  const [currentStep, setCurrentStep] = useState(0);
  const [rankings, setRankings] = useState<{ higher: Album[], lower: Album[] }>({ higher: [], lower: [] });
  const [selectedItem, setSelectedItem] = useState<string | null>(null);
  const [isComplete, setIsComplete] = useState(false);
  const [baseAlbum, setBaseAlbum] = useState<Album | null>(null);
  const [comparisonAlbums, setComparisonAlbums] = useState<Album[]>([]);
  const [albumScores, setAlbumScores] = useState<Record<string, number>>({});

  // Score calculation based on position within "I really liked it" category (6.8-10.0)
  const calculateScore = (position: number, totalItems: number): number => {
    const minScore = 6.8;
    const maxScore = 10.0;
    const scoreRange = maxScore - minScore;
    
    if (totalItems === 1) {
      return maxScore;
    }
    
    // Position 1 gets max score, last position gets min score + small buffer
    if (position === 1) {
      return maxScore;
    } else if (position === totalItems) {
      return minScore + 0.1;
    } else {
      // Items in between are evenly distributed
      const scoreStep = scoreRange / (totalItems - 1);
      const calculatedScore = maxScore - (scoreStep * (position - 1));
      return Math.round(calculatedScore * 10) / 10; // Round to 1 decimal place
    }
  };

  const initializeDemo = () => {
    // Randomly select a base album
    const randomBase = allAlbums[Math.floor(Math.random() * allAlbums.length)];
    setBaseAlbum(randomBase);
    
    // Get remaining albums for comparison, shuffled
    const remainingAlbums = allAlbums.filter(album => album.id !== randomBase.id);
    const shuffled = remainingAlbums.sort(() => Math.random() - 0.5).slice(0, 7); // Take 7 for comparison
    setComparisonAlbums(shuffled);
    
    // Reset state
    setCurrentStep(0);
    setRankings({ higher: [], lower: [] });
    setIsComplete(false);
    setSelectedItem(null);
    setAlbumScores({});
  };

  useEffect(() => {
    initializeDemo();
  }, []);

  // Calculate scores when ranking is complete
  useEffect(() => {
    if (isComplete && baseAlbum) {
      const finalRanking = [...rankings.higher, baseAlbum, ...rankings.lower];
      const finalScores: Record<string, number> = {};
      
      finalRanking.forEach((album, index) => {
        finalScores[album.id] = calculateScore(index + 1, finalRanking.length);
      });
      
      setAlbumScores(finalScores);
    }
  }, [isComplete, rankings, baseAlbum]);

  const handleSelect = (albumId: string) => {
    if (!baseAlbum) return;
    
    setSelectedItem(albumId);
    setTimeout(() => {
      const currentComparison = comparisonAlbums[currentStep];
      
      if (albumId === baseAlbum.id) {
        // Base album is better - comparison goes to "lower"
        setRankings(prev => ({ ...prev, lower: [...prev.lower, currentComparison] }));
      } else {
        // Comparison album is better - goes to "higher"
        setRankings(prev => ({ ...prev, higher: [...prev.higher, currentComparison] }));
      }

      if (currentStep < comparisonAlbums.length - 1) {
        setCurrentStep(currentStep + 1);
      } else {
        setIsComplete(true);
      }
      setSelectedItem(null);
    }, 500);
  };

  const resetDemo = () => {
    // Clear all state immediately to prevent display glitches
    setCurrentStep(0);
    setRankings({ higher: [], lower: [] });
    setIsComplete(false);
    setSelectedItem(null);
    setAlbumScores({});
    setBaseAlbum(null);
    setComparisonAlbums([]);
    
    // Reinitialize after state is cleared
    setTimeout(() => {
      initializeDemo();
    }, 100);
  };

  if (!baseAlbum || comparisonAlbums.length === 0) {
    return <div className="flex justify-center p-8"><div className="animate-spin w-8 h-8 border-2 border-[#7C4DFF] border-t-transparent rounded-full"></div></div>;
  }

  if (isComplete) {
    const finalRanking = [...rankings.higher, baseAlbum, ...rankings.lower];
    const basePosition = rankings.higher.length + 1;
    const baseScore = albumScores[baseAlbum.id];
    
    return (
      <div className="space-y-6 sm:space-y-8 p-4 sm:p-6">
        <div className="text-center space-y-4">
          <h3 className="text-2xl font-bold text-white">
            Ranking Complete!
          </h3>
          <p className="text-zinc-300">
            <span className="font-semibold text-[#7C4DFF]">{baseAlbum.name}</span> ranks #{basePosition} out of {finalRanking.length}
          </p>
          {baseScore && (
            <p className="text-sm text-zinc-400">
              Score: <span className="font-semibold text-green-600">{baseScore.toFixed(1)}</span>
            </p>
          )}
        </div>

        <div className="max-w-2xl mx-auto space-y-3 sm:space-y-4">
          {finalRanking.map((album, index) => {
            const score = albumScores[album.id];
            return (
              <div 
                key={`ranking-${album.id}-${index}`}
                className={`flex items-center gap-3 sm:gap-4 p-3 sm:p-4 rounded-xl border transition-all duration-300 ${
                  album.id === baseAlbum.id 
                    ? 'bg-gradient-to-r from-[#7C4DFF]/20 to-purple-500/20 border-[#7C4DFF]/50' 
                    : 'bg-white/5 border-white/10'
                }`}
              >
                <div className="flex-shrink-0 w-6 h-6 sm:w-8 sm:h-8 flex items-center justify-center bg-gray-200 dark:bg-gray-700 rounded-full text-xs sm:text-sm font-semibold text-black dark:text-white">
                  {index + 1}
                </div>
                <img 
                  src={album.imageUrl} 
                  alt={album.name}
                  className="w-10 h-10 sm:w-12 sm:h-12 rounded-lg object-cover"
                />
                <div className="flex-grow min-w-0">
                  <h4 className="font-semibold text-white text-sm sm:text-base truncate">{album.name}</h4>
                  <p className="text-xs sm:text-sm text-zinc-400 truncate">{album.artist}</p>
                  {score && (
                    <p className="text-xs text-green-400 font-medium">Score: {score.toFixed(1)}</p>
                  )}
                </div>
                {album.id === baseAlbum.id && (
                  <div className="flex-shrink-0">
                    <span className="inline-flex items-center gap-1 px-2 sm:px-3 py-1 rounded-full bg-[#7C4DFF] text-white text-xs font-semibold">
                      <span className="hidden sm:inline">YOUR PICK</span>
                      <span className="sm:hidden">PICK</span>
                    </span>
                  </div>
                )}
              </div>
            );
          })}
        </div>

        <div className="text-center">
          <Button
            onClick={resetDemo}
            className="group bg-white/5 hover:bg-white/10 text-white px-8 py-3 rounded-xl transition-all duration-300 transform hover:scale-[1.02] backdrop-blur-lg border border-white/20 hover:border-white/30 shadow-lg hover:shadow-xl relative overflow-hidden"
          >
            <div className="absolute inset-0 bg-gradient-to-br from-white/10 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300"></div>
            <div className="absolute inset-0 bg-gradient-to-t from-[#7C4DFF]/10 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300"></div>
            <span className="relative z-10">Try Again</span>
          </Button>
        </div>
      </div>
    );
  }

  const progress = ((currentStep + 1) / comparisonAlbums.length) * 100;

  return (
    <div className="space-y-6 sm:space-y-8 p-4 sm:p-6">
      {/* Progress Header */}
      <div className="text-center space-y-4">
        <h3 className="text-2xl font-bold text-white">
          Which do you prefer?
        </h3>
        
        <div className="flex items-center justify-center gap-2 text-sm text-zinc-300">
          <span>Step</span>
          <span className="font-semibold text-[#7C4DFF]">{currentStep + 1}</span>
          <span>of</span>
          <span className="font-semibold text-[#7C4DFF]">{comparisonAlbums.length}</span>
        </div>

        {/* Progress Bar */}
        <div className="w-full max-w-md mx-auto">
          <div className="h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
            <div 
              className="h-full bg-gradient-to-r from-[#7C4DFF] to-purple-600 transition-all duration-500 ease-out"
              style={{ width: `${progress}%` }}
            />
          </div>
        </div>
      </div>
      
      <div className="flex items-center gap-2 sm:gap-4 w-full max-w-full sm:max-w-4xl mx-auto px-2">
        {/* Base Album */}
        <div className="flex-1 max-w-[calc(50%-12px)] sm:max-w-[calc(50%-32px)]">
          <ComparisonCard
            album={baseAlbum}
            isSelected={selectedItem === baseAlbum.id}
            isBase={true}
            onSelect={() => handleSelect(baseAlbum.id)}
          />
        </div>

        {/* VS Divider */}
        <div className="flex items-center justify-center flex-shrink-0">
          <VersusIndicator />
        </div>

        {/* Current Comparison Album */}
        <div className="flex-1 max-w-[calc(50%-12px)] sm:max-w-[calc(50%-32px)]">
          <ComparisonCard
            album={comparisonAlbums[currentStep]}
            isSelected={selectedItem === comparisonAlbums[currentStep].id}
            isBase={false}
            onSelect={() => handleSelect(comparisonAlbums[currentStep].id)}
          />
        </div>
      </div>
      
      <div className="text-center">
        <p className="text-sm text-zinc-400">
          Click on the album you prefer more
        </p>
      </div>
    </div>
  );
};

interface ComparisonCardProps {
  album: Album;
  isSelected: boolean;
  isBase: boolean;
  onSelect: () => void;
}

const ComparisonCard: React.FC<ComparisonCardProps> = ({ album, isSelected, isBase, onSelect }) => {
  return (
    <Button
      variant="ghost"
      className={`group w-full h-full p-0 transition-all duration-300 transform hover:scale-[1.02] ${
        isSelected ? 'scale-[1.02]' : ''
      }`}
      onClick={onSelect}
    >
      <Card className={`w-full h-full transition-all duration-300 backdrop-blur-lg border relative overflow-hidden min-h-[250px] sm:min-h-0 ${
        isSelected 
          ? 'ring-2 ring-[#7C4DFF] shadow-lg shadow-[#7C4DFF]/20 bg-white/10 border-[#7C4DFF]/50' 
          : 'bg-white/5 border-white/20 hover:bg-white/[0.015] hover:border-white/[0.22] hover:shadow-sm hover:shadow-[#7C4DFF]/3'
      }`}>
        <div className={`absolute inset-0 bg-gradient-to-br from-white/3 to-transparent transition-opacity duration-300 ${
          isSelected ? 'opacity-100' : 'opacity-0 group-hover:opacity-25'
        }`}></div>
        <div className={`absolute inset-0 bg-gradient-to-t from-[#7C4DFF]/2 to-transparent transition-opacity duration-300 ${
          isSelected ? 'opacity-100' : 'opacity-0 group-hover:opacity-30'
        }`}></div>
        <CardContent className="relative z-10 p-2 sm:p-3 md:p-4 lg:p-6">
          <div className="space-y-2 sm:space-y-3">
            {/* Badge area - Fixed height on mobile to prevent layout shifts */}
            <div className="flex justify-center h-6 sm:h-5 md:h-6 lg:h-7 items-center">
              {isBase && (
                <div className="inline-flex items-center gap-1 px-2 py-1 rounded-full bg-gradient-to-r from-[#7C4DFF] to-purple-600 text-white text-xs font-semibold shadow-md">
                  <svg className="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
                    <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                  </svg>
                  <span className="hidden sm:inline">YOUR PICK</span>
                  <span className="sm:hidden">PICK</span>
                </div>
              )}
            </div>
            
            {/* Image */}
            <div className="w-full aspect-square rounded-lg sm:rounded-xl overflow-hidden bg-gray-200 dark:bg-gray-700 shadow-md">
              <img 
                src={album.imageUrl} 
                alt={album.name}
                className="w-full h-full object-cover"
                onError={(e) => {
                  const t = e.target as HTMLImageElement;
                  t.src = "/placeholder-album.png";
                }}
              />
            </div>
            
            {/* Text Content */}
            <div className="space-y-0.5 sm:space-y-1 min-w-0 h-10 sm:h-auto flex flex-col justify-center sm:block">
              <h4 className="font-black text-xs sm:text-sm md:text-base lg:text-lg text-white text-center leading-tight line-clamp-1 px-0.5 sm:px-1 drop-shadow-sm">
                {album.name}
              </h4>
              <p className="text-[10px] sm:text-xs md:text-sm text-zinc-200 text-center line-clamp-1 px-0.5 sm:px-1 drop-shadow-sm">
                {album.artist}
              </p>
            </div>
          </div>
        </CardContent>
      </Card>
    </Button>
  );
};

const VersusIndicator: React.FC = () => {
  return (
    <div className="relative">
      {/* Outer glow */}
      <div className="absolute inset-0 w-8 h-8 sm:w-10 sm:h-10 md:w-12 md:h-12 rounded-full bg-gradient-to-r from-[#7C4DFF]/30 to-purple-600/30 blur-md animate-pulse" />
      
      {/* Main circle */}
      <div className="relative w-8 h-8 sm:w-10 sm:h-10 md:w-12 md:h-12 rounded-full bg-gradient-to-r from-[#7C4DFF] to-purple-600 flex items-center justify-center shadow-lg">
        <span className="text-white font-black text-[10px] sm:text-xs md:text-sm">VS</span>
      </div>
    </div>
  );
};

export default InteractiveRankingDemo;