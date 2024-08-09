import { Carousel, CarouselContent } from "@/components/ui/carousel";
import AlbumCarouselItem from "@/pages/profile/components/AlbumCarouselItem";
import ArtistCarouselItem from "@/pages/profile/components/ArtistCarouselItem";
import TrackCarouselItem from "@/pages/profile/components/TrackCarouselItem";
import { Friend } from "@/hooks/useFriends";

type FavoritesCarouselProps = {
    type: "FavoriteTracks" | "FavoriteAlbums" | "FavoriteArtists",
    friend : Friend
}

const FriendFavoritesCarousel : React.FC<FavoritesCarouselProps> = ( { type, friend } ) => {

    return (
        <Carousel className="w-full mt-6">
          <CarouselContent>
            {type === "FavoriteTracks" && friend.favoriteTracks?.map((userTrack) => (
              <TrackCarouselItem key={userTrack.track.id} userTrack={userTrack} />
            ))}
            {type === "FavoriteAlbums" && friend.favoriteAlbums?.map((userAlbum) => (
              <AlbumCarouselItem key={userAlbum.album.id} userAlbum={userAlbum} />
            ))}
            {type === "FavoriteArtists" && friend.favoriteArtists?.map((userArtist) => (
              <ArtistCarouselItem key={userArtist.artist.id} userArtist={userArtist} />
            ))}
          </CarouselContent>
        </Carousel>
    )
};

export default FriendFavoritesCarousel