import React from 'react'
import ReactDOM from 'react-dom/client'
import './index.css'
import App from './components/navigation/App';
import { Auth0Provider } from '@auth0/auth0-react';
import { LocalDemoBackendGate } from './auth/LocalDemoBackendGate';
import { LocalDemoAuthProvider } from './auth/LocalDemoAuthProvider';

const isLocalDemo = import.meta.env.VITE_LOCAL_DEMO === 'true';

const app = (
  <React.StrictMode>
    {isLocalDemo ? (
      <LocalDemoBackendGate>
        <App />
      </LocalDemoBackendGate>
    ) : (
      <App />
    )}
  </React.StrictMode>
);

ReactDOM.createRoot(document.getElementById('root')!).render(
  isLocalDemo ? (
    <LocalDemoAuthProvider>
      {app}
    </LocalDemoAuthProvider>
  ) : (
    <Auth0Provider
    domain={import.meta.env.VITE_AUTH0_DOMAIN}
    clientId={import.meta.env.VITE_AUTH0_CLIENT_ID}
    authorizationParams={{
      redirect_uri: `${window.location.origin}/authorization`,
      audience: import.meta.env.VITE_API_AUDIENCE,
      scope: "openid profile email"
    }}
    >
      {app}
    </Auth0Provider>
  ),
)
