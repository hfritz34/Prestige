import { useMemo } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import UnauthenticatedRouter from './routing/UnauthenticatedRouter';
import AuthenticatedRouter from './routing/AuthenticatedRouter';
import { useAuth0 } from '@auth0/auth0-react';
import { RouterProvider } from 'react-router-dom';
import { AccountProvider } from '@/auth/AccountProvider';

const App: React.FC = () => {
  const { isAuthenticated, isLoading, user } = useAuth0();
  // Never reuse another account's cached profile, friends or ratings after an account switch.
  const queryClient = useMemo(() => new QueryClient(), [isAuthenticated, user?.sub]);

  if (isLoading) return <div role="status">Loading...</div>;

  return (
    <QueryClientProvider client={queryClient}>
      {isAuthenticated ? (
        <AccountProvider key={user?.sub}>
          <RouterProvider router={AuthenticatedRouter} />
        </AccountProvider>
      ) : (
        <RouterProvider router={UnauthenticatedRouter} />
      )}
    </QueryClientProvider>
  );
};

export default App;
