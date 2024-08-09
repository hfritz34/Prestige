using Prestige.Api.Endpoints.Spotify.RequestResponse;

namespace Prestige.Api.Domain
{
    public class Image
    {
        public string Url { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }

        public Image()
        {
        }

        public Image(string url, int height, int width)
        {
            Url = url;
            Height = height;
            Width = width;
        }

        public Image(ImageResponse imageResponse)
        {
            Url = imageResponse.Url;
            Height = imageResponse.Height;
            Width = imageResponse.Width;
        }
    }
}
