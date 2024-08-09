import React, { useState } from "react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import useUser from "@/hooks/useUsers";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { Label } from "@/components/ui/label";

const ProfileSetupPage: React.FC = () => {
  const { getUser, patchNickName } = useUser();
  const [ input, setInput ] = useState<string>("");
  const queryClient = useQueryClient();
  const nav = useNavigate();

  const userQuery = useQuery({
    queryKey: ['user'],
    queryFn: async () => {
      const ret = await getUser()
      return ret
    },
  });

  const setNickNameQuery = useMutation({
    mutationKey: ['users', 'set', 'nickname'],
    mutationFn: async (nickName: string) => {
      const ret = await patchNickName(nickName)
      return ret
    },
    onSuccess: () => {
      queryClient.invalidateQueries(
        {
          queryKey: ['user']
        }
      );
    }
  });

  return (
    <div className="h-screen w-screen flex flex-col gap-2 p-8 pt-16">
      <Label htmlFor="display_name" className="text-2xl">Set your display name.</Label>
      <div>
        <Input className="w-full" placeholder={userQuery.data?.name} id="display_name" onChange={(e) => {
          e.preventDefault();
          setInput(e.target.value);
        }} 
        value={input} />
      </div>
      <Label className="self-end opacity-70" htmlFor="display_name">(This can be changed later in settings)</Label>
      <div className="mt-4">
        <Button className="w-full text-lg drop-shadow-md" 
          onClick={() => 
            {
              setNickNameQuery.mutate(input)
              nav("/add-favorites");
            }}>Continue</Button>
      </div>
    </div>
  );
};

export default ProfileSetupPage;
