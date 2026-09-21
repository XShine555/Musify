
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Musify.Infrastructure.Persistence;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Musify.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(Database))]
    [Migration("20260717223358_MakePlayListPicturesNullable")]
    partial class MakePlayListPicturesNullable
    {

        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("MassTransit.EntityFrameworkCoreIntegration.InboxState", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<DateTime?>("Consumed")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("ConsumerId")
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("Delivered")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("ExpirationTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long?>("LastSequenceNumber")
                        .HasColumnType("bigint");

                    b.Property<Guid>("LockId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("MessageId")
                        .HasColumnType("uuid");

                    b.Property<int>("ReceiveCount")
                        .HasColumnType("integer");

                    b.Property<DateTime>("Received")
                        .HasColumnType("timestamp with time zone");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("bytea");

                    b.HasKey("Id");

                    b.HasIndex("Delivered");

                    b.ToTable("InboxState");
                });

            modelBuilder.Entity("MassTransit.EntityFrameworkCoreIntegration.OutboxMessage", b =>
                {
                    b.Property<long>("SequenceNumber")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("SequenceNumber"));

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<Guid?>("ConversationId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("CorrelationId")
                        .HasColumnType("uuid");

                    b.Property<string>("DestinationAddress")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<DateTime?>("EnqueueTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("ExpirationTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("FaultAddress")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("Headers")
                        .HasColumnType("text");

                    b.Property<Guid?>("InboxConsumerId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("InboxMessageId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("InitiatorId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("MessageId")
                        .HasColumnType("uuid");

                    b.Property<string>("MessageType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid?>("OutboxId")
                        .HasColumnType("uuid");

                    b.Property<string>("Properties")
                        .HasColumnType("text");

                    b.Property<Guid?>("RequestId")
                        .HasColumnType("uuid");

                    b.Property<string>("ResponseAddress")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<DateTime>("SentTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("SourceAddress")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("SequenceNumber");

                    b.HasIndex("EnqueueTime");

                    b.HasIndex("ExpirationTime");

                    b.HasIndex("OutboxId", "SequenceNumber")
                        .IsUnique();

                    b.HasIndex("InboxMessageId", "InboxConsumerId", "SequenceNumber")
                        .IsUnique();

                    b.ToTable("OutboxMessage");
                });

            modelBuilder.Entity("MassTransit.EntityFrameworkCoreIntegration.OutboxState", b =>
                {
                    b.Property<Guid>("OutboxId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("Created")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("Delivered")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long?>("LastSequenceNumber")
                        .HasColumnType("bigint");

                    b.Property<Guid>("LockId")
                        .HasColumnType("uuid");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("bytea");

                    b.HasKey("OutboxId");

                    b.HasIndex("Created");

                    b.ToTable("OutboxState");
                });

            modelBuilder.Entity("Musify.Domain.Entities.Artist", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("ExternalId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("NormalizedName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("ExternalId")
                        .IsUnique();

                    b.ToTable("Artists");
                });

            modelBuilder.Entity("Musify.Domain.Entities.ListeningHistory", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("ListenedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("TrackId")
                        .HasColumnType("uuid");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("TrackId");

                    b.HasIndex("UserId");

                    b.ToTable("ListeningHistory");
                });

            modelBuilder.Entity("Musify.Domain.Entities.PlayList", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<int>("LifeCycleStatus")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("NormalizedName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("PlayLists");
                });

            modelBuilder.Entity("Musify.Domain.Entities.PlayListHasTrack", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("AddedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("PlayListId")
                        .HasColumnType("uuid");

                    b.Property<int>("Position")
                        .HasColumnType("integer");

                    b.Property<Guid>("TrackId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("PlayListId");

                    b.HasIndex("TrackId");

                    b.ToTable("PlayListHasTrack");
                });

            modelBuilder.Entity("Musify.Domain.Entities.Track", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("DurationSeconds")
                        .HasColumnType("integer");

                    b.Property<int>("LifeCycleStatus")
                        .HasColumnType("integer");

                    b.Property<string>("NormalizedTitle")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("Tracks");

                    b.UseTptMappingStrategy();
                });

            modelBuilder.Entity("Musify.Domain.Entities.TrackArtist", b =>
                {
                    b.Property<Guid>("TrackId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("ArtistId")
                        .HasColumnType("uuid");

                    b.Property<int>("Position")
                        .HasColumnType("integer");

                    b.HasKey("TrackId", "ArtistId");

                    b.HasIndex("ArtistId");

                    b.ToTable("TrackArtist");
                });

            modelBuilder.Entity("Musify.Domain.Entities.Upload", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Bucket")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("State")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Upload");
                });

            modelBuilder.Entity("Musify.Domain.Entities.UploadIntent", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Bucket")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long?>("ExpectedSizeBytes")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)");

                    b.Property<string>("ObjectName")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<int>("Purpose")
                        .HasColumnType("integer");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UploadIntents");
                });

            modelBuilder.Entity("Musify.Domain.Entities.User", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("FirstName")
                        .HasMaxLength(48)
                        .HasColumnType("character varying(48)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(48)
                        .HasColumnType("character varying(48)");

                    b.Property<string>("NormalizedName")
                        .IsRequired()
                        .HasMaxLength(48)
                        .HasColumnType("character varying(48)");

                    b.Property<string>("ProfilePictureUrl")
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)");

                    b.Property<string>("SecondName")
                        .HasMaxLength(48)
                        .HasColumnType("character varying(48)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("User");
                });

            modelBuilder.Entity("Musify.Domain.Entities.UserHasTrack", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("TrackId")
                        .HasColumnType("uuid");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("TrackId");

                    b.HasIndex("UserId");

                    b.ToTable("UserHasTrack");
                });

            modelBuilder.Entity("Musify.Infrastructure.MassTransit.Sagas.PlayListProcessingState", b =>
                {
                    b.Property<Guid>("CorrelationId")
                        .HasColumnType("uuid");

                    b.Property<string>("Bucket")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("CurrentState")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("PictureKey")
                        .HasColumnType("text");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("CorrelationId");

                    b.ToTable("PlayListProcessingState");
                });

            modelBuilder.Entity("Musify.Infrastructure.MassTransit.Sagas.TrackProcessingState", b =>
                {
                    b.Property<Guid>("CorrelationId")
                        .HasColumnType("uuid");

                    b.Property<string>("AudioKey")
                        .HasColumnType("text");

                    b.Property<bool>("AudioProcessed")
                        .HasColumnType("boolean");

                    b.Property<string>("Bucket")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("CurrentState")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("PictureKey")
                        .HasColumnType("text");

                    b.Property<bool>("PictureProcessed")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("CorrelationId");

                    b.ToTable("TrackProcessingState");
                });

            modelBuilder.Entity("Musify.Domain.Entities.ExternalTrack", b =>
                {
                    b.HasBaseType("Musify.Domain.Entities.Track");

                    b.Property<string>("ExternalId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<int>("Source")
                        .HasColumnType("integer");

                    b.HasIndex("Source", "ExternalId")
                        .IsUnique();

                    b.ToTable("ExternalTracks", (string)null);
                });

            modelBuilder.Entity("Musify.Domain.Entities.LocalTrack", b =>
                {
                    b.HasBaseType("Musify.Domain.Entities.Track");

                    b.Property<long>("OwnerUserId")
                        .HasColumnType("bigint");

                    b.HasIndex("OwnerUserId");

                    b.ToTable("LocalTracks", (string)null);
                });

            modelBuilder.Entity("MassTransit.EntityFrameworkCoreIntegration.OutboxMessage", b =>
                {
                    b.HasOne("MassTransit.EntityFrameworkCoreIntegration.OutboxState", null)
                        .WithMany()
                        .HasForeignKey("OutboxId");

                    b.HasOne("MassTransit.EntityFrameworkCoreIntegration.InboxState", null)
                        .WithMany()
                        .HasForeignKey("InboxMessageId", "InboxConsumerId")
                        .HasPrincipalKey("MessageId", "ConsumerId");
                });

            modelBuilder.Entity("Musify.Domain.Entities.ListeningHistory", b =>
                {
                    b.HasOne("Musify.Domain.Entities.Track", "Track")
                        .WithMany("ListeningHistories")
                        .HasForeignKey("TrackId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Musify.Domain.Entities.User", "User")
                        .WithMany("ListeningHistories")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Track");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Musify.Domain.Entities.PlayList", b =>
                {
                    b.HasOne("Musify.Domain.Entities.User", "User")
                        .WithMany("PlayLists")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsOne("Musify.Domain.ValueObjects.PlayListPictures", "Pictures", b1 =>
                        {
                            b1.Property<Guid>("PlayListId")
                                .HasColumnType("uuid");

                            b1.Property<string>("LargeName")
                                .HasMaxLength(64)
                                .HasColumnType("character varying(64)")
                                .HasColumnName("LargePictureName");

                            b1.Property<string>("MediumName")
                                .HasMaxLength(64)
                                .HasColumnType("character varying(64)")
                                .HasColumnName("MediumPictureName");

                            b1.Property<string>("OriginalName")
                                .HasMaxLength(64)
                                .HasColumnType("character varying(64)")
                                .HasColumnName("OriginalPictureName");

                            b1.Property<string>("SmallName")
                                .HasMaxLength(64)
                                .HasColumnType("character varying(64)")
                                .HasColumnName("SmallPictureName");

                            b1.HasKey("PlayListId");

                            b1.ToTable("PlayLists");

                            b1.WithOwner()
                                .HasForeignKey("PlayListId");
                        });

                    b.Navigation("Pictures");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Musify.Domain.Entities.PlayListHasTrack", b =>
                {
                    b.HasOne("Musify.Domain.Entities.PlayList", "PlayList")
                        .WithMany("PlayListTracks")
                        .HasForeignKey("PlayListId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Musify.Domain.Entities.Track", "Track")
                        .WithMany("PlayListTracks")
                        .HasForeignKey("TrackId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PlayList");

                    b.Navigation("Track");
                });

            modelBuilder.Entity("Musify.Domain.Entities.Track", b =>
                {
                    b.OwnsOne("Musify.Domain.ValueObjects.TrackAudio", "Audio", b1 =>
                        {
                            b1.Property<Guid>("TrackId")
                                .HasColumnType("uuid");

                            b1.Property<bool>("DownloadRequested")
                                .HasColumnType("boolean")
                                .HasColumnName("DownloadRequested");

                            b1.Property<string>("FolderName")
                                .HasMaxLength(64)
                                .HasColumnType("character varying(64)")
                                .HasColumnName("AudioFolderName");

                            b1.Property<DateTime?>("LastRetryAt")
                                .HasColumnType("timestamp with time zone")
                                .HasColumnName("LastRetryAt");

                            b1.Property<string>("OriginalName")
                                .HasMaxLength(64)
                                .HasColumnType("character varying(64)")
                                .HasColumnName("OriginalAudioName");

                            b1.Property<int>("RetryCount")
                                .HasColumnType("integer")
                                .HasColumnName("RetryCount");

                            b1.Property<int>("TranscodeStatus")
                                .HasColumnType("integer")
                                .HasColumnName("AudioTranscodeProcessingStatus");

                            b1.HasKey("TrackId");

                            b1.ToTable("Tracks");

                            b1.WithOwner()
                                .HasForeignKey("TrackId");
                        });

                    b.OwnsOne("Musify.Domain.ValueObjects.TrackPictures", "Pictures", b1 =>
                        {
                            b1.Property<Guid>("TrackId")
                                .HasColumnType("uuid");

                            b1.Property<string>("LargeName")
                                .HasMaxLength(64)
                                .HasColumnType("character varying(64)")
                                .HasColumnName("LargePictureName");

                            b1.Property<string>("MediumName")
                                .HasMaxLength(64)
                                .HasColumnType("character varying(64)")
                                .HasColumnName("MediumPictureName");

                            b1.Property<string>("OriginalName")
                                .HasMaxLength(64)
                                .HasColumnType("character varying(64)")
                                .HasColumnName("OriginalPictureName");

                            b1.Property<int>("ProcessingStatus")
                                .HasColumnType("integer")
                                .HasColumnName("PicturesProcessingStatus");

                            b1.Property<string>("SmallName")
                                .HasMaxLength(64)
                                .HasColumnType("character varying(64)")
                                .HasColumnName("SmallPictureName");

                            b1.HasKey("TrackId");

                            b1.ToTable("Tracks");

                            b1.WithOwner()
                                .HasForeignKey("TrackId");
                        });

                    b.Navigation("Audio")
                        .IsRequired();

                    b.Navigation("Pictures")
                        .IsRequired();
                });

            modelBuilder.Entity("Musify.Domain.Entities.TrackArtist", b =>
                {
                    b.HasOne("Musify.Domain.Entities.Artist", "Artist")
                        .WithMany("TrackArtists")
                        .HasForeignKey("ArtistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Musify.Domain.Entities.ExternalTrack", "Track")
                        .WithMany("TrackArtists")
                        .HasForeignKey("TrackId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Artist");

                    b.Navigation("Track");
                });

            modelBuilder.Entity("Musify.Domain.Entities.UploadIntent", b =>
                {
                    b.HasOne("Musify.Domain.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Musify.Domain.Entities.UserHasTrack", b =>
                {
                    b.HasOne("Musify.Domain.Entities.Track", "Track")
                        .WithMany("UserTracks")
                        .HasForeignKey("TrackId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Musify.Domain.Entities.User", "User")
                        .WithMany("UserTracks")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Track");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Musify.Domain.Entities.ExternalTrack", b =>
                {
                    b.HasOne("Musify.Domain.Entities.Track", null)
                        .WithOne()
                        .HasForeignKey("Musify.Domain.Entities.ExternalTrack", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Musify.Domain.Entities.LocalTrack", b =>
                {
                    b.HasOne("Musify.Domain.Entities.Track", null)
                        .WithOne()
                        .HasForeignKey("Musify.Domain.Entities.LocalTrack", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Musify.Domain.Entities.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerUserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Owner");
                });

            modelBuilder.Entity("Musify.Domain.Entities.Artist", b =>
                {
                    b.Navigation("TrackArtists");
                });

            modelBuilder.Entity("Musify.Domain.Entities.PlayList", b =>
                {
                    b.Navigation("PlayListTracks");
                });

            modelBuilder.Entity("Musify.Domain.Entities.Track", b =>
                {
                    b.Navigation("ListeningHistories");

                    b.Navigation("PlayListTracks");

                    b.Navigation("UserTracks");
                });

            modelBuilder.Entity("Musify.Domain.Entities.User", b =>
                {
                    b.Navigation("ListeningHistories");

                    b.Navigation("PlayLists");

                    b.Navigation("UserTracks");
                });

            modelBuilder.Entity("Musify.Domain.Entities.ExternalTrack", b =>
                {
                    b.Navigation("TrackArtists");
                });
#pragma warning restore 612, 618
        }
    }
}
