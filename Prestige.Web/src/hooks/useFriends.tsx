import { useAuth0 } from '@auth0/auth0-react';
import useHttp from './useHttp';
import { UserAlbumResponse, UserArtistResponse, UserTrackResponse } from './useProfile';

export type Friend = {
  id: string;
  name: string;
  profilePicUrl: string;
  nickname: string;
  status?: 'Pending' | 'Accepted' | 'Declined';
  requestDate?: string;
  acceptedDate?: string;
  favoriteTracks: UserTrackResponse[];
  favoriteAlbums: UserAlbumResponse[];
  favoriteArtists: UserArtistResponse[];
  topTracks: UserTrackResponse[];
  topAlbums: UserAlbumResponse[];
  topArtists: UserArtistResponse[];
};

export type ItemComparisonResponse = {
  userStats: UserStats;
  friendStats: UserStats;
  itemId: string;
  itemType: string;
  itemName: string;
  itemImageUrl: string;
  friendId: string;
  friendNickname: string;
};

export type UserStats = {
  listeningTime?: number;
  ratingScore?: number;
  prestigeTier?: string;
  position?: number;
};

const useFriends = () => {
  const { getMany, getOne, post, deleteOne } = useHttp();
  const { user } = useAuth0();
  const userId = user?.sub?.split("|").pop();

  const baseEndpoint = `api/Friendships/${userId}`;

  const getAllFriends = async () : Promise<Friend[]> => getMany<Friend>(`${baseEndpoint}/friends`);
  const addFriend = async (friendId: string) : Promise<Friend> => post<Friend, { friendId: string }>({ friendId }, `${baseEndpoint}/friends/${friendId}`);  
  const removeFriend = async (friendId: string) : Promise<Friend> => deleteOne<Friend>(`${baseEndpoint}/friends/${friendId}`);
  const getFriend = async (friendId: string) : Promise<Friend> => getOne<Friend>(`${baseEndpoint}/friends/${friendId}`);
  
  const sendFriendRequest = async (friendId: string) : Promise<Friend> => post<Friend, {}>({}, `${baseEndpoint}/friend-requests/${friendId}`);
  const acceptFriendRequest = async (friendId: string) : Promise<Friend> => post<Friend, {}>({}, `${baseEndpoint}/friend-requests/${friendId}/accept`);
  const declineFriendRequest = async (friendId: string) : Promise<Friend> => post<Friend, {}>({}, `${baseEndpoint}/friend-requests/${friendId}/decline`);
  const getFriendRequests = async () : Promise<Friend[]> => getMany<Friend>(`${baseEndpoint}/friend-requests`);

  
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

const getFriendsWhoListenedToTrack = async (trackId: string): Promise<Friend[]> => {
  return await getMany<Friend>(`${baseEndpoint}/friends/listened-to-track/${trackId}`);
};

const getFriendsWhoListenedToArtist = async (artistId: string): Promise<Friend[]> => {
  return await getMany<Friend>(`${baseEndpoint}/friends/listened-to-artist/${artistId}`);
};

const getFriendsWhoListenedToAlbum = async (albumId: string): Promise<Friend[]> => {
  return await getMany<Friend>(`${baseEndpoint}/friends/listened-to-album/${albumId}`);
};

const compareTrackWithFriend = async (trackId: string, friendId: string): Promise<ItemComparisonResponse> => {
    return await getOne<ItemComparisonResponse>(`${baseEndpoint}/compare/track/${trackId}/with/${friendId}`);
};

const compareAlbumWithFriend = async (albumId: string, friendId: string): Promise<ItemComparisonResponse> => {
    return await getOne<ItemComparisonResponse>(`${baseEndpoint}/compare/album/${albumId}/with/${friendId}`);
};

const compareArtistWithFriend = async (artistId: string, friendId: string): Promise<ItemComparisonResponse> => {
    return await getOne<ItemComparisonResponse>(`${baseEndpoint}/compare/artist/${artistId}/with/${friendId}`);
};


  // Remove the useEffect that was causing infinite loops
  // Data loading is now handled by React Query in components

  return { 
    getAllFriends, 
    addFriend, 
    removeFriend, 
    getFriend, 
    sendFriendRequest,
    acceptFriendRequest,
    declineFriendRequest,
    getFriendRequests,
    getFriendTrackTimeListened, 
    getFriendArtistTimeListened, 
    getFriendAlbumTimeListened, 
    getFriendsWhoListenedToTrack,
    getFriendsWhoListenedToArtist,
    getFriendsWhoListenedToAlbum,
    compareTrackWithFriend,
    compareAlbumWithFriend,
    compareArtistWithFriend
  };
};

export default useFriends;
