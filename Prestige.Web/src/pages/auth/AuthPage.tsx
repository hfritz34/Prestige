import useUser, { UserRequest } from "@/hooks/useUsers";
import { useAuth0 } from "@auth0/auth0-react";
import { useMutation } from "@tanstack/react-query";
import React, { useEffect } from "react";
import { useNavigate } from "react-router-dom";

const AuthPage: React.FC = () => {
  const { postUser } = useUser();
  const { isLoading, user } = useAuth0();
  const navigate = useNavigate();

  const UserMutation = useMutation({
    mutationKey: ['users'],
    mutationFn: async (rq : UserRequest) => {
      return await postUser(rq);
    },
    onSuccess: (data) => {
      data.isSetup ? navigate("/home") : navigate("/profile-setup");
    }
  });
  

  useEffect(() => {
      if (!isLoading) {
          if(user?.sub != undefined && UserMutation.isIdle){
            UserMutation.mutate({id: user.sub.split("|").pop() as string, email: user.email, name: user.name, nickName: undefined, profilePicURL: user.picture});
          }
      }
  }, [UserMutation, isLoading, navigate, user?.email, user?.name, user?.picture, user?.sub]);


  return (
    <div>Loading...</div>
  )
};

export default AuthPage;
