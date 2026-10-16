import { useAccount } from '@/auth/AccountProvider';
import { Navigate } from 'react-router-dom';

const AuthPage: React.FC = () => {
  const account = useAccount();
  return <Navigate to={account.isSetup ? '/home' : '/profile-setup'} replace />;
};

export default AuthPage;
