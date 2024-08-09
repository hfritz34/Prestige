import useHttp from "./useHttp";

export type SearchResponse = {
    artists: ArtistResponse[]
    albums: AlbumResponse[]
    tracks: TrackResponse[]
}

export type ImageResponse = {
    url: string
    height: number
    width: number
}

export type ArtistResponse = {
    id: string
    name: string
    images: ImageResponse[]
}

export type AlbumResponse = {
    id: string
    name: string
    images: ImageResponse[]
    artists: ArtistResponse[]
}

export type TrackResponse = {
    id: string
    name: string
    album: AlbumResponse
    artists: ArtistResponse[]
    duration_ms: number
}

export type SearchType = "track"|"album"|"artist";

const pure = (response : TrackResponse[] | AlbumResponse[] | ArtistResponse[], type : SearchType) => {
    const searchResponse : SearchResponse  = {
        artists: (type == "artist") ? response as ArtistResponse[] : [],
        albums: (type == "album") ? response as AlbumResponse[] : [],
        tracks: (type == "track") ? response as TrackResponse[] : []
    }
    return searchResponse;
}


const useSpotify = () => {
    const { getOne } = useHttp();

    const search = async (query: string, searchType: SearchType): Promise<SearchResponse> => {
        return getOne<TrackResponse[] | AlbumResponse[] | ArtistResponse[]>(`spotify/${searchType}s/search?Query=${query}`)
        .then((response) => {
            return pure(response, searchType);
        });
    }

    return {search};
}

export default useSpotify;