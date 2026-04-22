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
    domain="dev-tfgyd3i2jqk0igxv.us.auth0.com"
    clientId="NZ5N1xnHOdgdVuXNPoBuNydMhg83Oe0p"
    authorizationParams={{
      redirect_uri: `${import.meta.env.VITE_WEB_ADDRESS}/authorization`,
      audience: "https://prestige-auth0-resource",
      scope: "openid profile email user-read-email user-read-private user-read-recently-played user-top-read user-read-currently-playing app-remote-control"
    }}
    >
      {app}
    </Auth0Provider>
  ),
)
