export type Image = {
  url: string;
  height: number;
  width: number;
}

export type ExternalUrls = {
  spotify: string;
}

export type Artist = {
  id: string;
  name: string;
  images: Image[];
  spotifyUrl: string;
}

export type Album = {
  id: string;
  name: string;
  imageUrl: string;  
  spotifyUrl: string;
  releaseDate: string;
  artistName: string;  
}

export type Track = {
  id: string;
  name: string;
  imageUrl: string;  
  spotifyUrl: string;
  albumName: string;  
  artistName: string;  
  durationMs: number;  
}

export type SearchResults = {
  artists?: Artist[];
  albums?: Album[];
  tracks?: Track[];
}
