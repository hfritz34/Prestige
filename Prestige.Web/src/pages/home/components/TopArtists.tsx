import React from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { UserArtistResponse } from "@/hooks/useProfile";
import useRedirectToPrestigePages from "@/hooks/useRedirectToPrestigePages";
import usePrestige from "@/hooks/usePrestige";


type TopArtistsProps = {
  topArtists: UserArtistResponse[];
};

const TopArtists: React.FC<TopArtistsProps> = ({ topArtists }) => {
  const { redirectToArtistPage } = useRedirectToPrestigePages();
  const { getArtistPrestigeTier } = usePrestige();

  const handleArtistClick = (artist: UserArtistResponse) => {
    console.log(`TopArtists - Artist Total Time (minutes): ${Math.floor(artist.totalTime / 60)}`);
    redirectToArtistPage({
      artistId: artist.artist.id,
      artistName: artist.artist.name,
      totalTime: artist.totalTime,
      imageUrl: artist.artist.images[0]?.url,
    });
  };

  const topThreeArtists = topArtists.slice(0, 3);
  const otherArtists = topArtists.slice(3);

  return (
    <div className="p-4 overflow-visible">
      <h2 className="text-lg font-bold mt-6 mb-5 text-center">Top 3 Prestiges</h2>
      <div className="flex justify-center mb-8">
        <div className="flex w-full max-w-screen-lg justify-between px-2 md:px-0">
          {topThreeArtists.map((artist) => {
            const prestige = getArtistPrestigeTier(artist.totalTime);
            return (
              <Card
                key={artist.artist.name}
                className="w-full sm:w-1/3 bg-gray-800 text-white relative p-2 mx-1"
                onClick={() => handleArtistClick(artist)}
              >
                {prestige && (
                  <img
                    src={`src/assets/tiers/${prestige}.png`}
                    alt={prestige}
                    className="absolute inset-0 w-full h-full object-cover rounded-lg z-0"
                  />
                )}
                <CardHeader className="relative z-10">
                  <img
                    src={artist.artist.images[0]?.url}
                    alt={artist.artist.name}
                    className="w-full h-auto aspect-square object-cover rounded-t-lg"
                  />
                </CardHeader>
                <CardContent className="relative z-10">
                  <CardTitle className="text-center text-md font-bold">{artist.artist.name}</CardTitle>
                  <p className="text-center text-xs text-zinc-50 font-bold">
                    {Math.floor(artist.totalTime / 60)} minutes
                  </p>
                </CardContent>
              </Card>
            );
          })}
        </div>
      </div>
      <h2 className="text-lg font-bold mt-6 mb-5 text-center">More Top Prestiges</h2>
      <ul className="space-y-4">
        {otherArtists.map((artist) => {
          const prestige = getArtistPrestigeTier(artist.totalTime);
          return (
            <li
              key={artist.artist.name}
              className="flex items-center bg-gray-700 rounded-lg p-4 relative"
              onClick={() => handleArtistClick(artist)}
            >
              {prestige && (
                <img
                  src={`src/assets/tiers/${prestige}.png`}
                  alt={prestige}
                  className="absolute inset-0 w-full h-full object-cover rounded-lg z-0"
                />
              )}
              <div className="flex-shrink-0 w-24 h-24 relative z-10">
                <img src={artist.artist.images[0]?.url} alt={artist.artist.name} className="w-full h-full object-cover rounded-lg" />
              </div>
              <div className="ml-4 relative z-10">
                <h3 className="text-lg font-bold">{artist.artist.name}</h3>
                <p className="text-zinc-50">{Math.floor(artist.totalTime / 60)} minutes</p>
              </div>
            </li>
          );
        })}
      </ul>
    </div>
  );
};

export default TopArtists;
