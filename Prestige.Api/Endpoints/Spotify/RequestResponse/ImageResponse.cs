using System.Text.Json.Serialization;

namespace Prestige.Api.Endpoints.Spotify.RequestResponse
{
    public class ImageResponse
    {
        public int Height { get; set; }
        public int Width { get; set; }
        public string Url { get; set; }

        public ImageResponse()
        {
        }

        public ImageResponse(int height, int width, string url)
        {
            Height = height;
            Width = width;
            Url = url;
        }

        public ImageResponse(Domain.Image image)
        {
            Height = image.Height;
            Width = image.Width;
            Url = image.Url;
        }

    }
}