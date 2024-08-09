import { Navigate, createBrowserRouter } from 'react-router-dom';
import SignInPage from "@/pages/sign-in/SignInPage";
import SplashPage from "@/pages/SplashPage";
import AuthPage from "@/pages/auth/AuthPage";


const UnauthenticatedRouter = createBrowserRouter([
  {
    path: "/",
    element: <SplashPage />,
  },
  {
    path: "/sign-in",
    element: <SignInPage />,
  },
  {
    path: "/authorization",
    element: <AuthPage />,
  },
  {
    path: '*',
    element: <Navigate to= {"/sign-in"}/>
  },
]);

export default UnauthenticatedRouter;
