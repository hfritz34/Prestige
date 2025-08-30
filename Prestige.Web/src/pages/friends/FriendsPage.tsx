import React from 'react';
import { useNavigate } from 'react-router-dom';
import NavBar from '@/components/navigation/NavBar';
import useFriends from '@/hooks/useFriends';
import { UserSearch } from './components/UserSearch';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Button } from '@/components/ui/button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useAuth0 } from '@auth0/auth0-react';

const FriendsPage: React.FC = () => {
    const { getAllFriends, getFriendRequests, acceptFriendRequest, declineFriendRequest } = useFriends();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const { user } = useAuth0();
    const userId = user?.sub?.split("|").pop();

    const handleFriendClick = (friendId: string) => {
        navigate(`/friends/${friendId}`);
    };

    const friendsQuery = useQuery({
        queryKey: ['users', 'friends'],
        queryFn: getAllFriends,
        staleTime: 5 * 60 * 1000, // Data stays fresh for 5 minutes
        gcTime: 10 * 60 * 1000, // Cached for 10 minutes
        refetchOnWindowFocus: false, // Don't refetch on window focus
        refetchOnMount: false, // Don't refetch on component mount
        enabled: !!userId, // Only fetch if userId exists
    });

    const friendRequestsQuery = useQuery({
        queryKey: ['users', 'friend-requests'],
        queryFn: getFriendRequests,
        staleTime: 2 * 60 * 1000, // Refresh friend requests more frequently
        gcTime: 5 * 60 * 1000,
        refetchOnWindowFocus: false,
        refetchOnMount: false,
        enabled: !!userId,
    });

    const acceptRequest = useMutation({
        mutationFn: acceptFriendRequest,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['users', 'friends'] });
            queryClient.invalidateQueries({ queryKey: ['users', 'friend-requests'] });
        }
    });

    const declineRequest = useMutation({
        mutationFn: declineFriendRequest,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['users', 'friend-requests'] });
        }
    });

    const friends = friendsQuery.data;
    const friendRequests = friendRequestsQuery.data;
    const loading = friendsQuery.isLoading;

    return (
        <div className="min-h-screen flex flex-col bg-gray-900 text-white">
            <div className="flex justify-between items-center p-4 bg-gray-800">
                <h1 className="text-2xl font-bold">Friends</h1>
            </div>
            <div className="flex-1">
                <UserSearch />
                
                <Tabs defaultValue="friends" className="p-4">
                    <TabsList className="grid w-full grid-cols-2">
                        <TabsTrigger value="friends">My Friends</TabsTrigger>
                        <TabsTrigger value="requests">
                            Friend Requests {friendRequests && friendRequests.length > 0 && `(${friendRequests.length})`}
                        </TabsTrigger>
                    </TabsList>
                    
                    <TabsContent value="friends">
                        {loading && <p>Loading friends...</p>}
                        {friends && friends.length === 0 && (
                            <p className="text-gray-400 text-center py-8">No friends yet. Search for users above to send friend requests!</p>
                        )}
                        <ul className="space-y-2">
                            {friends?.map((friend) => (
                                <li
                                    key={friend.id}
                                    className="flex items-center justify-between p-4 border border-gray-700 rounded-lg hover:bg-gray-800 transition-colors"
                                >
                                    <div className="flex items-center">
                                        <img 
                                            src={friend.profilePicUrl} 
                                            alt={`${friend.nickname}'s profile`} 
                                            className="w-12 h-12 rounded-full mr-4" 
                                        />
                                        <div>
                                            <span className="text-lg font-bold">{friend.nickname}</span>
                                            <p className="text-sm text-gray-400">{friend.name}</p>
                                        </div>
                                    </div>
                                    <Button
                                        onClick={() => handleFriendClick(friend.id)}
                                        variant="outline"
                                        size="sm"
                                    >
                                        View Profile
                                    </Button>
                                </li>
                            ))}
                        </ul>
                    </TabsContent>
                    
                    <TabsContent value="requests">
                        {friendRequestsQuery.isLoading && <p>Loading friend requests...</p>}
                        {friendRequests && friendRequests.length === 0 && (
                            <p className="text-gray-400 text-center py-8">No pending friend requests.</p>
                        )}
                        <ul className="space-y-2">
                            {friendRequests?.map((request) => (
                                <li
                                    key={request.id}
                                    className="flex items-center justify-between p-4 border border-gray-700 rounded-lg hover:bg-gray-800 transition-colors"
                                >
                                    <div className="flex items-center">
                                        <img 
                                            src={request.profilePicUrl} 
                                            alt={`${request.nickname}'s profile`} 
                                            className="w-12 h-12 rounded-full mr-4" 
                                        />
                                        <div>
                                            <span className="text-lg font-bold">{request.nickname}</span>
                                            <p className="text-sm text-gray-400">{request.name}</p>
                                            <p className="text-xs text-gray-500">
                                                Requested {request.requestDate ? new Date(request.requestDate).toLocaleDateString() : 'recently'}
                                            </p>
                                        </div>
                                    </div>
                                    <div className="flex space-x-2">
                                        <Button
                                            onClick={() => acceptRequest.mutate(request.id)}
                                            disabled={acceptRequest.isPending}
                                            variant="default"
                                            size="sm"
                                        >
                                            Accept
                                        </Button>
                                        <Button
                                            onClick={() => declineRequest.mutate(request.id)}
                                            disabled={declineRequest.isPending}
                                            variant="destructive"
                                            size="sm"
                                        >
                                            Decline
                                        </Button>
                                    </div>
                                </li>
                            ))}
                        </ul>
                    </TabsContent>
                </Tabs>
            </div>
            <NavBar />
        </div>
    );
};

export default FriendsPage;
