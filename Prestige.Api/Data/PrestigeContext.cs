using Microsoft.EntityFrameworkCore;
using Prestige.Api.Data.Configurations;
using Prestige.Api.Domain;

namespace Prestige.Api.Data
{
    public class PrestigeContext : DbContext
    {
        public PrestigeContext(DbContextOptions<PrestigeContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Track> Tracks { get; set; }
        public DbSet<Album> Albums { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<UserTrack> UserTracks { get; set; }
        public DbSet<UserAlbum> UserAlbums { get; set; }
        public DbSet<UserArtist> UserArtists { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Friendship> Friendships { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new UserTrackConfiguration());
            modelBuilder.ApplyConfiguration(new UserAlbumConfiguration());
            modelBuilder.ApplyConfiguration(new UserArtistConfiguration());
            modelBuilder.ApplyConfiguration(new TrackConfiguration());
            modelBuilder.ApplyConfiguration(new AlbumConfiguration());
            modelBuilder.ApplyConfiguration(new ArtistConfiguration());
            modelBuilder.ApplyConfiguration(new ImageConfiguration());
            modelBuilder.ApplyConfiguration(new FriendshipConfiguration());
        }
    }
}
