import React from 'react';
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";
import placeholderImage from "@/assets/music.jpg"; 
import { ArtistResponse } from '@/hooks/useSpotify';

interface ArtistSearchCardProps {
  artist: ArtistResponse;
  onClick: () => void;
}

const ArtistSearchCard: React.FC<ArtistSearchCardProps> = ({ artist, onClick }) => {
  return (
    <Card onClick={onClick} className="h-24 cursor-pointer flex items-center border rounded-md shadow-md hover:bg-gray-100 transition">
      <CardHeader className='flex justify-center items-center p-0 w-1/4'>
        <img src={artist.images.at(0)?.url || placeholderImage} alt={artist.name} className="w-12 h-12 object-cover" />
      </CardHeader>
      <CardContent className='flex flex-col items-start p-0 w-3/4'>
        <CardTitle className="text-xl overflow-hidden text-ellipsis w-full whitespace-nowrap pr-4">{artist.name}</CardTitle>
      </CardContent>
    </Card>
  );
};

export default ArtistSearchCard;
