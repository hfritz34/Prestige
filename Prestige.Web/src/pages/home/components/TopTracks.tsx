import React from "react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { UserTrackResponse } from "@/hooks/useProfile";
import useRedirectToPrestigePages from "@/hooks/useRedirectToPrestigePages";
import usePrestige from "@/hooks/usePrestige";


type TopTracksProps = {
  topTracks: UserTrackResponse[];
};

const TopTracks: React.FC<TopTracksProps> = ({ topTracks }) => {
  const { redirectToSongPage } = useRedirectToPrestigePages();
    const { getTrackPrestigeTier } = usePrestige();

  const handleTrackClick = (track: UserTrackResponse) => {
    redirectToSongPage({
      trackId: track.track.id,
      trackName: track.track.name,
      albumName: track.track.album.name,
      artistName: track.track.artists.map((artist) => artist.name).join(", "),
      totalTime: track.totalTime,
      imageUrl: track.track.album.images[0]?.url,
    });
  };

  const topThreeTracks = topTracks.slice(0, 3);
  const otherTracks = topTracks.slice(3);

  return (
    <div className="p-4 overflow-visible">
      <h2 className="text-lg font-bold mt-6 mb-5 text-center">Top 3 Prestiges</h2>
      <div className="flex justify-center mb-8">
        <div className="flex w-full max-w-screen-lg justify-between px-2 md:px-0">
          {topThreeTracks.map((track) => {
            const prestige = getTrackPrestigeTier(track.totalTime);
            return (
              <Card
                key={track.track.name}
                className="w-full sm:w-1/3 bg-gray-800 text-white relative p-2 mx-1"
                onClick={() => handleTrackClick(track)}
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
                    src={track.track.album.images[0]?.url}
                    alt={track.track.name}
                    className="w-full h-full aspect-square rounded-t-lg object-cover"
                  />
                </CardHeader>
                <CardContent className="relative z-10">
                  <CardTitle className="text-center text-sm font-bold">{track.track.name}</CardTitle>
                  <CardDescription className="text-center text- text-zinc-50 font-bold">
                    {track.track.artists.flatMap((artist) => artist.name + " ")}
                  </CardDescription>
                  <p className="text-center text-xs text-zinc-50 font-bold">
                    {Math.floor(track.totalTime / 60)} minutes
                  </p>
                </CardContent>
              </Card>
            );
          })}
        </div>
      </div>
      <h2 className="text-lg font-bold mt-6 mb-5 text-center">More Top Prestiges</h2>
      <ul className="space-y-4">
        {otherTracks.map((track) => {
          const prestige = getTrackPrestigeTier(track.totalTime);
          return (
            <li
              key={track.track.name}
              className="flex items-center bg-gray-700 rounded-lg p-4 relative"
              onClick={() => handleTrackClick(track)}
            >
              {prestige && (
                <img
                  src={`src/assets/tiers/${prestige}.png`}
                  alt={prestige}
                  className="absolute inset-0 w-full h-full object-cover rounded-lg z-0"
                />
              )}
              <div className="flex-shrink-0 w-24 h-24 relative z-10">
                <img src={track.track.album.images[0]?.url} alt={track.track.name} className="w-full h-full object-cover rounded-lg" />
              </div>
              <div className="ml-4 relative z-10">
                <h3 className="text-lg font-bold">{track.track.name}</h3>
                <p className="text-zinc-50">{track.track.artists.flatMap((artist) => artist.name + " ")}</p>
                <p className="text-zinc-50">{Math.floor(track.totalTime / 60)} minutes</p>
              </div>
            </li>
          );
        })}
      </ul>
    </div>
  );
};

export default TopTracks;
