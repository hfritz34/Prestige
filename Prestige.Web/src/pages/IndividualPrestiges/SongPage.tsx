import React, { useState } from 'react';
import { useLocation } from 'react-router-dom';
import NavBar from '@/components/navigation/NavBar';
import useFriends from '@/hooks/useFriends';
import usePrestige from '@/hooks/usePrestige';

const SongPage: React.FC = () => {
  const { getFriendsWhoListenedToTrack, getFriendTrackTimeListened, friends, loading } = useFriends();
  const { getTrackPrestigeTier } = usePrestige();
  const [showFriends, setShowFriends] = useState(false);
  const [friendTimes, setFriendTimes] = useState<{ [key: string]: number }>({});

  const location = useLocation();
  const track = location.state;

  const handleShowFriends = async () => {
    if (!showFriends) {
      await getFriendsWhoListenedToTrack(track.trackId);
      const times = await Promise.all(
        friends.map(async (friend) => {
          const time = await getFriendTrackTimeListened(friend.id, track.trackId);
          return { [friend.id]: time };
        })
      );
      setFriendTimes(Object.assign({}, ...times));
    }
    setShowFriends(!showFriends);
  };

  const prestigeLevel = getTrackPrestigeTier(track.totalTime) || "None";

  return (
    <div className="min-h-screen bg-gray-900 text-white flex flex-col items-center justify-center pb-20">
      <div className="relative flex flex-col items-center p-4">
        <div className="relative w-64 h-64 mb-6">
          {prestigeLevel !== "None" && (
            <img
              src={`../../src/assets/tiers/${prestigeLevel}.png`}
              alt={prestigeLevel}
              className="absolute inset-0 w-full h-full object-cover z-0 opacity-100"
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
          <h2 className="text-xl font-bold mb-2">{track.trackName}</h2>
          <p className="text-lg">{track.artistName}</p>
          <p className="text-lg mb-4">{track.albumName}</p>
        </div>
        <div className="flex justify-between w-full max-w-xs mb-6 z-10">
          <div className="text-center">
            <p className="text-gray-400">Minutes</p>
            <p className="text-2xl font-bold">{(track.totalTime / 60).toFixed(1)}</p>
          </div>
          <div className="text-center">
            <p className="text-gray-400">Prestige Level</p>
            <p className="text-2xl font-bold">{prestigeLevel}</p>
          </div>
        </div>
      </div>
      <button
        onClick={handleShowFriends}
        className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded mb-4"
      >
        {showFriends ? 'Hide Friends' : 'Compare With Friends'}
      </button>
      {showFriends && (
        <div className="mt-4 w-full max-w-lg relative z-10 p-4 rounded-lg">
          {loading && <p>Loading friends...</p>}
          <ul>
            {friends.map((friend) => {
              const friendPrestige = getTrackPrestigeTier(friendTimes[friend.id]) || "None";
              return (
                <li
                  key={friend.id}
                  className="flex items-center bg-gray-700 rounded-lg p-4 mb-4 relative"
                >
                  {friendPrestige !== "None" && (
                    <img
                      src={`../../src/assets/tiers/${friendPrestige}.png`}
                      alt={friendPrestige}
                      className="absolute inset-0 w-full h-full object-cover rounded-lg z-0"
                    />
                  )}
                  <div className="flex-shrink-0 w-20 h-20 relative z-10">
                    <img src={friend.profilePicUrl} alt={`${friend.nickname}'s profile`} className="w-full h-full object-cover rounded-full" />
                  </div>
                  <div className="ml-4 relative z-10 w-3/4">
                    <h3 className="text-lg font-bold">{friend.name}</h3>
                    <p className="text-zinc-50">Total Time: {(friendTimes[friend.id] / 60).toFixed(1)} minutes</p>
                    <p className="text-zinc-50">Prestige Level: {friendPrestige}</p>
                  </div>
                </li>
              );
            })}
          </ul>
        </div>
      )}
      <NavBar />
    </div>
  );
};

export default SongPage;
