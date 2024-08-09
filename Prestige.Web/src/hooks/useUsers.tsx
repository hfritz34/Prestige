import { useAuth0 } from "@auth0/auth0-react";
import useHttp from "./useHttp";

export type UserResponse = {
    email: string,
    id: string,
    name: string,
    nickName: string,
    profilePicURL: string,
    isSetup: boolean
}

export type UserRequest = {
  id: string,
  email: string | undefined,
  name: string | undefined,
  profilePicURL: string | undefined,
  nickName: string | undefined
}


const useUser = () => {
  const { getOne, patch, post, getMany } = useHttp();
  const { user } = useAuth0();
  const userId = user?.sub?.split("|").pop();

  const postUser = async (user: UserRequest): Promise<UserResponse> => post<UserResponse, UserRequest>(user, 'users')

  const getUser = async (): Promise<UserResponse> => getOne<UserResponse>(`users/${userId}`)

  const patchNickName = async (nickName: string): Promise<UserResponse> => patch<UserResponse, { nickName: string }>({ nickName }, `users/${userId}/nickname`)
  
  const patchIsSetup = async (isSetup: boolean): Promise<UserResponse> => patch<UserResponse, { isSetup: boolean }>({ isSetup }, `users/${userId}/is-setup?isSetup=${isSetup}`)

  const searchUsers = async (query: string): Promise<UserResponse[]> => getMany<UserResponse>(`users/search?query=${query}`)

  return {getUser, patchNickName, postUser, patchIsSetup, searchUsers};
}

export default useUser;