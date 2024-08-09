
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import UnauthenticatedRouter from "./routing/UnauthenticatedRouter";
import AuthenticatedRouter from "./routing/AuthenticatedRouter";
import { useAuth0 } from "@auth0/auth0-react";
import { RouterProvider } from "react-router-dom";



const queryClient = new QueryClient();

const App: React.FC = () =>  {
  const {isAuthenticated} = useAuth0();


  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={isAuthenticated ? AuthenticatedRouter : UnauthenticatedRouter} />
    </QueryClientProvider>
  );
}

export default App;