import  { useState } from 'react';
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import useUser from "@/hooks/useUsers";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";

const Account = () => {
  const { getUser, patchNickName } = useUser();
  const [nickname, setNickname] = useState<string>("");

  const userQuery = useQuery({
    queryKey: ['user'],
    queryFn: async () => {
      const ret = await getUser();
      setNickname(ret.nickName);
      return ret;
    },
  });

  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: async (newNickName: string) => {
      return await patchNickName(newNickName);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['user'] });
    }
  });

  const handleNicknameChange = () => {
    if (nickname) {
      mutation.mutate(nickname);
    }
  };

  return (
    <div className="min-h-screen bg-gray-900 text-white">
      <div className="max-w-md mx-auto p-4 bg-gray-900 text-white rounded-lg shadow-md">
        <h2 className="text-center text-2xl font-bold mb-6">Account</h2>
        <Avatar className="w-24 h-24 ">
          <AvatarImage src={userQuery.data?.profilePicURL || 'https://github.com/shadcn.png'} />
          <AvatarFallback>
            {userQuery.data?.nickName ? userQuery.data.nickName.charAt(0).toUpperCase() : 'CN'}
          </AvatarFallback>
        </Avatar>
        <div className="mb-6">
          <label className="block text-sm font-medium">Nickname</label>
          <Input
            className="mt-1 block w-full bg-gray-800 border border-gray-700 rounded py-2 px-3"
            placeholder={userQuery.data?.name || 'Nickname'}
            value={nickname}
            onChange={(e) => setNickname(e.target.value)}
          />
          <Button
            className="mt-2 bg-blue-600 text-white py-2 px-4 rounded hover:bg-blue-700 cursor-pointer"
            onClick={handleNicknameChange}
          >
            Change Nickname
          </Button>
        </div>
        
        <div className="mb-6">
          <label className="block text-sm font-medium">Username</label>
          <Input
            className="mt-1 block w-full bg-gray-800 border border-gray-700 rounded py-2 px-3"
            value={userQuery.data?.id || ''}
            readOnly
          />
          
        </div>

        <div className="mb-6">
          <label className="block text-sm font-medium">Email</label>
          <Input
            className="mt-1 block w-full bg-gray-800 border border-gray-700 rounded py-2 px-3"
            value={userQuery.data?.email || ''}
            readOnly
          />
        </div>

        <AlertDialog>
          <AlertDialogTrigger className="mt-6 bg-red-600 text-white py-2 px-4 rounded hover:bg-green-700 cursor-pointer">
            Delete Account
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Are you absolutely sure?</AlertDialogTitle>
              <AlertDialogDescription>
                This action cannot be undone. This will permanently delete your account
                and remove your data from our servers.
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>Cancel</AlertDialogCancel>
              <AlertDialogAction className="bg-red-600">DELETE</AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </div>
    </div>
  );
};

export default Account;
