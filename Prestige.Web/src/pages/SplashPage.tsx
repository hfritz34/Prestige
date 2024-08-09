import React, { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import "../index.css";
import { useAuth0 } from "@auth0/auth0-react";

const SplashPage: React.FC = () => {
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth0();

   useEffect(() => {
        setTimeout(() => {
          isAuthenticated ? navigate('/home') : navigate('/sign-in');
        }, 2000)
      }, [isAuthenticated, navigate])

  return (
    <div className="splash-container">
      <img
        className="ml-auto mr-auto block w-full"
        src="../src/assets/Spotify_Image.jpg"
        alt="Splash"
      />
      <h1>Prestige</h1>
    </div>
  );
};

export default SplashPage;
