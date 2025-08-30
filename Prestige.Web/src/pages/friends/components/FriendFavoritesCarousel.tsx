import { Carousel, CarouselContent } from "@/components/ui/carousel";
import FriendTrackCarouselItem from "./FriendTrackCarouselItem";
import FriendAlbumCarouselItem from "./FriendAlbumCarouselItem";
import FriendArtistCarouselItem from "./FriendArtistCarouselItem";
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
              <FriendTrackCarouselItem key={userTrack.track.id} userTrack={userTrack} friendNickname={friend.nickname} />
            ))}
            {type === "FavoriteAlbums" && friend.favoriteAlbums?.map((userAlbum) => (
              <FriendAlbumCarouselItem key={userAlbum.album.id} userAlbum={userAlbum} friendNickname={friend.nickname} />
            ))}
            {type === "FavoriteArtists" && friend.favoriteArtists?.map((userArtist) => (
              <FriendArtistCarouselItem key={userArtist.artist.id} userArtist={userArtist} friendNickname={friend.nickname} />
            ))}
          </CarouselContent>
        </Carousel>
    )
};

export default FriendFavoritesCarousel