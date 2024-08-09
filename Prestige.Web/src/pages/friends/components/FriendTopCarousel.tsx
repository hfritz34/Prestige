import { Carousel, CarouselContent } from "@/components/ui/carousel";
import AlbumCarouselItem from "@/pages/profile/components/AlbumCarouselItem";
import ArtistCarouselItem from "@/pages/profile/components/ArtistCarouselItem";
import TrackCarouselItem from "@/pages/profile/components/TrackCarouselItem";
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
              <TrackCarouselItem key={topTrack.track.id} userTrack={topTrack} />
            )) ?? [])}
            {type === "TopAlbums" && (friend.topAlbums?.map((topAlbum) => (
              <AlbumCarouselItem key={topAlbum.album.id} userAlbum={topAlbum} />
            )) ?? [])}
            {type === "TopArtists" && (friend.topArtists?.map((topArtist) => (
              <ArtistCarouselItem key={topArtist.artist.id} userArtist={topArtist} />
            )) ?? [])}
          </CarouselContent>
        </Carousel>
    )
};

export default FriendTopCarousel