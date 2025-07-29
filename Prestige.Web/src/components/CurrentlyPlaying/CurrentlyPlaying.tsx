import React from "react";
import { motion } from "framer-motion";
import useRedirectToPrestigePages from "@/hooks/useRedirectToPrestigePages";
import "./CurrentlyPlaying.css";

interface CurrentlyPlayingProps {
  track: {
    id: string;
    name: string;
    artists: { name: string }[];
    album: {
      name: string;
      images: { url: string }[];
    };
    durationMs: number;
  };
  isPlaying: boolean;
  progressMs: number;
  totalTime: number;
}

const CurrentlyPlaying: React.FC<CurrentlyPlayingProps> = ({
  track,
  isPlaying,
  progressMs,
  totalTime,
}) => {
  const { redirectToSongPage } = useRedirectToPrestigePages();

  const handleClick = () => {
    redirectToSongPage({
      trackId: track.id,
      trackName: track.name,
      albumName: track.album.name,
      artistName: track.artists.map((artist) => artist.name).join(", "),
      totalTime: totalTime,
      imageUrl: track.album.images[0]?.url || "",
    });
  };

  const progressPercentage = (progressMs / track.durationMs) * 100;

  return (
    <motion.div
      className="currently-playing"
      initial={{ opacity: 0, scale: 0.8 }}
      animate={{ opacity: 1, scale: 1 }}
      exit={{ opacity: 0, scale: 0.8 }}
      transition={{ duration: 0.3 }}
      onClick={handleClick}
    >
      <div className="currently-playing-content">
        <div className="album-art-container">
          <img
            src={track.album.images[0]?.url || ""}
            alt={track.album.name}
            className="album-art"
          />
          <div className="synthesizer">
            {[...Array(5)].map((_, i) => (
              <motion.div
                key={i}
                className="bar"
                animate={
                  isPlaying
                    ? {
                        height: ["20%", "100%", "20%"],
                      }
                    : { height: "20%" }
                }
                transition={
                  isPlaying
                    ? {
                        duration: 0.5 + i * 0.1,
                        repeat: Infinity,
                        repeatType: "reverse",
                      }
                    : {}
                }
              />
            ))}
          </div>
        </div>
        <div className="track-info">
          <div className="track-name">{track.name}</div>
          <div className="artist-name">
            {track.artists.map((artist) => artist.name).join(", ")}
          </div>
          <div className="progress-bar">
            <div
              className="progress-fill"
              style={{ width: `${progressPercentage}%` }}
            />
          </div>
        </div>
      </div>
    </motion.div>
  );
};

export default CurrentlyPlaying;