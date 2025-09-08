import React, { useState } from 'react';
import { useLocation } from 'react-router-dom';
import NavBar from '@/components/navigation/NavBar';
import useFriends from '@/hooks/useFriends';
import usePrestige, { AlbumTracksWithRankingsResponse } from '@/hooks/usePrestige';
import { useAuth0 } from '@auth0/auth0-react';
import { useMutation, useQueryClient, useQuery } from '@tanstack/react-query';
import PrestigeProgressBar from '@/components/ui/prestige-progress-bar';

const AlbumPage: React.FC = () => {
  const { getFriendsWhoListenedToAlbum, getFriendAlbumTimeListened } = useFriends();
  const { togglePinAlbum, getAlbumTracksWithRankings } = usePrestige();
  const { user } = useAuth0();
  const queryClient = useQueryClient();
  const location = useLocation();
  const album = location.state;
  
  const [showFriends, setShowFriends] = useState(false);
  const [showTracks, setShowTracks] = useState(false);
  // Individual React Query hooks for each friend's time - provides automatic caching
  const useFriendAlbumTime = (friendId: string, albumId: string, enabled: boolean) => {
    return useQuery({
      queryKey: ['friend-album-time', friendId, albumId],
      queryFn: async () => {
        const time = await getFriendAlbumTimeListened(friendId, albumId);
        return time || 0;
      },
      enabled: enabled && !!friendId && !!albumId,
      staleTime: 10 * 60 * 1000, // Cache for 10 minutes
      gcTime: 30 * 60 * 1000,
      retry: 2,
      retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 5000)
    });
  };
  const [isPinned, setIsPinned] = useState(album?.isPinned || false);

  const userId = user?.sub?.split('|').pop();

  // React Query for friends who listened to this album
  const friendsQuery = useQuery({
    queryKey: ['friends-who-listened-album', album?.albumId],
    queryFn: async () => {
      if (!album?.albumId) return [];
      return await getFriendsWhoListenedToAlbum(album.albumId);
    },
    enabled: showFriends && !!album?.albumId,
    staleTime: 10 * 60 * 1000,
    gcTime: 30 * 60 * 1000,
    refetchOnWindowFocus: false,
    retry: 2,
    retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 5000)
  });

  const { data: albumTracks, isLoading: tracksLoading, isError: tracksError } = useQuery<AlbumTracksWithRankingsResponse | null>({
    queryKey: ['albumTracks', userId, album?.albumId],
    queryFn: () => userId ? getAlbumTracksWithRankings(userId, album.albumId) : null,
    enabled: showTracks && !!userId && !!album?.albumId
  });

  const pinMutation = useMutation({
    mutationFn: async () => {
      const userId = user?.sub?.split('|').pop();
      if (userId) {
        await togglePinAlbum(userId, album.albumId);
      }
    },
    onSuccess: () => {
      setIsPinned(!isPinned);
      queryClient.invalidateQueries({ queryKey: ['pinnedItems'] });
      queryClient.invalidateQueries({ queryKey: ['topTracks'] });
      queryClient.invalidateQueries({ queryKey: ['topAlbums'] });
      queryClient.invalidateQueries({ queryKey: ['topArtists'] });
    }
  });


  const handleShowFriends = () => {
    setShowFriends(!showFriends);
  };

  const prestigeLevel = album.prestigeTier || "None";

  return (
    <div className="min-h-screen bg-gray-900 text-white flex flex-col items-center justify-center pb-20">
      <div className="relative flex flex-col items-center p-4">
        <div className="relative w-64 h-64 mb-6">
          {prestigeLevel !== "None" && (
            <img
              src={`../../src/assets/tiers/${prestigeLevel.toLowerCase().replace(' ', '')}.png`}
              alt={prestigeLevel}
              className="absolute inset-0 w-full h-full object-cover z-0 opacity-100 rounded-lg"
              style={{ transform: 'translate(-50%, -50%)', top: '50%', left: '50%' }}
            />
          )}
          <img
            src={album.imageUrl}
            alt="Album Cover"
            className="absolute inset-0 w-49 h-48 z-10 rounded-e-sm transform -translate-x-1/2 -translate-y-1/2 top-1/2 left-1/2"
          />
        </div>
        <div className="text-center z-10">
          <h2 className="text-xl font-bold mb-2">{album.albumName}</h2>
          <p className="text-lg">{album.artistName}</p>
        </div>
        <div className="flex justify-between w-full max-w-xs mb-6 z-10">
          <div className="text-center">
            <p className="text-gray-400">Minutes</p>
            <p className="text-2xl font-bold">{(album.totalTime / 60).toFixed(1)}</p>
          </div>
          <div className="text-center">
            <p className="text-gray-400">Prestige Level</p>
            <p className="text-2xl font-bold">{prestigeLevel}</p>
          </div>
        </div>

        {/* Prestige Progress Bar */}
        <div className="w-full max-w-md mb-6 px-4">
          <PrestigeProgressBar 
            itemId={album.albumId}
            itemType="albums"
            itemName={album.albumName}
            showDetails={true}
          />
        </div>
      </div>
      <div className="flex gap-4 mb-4">
        <button
          onClick={() => pinMutation.mutate()}
          className={`${isPinned ? 'bg-yellow-500 hover:bg-yellow-600' : 'bg-gray-600 hover:bg-gray-700'} text-white font-bold py-2 px-4 rounded`}
        >
          {isPinned ? '📌 Pinned' : '📌 Pin'}
        </button>
        <button
          onClick={handleShowFriends}
          className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded"
        >
          {showFriends ? 'Hide Friends' : 'Compare With Friends'}
        </button>
        <button
          onClick={() => setShowTracks(!showTracks)}
          className="bg-green-500 hover:bg-green-700 text-white font-bold py-2 px-4 rounded"
        >
          {showTracks ? 'Hide Tracks' : 'Show All Tracks'}
        </button>
      </div>
      {showFriends && (
        <div className="mt-4 w-full max-w-lg relative z-10 p-4 rounded-lg">
          {friendsQuery.isLoading && <p>Loading friends...</p>}
          {friendsQuery.isError && <p className="text-red-500">Failed to load friends who listened to this album.</p>}
          {friendsQuery.data && friendsQuery.data.length === 0 && (
            <p className="text-gray-400 text-center py-4">None of your friends have listened to this album yet.</p>
          )}
          <ul>
            {friendsQuery.data?.map((friend) => {
              const FriendItem = () => {
                const friendTimeQuery = useFriendAlbumTime(friend.id, album.albumId, showFriends);
                const friendTime = friendTimeQuery.data || 0;
                
                return (
                  <li
                    key={friend.id}
                    className="flex items-center bg-gray-700 rounded-lg p-4 mb-4 relative"
                  >
                    <div className="shrink-0 w-20 h-20 relative z-10">
                      <img
                        src={friend.profilePicUrl}
                        alt={`${friend.nickname}'s profile`}
                        className="w-full h-full object-cover rounded-full"
                      />
                    </div>
                    <div className="ml-4 relative z-10 w-3/4">
                      <h3 className="text-lg font-bold">{friend.name}</h3>
                      <p className="text-zinc-50">
                        Total Time: {friendTimeQuery.isLoading ? 'Loading...' : `${(friendTime / 60).toFixed(1)} minutes`}
                      </p>
                    </div>
                  </li>
                );
              };
              
              return <FriendItem key={friend.id} />;
            })}
          </ul>
        </div>
      )}
      {showTracks && (
        <div className="mt-4 w-full max-w-4xl relative z-10 p-4 rounded-lg">
          {tracksLoading && <p className="text-center">Loading tracks...</p>}
          {tracksError && <p className="text-center text-red-500">Failed to load tracks</p>}
          {albumTracks && (
            <div className="bg-gray-800 rounded-lg p-6">
              <div className="flex justify-between items-center mb-4">
                <h3 className="text-xl font-bold">Album Tracks</h3>
                <div className="text-sm text-gray-400">
                  {albumTracks.ratedTracks} of {albumTracks.totalTracks} tracks rated
                  {albumTracks.allTracksRated && <span className="ml-2 text-yellow-500">⭐ Complete!</span>}
                </div>
              </div>
              <div className="space-y-2">
                {albumTracks.tracks?.map((track) => {
                  return (
                    <div
                      key={track.trackId}
                      className={`flex items-center p-3 rounded-lg ${
                        track.hasUserRating 
                          ? 'bg-gray-700 text-white font-semibold' 
                          : 'bg-gray-900 text-gray-400'
                      } hover:bg-gray-600 transition-colors`}
                    >
                      <div className="w-8 text-center font-bold text-lg">
                        {track.albumRanking !== undefined && track.albumRanking !== null ? track.albumRanking : '—'}
                      </div>
                      <div className="w-8 text-center text-sm text-gray-500">
                        {track.trackNumber}
                      </div>
                      <div className="flex-1 ml-4">
                        <div className="font-medium">{track.trackName}</div>
                        <div className="text-sm text-gray-400">
                          {track.artists?.map((artist) => artist.name).join(', ')}
                        </div>
                      </div>
                      <div className="ml-4 text-right">
                        {track.isPinned && <span className="text-yellow-500">📌</span>}
                        {track.isFavorite && <span className="text-red-500">❤️</span>}
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          )}
        </div>
      )}
      <NavBar />
    </div>
  );
};

export default AlbumPage;
