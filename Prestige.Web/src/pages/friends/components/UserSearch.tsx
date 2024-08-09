import * as React from "react";
import { Input } from "@/components/ui/input";
import { useDebounce } from 'use-debounce';
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ScrollArea } from "@/components/ui/scroll-area";
import useUsers, { UserResponse } from "@/hooks/useUsers";
import useFriends from "@/hooks/useFriends";
import { Label } from "@/components/ui/label";

const UserSearch: React.FC= () => {
  const [searchTerm, setSearchTerm] = React.useState("");
  const [debouncedSearch] = useDebounce(searchTerm.trim(), 1000);
  const users = useUsers();
  const queryClient = useQueryClient();
  const friends = useFriends();

  const Search = (query : string) =>{
    return useQuery({
      queryKey : ['search', 'users', query],
      queryFn : async () => {
        if(query == "") return [];
        return await users.searchUsers(query);
      }
    });
  }

  const AddFriend = useMutation({
      mutationKey: ['users', 'friends'],
      mutationFn: async (id : string) => {
        return await friends.addFriend(id);
      },
      onSuccess: () => {
        queryClient.invalidateQueries(
            {
                queryKey: ['users', 'friends']
            }
        );
        }
    });

  const data = Search(debouncedSearch).data;


  return (
    <div className="search-bar w-screen h-full flex flex-col items-center text-foreground">
        <Label htmlFor="search-input" className="text-background w-3/4 text-start p-2 pl-0" >Search for new friends</Label>
        <Input
            id="search-input"
            name="search"
            type="text"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="mb-2 w-3/4"
        />
        <ScrollArea id="SearchResults" className="w-screen p-4 min-h-0 grow flex flex-col items-center text-background">
            {data?.map((user: UserResponse) => (
            <li
                key={user.id}
                 className="flex items-center p-4 border-b border-gray-700 cursor-pointer"
                onClick={() => AddFriend.mutate(user.id)}
            >
                <img src={user.profilePicURL} alt={`${user.nickName}'s profile`} className="w-10 h-10 rounded-full mr-4" />
                <span className="text-lg font-bold ml-4">{user.nickName}</span>
            </li>
            ))}
        </ScrollArea>
    </div>
  );
};

export { UserSearch };
