import { Carousel, CarouselContent } from "@/components/ui/carousel";
import AlbumCarouselItem from "./AlbumCarouselItem";
import ArtistCarouselItem from "./ArtistCarouselItem";
import TrackCarouselItem from "./TrackCarouselItem";
import { useQuery } from "@tanstack/react-query";
import useProfile from "@/hooks/useProfile";

type TopCarouselProps = {
    type: "TopTracks" | "TopAlbums" | "TopArtists",
}

const TopCarousel : React.FC<TopCarouselProps> = ( { type } ) => {
    const profile = useProfile();

    const TopTracksQuery = () => {
        return useQuery({
            queryKey: ["top", "tracks"],
            queryFn: async () => {
                const response = await profile.getTopTracks();
                return response;
            }
        });
    }
    const topTracks = TopTracksQuery().data;

    const TopAlbumsQuery = () => {
        return useQuery({
            queryKey: ["top", "albums"],
            queryFn: async () => {
                const response = await profile.getTopAlbums();
                return response;
            }
        });
    }
    const topAlbums = TopAlbumsQuery().data;

    const TopArtistsQuery = () => {
        return useQuery({
            queryKey: ["top", "artists"],
            queryFn: async () => {
                const response = await profile.getTopArtists();
                return response;
            }
        });
    }
    const topArtists = TopArtistsQuery().data;

    return (
        <Carousel className="w-full mt-6">
          <CarouselContent>
            {type === "TopTracks" && (topTracks?.map((topTrack) => (
              <TrackCarouselItem key={topTrack.track.id} userTrack={topTrack} />
            )) ?? [])}
            {type === "TopAlbums" && (topAlbums?.map((topAlbum) => (
              <AlbumCarouselItem key={topAlbum.album.id} userAlbum={topAlbum} />
            )) ?? [])}
            {type === "TopArtists" && (topArtists?.map((topArtist) => (
              <ArtistCarouselItem key={topArtist.artist.id} userArtist={topArtist} />
            )) ?? [])}
          </CarouselContent>
        </Carousel>
    )
};

export default TopCarousel