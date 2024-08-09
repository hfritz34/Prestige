﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Prestige.Api.Data;

#nullable disable

namespace Prestige.Api.Migrations
{
    [DbContext(typeof(PrestigeContext))]
    [Migration("20240807152208_FullUserIndex")]
    partial class FullUserIndex
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("AlbumArtist", b =>
                {
                    b.Property<string>("AlbumId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ArtistsId")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("AlbumId", "ArtistsId");

                    b.HasIndex("ArtistsId");

                    b.ToTable("AlbumArtist");
                });

            modelBuilder.Entity("ArtistTrack", b =>
                {
                    b.Property<string>("ArtistsId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("TrackId")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("ArtistsId", "TrackId");

                    b.HasIndex("TrackId");

                    b.ToTable("ArtistTrack");
                });

            modelBuilder.Entity("Prestige.Api.Domain.Album", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.HasKey("Id");

                    b.ToTable("Album", (string)null);
                });

            modelBuilder.Entity("Prestige.Api.Domain.Artist", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.HasKey("Id");

                    b.ToTable("Artist", (string)null);
                });

            modelBuilder.Entity("Prestige.Api.Domain.Friendship", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("FriendId")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("UserId", "FriendId");

                    b.HasIndex("FriendId");

                    b.ToTable("Friendships");
                });

            modelBuilder.Entity("Prestige.Api.Domain.Image", b =>
                {
                    b.Property<string>("Url")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("AlbumId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ArtistId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("Height")
                        .HasColumnType("int");

                    b.Property<int>("Width")
                        .HasColumnType("int");

                    b.HasKey("Url");

                    b.HasIndex("AlbumId");

                    b.HasIndex("ArtistId");

                    b.ToTable("Image", (string)null);
                });

            modelBuilder.Entity("Prestige.Api.Domain.Track", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("AlbumId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("DurationMs")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("AlbumId");

                    b.ToTable("Track", (string)null);
                });

            modelBuilder.Entity("Prestige.Api.Domain.User", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("AccessToken")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("NickName")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("ProfilePicURL")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RefreshToken")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("User", (string)null);
                });

            modelBuilder.Entity("Prestige.Api.Domain.UserAlbum", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("AlbumId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<bool>("IsFavorite")
                        .HasColumnType("bit");

                    b.Property<int>("TotalTime")
                        .HasColumnType("int");

                    b.HasKey("UserId", "AlbumId");

                    b.HasIndex("AlbumId");

                    b.ToTable("UserAlbum", (string)null);
                });

            modelBuilder.Entity("Prestige.Api.Domain.UserArtist", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ArtistId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<bool>("IsFavorite")
                        .HasColumnType("bit");

                    b.Property<int>("TotalTime")
                        .HasColumnType("int");

                    b.HasKey("UserId", "ArtistId");

                    b.HasIndex("ArtistId");

                    b.ToTable("UserArtist", (string)null);
                });

            modelBuilder.Entity("Prestige.Api.Domain.UserTrack", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("TrackId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<bool>("IsFavorite")
                        .HasColumnType("bit");

                    b.Property<int>("TotalTime")
                        .HasColumnType("int");

                    b.HasKey("UserId", "TrackId");

                    b.HasIndex("TrackId");

                    b.ToTable("UserTrack", (string)null);
                });

            modelBuilder.Entity("AlbumArtist", b =>
                {
                    b.HasOne("Prestige.Api.Domain.Album", null)
                        .WithMany()
                        .HasForeignKey("AlbumId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Prestige.Api.Domain.Artist", null)
                        .WithMany()
                        .HasForeignKey("ArtistsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ArtistTrack", b =>
                {
                    b.HasOne("Prestige.Api.Domain.Artist", null)
                        .WithMany()
                        .HasForeignKey("ArtistsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Prestige.Api.Domain.Track", null)
                        .WithMany()
                        .HasForeignKey("TrackId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Prestige.Api.Domain.Friendship", b =>
                {
                    b.HasOne("Prestige.Api.Domain.User", "Friend")
                        .WithMany("Friends")
                        .HasForeignKey("FriendId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Prestige.Api.Domain.User", "User")
                        .WithMany("Friendships")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Friend");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Prestige.Api.Domain.Image", b =>
                {
                    b.HasOne("Prestige.Api.Domain.Album", null)
                        .WithMany("Images")
                        .HasForeignKey("AlbumId");

                    b.HasOne("Prestige.Api.Domain.Artist", null)
                        .WithMany("Images")
                        .HasForeignKey("ArtistId");
                });

            modelBuilder.Entity("Prestige.Api.Domain.Track", b =>
                {
                    b.HasOne("Prestige.Api.Domain.Album", "Album")
                        .WithMany()
                        .HasForeignKey("AlbumId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Album");
                });

            modelBuilder.Entity("Prestige.Api.Domain.UserAlbum", b =>
                {
                    b.HasOne("Prestige.Api.Domain.Album", "Album")
                        .WithMany()
                        .HasForeignKey("AlbumId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Prestige.Api.Domain.User", "User")
                        .WithMany("UserAlbums")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Album");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Prestige.Api.Domain.UserArtist", b =>
                {
                    b.HasOne("Prestige.Api.Domain.Artist", "Artist")
                        .WithMany()
                        .HasForeignKey("ArtistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Prestige.Api.Domain.User", "User")
                        .WithMany("UserArtists")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Artist");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Prestige.Api.Domain.UserTrack", b =>
                {
                    b.HasOne("Prestige.Api.Domain.Track", "Track")
                        .WithMany()
                        .HasForeignKey("TrackId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Prestige.Api.Domain.User", "User")
                        .WithMany("UserTracks")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Track");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Prestige.Api.Domain.Album", b =>
                {
                    b.Navigation("Images");
                });

            modelBuilder.Entity("Prestige.Api.Domain.Artist", b =>
                {
                    b.Navigation("Images");
                });

            modelBuilder.Entity("Prestige.Api.Domain.User", b =>
                {
                    b.Navigation("Friends");

                    b.Navigation("Friendships");

                    b.Navigation("UserAlbums");

                    b.Navigation("UserArtists");

                    b.Navigation("UserTracks");
                });
#pragma warning restore 612, 618
        }
    }
}
