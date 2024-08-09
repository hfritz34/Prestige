import { CarouselItem } from "@/components/ui/carousel";
import { UserArtistResponse } from "@/hooks/useProfile";
import useRedirectToPrestigePages from "@/hooks/useRedirectToPrestigePages";

type ArtistCarouselItemProps = {
    userArtist: UserArtistResponse
}

const ArtistCarouselItem: React.FC<ArtistCarouselItemProps> = ({ userArtist }) => {
    const { redirectToAlbumPage } = useRedirectToPrestigePages();

    const handleArtistClick = () => {
        redirectToAlbumPage({
            albumId: userArtist.artist.id,
            albumName: userArtist.artist.name,
            artistName: userArtist.artist.name,
            totalTime: userArtist.totalTime,
            imageUrl: userArtist.artist.images[0]?.url,
        });
    };

    return (
        <CarouselItem key={userArtist.artist.name} className="basis-1/3">
            <div className="track-item" onClick={handleArtistClick}>
                <img src={userArtist.artist.images[0]?.url} alt={userArtist.artist.name} />
                <h3 className="text-lg font-bold">{userArtist.artist.name}</h3>
                <p className="text-gray-600">{Math.floor(userArtist.totalTime / 60)} minutes</p>
            </div>
        </CarouselItem>
    );
};

export default ArtistCarouselItem;
