import React from 'react';
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";
import placeholderImage from "@/assets/music.jpg";
import { TrackResponse } from '@/hooks/useSpotify';

interface TrackSearchCardProps {
  track : TrackResponse;
  onClick: () => void;
}

const TrackSearchCard: React.FC<TrackSearchCardProps> = ({ track, onClick }) => {
  return (
    <Card onClick={onClick} className="h-24 cursor-pointer flex items-center border-none rounded-md hover:bg-accent transition duration-200">
      <CardHeader className='flex justify-center items-center p-0 w-1/4'>
        <img src={track.album.images.at(0)?.url || placeholderImage} alt={track.name} className="w-12 h-12 object-cover" />
      </CardHeader>
      <CardContent className='flex flex-col w-7/12 p-0'>
        <CardTitle className="text-xl overflow-hidden text-ellipsis whitespace-nowrap">{track.name}</CardTitle>
        <div id="artists_list" className="overflow-hidden text-ellipsis whitespace-nowrap" >
          {track.artists.flatMap((artist) => artist.name + " ")}
        </div>
        <p className="text-xs text-gray-400 overflow-hidden text-ellipsis whitespace-nowrap" key={track.album.id}>{track.album.name}</p>
      </CardContent>
    </Card>
  );
};

export default TrackSearchCard;
