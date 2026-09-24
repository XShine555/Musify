using Microsoft.EntityFrameworkCore;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.MassTransit.Sagas;

namespace Musify.Infrastructure.Persistence;

/// <summary>
/// Entity model shared by <see cref="Database"/> and the in-memory test database, so both always agree.
/// </summary>
public static class ModelConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        var trackProcessing = modelBuilder.Entity<TrackProcessingState>();
        trackProcessing.HasKey(state => state.CorrelationId);
        trackProcessing.Property(state => state.CorrelationId).ValueGeneratedNever();
        trackProcessing.Property(state => state.CurrentState).HasMaxLength(64);

        modelBuilder.Entity<Track>()
            .OwnsOne(track => track.Pictures, pictures =>
            {
                pictures.Property(p => p.OriginalName).HasColumnName("OriginalPictureName").HasMaxLength(64);
                pictures.Property(p => p.SmallName).HasColumnName("SmallPictureName").HasMaxLength(64);
                pictures.Property(p => p.MediumName).HasColumnName("MediumPictureName").HasMaxLength(64);
                pictures.Property(p => p.LargeName).HasColumnName("LargePictureName").HasMaxLength(64);
                pictures.Property(p => p.ProcessingStatus).HasColumnName("PicturesProcessingStatus");
            });
        modelBuilder.Entity<Track>().Navigation(track => track.Pictures).IsRequired();

        modelBuilder.Entity<Track>()
            .OwnsOne(track => track.Audio, audio =>
            {
                audio.Property(a => a.OriginalName).HasColumnName("OriginalAudioName").HasMaxLength(64);
                audio.Property(a => a.FolderName).HasColumnName("AudioFolderName").HasMaxLength(64);
                audio.Property(a => a.TranscodeStatus).HasColumnName("AudioTranscodeProcessingStatus");
            });
        modelBuilder.Entity<Track>().Navigation(track => track.Audio).IsRequired();

        modelBuilder.Entity<Track>()
            .HasOne(track => track.Owner)
            .WithMany()
            .HasForeignKey(track => track.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PlayList>()
            .Property(playList => playList.Visibility)
            .HasDefaultValue(PlayListVisibility.Private);

        modelBuilder.Entity<PlayList>()
            .OwnsOne(playList => playList.Pictures, pictures =>
            {
                pictures.Property(p => p.OriginalName).HasColumnName("OriginalPictureName").HasMaxLength(64).IsRequired(false);
                pictures.Property(p => p.SmallName).HasColumnName("SmallPictureName").HasMaxLength(64).IsRequired(false);
                pictures.Property(p => p.MediumName).HasColumnName("MediumPictureName").HasMaxLength(64).IsRequired(false);
                pictures.Property(p => p.LargeName).HasColumnName("LargePictureName").HasMaxLength(64).IsRequired(false);
            });
        modelBuilder.Entity<PlayList>().Navigation(playList => playList.Pictures).IsRequired(false);

        modelBuilder.Entity<Album>()
            .OwnsOne(album => album.Pictures, pictures =>
            {
                pictures.Property(p => p.OriginalName).HasColumnName("OriginalPictureName").HasMaxLength(64);
                pictures.Property(p => p.SmallName).HasColumnName("SmallPictureName").HasMaxLength(64).IsRequired(false);
                pictures.Property(p => p.MediumName).HasColumnName("MediumPictureName").HasMaxLength(64).IsRequired(false);
                pictures.Property(p => p.LargeName).HasColumnName("LargePictureName").HasMaxLength(64).IsRequired(false);
            });
        modelBuilder.Entity<Album>().Navigation(album => album.Pictures).IsRequired(false);

        modelBuilder.Entity<Album>()
            .HasOne(album => album.Owner)
            .WithMany()
            .HasForeignKey(album => album.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AlbumHasTrack>()
            .HasOne(albumTrack => albumTrack.Album)
            .WithMany(album => album.AlbumTracks)
            .HasForeignKey(albumTrack => albumTrack.AlbumId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AlbumHasTrack>()
            .HasOne(albumTrack => albumTrack.Track)
            .WithMany(track => track.AlbumTracks)
            .HasForeignKey(albumTrack => albumTrack.TrackId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AlbumHasTrack>()
            .HasIndex(albumTrack => new { albumTrack.AlbumId, albumTrack.TrackId })
            .IsUnique();

        modelBuilder.Entity<PlayListHasTrack>()
            .HasIndex(playListTrack => new { playListTrack.PlayListId, playListTrack.TrackId })
            .IsUnique();

        modelBuilder.Entity<Mix>()
            .HasIndex(mix => new { mix.UserId, mix.Position });

        modelBuilder.Entity<MixItem>()
            .HasOne(item => item.Mix)
            .WithMany(mix => mix.Items)
            .HasForeignKey(item => item.MixId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MixItem>()
            .HasOne(item => item.Track)
            .WithMany()
            .HasForeignKey(item => item.TrackId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MixItem>()
            .HasIndex(item => new { item.MixId, item.Position });

        modelBuilder.Entity<TrackLike>()
            .HasIndex(like => new { like.UserId, like.TrackId })
            .IsUnique();

        modelBuilder.Entity<TrackTag>()
            .HasOne(trackTag => trackTag.Track)
            .WithMany(track => track.Tags)
            .HasForeignKey(trackTag => trackTag.TrackId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TrackTag>()
            .HasIndex(trackTag => new { trackTag.TrackId, trackTag.Tag })
            .IsUnique();

        modelBuilder.Entity<UserFollow>()
            .HasOne(follow => follow.Follower)
            .WithMany()
            .HasForeignKey(follow => follow.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserFollow>()
            .HasOne(follow => follow.Followed)
            .WithMany()
            .HasForeignKey(follow => follow.FollowedId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserFollow>()
            .HasIndex(follow => new { follow.FollowerId, follow.FollowedId })
            .IsUnique();

        var playListProcessing = modelBuilder.Entity<PlayListProcessingState>();
        playListProcessing.HasKey(state => state.CorrelationId);
        playListProcessing.Property(state => state.CorrelationId).ValueGeneratedNever();
        playListProcessing.Property(state => state.CurrentState).HasMaxLength(64);

        var albumProcessing = modelBuilder.Entity<AlbumProcessingState>();
        albumProcessing.HasKey(state => state.CorrelationId);
        albumProcessing.Property(state => state.CorrelationId).ValueGeneratedNever();
        albumProcessing.Property(state => state.CurrentState).HasMaxLength(64);
    }
}
