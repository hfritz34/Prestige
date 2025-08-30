import { Carousel, CarouselContent } from "@/components/ui/carousel";
import FriendTrackCarouselItem from "./FriendTrackCarouselItem";
import FriendAlbumCarouselItem from "./FriendAlbumCarouselItem";
import FriendArtistCarouselItem from "./FriendArtistCarouselItem";
import { Friend } from "@/hooks/useFriends";

type TopCarouselProps = {
    type: "TopTracks" | "TopAlbums" | "TopArtists",
    friend : Friend
}

const FriendTopCarousel : React.FC<TopCarouselProps> = ( { type, friend } ) => {

    return (
        <Carousel className="w-full mt-6">
          <CarouselContent>
            {type === "TopTracks" && (friend.topTracks?.map((topTrack) => (
              <FriendTrackCarouselItem key={topTrack.track.id} userTrack={topTrack} friendNickname={friend.nickname} />
            )) ?? [])}
            {type === "TopAlbums" && (friend.topAlbums?.map((topAlbum) => (
              <FriendAlbumCarouselItem key={topAlbum.album.id} userAlbum={topAlbum} friendNickname={friend.nickname} />
            )) ?? [])}
            {type === "TopArtists" && (friend.topArtists?.map((topArtist) => (
              <FriendArtistCarouselItem key={topArtist.artist.id} userArtist={topArtist} friendNickname={friend.nickname} />
            )) ?? [])}
          </CarouselContent>
        </Carousel>
    )
};

export default FriendTopCarousel