import { Navigate, createBrowserRouter } from 'react-router-dom';
import FriendsPage from "@/pages/friends/FriendsPage";
import HomePage from "@/pages/home/HomePage";
import ProfilePage from "@/pages/profile/ProfilePage";
import ProfileSetupPage from "@/pages/profile-setup/ProfileSetupPage";
import SplashPage from '@/pages/SplashPage';
import SettingsPage from '@/pages/Settings/SettingsPage';
import SongPage from '@/pages/IndividualPrestiges/SongPage';
import AuthPage from '@/pages/auth/AuthPage';
import AddFavoritesPage from '@/pages/profile-setup/AddFavoritesPage';
import AlbumPage from '@/pages/IndividualPrestiges/AlbumPage';
import ArtistPage from '@/pages/IndividualPrestiges/ArtistPage';
import FriendProfilePage from '@/pages/friends/FriendProfilePage';


const AuthenticatedRouter = createBrowserRouter([
    {
        path: "/",
        element: <SplashPage />,
    },
    {
        path: "/profile-setup",
        element: <ProfileSetupPage />,
    },
    {
        path: "/add-favorites",
        element: <AddFavoritesPage />
    },
    {
        path: "/authorization",
        element: <AuthPage />,
    },
    {
        path: "/home",
        element: <HomePage />,
    },
    {
        path: "/friends",
        element: <FriendsPage />,
    },
    {
        path: "/friends/:friendId",
        element: <FriendProfilePage />, 
    },
    {
        path: "/profile",
        element: <ProfilePage />,
    },
    {
        path: '/settings',
        element: <SettingsPage />
    },
    {
        path: "/prestige",
        children: [
            {
                path: "song",
                element: <SongPage />,
            },
            {
                path: "album",
                element: <AlbumPage />,
            },
            {
                path: "artist",
                element: <ArtistPage />,
            },
        ],
    },
    {
        path: '*',
        element: <Navigate to="/home" />,
    }
]);

export default AuthenticatedRouter;