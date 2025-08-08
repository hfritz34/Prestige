import React, { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Button } from '@/components/ui/button';
import NavBar from '@/components/navigation/NavBar';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import RatingItemCard from './components/RatingItemCard';
import RatingModal from './components/RatingModal';
import useProfile from '@/hooks/useProfile';
import useRating from '@/hooks/useRating';

interface RatingItem {
  id: string;
  name: string;
  subtitle: string;
  imageUrl?: string;
  type: 'track' | 'album' | 'artist';
  albumId?: string;
}

const RatingPage: React.FC = () => {
  const { getTopTracks, getTopAlbums, getTopArtists, getRecentlyPlayed, getRecentlyPlayedAlbums, getRecentlyPlayedArtists } = useProfile();
  const { getUserRatings, saveRating, deleteRating } = useRating();
  const queryClient = useQueryClient();
  
  const [selectedItem, setSelectedItem] = useState<RatingItem | null>(null);
  const [isRatingModalOpen, setIsRatingModalOpen] = useState(false);
  const [selectedType, setSelectedType] = useState<'track' | 'album' | 'artist'>('track');

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

  const { data: recentlyPlayedAlbums, isLoading: recentAlbumsLoading } = useQuery({
    queryKey: ['recentlyPlayedAlbums'],
    queryFn: getRecentlyPlayedAlbums
  });

  const { data: recentlyPlayedArtists, isLoading: recentArtistsLoading } = useQuery({
    queryKey: ['recentlyPlayedArtists'],
    queryFn: getRecentlyPlayedArtists
  });

  // Fetch existing ratings for each type
  const { data: trackRatings, isLoading: trackRatingsLoading } = useQuery({
    queryKey: ['ratings', 'track'],
    queryFn: () => getUserRatings('track')
  });

  const { data: albumRatings, isLoading: albumRatingsLoading } = useQuery({
    queryKey: ['ratings', 'album'],
    queryFn: () => getUserRatings('album')
  });

  const { data: artistRatings, isLoading: artistRatingsLoading } = useQuery({
    queryKey: ['ratings', 'artist'],
    queryFn: () => getUserRatings('artist')
  });

  const handleRate = (id: string, type: string, name: string, subtitle: string, imageUrl?: string, albumId?: string) => {
    setSelectedItem({
      id,
      name,
      subtitle,
      imageUrl,
      type: type as 'track' | 'album' | 'artist',
      albumId
    });
    setIsRatingModalOpen(true);
  };

  const handleRatingComplete = async (itemId: string, _partition: 'loved' | 'liked' | 'disliked', finalScore: number) => {
    try {
      const itemType = selectedItem?.type || 'track';
      console.log(`Rating completed: ${itemType} ${itemId} with score ${finalScore}`);
      
      // Determine category ID based on partition and score
      const getCategoryId = (_partition: 'loved' | 'liked' | 'disliked', score: number) => {
        if (score >= 7) return 1; // Loved category
        if (score >= 4) return 2; // Liked category  
        return 3; // Disliked category
      };

      const categoryId = getCategoryId(_partition, finalScore);
      
      // Save the rating to the backend
      await saveRating(itemType, itemId, finalScore, categoryId);
      console.log(`Rating saved successfully: ${itemType} ${itemId} score ${finalScore}`);
      
      setIsRatingModalOpen(false);
      setSelectedItem(null);
      
      // Invalidate and refetch rating queries to update the UI
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['ratings', 'track'] }),
        queryClient.invalidateQueries({ queryKey: ['ratings', 'album'] }),
        queryClient.invalidateQueries({ queryKey: ['ratings', 'artist'] })
      ]);
    } catch (error) {
      console.error('Error completing rating:', error);
    }
  };

  // Helper function to check if item is already rated
  const isItemRated = (itemId: string, type: 'track' | 'album' | 'artist') => {
    const ratings = type === 'track' ? trackRatings : type === 'album' ? albumRatings : artistRatings;
    return Array.isArray(ratings) ? ratings.some(rating => rating.itemId === itemId) : false;
  };

  // Helper function to get item rating score
  const getItemRating = (itemId: string, type: 'track' | 'album' | 'artist') => {
    const ratings = type === 'track' ? trackRatings : type === 'album' ? albumRatings : artistRatings;
    return Array.isArray(ratings) ? ratings.find(rating => rating.itemId === itemId)?.personalScore : undefined;
  };

  const handleUnrate = async (itemId: string, type: 'track' | 'album' | 'artist') => {
    try {
      await deleteRating(type, itemId);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['ratings', 'track'] }),
        queryClient.invalidateQueries({ queryKey: ['ratings', 'album'] }),
        queryClient.invalidateQueries({ queryKey: ['ratings', 'artist'] })
      ]);
    } catch (e) {
      console.error('Failed to delete rating', e);
    }
  };

  // Process tracks
  const trackItems = topTracks?.map(item => ({
    id: item.track.id,
    name: item.track.name,
    subtitle: `${item.track.artists.map(a => a.name).join(', ')} • ${item.track.album.name}`,
    imageUrl: item.track.album.images[0]?.url,
    type: 'track' as const,
    totalTime: item.totalTime,
    isRated: isItemRated(item.track.id, 'track'),
    score: getItemRating(item.track.id, 'track'),
    albumId: item.track.album.id
  })) || [];

  // Process albums  
  const albumItems = topAlbums?.map(item => ({
    id: item.album.id,
    name: item.album.name,
    subtitle: item.album.artists.map(a => a.name).join(', '),
    imageUrl: item.album.images[0]?.url,
    type: 'album' as const,
    totalTime: item.totalTime,
    isRated: isItemRated(item.album.id, 'album'),
    score: getItemRating(item.album.id, 'album'),
    albumId: undefined
  })) || [];

  // Process artists
  const artistItems = topArtists?.map(item => ({
    id: item.artist.id,
    name: item.artist.name,
    subtitle: 'Artist',
    imageUrl: item.artist.images[0]?.url,
    type: 'artist' as const,
    totalTime: item.totalTime,
    isRated: isItemRated(item.artist.id, 'artist'),
    score: getItemRating(item.artist.id, 'artist'),
    albumId: undefined
  })) || [];

  // Recent items - only tracks from recently played, deduplicated
  const recentTrackItems = recentlyPlayed ? 
    Array.from(new Map(recentlyPlayed.map(item => [
      item.id, 
      {
        id: item.id,
        name: item.trackName,
        subtitle: item.artistName,
        imageUrl: item.imageUrl,
        type: 'track' as const,
        isRated: isItemRated(item.id, 'track'),
        score: getItemRating(item.id, 'track'),
        albumId: undefined // Recently played doesn't have album info yet
      }
    ])).values())
    : [];

  // Process recently played albums
  const recentAlbumItems = recentlyPlayedAlbums?.map(item => ({
    id: item.id,
    name: item.albumName,
    subtitle: item.artistName,
    imageUrl: item.imageUrl,
    type: 'album' as const,
    isRated: isItemRated(item.id, 'album'),
    score: getItemRating(item.id, 'album'),
    albumId: undefined
  })) || [];

  // Process recently played artists
  const recentArtistItems = recentlyPlayedArtists?.map(item => ({
    id: item.id,
    name: item.artistName,
    subtitle: 'Artist',
    imageUrl: item.imageUrl,
    type: 'artist' as const,
    isRated: isItemRated(item.id, 'artist'),
    score: getItemRating(item.id, 'artist'),
    albumId: undefined
  })) || [];

  // Get current type items
  const getCurrentTypeItems = () => {
    switch (selectedType) {
      case 'track': return trackItems;
      case 'album': return albumItems;
      case 'artist': return artistItems;
    }
  };

  const currentItems = getCurrentTypeItems();
  const unratedItems = currentItems.filter(item => !item.isRated);
  const ratedItems = currentItems.filter(item => item.isRated);
  
  // Get recent items based on selected type
  const getRecentItemsByType = () => {
    switch (selectedType) {
      case 'track': return recentTrackItems;
      case 'album': return recentAlbumItems;
      case 'artist': return recentArtistItems;
    }
  };
  
  const recentItems = getRecentItemsByType();
  const unratedRecentItems = recentItems.filter(item => !item.isRated);

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pb-20">
        <h1 className="text-3xl font-bold text-gray-900 dark:text-white py-6">
          Rate Your Music
        </h1>
        
        {/* Type Selector */}
        <div className="mb-6">
          <Tabs value={selectedType} onValueChange={(value) => setSelectedType(value as 'track' | 'album' | 'artist')} className="w-full">
            <TabsList className="grid w-full grid-cols-3">
              <TabsTrigger value="track">Tracks</TabsTrigger>
              <TabsTrigger value="album">Albums</TabsTrigger>
              <TabsTrigger value="artist">Artists</TabsTrigger>
            </TabsList>
          </Tabs>
        </div>
        
        {/* Content Tabs */}
        <Tabs defaultValue="unrated" className="w-full">
          <TabsList className="grid w-full grid-cols-3">
            <TabsTrigger value="unrated">Unrated</TabsTrigger>
            <TabsTrigger value="recent">Recently Played</TabsTrigger>
            <TabsTrigger value="rated">Rate Again</TabsTrigger>
          </TabsList>
          
          <TabsContent value="unrated">
            <div className="bg-white dark:bg-gray-800 rounded-lg p-6 mt-4">
              <h2 className="text-xl font-semibold mb-4">
                Unrated {selectedType.charAt(0).toUpperCase() + selectedType.slice(1)}s from Your Collection
              </h2>
              {tracksLoading || albumsLoading || artistsLoading || trackRatingsLoading || albumRatingsLoading || artistRatingsLoading ? (
                <div className="flex justify-center py-8">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                </div>
              ) : (
                <div className="space-y-3">
                  {unratedItems.length > 0 ? (
                    unratedItems.map((item) => (
                      <RatingItemCard
                        key={`${item.type}-${item.id}`}
                        id={item.id}
                        name={item.name}
                        subtitle={item.subtitle}
                        imageUrl={item.imageUrl}
                        type={item.type}
                        onRate={handleRate}
                        albumId={item.albumId}
                      />
                    ))
                  ) : (
                    <p className="text-gray-600 dark:text-gray-400 text-center py-8">
                      No unrated {selectedType}s found. All your {selectedType}s have been rated!
                    </p>
                  )}
                </div>
              )}
            </div>
          </TabsContent>
          
          <TabsContent value="recent">
            <div className="bg-white dark:bg-gray-800 rounded-lg p-6 mt-4">
              <h2 className="text-xl font-semibold mb-4">
                Recently Played {selectedType.charAt(0).toUpperCase() + selectedType.slice(1)}s
              </h2>
              {recentLoading || recentAlbumsLoading || recentArtistsLoading || trackRatingsLoading || albumRatingsLoading || artistRatingsLoading ? (
                <div className="flex justify-center py-8">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                </div>
              ) : (
                <div className="space-y-3">
                  {unratedRecentItems.length > 0 ? (
                    unratedRecentItems.slice(0, 50).map((item, index) => (
                      <RatingItemCard
                        key={`recent-${item.id}-${index}`}
                        id={item.id}
                        name={item.name}
                        subtitle={item.subtitle}
                        imageUrl={item.imageUrl}
                        type={item.type}
                        onRate={handleRate}
                        albumId={item.albumId}
                      />
                    ))
                  ) : (
                    <p className="text-gray-600 dark:text-gray-400 text-center py-8">
                      No unrated recently played {selectedType}s found.
                    </p>
                  )}
                </div>
              )}
            </div>
          </TabsContent>
          
          <TabsContent value="rated">
            <div className="bg-white dark:bg-gray-800 rounded-lg p-6 mt-4">
              <h2 className="text-xl font-semibold mb-4">
                Your Rated {selectedType.charAt(0).toUpperCase() + selectedType.slice(1)}s
              </h2>
              {tracksLoading || albumsLoading || artistsLoading || trackRatingsLoading || albumRatingsLoading || artistRatingsLoading ? (
                <div className="flex justify-center py-8">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                </div>
              ) : (
                <div className="space-y-3">
                  {ratedItems.length > 0 ? (
                    ratedItems.map((item) => (
                      <div key={`rated-${item.id}`} className="grid grid-cols-[1fr_auto] items-center gap-3">
                        <RatingItemCard
                          id={item.id}
                          name={item.name}
                          subtitle={`${item.subtitle} • Score: ${item.score?.toFixed(1) || 'N/A'}`}
                          imageUrl={item.imageUrl}
                          type={item.type}
                          onRate={handleRate}
                          albumId={item.albumId}
                        />
                        <div className="justify-self-end">
                          <Button variant="destructive" size="sm" onClick={() => handleUnrate(item.id, item.type)}>
                            Remove rating
                          </Button>
                        </div>
                      </div>
                    ))
                  ) : (
                    <p className="text-gray-600 dark:text-gray-400 text-center py-8">
                      No rated {selectedType}s found. Start rating to see them here!
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
        topTracks={topTracks || []}
        topAlbums={topAlbums || []}
        topArtists={topArtists || []}
      />
      
      <NavBar />
    </div>
  );
};

export default RatingPage;