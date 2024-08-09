import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import Account from '@/pages/Settings/Account';
import ImportData from '@/pages/Settings/ImportData';
import HowWeTrackPrestige from '@/pages/Settings/HowWeTrackPrestige';
import PrivacyPolicy from '@/pages/Settings/PrivacyPolicy';
import TermsOfService from '@/pages/Settings/TermsOfService';
import AboutUs from '@/pages/Settings/AboutUs';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { useAuth0 } from '@auth0/auth0-react';

const componentsMap: { [key: string]: React.FC } = {
  Account,
  ImportData,
  HowWeTrackPrestige,
  PrivacyPolicy,
  TermsOfService,
  AboutUs,
};

const SettingsPage: React.FC = () => {
  const [activeSection, setActiveSection] = useState<string | null>(null);
  const [transitioningSection, setTransitioningSection] = useState<string | null>(null);
  const [isExiting, setIsExiting] = useState<boolean>(false);
  const { logout } = useAuth0();

  const handleSectionChange = (section: string) => {
    if (activeSection === section) return;
    setIsExiting(true);
    setTransitioningSection(activeSection);
    setTimeout(() => {
      setActiveSection(section);
      setIsExiting(false);
      setTransitioningSection(null);
    }, 300); 
  };

  const handleBackClick = () => {
    setIsExiting(true);
    setTransitioningSection(activeSection);
    setTimeout(() => {
      setActiveSection(null);
      setIsExiting(false);
      setTransitioningSection(null);
    }, 300); 
  };

  const ActiveComponent = activeSection ? componentsMap[activeSection] : null;
  const TransitioningComponent = transitioningSection ? componentsMap[transitioningSection] : null;

  return (
    <div className="min-h-screen bg-gray-900 text-white relative overflow-y-auto">
      {TransitioningComponent && (
        <div className={`absolute inset-0 transition-transform duration-300 transform ${isExiting ? '-translate-x-full' : 'translate-x-0'} overflow-y-auto`}>
          <TransitioningComponent />
        </div>
      )}
      <div className={`absolute inset-0 transition-transform duration-300 transform ${isExiting ? 'translate-x-full' : 'translate-x-0'} overflow-y-auto`}>
        {!ActiveComponent && (
          <Link to="/profile" className="absolute top-4 left-4 text-white py-2 px-4 rounded  cursor-pointer z-10">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="1.5" stroke="currentColor" className="size-6">
  <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" />
</svg>

          </Link>
        )}
        {ActiveComponent ? (
          <>
            <button onClick={handleBackClick} className="absolute top-4 left-4">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="1.5" stroke="currentColor" className="w-6 h-6">
                <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
              </svg>
            </button>
            <ActiveComponent />
          </>
        ) : (
          <div className="max-w-md mx-auto p-4 bg-gray-900 text-white rounded-lg shadow-md overflow-y-auto">
            <h1 className="text-center text-2xl font-bold mb-6">Settings</h1>
            <div className="settings-list">
              {Object.keys(componentsMap).map((section) => (
                <div
                  key={section}
                  className="settings-item flex justify-between items-center py-4 px-4 border-b border-gray-700 hover:bg-gray-800 cursor-pointer transition-transform ease-in-out duration-300"
                  onClick={() => handleSectionChange(section)}
                >
                  {section.replace(/([A-Z])/g, ' $1').trim()}
                  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="1.5" stroke="currentColor" className="w-6 h-6">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
                  </svg>
                </div>
              ))}
            </div>
            <AlertDialog>
              <AlertDialogTrigger className="mt-6 bg-red-600 text-white py-2 px-4 rounded hover:bg-red-700 cursor-pointer transition-colors duration-300">
                Log Out
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>Are you sure you want to Log Out?</AlertDialogTitle>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel>Cancel</AlertDialogCancel>
                  <AlertDialogAction onClick={() => logout()}>Continue</AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>
          </div>
        )}
      </div>
    </div>
  );
};

export default SettingsPage;
