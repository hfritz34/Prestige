import { useAuth0 } from '@auth0/auth0-react';
import useHttp from './useHttp';
import { UserAlbumResponse, UserArtistResponse, UserTrackResponse } from './useProfile';
import { useEffect, useState } from 'react';

export type Friend = {
  id: string;
  name: string;
  profilePicUrl: string;
  nickname: string;
  favoriteTracks: UserTrackResponse[];
  favoriteAlbums: UserAlbumResponse[];
  favoriteArtists: UserArtistResponse[];
  topTracks: UserTrackResponse[];
  topAlbums: UserAlbumResponse[];
  topArtists: UserArtistResponse[];
};

const useFriends = () => {
  const { getMany, getOne, post, deleteOne } = useHttp();
  const { user } = useAuth0();
  const userId = user?.sub?.split("|").pop();
  const [friends, setFriends] = useState<Friend[]>([]);
  const [loading, setLoading] = useState(true);

  const baseEndpoint = `api/Friendships/${userId}`;

  const getAllFriends = async () : Promise<Friend[]> => getMany<Friend>(`${baseEndpoint}/friends`);
  const addFriend = async (friendId: string) : Promise<Friend> => post<Friend, { friendId: string }>({ friendId }, `${baseEndpoint}/friends`);  
  const removeFriend = async (friendId: string) : Promise<Friend> => deleteOne<Friend>(`${baseEndpoint}/friends/${friendId}`);
  const getFriend = async (friendId: string) : Promise<Friend> => getOne<Friend>(`${baseEndpoint}/friends/${friendId}`);

  
  const getFriendTrackTimeListened = async (friendId: string, trackId: string): Promise<number | null> => {
    const response = await getOne<number | null>(`api/Friendships/friend/${friendId}/track/${trackId}`);
    return response;
};

const getFriendArtistTimeListened = async (friendId: string, artistId: string): Promise<number | null> => {
  const response = await getOne<number | null>(`api/Friendships/friend/${friendId}/artist/${artistId}`);
  return response;
};

const getFriendAlbumTimeListened = async (friendId: string, albumId: string): Promise<number | null> => {
  const response = await getOne<number | null>(`api/Friendships/friend/${friendId}/album/${albumId}`);
  return response;
};

const getFriendsWhoListenedToTrack = async (trackId: string) => {
  setLoading(true);
  const friendsData = await getMany<Friend>(`${baseEndpoint}/friends/listened-to-track/${trackId}`);
  setFriends(friendsData);
  setLoading(false);
};

const getFriendsWhoListenedToArtist = async (artistId: string) => {
    setLoading(true);
    const friendsData = await getMany<Friend>(`${baseEndpoint}/friends/listened-to-artist/${artistId}`);
    setFriends(friendsData);
    setLoading(false);
};

const getFriendsWhoListenedToAlbum = async (albumId: string) => {
    setLoading(true);
    const friendsData = await getMany<Friend>(`${baseEndpoint}/friends/listened-to-album/${albumId}`);
    setFriends(friendsData);
    setLoading(false);
};


  useEffect(() => {
    if (userId) {
      getAllFriends();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [userId]);

  return { 
    friends, 
    loading, 
    getAllFriends, 
    addFriend, 
    removeFriend, 
    getFriend, 
    getFriendTrackTimeListened, 
    getFriendArtistTimeListened, 
    getFriendAlbumTimeListened, 
    getFriendsWhoListenedToTrack,
    getFriendsWhoListenedToArtist,
    getFriendsWhoListenedToAlbum 
  };
};

export default useFriends;
