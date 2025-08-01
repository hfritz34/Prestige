import React from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';

interface RatingItemCardProps {
  id: string;
  name: string;
  subtitle: string;
  imageUrl?: string;
  type: 'track' | 'album' | 'artist';
  onRate: (id: string, type: string, name: string, subtitle: string, imageUrl?: string) => void;
}

const RatingItemCard: React.FC<RatingItemCardProps> = ({
  id,
  name,
  subtitle,
  imageUrl,
  type,
  onRate
}) => {
  return (
    <Card className="hover:shadow-md transition-shadow">
      <CardContent className="p-4">
        <div className="flex items-center space-x-4">
          <div className="w-16 h-16 rounded-md overflow-hidden bg-gray-200 flex-shrink-0">
            {imageUrl ? (
              <img 
                src={imageUrl} 
                alt={name}
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
          
          <div className="flex-1 min-w-0">
            <h3 className="font-medium text-gray-900 dark:text-white truncate">
              {name}
            </h3>
            <p className="text-sm text-gray-500 dark:text-gray-400 truncate">
              {subtitle}
            </p>
            <p className="text-xs text-gray-400 dark:text-gray-500 mt-1 capitalize">
              {type}
            </p>
          </div>
          
          <Button 
            onClick={() => onRate(id, type, name, subtitle, imageUrl)}
            variant="outline"
            size="sm"
          >
            Rate
          </Button>
        </div>
      </CardContent>
    </Card>
  );
};

export default RatingItemCard;