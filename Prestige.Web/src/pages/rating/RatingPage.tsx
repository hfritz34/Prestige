import React from 'react';
import NavBar from '@/components/navigation/NavBar';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';

const RatingPage: React.FC = () => {
  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pb-20">
        <h1 className="text-3xl font-bold text-gray-900 dark:text-white py-6">
          Rate Your Music
        </h1>
        
        <Tabs defaultValue="prestige" className="w-full">
          <TabsList className="grid w-full grid-cols-3">
            <TabsTrigger value="prestige">My Prestige</TabsTrigger>
            <TabsTrigger value="recent">Recently Played</TabsTrigger>
            <TabsTrigger value="liked">Liked Songs</TabsTrigger>
          </TabsList>
          
          <TabsContent value="prestige">
            <div className="bg-white dark:bg-gray-800 rounded-lg p-6 mt-4">
              <h2 className="text-xl font-semibold mb-4">Rate from Your Prestige Collection</h2>
              <p className="text-gray-600 dark:text-gray-400">
                Browse through your prestige tiers and rate albums, artists, and tracks.
              </p>
            </div>
          </TabsContent>
          
          <TabsContent value="recent">
            <div className="bg-white dark:bg-gray-800 rounded-lg p-6 mt-4">
              <h2 className="text-xl font-semibold mb-4">Rate Recently Played</h2>
              <p className="text-gray-600 dark:text-gray-400">
                Rate music you've listened to recently on Spotify.
              </p>
            </div>
          </TabsContent>
          
          <TabsContent value="liked">
            <div className="bg-white dark:bg-gray-800 rounded-lg p-6 mt-4">
              <h2 className="text-xl font-semibold mb-4">Rate Your Liked Songs</h2>
              <p className="text-gray-600 dark:text-gray-400">
                Rate songs from your Spotify liked songs playlist.
              </p>
            </div>
          </TabsContent>
        </Tabs>
      </div>
      
      <NavBar />
    </div>
  );
};

export default RatingPage;