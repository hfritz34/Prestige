import React, { useState } from 'react';
import { useLocation } from 'react-router-dom';
import NavBar from '@/components/navigation/NavBar';
import useFriends from '@/hooks/useFriends';
import usePrestige, { ArtistAlbumsWithRankingsResponse } from '@/hooks/usePrestige';
import { useAuth0 } from '@auth0/auth0-react';
import { useMutation, useQueryClient, useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import PrestigeProgressBar from '@/components/ui/prestige-progress-bar';

const ArtistPage: React.FC = () => {
  const { getFriendsWhoListenedToArtist, getFriendArtistTimeListened } = useFriends();
  const { togglePinArtist, getArtistAlbumsWithUserActivity } = usePrestige();
  const { user } = useAuth0();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const location = useLocation();
  const artist = location.state;
  
  const [showFriends, setShowFriends] = useState(false);
  const [showAlbums, setShowAlbums] = useState(false);
  // Individual React Query hooks for each friend's time - provides automatic caching
  const useFriendArtistTime = (friendId: string, artistId: string, enabled: boolean) => {
    return useQuery({
      queryKey: ['friend-artist-time', friendId, artistId],
      queryFn: async () => {
        const time = await getFriendArtistTimeListened(friendId, artistId);
        return time || 0;
      },
      enabled: enabled && !!friendId && !!artistId,
      staleTime: 10 * 60 * 1000, // Cache for 10 minutes
      gcTime: 30 * 60 * 1000,
      retry: 2,
      retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 5000)
    });
  };
  const [isPinned, setIsPinned] = useState(artist?.isPinned || false);

  const userId = user?.sub?.split('|').pop();

  // React Query for friends who listened to this artist
  const friendsQuery = useQuery({
    queryKey: ['friends-who-listened-artist', artist?.artistId],
    queryFn: async () => {
      if (!artist?.artistId) return [];
      return await getFriendsWhoListenedToArtist(artist.artistId);
    },
    enabled: showFriends && !!artist?.artistId,
    staleTime: 10 * 60 * 1000,
    gcTime: 30 * 60 * 1000,
    refetchOnWindowFocus: false,
    retry: 2,
    retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 5000)
  });

  // Fetch albums that the user has rated (albums only, simplified)
  const { data: artistAlbums, isLoading: albumsLoading } = useQuery<ArtistAlbumsWithRankingsResponse | null>({
    queryKey: ['artistAlbums', userId, artist?.artistId],
    queryFn: async (): Promise<ArtistAlbumsWithRankingsResponse | null> => {
      return userId ? await getArtistAlbumsWithUserActivity(userId, artist.artistId) : null;
    },
    enabled: showAlbums && !!userId && !!artist?.artistId
  });

  const pinMutation = useMutation({
    mutationFn: async () => {
      const userId = user?.sub?.split('|').pop();
      if (userId) {
        await togglePinArtist(userId, artist.artistId);
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

  const prestigeLevel = artist.prestigeTier || "None";

  const handleAlbumClick = (album: any) => {
    navigate('/prestige/album', { 
      state: { 
        albumId: album.albumId,
        albumName: album.albumName,
        artistName: album.artistName,
        imageUrl: album.albumImage,
        totalTime: album.totalListeningTime
      }
    });
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
            src={artist.imageUrl}
            alt="Album Cover"
            className="absolute inset-0 w-49 h-48 z-10 rounded-e-sm transform -translate-x-1/2 -translate-y-1/2 top-1/2 left-1/2"
          />
        </div>
        <div className="text-center z-10">
          <h2 className="text-xl font-bold mb-2">{artist.artistName}</h2>
        </div>
        <div className="flex justify-between w-full max-w-xs mb-6 z-10">
          <div className="text-center">
            <p className="text-gray-400">Minutes</p>
            <p className="text-2xl font-bold">{(artist.totalTime / 60).toFixed(1)}</p>
          </div>
          <div className="text-center">
            <p className="text-gray-400">Prestige Level</p>
            <p className="text-2xl font-bold">{prestigeLevel}</p>
          </div>
        </div>

        {/* Prestige Progress Bar */}
        <div className="w-full max-w-md mb-6 px-4">
          <PrestigeProgressBar 
            itemId={artist.artistId}
            itemType="artists"
            itemName={artist.artistName}
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
          onClick={() => setShowAlbums(!showAlbums)}
          className="bg-green-500 hover:bg-green-700 text-white font-bold py-2 px-4 rounded"
        >
          {showAlbums ? 'Hide Albums' : 'Show Rated Albums'}
        </button>
      </div>
      {showFriends && (
        <div className="mt-4 w-full max-w-lg relative z-10 p-4 rounded-lg">
          {friendsQuery.isLoading && <p>Loading friends...</p>}
          {friendsQuery.isError && <p className="text-red-500">Failed to load friends who listened to this artist.</p>}
          {friendsQuery.data && friendsQuery.data.length === 0 && (
            <p className="text-gray-400 text-center py-4">None of your friends have listened to this artist yet.</p>
          )}
          <ul>
            {friendsQuery.data?.map((friend) => {
              const FriendItem = () => {
                const friendTimeQuery = useFriendArtistTime(friend.id, artist.artistId, showFriends);
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
      {showAlbums && (
        <div className="mt-4 w-full max-w-4xl relative z-10 p-4 rounded-lg">
          {albumsLoading && <p className="text-center">Loading albums...</p>}
          {artistAlbums && (
            <div className="bg-gray-800 rounded-lg p-6">
              <div className="flex justify-between items-center mb-4">
                <h3 className="text-xl font-bold">Rated Albums</h3>
                <div className="text-sm text-gray-400">
                  {artistAlbums?.totalAlbums || 0} albums
                </div>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {artistAlbums?.albums?.map((album) => (
                  <div
                    key={album.albumId}
                    onClick={() => handleAlbumClick(album)}
                    className="flex items-center p-4 bg-gray-700 rounded-lg cursor-pointer hover:bg-gray-600 transition-colors"
                  >
                    <div className="w-16 h-16 flex-shrink-0">
                      <img
                        src={album.albumImage || '/placeholder-album.png'}
                        alt={album.albumName}
                        className="w-full h-full object-cover rounded-lg"
                      />
                    </div>
                    <div className="ml-4 flex-1">
                      <h4 className="font-semibold text-white">{album.albumName}</h4>
                      <p className="text-sm text-gray-300">{album.artistName}</p>
                      <div className="flex items-center gap-4 mt-2 text-sm text-gray-400">
                        <span>Score: {album.albumRatingScore?.toFixed?.(1) ?? album.albumRatingScore}</span>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
              {(!artistAlbums?.albums || artistAlbums.albums.length === 0) && (
                <div className="text-center py-8 text-gray-400">
                  <p>No rated albums yet.</p>
                  <p>Rate some albums to see them here!</p>
                </div>
              )}
            </div>
          )}
        </div>
      )}
      <NavBar />
    </div>
  );
};

export default ArtistPage;
