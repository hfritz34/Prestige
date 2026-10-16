import { useQueryClient } from '@tanstack/react-query';
import { accountQueryKey, useAccount } from '@/auth/AccountProvider';
import useHttp from './useHttp';

export type UserResponse = {
  email: string;
  id: string;
  name: string;
  nickName: string;
  profilePicURL: string;
  isSetup: boolean;
};

export type UserSearchResult = Omit<UserResponse, 'email' | 'isSetup'> & {
  bio: string | null;
  isVerified: boolean;
};

const useUser = () => {
  const { getOne, patch, getMany } = useHttp();
  const account = useAccount();
  const queryClient = useQueryClient();

  const getUser = async (): Promise<UserResponse> => getOne<UserResponse>('users/me');

  const updateAccount = async (request: Promise<UserResponse>) => {
    const updated = await request;
    queryClient.setQueryData(accountQueryKey, updated);
    return updated;
  };

  const patchNickName = (nickName: string) => updateAccount(
    patch<UserResponse, { nickName: string }>({ nickName }, `users/${account.id}/nickname`)
  );

  const patchIsSetup = (isSetup: boolean) => updateAccount(
    patch<UserResponse, { isSetup: boolean }>({ isSetup }, `users/${account.id}/is-setup?isSetup=${isSetup}`)
  );

  const searchUsers = (query: string): Promise<UserSearchResult[]> =>
    getMany<UserSearchResult>(`users/search?query=${encodeURIComponent(query)}`);

  return { getUser, patchNickName, patchIsSetup, searchUsers };
};

export default useUser;
