import React from "react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { UserAlbumResponse } from "@/hooks/useProfile";
import useRedirectToPrestigePages from "@/hooks/useRedirectToPrestigePages";
import usePrestige from "@/hooks/usePrestige";


type TopAlbumsProps = {
  topAlbums: UserAlbumResponse[];
};

const TopAlbums: React.FC<TopAlbumsProps> = ({ topAlbums }) => {
  const { redirectToAlbumPage } = useRedirectToPrestigePages();
  const { getAlbumPrestigeTier } = usePrestige();

  const handleAlbumClick = (album: UserAlbumResponse) => {
    console.log(`TopAlbums - Album Total Time (minutes): ${Math.floor(album.totalTime / 60)}`);
    redirectToAlbumPage({
      albumId: album.album.id,
      albumName: album.album.name,
      artistName: album.album.artists.map(artist => artist.name).join(", "),
      totalTime: album.totalTime,
      imageUrl: album.album.images[0].url,
    });
  };

  const topThreeAlbums = topAlbums.slice(0, 3);
  const otherAlbums = topAlbums.slice(3);

  return (
    <div className="p-4 overflow-visible">
      <h2 className="text-lg font-bold mt-6 mb-5 text-center">Top 3 Prestiges</h2>
      <div className="flex justify-center mb-8">
        <div className="flex w-full max-w-screen-lg justify-between px-2 md:px-0">
          {topThreeAlbums.map((album) => {
            const prestige = getAlbumPrestigeTier(album.totalTime);
            return (
              <Card
                key={album.album.name}
                className="w-full sm:w-1/3 bg-gray-800 text-white relative p-2 mx-1"
                onClick={() => handleAlbumClick(album)}
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
                    src={album.album.images[0]?.url}
                    alt={album.album.name}
                    className="w-full h-auto aspect-square object-cover rounded-t-lg"
                  />
                </CardHeader>
                <CardContent className="relative z-10">
                  <CardTitle className="text-center text-md font-bold">{album.album.name}</CardTitle>
                  <CardDescription className="text-center text-xs text-zinc-50 font-bold">
                    {album.album.artists.flatMap((artist) => artist.name + " ")}
                  </CardDescription>
                  <p className="text-center text-xs text-zinc-50 font-bold">
                    {Math.floor(album.totalTime / 60)} minutes
                  </p>
                </CardContent>
              </Card>
            );
          })}
        </div>
      </div>
      <h2 className="text-lg font-bold mt-6 mb-5 text-center">More Top Prestiges</h2>
      <ul className="space-y-4">
        {otherAlbums.map((album) => {
          const prestige = getAlbumPrestigeTier(album.totalTime);
          return (
            <li
              key={album.album.name}
              className="flex items-center bg-gray-700 rounded-lg p-4 relative"
              onClick={() => handleAlbumClick(album)}
            >
              {prestige && (
                <img
                  src={`src/assets/tiers/${prestige}.png`}
                  alt={prestige}
                  className="absolute inset-0 w-full h-full object-cover rounded-lg z-0"
                />
              )}
              <div className="flex-shrink-0 w-24 h-24 relative z-10">
                <img src={album.album.images[0]?.url} alt={album.album.name} className="w-full h-full object-cover rounded-lg" />
              </div>
              <div className="ml-4 relative z-10">
                <h3 className="text-lg font-bold">{album.album.name}</h3>
                <p className="text-zinc-50">{album.album.artists.flatMap((artist) => artist.name + " ")}</p>
                <p className="text-zinc-50">{Math.floor(album.totalTime / 60)} minutes</p>
              </div>
            </li>
          );
        })}
      </ul>
    </div>
  );
};

export default TopAlbums;
