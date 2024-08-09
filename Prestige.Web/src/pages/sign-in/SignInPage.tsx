import { Button } from '@/components/ui/button';
import { useAuth0 } from '@auth0/auth0-react';
import React from 'react';

const SignInPage: React.FC = () => {
    const { loginWithRedirect } = useAuth0();

    return (
        <div className="h-screen w-screen flex flex-col justify-center items-center gap-[10%]">
            <h1 className="text-center ">Welcome to Prestige</h1>
            <Button className="w-2/3 active:bg-spotify-green active:text-spotify-black" variant="outline" onClick={() => loginWithRedirect()}>Sign in</Button>
        </div>
    );
};

export default SignInPage;