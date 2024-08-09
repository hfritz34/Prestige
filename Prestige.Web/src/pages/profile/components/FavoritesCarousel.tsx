import { Carousel, CarouselContent } from "@/components/ui/carousel";
import AlbumCarouselItem from "./AlbumCarouselItem";
import ArtistCarouselItem from "./ArtistCarouselItem";
import TrackCarouselItem from "./TrackCarouselItem";
import { useQuery } from "@tanstack/react-query";
import useProfile, { UserAlbumResponse, UserArtistResponse, UserTrackResponse } from "@/hooks/useProfile";

type FavoritesCarouselProps = {
    type: "FavoriteTracks" | "FavoriteAlbums" | "FavoriteArtists",
}

const FavoritesCarousel : React.FC<FavoritesCarouselProps> = ( { type } ) => {
    const profile = useProfile();

    const FavoritesTracksQuery = () => {
        return useQuery({
            queryKey: ["favorite", "tracks"],
            queryFn: async () => {
                const response = await profile.getFavorites("tracks");
                return response;
            }
        });
    }
    const favoriteTracks = FavoritesTracksQuery().data as unknown as UserTrackResponse[];

    const FavoritesAlbumsQuery = () => {
        return useQuery({
            queryKey: ["favorite", "albums"],
            queryFn: async () => {
                const response = await profile.getFavorites("albums");
                return response;
            }
        });
    }
    const favoriteAlbums = FavoritesAlbumsQuery().data as unknown as UserAlbumResponse[];

    const FavoritesArtistsQuery = () => {
        return useQuery({
            queryKey: ["favorite", "artists"],
            queryFn: async () => {
                const response = await profile.getFavorites("artists");
                return response;
            }
        });
    }
    const favoriteArtists = FavoritesArtistsQuery().data as unknown as UserArtistResponse[];

    return (
        <Carousel className="w-full mt-6">
          <CarouselContent>
            {type === "FavoriteTracks" && favoriteTracks?.map((userTrack) => (
              <TrackCarouselItem key={userTrack.track.id} userTrack={userTrack} />
            ))}
            {type === "FavoriteAlbums" && favoriteAlbums?.map((userAlbum) => (
              <AlbumCarouselItem key={userAlbum.album.id} userAlbum={userAlbum} />
            ))}
            {type === "FavoriteArtists" && favoriteArtists?.map((userArtist) => (
              <ArtistCarouselItem key={userArtist.artist.id} userArtist={userArtist} />
            ))}
          </CarouselContent>
        </Carousel>
    )
};

export default FavoritesCarousel