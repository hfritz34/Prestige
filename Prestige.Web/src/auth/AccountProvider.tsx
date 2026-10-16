import { createContext, useContext, type ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import useHttp from '@/hooks/useHttp';
import type { UserResponse } from '@/hooks/useUsers';

const AccountContext = createContext<UserResponse | null>(null);
export const accountQueryKey = ['account'] as const;

export function AccountProvider({ children }: { children: ReactNode }) {
  const { getOne } = useHttp();
  const account = useQuery({
    queryKey: accountQueryKey,
    queryFn: () => getOne<UserResponse>('users/me'),
    staleTime: Infinity,
    retry: false,
  });

  if (account.isError) {
    return (
      <main role="alert">
        <p>We couldn’t load your Prestige account.</p>
        <button type="button" onClick={() => void account.refetch()}>Try again</button>
      </main>
    );
  }
  if (!account.data) return <div role="status">Loading your account...</div>;

  return <AccountContext.Provider value={account.data}>{children}</AccountContext.Provider>;
}

export function useAccount() {
  const account = useContext(AccountContext);
  if (!account) throw new Error('useAccount must be used inside AccountProvider');
  return account;
}
