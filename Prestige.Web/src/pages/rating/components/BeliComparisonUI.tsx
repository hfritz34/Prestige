import React, { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';

interface ComparisonItem {
  id: string;
  name: string;
  subtitle: string;
  imageUrl?: string;
  type: 'track' | 'album' | 'artist';
  score?: number;
}

interface BeliComparisonUIProps {
  newItem: ComparisonItem;
  comparisonItem: ComparisonItem;
  onSelect: (selectedId: string) => void;
  comparisonNumber: number;
  totalComparisons: number;
}

const BeliComparisonUI: React.FC<BeliComparisonUIProps> = ({
  newItem,
  comparisonItem,
  onSelect,
  comparisonNumber,
  totalComparisons
}) => {
  const [selectedItem, setSelectedItem] = useState<string | null>(null);

  const handleSelect = (itemId: string) => {
    setSelectedItem(itemId);
    setTimeout(() => {
      onSelect(itemId);
      setSelectedItem(null);
    }, 300);
  };

  const progress = (comparisonNumber / totalComparisons) * 100;

  return (
    <div className="space-y-8 p-6">
      {/* Progress Header */}
      <div className="text-center space-y-4">
        <h3 className="text-2xl font-bold text-gray-900 dark:text-white">
          Which do you prefer?
        </h3>
        
        <div className="flex items-center justify-center gap-2 text-sm text-gray-600 dark:text-gray-400">
          <span>Step</span>
          <span className="font-semibold text-blue-600 dark:text-blue-400">{comparisonNumber}</span>
          <span>of</span>
          <span className="font-semibold text-blue-600 dark:text-blue-400">{totalComparisons}</span>
        </div>

        {/* Progress Bar */}
        <div className="w-full max-w-md mx-auto">
          <div className="h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
            <div 
              className="h-full bg-gradient-to-r from-blue-600 to-purple-600 transition-all duration-500 ease-out"
              style={{ width: `${progress}%` }}
            />
          </div>
        </div>
      </div>
      
      <div className="flex items-center gap-6 w-full max-w-4xl mx-auto">
        {/* New Item */}
        <div className="flex-1">
          <ComparisonCard
            item={newItem}
            isSelected={selectedItem === newItem.id}
            isNew={true}
            onSelect={() => handleSelect(newItem.id)}
          />
        </div>

        {/* VS Divider */}
        <div className="flex items-center justify-center px-4">
          <VersusIndicator />
        </div>

        {/* Comparison Item */}
        <div className="flex-1">
          <ComparisonCard
            item={comparisonItem}
            isSelected={selectedItem === comparisonItem.id}
            isNew={false}
            onSelect={() => handleSelect(comparisonItem.id)}
          />
        </div>
      </div>
      
      <div className="text-center">
        <p className="text-sm text-gray-500 dark:text-gray-400">
          Click on the {newItem.type} you prefer more
        </p>
      </div>
    </div>
  );
};

interface ComparisonCardProps {
  item: ComparisonItem;
  isSelected: boolean;
  isNew: boolean;
  onSelect: () => void;
}

const ComparisonCard: React.FC<ComparisonCardProps> = ({ item, isSelected, isNew, onSelect }) => {
  return (
    <Button
      variant="ghost"
      className={`w-full h-full p-0 transition-all duration-300 transform hover:scale-105 ${
        isSelected ? 'scale-105' : ''
      }`}
      onClick={onSelect}
    >
      <Card className={`w-full transition-all duration-300 ${
        isSelected 
          ? 'ring-2 ring-blue-500 shadow-lg shadow-blue-500/20 bg-gradient-to-br from-blue-50/50 to-purple-50/50 dark:from-blue-900/20 dark:to-purple-900/20' 
          : 'hover:shadow-lg hover:border-blue-200 dark:hover:border-blue-700'
      }`}>
        <CardContent className="p-6">
          <div className="space-y-4">
            {/* Badge area - NEW badge or spacing */}
            <div className="flex justify-center h-7 items-center">
              {isNew && (
                <div className="inline-flex items-center gap-1 px-3 py-1 rounded-full bg-gradient-to-r from-blue-600 to-purple-600 text-white text-xs font-semibold shadow-md">
                  <svg className="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
                    <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                  </svg>
                  NEW
                </div>
              )}
            </div>
            
            {/* Image */}
            <div className="w-full aspect-square rounded-xl overflow-hidden bg-gray-200 dark:bg-gray-700 shadow-md">
              {item.imageUrl ? (
                <img 
                  src={item.imageUrl} 
                  alt={item.name}
                  className="w-full h-full object-cover"
                  onError={(e) => {
                    const t = e.target as HTMLImageElement;
                    t.src = "/placeholder-album.png";
                  }}
                />
              ) : (
                <div className="w-full h-full flex items-center justify-center">
                  <svg className="w-16 h-16 text-gray-400" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M4 3a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V5a2 2 0 00-2-2H4zm12 12H4l4-8 3 6 2-4 3 6z" clipRule="evenodd" />
                  </svg>
                </div>
              )}
            </div>
            
            {/* Text Content */}
            <div className="space-y-2">
              <h4 className="font-bold text-lg text-gray-900 dark:text-white text-center leading-tight">
                {item.name}
              </h4>
              <p className="text-sm text-gray-600 dark:text-gray-400 text-center">
                {item.subtitle}
              </p>
              {item.score !== undefined && !isNew && (
                <p className="text-sm text-green-600 dark:text-green-400 font-semibold text-center">
                  Score: {item.score.toFixed(1)}
                </p>
              )}
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
      <div className="absolute inset-0 w-12 h-12 rounded-full bg-gradient-to-r from-blue-600/30 to-purple-600/30 blur-md animate-pulse" />
      
      {/* Main circle */}
      <div className="relative w-12 h-12 rounded-full bg-gradient-to-r from-blue-600 to-purple-600 flex items-center justify-center shadow-lg">
        <span className="text-white font-black text-sm">VS</span>
      </div>
    </div>
  );
};

export default BeliComparisonUI;