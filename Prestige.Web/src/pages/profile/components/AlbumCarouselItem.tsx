import { CarouselItem } from "@/components/ui/carousel";
import { UserAlbumResponse } from "@/hooks/useProfile";
import useRedirectToPrestigePages from "@/hooks/useRedirectToPrestigePages";

type AlbumCarouselItemProps = {
    userAlbum: UserAlbumResponse;
};

const AlbumCarouselItem: React.FC<AlbumCarouselItemProps> = ({ userAlbum }) => {
    const { redirectToAlbumPage } = useRedirectToPrestigePages();

    const handleAlbumClick = () => {
        redirectToAlbumPage({
            albumId: userAlbum.album.id,
            albumName: userAlbum.album.name,
            artistName: userAlbum.album.artists.map(artist => artist.name).join(", "),
            totalTime: userAlbum.totalTime,
            imageUrl: userAlbum.album.images[0].url,
        });
    };

    return (
        <CarouselItem key={userAlbum.album.name} className="basis-1/3">
            <div className="track-item" onClick={handleAlbumClick}>
                <img src={userAlbum.album.images[0]?.url} alt={userAlbum.album.name} />
                <h3 className="text-lg font-bold">{userAlbum.album.name}</h3>
                <div id="artists_list" className="text-gray-600 overflow-hidden text-ellipsis whitespace-nowrap">
                    {userAlbum.album.artists.map(artist => artist.name).join(", ")}
                </div>
                <p className="text-gray-600">{Math.floor(userAlbum.totalTime / 60)} minutes</p>
            </div>
        </CarouselItem>
    );
};

export default AlbumCarouselItem;
