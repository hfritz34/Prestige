import { type ReactNode } from 'react';
import { Auth0Context, type Auth0ContextInterface } from '@auth0/auth0-react';
import { type User } from '@auth0/auth0-spa-js';

const demoUser: User = {
  sub: 'auth0|demo-user',
  name: 'Prestige Demo',
  nickname: 'demo',
  email: 'demo@prestige.local',
  picture: 'https://placehold.co/512x512/111827/f9fafb?text=PD'
};

const demoAuth = {
  isAuthenticated: true,
  isLoading: false,
  user: demoUser,
  getAccessTokenSilently: async () => 'local-demo-token',
  getAccessTokenWithPopup: async () => 'local-demo-token',
  getIdTokenClaims: async () => ({ __raw: 'local-demo-id-token', sub: demoUser.sub }),
  loginWithRedirect: async () => {},
  loginWithPopup: async () => {},
  logout: async () => {
    window.location.assign('/');
  },
  handleRedirectCallback: async () => ({ appState: {} }),
  buildAuthorizeUrl: async () => window.location.href,
  buildLogoutUrl: () => window.location.origin
} as unknown as Auth0ContextInterface<User>;

export function LocalDemoAuthProvider({ children }: { children: ReactNode }) {
  return (
    <Auth0Context.Provider value={demoAuth}>
      {children}
    </Auth0Context.Provider>
  );
}
