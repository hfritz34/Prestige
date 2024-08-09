import React from 'react';
import { useNavigate } from 'react-router-dom';
import NavBar from '@/components/navigation/NavBar';
import useFriends from '@/hooks/useFriends';
import { UserSearch } from './components/UserSearch';
import { useQuery } from '@tanstack/react-query';

const FriendsPage: React.FC = () => {
    const { getAllFriends } = useFriends();
    const navigate = useNavigate();

    const handleFriendClick = (friendId: string) => {
        navigate(`/friends/${friendId}`);
    };

    const friendsQuery = useQuery({
        queryKey: ['users', 'friends'],
        queryFn: getAllFriends,
    });
    const friends =  friendsQuery.data;
    const loading = friendsQuery.isLoading;

    return (
        <div className="min-h-screen flex flex-col bg-gray-900 text-white">
            <div className="flex justify-between items-center p-4 bg-gray-800">
                <h1 className="text-2xl font-bold">Friends</h1>
            </div>
            <div className="flex-1">
                <UserSearch />
                {loading && <p>Loading friends...</p>}
                {friends && <p className='text-lg pl-4 font-semibold'>Current Friends</p>}
                <ul className='p-4'>
                    {friends?.map((friend) => (
                        <li
                            key={friend.id}
                            className="flex items-center p-4 border-b border-gray-700 cursor-pointer"
                            onClick={() => handleFriendClick(friend.id)}
                        >
                            <img src={friend.profilePicUrl} alt={`${friend.nickname}'s profile`} className="w-10 h-10 rounded-full mr-4" />
                            <span className="text-lg font-bold ml-4">{friend.nickname}</span>
                        </li>
                    ))}
                </ul>
            </div>
            <NavBar />
        </div>
    );
};

export default FriendsPage;
