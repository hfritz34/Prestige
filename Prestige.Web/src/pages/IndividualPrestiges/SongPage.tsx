import React, { useState } from 'react';
import { useLocation } from 'react-router-dom';
import NavBar from '@/components/navigation/NavBar';
import useFriends from '@/hooks/useFriends';
import usePrestige, { AlbumTracksWithRankingsResponse } from '@/hooks/usePrestige';
import useCurrentlyPlaying from '@/hooks/useCurrentlyPlaying';
import { motion } from 'framer-motion';
import { useAuth0 } from '@auth0/auth0-react';
import { useMutation, useQueryClient, useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import PrestigeProgressBar from '@/components/ui/prestige-progress-bar';

const SongPage: React.FC = () => {
  const { getFriendsWhoListenedToTrack, getFriendTrackTimeListened } = useFriends();
  const { togglePinTrack, getAlbumTracksWithRankings } = usePrestige();
  const { currentlyPlaying } = useCurrentlyPlaying();
  const { user } = useAuth0();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const location = useLocation();
  const track = location.state;
  
  console.log("SongPage - Received track data:", track);
  
  const [showFriends, setShowFriends] = useState(false);
  // Individual React Query hooks for each friend's time - provides automatic caching
  const useFriendTrackTime = (friendId: string, trackId: string, enabled: boolean) => {
    return useQuery({
      queryKey: ['friend-track-time', friendId, trackId],
      queryFn: async () => {
        const time = await getFriendTrackTimeListened(friendId, trackId);
        return time || 0;
      },
      enabled: enabled && !!friendId && !!trackId,
      staleTime: 10 * 60 * 1000, // Cache for 10 minutes
      gcTime: 30 * 60 * 1000,
      retry: 2,
      retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 5000)
    });
  };
  const [isPinned, setIsPinned] = useState(track?.isPinned || false);
  
  const userId = user?.sub?.split('|').pop();
  const isNowPlaying = currentlyPlaying?.track?.id === track?.trackId;

  // React Query for friends who listened to this track
  const friendsQuery = useQuery({
    queryKey: ['friends-who-listened-track', track?.trackId],
    queryFn: async () => {
      if (!track?.trackId) return [];
      return await getFriendsWhoListenedToTrack(track.trackId);
    },
    enabled: showFriends && !!track?.trackId,
    staleTime: 10 * 60 * 1000,
    gcTime: 30 * 60 * 1000,
    refetchOnWindowFocus: false,
    retry: 2,
    retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 5000)
  });

  // Extract album ID from track data - we'll need this to fetch album context
  const albumId = track?.albumId || track?.track?.album?.id;

  // Fetch album context to show track ranking within album
  const { data: albumTracks, isLoading: albumLoading } = useQuery<AlbumTracksWithRankingsResponse | null>({
    queryKey: ['albumTracks', userId, albumId],
    queryFn: () => userId && albumId ? getAlbumTracksWithRankings(userId, albumId) : null,
    enabled: !!userId && !!albumId
  });

  // Find this track within the album tracks
  const trackInAlbum = albumTracks?.tracks?.find(t => t.trackId === track?.trackId);
  const albumRanking = trackInAlbum?.albumRanking;
  const totalTracks = albumTracks?.totalTracks;

  const pinMutation = useMutation({
    mutationFn: async () => {
      const userId = user?.sub?.split('|').pop();
      if (userId) {
        await togglePinTrack(userId, track.trackId);
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

  const prestigeLevel = track.prestigeTier || "None";

  const handleViewAlbum = () => {
    if (albumId && track?.albumName) {
      navigate('/prestige/album', { 
        state: { 
          albumId: albumId,
          albumName: track.albumName,
          artistName: track.artistName,
          imageUrl: track.imageUrl,
          totalTime: 0 // Will be populated by album page
        }
      });
    }
  };

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
            src={track.imageUrl}
            alt="Album Cover"
            className="absolute inset-0 w-49 h-48 z-10 rounded-e-sm transform -translate-x-1/2 -translate-y-1/2 top-1/2 left-1/2"
          />
        </div>
        <div className="text-center z-10">
          <h2 className="text-xl font-bold mb-2">
            {track.trackName}
            {isNowPlaying && (
              <motion.span
                className="ml-3 text-blue-400 text-sm font-normal"
                initial={{ opacity: 0, scale: 0.8 }}
                animate={{ opacity: 1, scale: 1 }}
                transition={{ duration: 0.3 }}
              >
                🎵 Now Playing
              </motion.span>
            )}
          </h2>
          <p className="text-lg">{track.artistName}</p>
          <p className="text-lg mb-4">{track.albumName}</p>
        </div>
        <div className="flex justify-between w-full max-w-sm mb-6 z-10">
          <div className="text-center">
            <p className="text-gray-400">Minutes</p>
            <p className="text-2xl font-bold">{(track.totalTime / 60).toFixed(1)}</p>
          </div>
          <div className="text-center">
            <p className="text-gray-400">Prestige Level</p>
            <p className="text-2xl font-bold">{prestigeLevel}</p>
          </div>
          <div className="text-center">
            <p className="text-gray-400">Album Rank</p>
            <p className="text-2xl font-bold">
              {albumLoading
                ? '...'
                : albumRanking && totalTracks
                  ? `🏆 #${albumRanking} of ${totalTracks}`
                  : '—'}
            </p>
          </div>
        </div>

        {/* Prestige Progress Bar */}
        <div className="w-full max-w-md mb-6 px-4">
          <PrestigeProgressBar 
            itemId={track.trackId}
            itemType="tracks"
            itemName={track.trackName}
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
        {albumId && (
          <button
            onClick={handleViewAlbum}
            className="bg-green-500 hover:bg-green-700 text-white font-bold py-2 px-4 rounded"
          >
            View Album
          </button>
        )}
      </div>
      {showFriends && (
        <div className="mt-4 w-full max-w-lg relative z-10 p-4 rounded-lg">
          {friendsQuery.isLoading && <p>Loading friends...</p>}
          {friendsQuery.isError && <p className="text-red-500">Failed to load friends who listened to this track.</p>}
          {friendsQuery.data && friendsQuery.data.length === 0 && (
            <p className="text-gray-400 text-center py-4">None of your friends have listened to this track yet.</p>
          )}
          <ul>
            {friendsQuery.data?.map((friend) => {
              const FriendItem = () => {
                const friendTimeQuery = useFriendTrackTime(friend.id, track.trackId, showFriends);
                const friendTime = friendTimeQuery.data || 0;
                
                return (
                  <li
                    key={friend.id}
                    className="flex items-center bg-gray-700 rounded-lg p-4 mb-4 relative"
                  >
                    <div className="shrink-0 w-20 h-20 relative z-10">
                      <img src={friend.profilePicUrl} alt={`${friend.nickname}'s profile`} className="w-full h-full object-cover rounded-full" />
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
      <NavBar />
    </div>
  );
};

export default SongPage;
