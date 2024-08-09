import React from 'react'
import ReactDOM from 'react-dom/client'
import './index.css'
import App from './components/navigation/App';
import { Auth0Provider } from '@auth0/auth0-react';


ReactDOM.createRoot(document.getElementById('root')!).render(
  <Auth0Provider
  domain="dev-u10jtlqih3lq02fh.us.auth0.com"
  clientId="O1NAzxf4JzsPtFHbbuHf6yg05lDqgCrY"
  authorizationParams = {{
      redirect_uri: `${import.meta.env.VITE_WEB_ADDRESS}/authorization`,
  }}
  >
    <React.StrictMode>
      <App />
    </React.StrictMode>
  </Auth0Provider>,
)
