import { CarouselItem } from "@/components/ui/carousel";
import { UserTrackResponse } from "@/hooks/useProfile";
import useRedirectToPrestigePages from "@/hooks/useRedirectToPrestigePages";

type TrackCarouselItemProps = {
    userTrack: UserTrackResponse;
};

const TrackCarouselItem: React.FC<TrackCarouselItemProps> = ({ userTrack }) => {
    const { redirectToSongPage } = useRedirectToPrestigePages();

    const handleTrackClick = () => {
        redirectToSongPage({
            trackId: userTrack.track.id,
            trackName: userTrack.track.name,
            albumName: userTrack.track.album.name,
            artistName: userTrack.track.artists.map(artist => artist.name).join(", "),
            totalTime: userTrack.totalTime, 
            imageUrl: userTrack.track.album.images[0].url,
        });
    };

    return (
        <CarouselItem key={userTrack.track.id} className="basis-1/3">
            <div className="track-item" onClick={handleTrackClick}>
                <img src={userTrack.track.album.images[0].url} alt={userTrack.track.name} />
                <h3 className="text-lg font-bold">{userTrack.track.name}</h3>
                <div id="artists_list" className="text-gray-600 overflow-hidden text-ellipsis whitespace-nowrap">
                    {userTrack.track.artists.flatMap((artist) => artist.name + " ")}
                </div>
                <p className="text-gray-600">{Math.floor(userTrack.totalTime / 60)} minutes</p>
            </div>
        </CarouselItem>
    );
};

export default TrackCarouselItem;
