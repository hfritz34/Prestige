import React from 'react';
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
  return (
    <div className="space-y-6">
      <div className="text-center">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
          Which do you prefer?
        </h3>
        <p className="text-sm text-gray-600 dark:text-gray-400">
          {comparisonNumber} of {totalComparisons}
        </p>
      </div>
      
      <div className="grid grid-cols-3 gap-4 items-stretch w-full">
        {/* New Item */}
        <div className="col-span-1">
          <Button
            variant="outline"
            className="w-full h-full p-0 border-2 hover:border-blue-500 transition-colors"
            onClick={() => onSelect(newItem.id)}
          >
            <Card className="w-full h-full border-0">
              <CardContent className="p-4 h-full">
                <div className="space-y-3">
                  {/* Image */}
                  <div className="w-full aspect-square rounded-lg overflow-hidden bg-gray-200 dark:bg-gray-700">
                    {newItem.imageUrl ? (
                      <img 
                        src={newItem.imageUrl} 
                        alt={newItem.name}
                        className="w-full h-full object-cover"
                        onError={(e) => {
                          const t = e.target as HTMLImageElement;
                          t.src = "/placeholder-album.png";
                        }}
                      />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center">
                        <svg className="w-12 h-12 text-gray-400" fill="currentColor" viewBox="0 0 20 20">
                          <path fillRule="evenodd" d="M4 3a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V5a2 2 0 00-2-2H4zm12 12H4l4-8 3 6 2-4 3 6z" clipRule="evenodd" />
                        </svg>
                      </div>
                    )}
                  </div>
                  
                  {/* Text */}
                  <div className="space-y-1">
                    <h4 className="font-semibold text-sm text-gray-900 dark:text-white truncate">
                      {newItem.name}
                    </h4>
                    <p className="text-xs text-gray-600 dark:text-gray-400 truncate">
                      {newItem.subtitle}
                    </p>
                    <p className="text-xs text-blue-600 dark:text-blue-400 font-medium">
                      New {newItem.type}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          </Button>
        </div>

        {/* VS Divider */}
        <div className="col-span-1 flex items-center justify-center px-2">
          <div className="w-8 h-8 rounded-full bg-gray-200 dark:bg-gray-700 flex items-center justify-center">
            <span className="text-xs font-bold text-gray-600 dark:text-gray-400">VS</span>
          </div>
        </div>

        {/* Comparison Item */}
        <div className="col-span-1">
          <Button
            variant="outline"
            className="w-full h-full p-0 border-2 hover:border-blue-500 transition-colors"
            onClick={() => onSelect(comparisonItem.id)}
          >
            <Card className="w-full h-full border-0">
              <CardContent className="p-4 h-full">
                <div className="space-y-3">
                  {/* Image */}
                  <div className="w-full aspect-square rounded-lg overflow-hidden bg-gray-200 dark:bg-gray-700">
                    {comparisonItem.imageUrl ? (
                      <img 
                        src={comparisonItem.imageUrl} 
                        alt={comparisonItem.name}
                        className="w-full h-full object-cover"
                        onError={(e) => {
                          const t = e.target as HTMLImageElement;
                          t.src = "/placeholder-album.png";
                        }}
                      />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center">
                        <svg className="w-12 h-12 text-gray-400" fill="currentColor" viewBox="0 0 20 20">
                          <path fillRule="evenodd" d="M4 3a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V5a2 2 0 00-2-2H4zm12 12H4l4-8 3 6 2-4 3 6z" clipRule="evenodd" />
                        </svg>
                      </div>
                    )}
                  </div>
                  
                  {/* Text */}
                  <div className="space-y-1">
                    <h4 className="font-semibold text-sm text-gray-900 dark:text-white truncate">
                      {comparisonItem.name}
                    </h4>
                    <p className="text-xs text-gray-600 dark:text-gray-400 truncate">
                      {comparisonItem.subtitle}
                    </p>
                    {comparisonItem.score !== undefined && (
                      <p className="text-xs text-green-600 dark:text-green-400 font-medium">
                        Current score: {comparisonItem.score.toFixed(1)}
                      </p>
                    )}
                  </div>
                </div>
              </CardContent>
            </Card>
          </Button>
        </div>
      </div>
      
      <div className="text-center">
        <p className="text-xs text-gray-500 dark:text-gray-400">
          Click on the {newItem.type} you prefer more
        </p>
      </div>
    </div>
  );
};

export default BeliComparisonUI;