import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import NavBar from '@/components/navigation/NavBar';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import RatingItemCard from './components/RatingItemCard';
import RatingModal from './components/RatingModal';
import useProfile from '@/hooks/useProfile';
import useSpotify from '@/hooks/useSpotify';
import useRating from '@/hooks/useRating';

interface RatingItem {
  id: string;
  name: string;
  subtitle: string;
  imageUrl?: string;
  type: 'track' | 'album' | 'artist';
}

const RatingPage: React.FC = () => {
  const { getTopTracks, getTopAlbums, getTopArtists, getRecentlyPlayed } = useProfile();
  const { getLikedTracks } = useSpotify();
  const { startRating } = useRating();
  
  const [selectedItem, setSelectedItem] = useState<RatingItem | null>(null);
  const [isRatingModalOpen, setIsRatingModalOpen] = useState(false);

  // Fetch prestige data
  const { data: topTracks, isLoading: tracksLoading } = useQuery({
    queryKey: ['topTracks'],
    queryFn: getTopTracks
  });

  const { data: topAlbums, isLoading: albumsLoading } = useQuery({
    queryKey: ['topAlbums'],
    queryFn: getTopAlbums
  });

  const { data: topArtists, isLoading: artistsLoading } = useQuery({
    queryKey: ['topArtists'],
    queryFn: getTopArtists
  });

  // Fetch recently played
  const { data: recentlyPlayed, isLoading: recentLoading } = useQuery({
    queryKey: ['recentlyPlayed'],
    queryFn: getRecentlyPlayed
  });

  // Fetch liked songs
  const { data: likedTracks, isLoading: likedLoading } = useQuery({
    queryKey: ['likedTracks'],
    queryFn: () => getLikedTracks(100)
  });

  const handleRate = (id: string, type: string, name: string, subtitle: string, imageUrl?: string) => {
    setSelectedItem({
      id,
      name,
      subtitle,
      imageUrl,
      type: type as 'track' | 'album' | 'artist'
    });
    setIsRatingModalOpen(true);
  };

  const handleRatingComplete = async (itemId: string, partition: 'loved' | 'liked' | 'disliked', finalScore: number) => {
    try {
      const itemType = selectedItem?.type || 'track';
      console.log(`Rating completed: ${itemType} ${itemId} with score ${finalScore} in partition ${partition}`);
      
      // Start the rating process with the backend
      await startRating(itemType, itemId);
      
      // TODO: Submit the comparison results and final score
      
      setIsRatingModalOpen(false);
      setSelectedItem(null);
    } catch (error) {
      console.error('Error completing rating:', error);
    }
  };

  const prestigeItems = [
    ...(topTracks?.map(item => ({
      id: item.track.id,
      name: item.track.name,
      subtitle: `${item.track.artists.map(a => a.name).join(', ')} • ${item.track.album.name}`,
      imageUrl: item.track.album.images[0]?.url,
      type: 'track' as const,
      totalTime: item.totalTime
    })) || []),
    ...(topAlbums?.map(item => ({
      id: item.album.id,
      name: item.album.name,
      subtitle: item.album.artists.map(a => a.name).join(', '),
      imageUrl: item.album.images[0]?.url,
      type: 'album' as const,
      totalTime: item.totalTime
    })) || []),
    ...(topArtists?.map(item => ({
      id: item.artist.id,
      name: item.artist.name,
      subtitle: 'Artist',
      imageUrl: item.artist.images[0]?.url,
      type: 'artist' as const,
      totalTime: item.totalTime
    })) || [])
  ];

  const recentItems = recentlyPlayed?.map(item => ({
    id: item.id,
    name: item.trackName,
    subtitle: item.artistName,
    imageUrl: item.imageUrl,
    type: 'track' as const
  })) || [];

  const likedItems = likedTracks?.map(track => ({
    id: track.id,
    name: track.name,
    subtitle: `${track.artists.map(a => a.name).join(', ')} • ${track.album.name}`,
    imageUrl: track.album.images[0]?.url,
    type: 'track' as const
  })) || [];

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pb-20">
        <h1 className="text-3xl font-bold text-gray-900 dark:text-white py-6">
          Rate Your Music
        </h1>
        
        <Tabs defaultValue="prestige" className="w-full">
          <TabsList className="grid w-full grid-cols-3">
            <TabsTrigger value="prestige">My Prestige</TabsTrigger>
            <TabsTrigger value="recent">Recently Played</TabsTrigger>
            <TabsTrigger value="liked">Liked Songs</TabsTrigger>
          </TabsList>
          
          <TabsContent value="prestige">
            <div className="bg-white dark:bg-gray-800 rounded-lg p-6 mt-4">
              <h2 className="text-xl font-semibold mb-4">Rate from Your Prestige Collection</h2>
              {tracksLoading || albumsLoading || artistsLoading ? (
                <div className="flex justify-center py-8">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                </div>
              ) : (
                <div className="space-y-3">
                  {prestigeItems.length > 0 ? (
                    prestigeItems.map((item) => (
                      <RatingItemCard
                        key={`${item.type}-${item.id}`}
                        id={item.id}
                        name={item.name}
                        subtitle={item.subtitle}
                        imageUrl={item.imageUrl}
                        type={item.type}
                        onRate={handleRate}
                      />
                    ))
                  ) : (
                    <p className="text-gray-600 dark:text-gray-400 text-center py-8">
                      No prestige items found. Start listening to build your collection!
                    </p>
                  )}
                </div>
              )}
            </div>
          </TabsContent>
          
          <TabsContent value="recent">
            <div className="bg-white dark:bg-gray-800 rounded-lg p-6 mt-4">
              <h2 className="text-xl font-semibold mb-4">Rate Recently Played</h2>
              {recentLoading ? (
                <div className="flex justify-center py-8">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                </div>
              ) : (
                <div className="space-y-3">
                  {recentItems.length > 0 ? (
                    recentItems.slice(0, 50).map((item, index) => (
                      <RatingItemCard
                        key={`recent-${item.id}-${index}`}
                        id={item.id}
                        name={item.name}
                        subtitle={item.subtitle}
                        imageUrl={item.imageUrl}
                        type={item.type}
                        onRate={handleRate}
                      />
                    ))
                  ) : (
                    <p className="text-gray-600 dark:text-gray-400 text-center py-8">
                      No recently played tracks found.
                    </p>
                  )}
                </div>
              )}
            </div>
          </TabsContent>
          
          <TabsContent value="liked">
            <div className="bg-white dark:bg-gray-800 rounded-lg p-6 mt-4">
              <h2 className="text-xl font-semibold mb-4">Rate Your Liked Songs</h2>
              {likedLoading ? (
                <div className="flex justify-center py-8">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                </div>
              ) : (
                <div className="space-y-3">
                  {likedItems.length > 0 ? (
                    likedItems.map((item) => (
                      <RatingItemCard
                        key={`liked-${item.id}`}
                        id={item.id}
                        name={item.name}
                        subtitle={item.subtitle}
                        imageUrl={item.imageUrl}
                        type={item.type}
                        onRate={handleRate}
                      />
                    ))
                  ) : (
                    <p className="text-gray-600 dark:text-gray-400 text-center py-8">
                      No liked songs found. Like some songs on Spotify to see them here!
                    </p>
                  )}
                </div>
              )}
            </div>
          </TabsContent>
        </Tabs>
      </div>
      
      <RatingModal
        isOpen={isRatingModalOpen}
        onClose={() => {
          setIsRatingModalOpen(false);
          setSelectedItem(null);
        }}
        item={selectedItem}
        onComplete={handleRatingComplete}
      />
      
      <NavBar />
    </div>
  );
};

export default RatingPage;