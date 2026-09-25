using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Coauthors;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Xunit;

namespace Musify.Application.Tests.Coauthors
{
    public sealed class CoauthorHandlersTests : HandlerTestBase
    {
        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private InviteCoauthorsCommandHandler CreateInviteHandler() => new(Database, NoOpLogger<InviteCoauthorsCommandHandler>());

        private RemoveCoauthorCommandHandler CreateRemoveHandler() => new(Database, NoOpLogger<RemoveCoauthorCommandHandler>());

        private RespondToCoauthorInvitationCommandHandler CreateRespondHandler() => new(Database, NoOpLogger<RespondToCoauthorInvitationCommandHandler>());

        private static TrackCoauthor Coauthor(Track track, User user, CoauthorStatus status) =>
            new() { TrackId = track.Id, UserId = user.Id, Status = status };

        [Fact]
        public async Task Invite_Owner_CreatesPendingInvitations()
        {
            var owner = TestEntities.User(1, "owner");
            var guest = TestEntities.User(2, "guest");
            var track = TestEntities.Track(owner);
            await SeedAsync(owner, guest, track);

            var result = await CreateInviteHandler().Handle(new InviteCoauthorsCommand(owner.Id, track.Id, [guest.Id]), Ct);

            Assert.False(result.IsError);
            var invite = Assert.Single(result.Value);
            Assert.Equal(guest.Id, invite.UserId);
            Assert.Equal("guest", invite.Name);
            Assert.Equal(CoauthorStatus.Pending, invite.Status);
        }

        [Fact]
        public async Task Invite_NotTheOwner_IsForbidden()
        {
            var owner = TestEntities.User(1, "owner");
            var other = TestEntities.User(2, "other");
            var track = TestEntities.Track(owner);
            await SeedAsync(owner, other, track);

            var result = await CreateInviteHandler().Handle(new InviteCoauthorsCommand(other.Id, track.Id, [other.Id]), Ct);

            Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
        }

        [Fact]
        public async Task Invite_Self_IsRejected()
        {
            var owner = TestEntities.User(1, "owner");
            var track = TestEntities.Track(owner);
            await SeedAsync(owner, track);

            var result = await CreateInviteHandler().Handle(new InviteCoauthorsCommand(owner.Id, track.Id, [owner.Id]), Ct);

            Assert.Equal(ErrorType.Validation, result.FirstError.Type);
            Assert.Empty(Database.TrackCoauthors);
        }

        [Fact]
        public async Task Invite_UnknownUser_IsNotFound()
        {
            var owner = TestEntities.User(1, "owner");
            var track = TestEntities.Track(owner);
            await SeedAsync(owner, track);

            var result = await CreateInviteHandler().Handle(new InviteCoauthorsCommand(owner.Id, track.Id, [99]), Ct);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Theory]
        [InlineData(CoauthorStatus.Pending)]
        [InlineData(CoauthorStatus.Accepted)]
        public async Task Invite_AlreadyInvited_IsConflict(CoauthorStatus status)
        {
            var owner = TestEntities.User(1, "owner");
            var guest = TestEntities.User(2, "guest");
            var track = TestEntities.Track(owner);
            await SeedAsync(owner, guest, track, Coauthor(track, guest, status));

            var result = await CreateInviteHandler().Handle(new InviteCoauthorsCommand(owner.Id, track.Id, [guest.Id]), Ct);

            Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
            Assert.Equal(status, Assert.Single(Database.TrackCoauthors).Status);
        }

        [Fact]
        public async Task Invite_PreviouslyRejected_ResetsToPending()
        {
            var owner = TestEntities.User(1, "owner");
            var guest = TestEntities.User(2, "guest");
            var track = TestEntities.Track(owner);
            var rejected = Coauthor(track, guest, CoauthorStatus.Rejected);
            rejected.RespondedAt = DateTime.UtcNow;
            await SeedAsync(owner, guest, track, rejected);

            var result = await CreateInviteHandler().Handle(new InviteCoauthorsCommand(owner.Id, track.Id, [guest.Id]), Ct);

            Assert.False(result.IsError);
            var stored = await Database.TrackCoauthors.AsNoTracking().SingleAsync(Ct);
            Assert.Equal(CoauthorStatus.Pending, stored.Status);
            Assert.Null(stored.RespondedAt);
        }

        [Fact]
        public async Task Accept_Invitee_MakesThemAnAcceptedCoauthor()
        {
            var owner = TestEntities.User(1, "owner");
            var guest = TestEntities.User(2, "guest");
            var track = TestEntities.Track(owner);
            var invitation = Coauthor(track, guest, CoauthorStatus.Pending);
            await SeedAsync(owner, guest, track, invitation);

            var result = await CreateRespondHandler().Handle(new RespondToCoauthorInvitationCommand(guest.Id, invitation.Id, Accept: true), Ct);

            Assert.False(result.IsError);
            var stored = await Database.TrackCoauthors.AsNoTracking().SingleAsync(Ct);
            Assert.Equal(CoauthorStatus.Accepted, stored.Status);
            Assert.NotNull(stored.RespondedAt);
        }

        [Fact]
        public async Task Reject_Invitee_MarksTheInvitationRejected()
        {
            var owner = TestEntities.User(1, "owner");
            var guest = TestEntities.User(2, "guest");
            var track = TestEntities.Track(owner);
            var invitation = Coauthor(track, guest, CoauthorStatus.Pending);
            await SeedAsync(owner, guest, track, invitation);

            var result = await CreateRespondHandler().Handle(new RespondToCoauthorInvitationCommand(guest.Id, invitation.Id, Accept: false), Ct);

            Assert.False(result.IsError);
            Assert.Equal(CoauthorStatus.Rejected, (await Database.TrackCoauthors.AsNoTracking().SingleAsync(Ct)).Status);
        }

        [Fact]
        public async Task Respond_NotTheInvitee_IsForbidden()
        {
            var owner = TestEntities.User(1, "owner");
            var guest = TestEntities.User(2, "guest");
            var track = TestEntities.Track(owner);
            var invitation = Coauthor(track, guest, CoauthorStatus.Pending);
            await SeedAsync(owner, guest, track, invitation);

            var result = await CreateRespondHandler().Handle(new RespondToCoauthorInvitationCommand(owner.Id, invitation.Id, Accept: true), Ct);

            Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
            Assert.Equal(CoauthorStatus.Pending, (await Database.TrackCoauthors.AsNoTracking().SingleAsync(Ct)).Status);
        }

        [Theory]
        [InlineData(CoauthorStatus.Accepted, true)]
        [InlineData(CoauthorStatus.Accepted, false)]
        [InlineData(CoauthorStatus.Rejected, true)]
        [InlineData(CoauthorStatus.Rejected, false)]
        public async Task Respond_AlreadyAnswered_IsConflict(CoauthorStatus status, bool accept)
        {
            var owner = TestEntities.User(1, "owner");
            var guest = TestEntities.User(2, "guest");
            var track = TestEntities.Track(owner);
            var invitation = Coauthor(track, guest, status);
            await SeedAsync(owner, guest, track, invitation);

            var result = await CreateRespondHandler().Handle(new RespondToCoauthorInvitationCommand(guest.Id, invitation.Id, accept), Ct);

            Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
            Assert.Equal(status, (await Database.TrackCoauthors.AsNoTracking().SingleAsync(Ct)).Status);
        }

        [Fact]
        public async Task Respond_UnknownInvitation_IsNotFound()
        {
            var result = await CreateRespondHandler().Handle(new RespondToCoauthorInvitationCommand(1, Guid.NewGuid(), Accept: true), Ct);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Remove_OwnerOrCoauthorThemself_DeletesTheRow()
        {
            var owner = TestEntities.User(1, "owner");
            var guest = TestEntities.User(2, "guest");
            var second = TestEntities.User(3, "second");
            var track = TestEntities.Track(owner);
            await SeedAsync(owner, guest, second, track,
                Coauthor(track, guest, CoauthorStatus.Accepted),
                Coauthor(track, second, CoauthorStatus.Pending));

            var byOwner = await CreateRemoveHandler().Handle(new RemoveCoauthorCommand(owner.Id, track.Id, second.Id), Ct);
            var byCoauthor = await CreateRemoveHandler().Handle(new RemoveCoauthorCommand(guest.Id, track.Id, guest.Id), Ct);

            Assert.False(byOwner.IsError);
            Assert.False(byCoauthor.IsError);
            Assert.Empty(Database.TrackCoauthors);
        }

        [Fact]
        public async Task Remove_AnotherUser_IsForbidden()
        {
            var owner = TestEntities.User(1, "owner");
            var guest = TestEntities.User(2, "guest");
            var stranger = TestEntities.User(3, "stranger");
            var track = TestEntities.Track(owner);
            await SeedAsync(owner, guest, stranger, track, Coauthor(track, guest, CoauthorStatus.Accepted));

            var result = await CreateRemoveHandler().Handle(new RemoveCoauthorCommand(stranger.Id, track.Id, guest.Id), Ct);

            Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
            Assert.Single(Database.TrackCoauthors);
        }

        [Fact]
        public async Task Remove_NoSuchCoauthor_IsNotFound()
        {
            var owner = TestEntities.User(1, "owner");
            var track = TestEntities.Track(owner);
            await SeedAsync(owner, track);

            var result = await CreateRemoveHandler().Handle(new RemoveCoauthorCommand(owner.Id, track.Id, 2), Ct);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task GetInvitations_ReturnsOnlyPendingOnesOfTheCaller()
        {
            var owner = TestEntities.User(1, "owner");
            var guest = TestEntities.User(2, "guest");
            var pendingTrack = TestEntities.Track(owner, "Pending one");
            var acceptedTrack = TestEntities.Track(owner, "Accepted one");
            var rejectedTrack = TestEntities.Track(owner, "Rejected one");
            var otherTrack = TestEntities.Track(owner, "For someone else");
            await SeedAsync(owner, guest, pendingTrack, acceptedTrack, rejectedTrack, otherTrack,
                Coauthor(pendingTrack, guest, CoauthorStatus.Pending),
                Coauthor(acceptedTrack, guest, CoauthorStatus.Accepted),
                Coauthor(rejectedTrack, guest, CoauthorStatus.Rejected),
                Coauthor(otherTrack, owner, CoauthorStatus.Pending));

            var result = await new GetCoauthorInvitationsQueryHandler(Database)
                .Handle(new GetCoauthorInvitationsQuery(guest.Id, 1, 10), Ct);

            var invitation = Assert.Single(result.Items);
            Assert.Equal("Pending one", invitation.TrackTitle);
            Assert.Equal("owner", invitation.Artist);
            Assert.Equal(owner.Id, invitation.OwnerUserId);
        }

        [Fact]
        public async Task GetTrackById_ShowsOnlyAcceptedCoauthors()
        {
            var owner = TestEntities.User(1, "owner");
            var accepted = TestEntities.User(2, "accepted");
            var pending = TestEntities.User(3, "pending");
            var rejected = TestEntities.User(4, "rejected");
            var track = TestEntities.Track(owner);
            await SeedAsync(owner, accepted, pending, rejected, track,
                Coauthor(track, accepted, CoauthorStatus.Accepted),
                Coauthor(track, pending, CoauthorStatus.Pending),
                Coauthor(track, rejected, CoauthorStatus.Rejected));

            var result = await new GetTrackByIdQueryHandler(Database).Handle(new GetTrackByIdQuery(track.Id, owner.Id), Ct);

            var coauthor = Assert.Single(result.Value.Coauthors);
            Assert.Equal(accepted.Id, coauthor.Id);
            Assert.Equal("accepted", coauthor.Name);
        }

        [Fact]
        public async Task GetTrackById_UnpublishedTrack_StaysHiddenFromItsCoauthor()
        {
            var owner = TestEntities.User(1, "owner");
            var guest = TestEntities.User(2, "guest");
            var track = TestEntities.Track(owner, isPublished: false);
            await SeedAsync(owner, guest, track, Coauthor(track, guest, CoauthorStatus.Accepted));

            var result = await new GetTrackByIdQueryHandler(Database).Handle(new GetTrackByIdQuery(track.Id, guest.Id), Ct);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task GetTracksByUserId_PublicProfile_IncludesPublishedCoauthoredTracksOnly()
        {
            var owner = TestEntities.User(1, "owner");
            var guest = TestEntities.User(2, "guest");
            var own = TestEntities.Track(guest, "Own");
            var shared = TestEntities.Track(owner, "Shared");
            var hidden = TestEntities.Track(owner, "Hidden", isPublished: false);
            var pending = TestEntities.Track(owner, "Pending");
            var rejected = TestEntities.Track(owner, "Rejected");
            await SeedAsync(owner, guest, own, shared, hidden, pending, rejected,
                Coauthor(shared, guest, CoauthorStatus.Accepted),
                Coauthor(hidden, guest, CoauthorStatus.Accepted),
                Coauthor(pending, guest, CoauthorStatus.Pending),
                Coauthor(rejected, guest, CoauthorStatus.Rejected));

            var result = await new GetTracksByUserIdQueryHandler(Database)
                .Handle(new GetTracksByUserIdQuery(guest.Id, null, 1, 10, ViewerId: owner.Id), Ct);

            Assert.Equal(["Own", "Shared"], result.Value.Items.Select(track => track.Title).Order().ToList());
        }

        [Fact]
        public async Task GetTracksByUserId_OwnLibrary_ExcludesCoauthoredTracksByDefault()
        {
            var owner = TestEntities.User(1, "owner");
            var guest = TestEntities.User(2, "guest");
            var own = TestEntities.Track(guest, "Own");
            var shared = TestEntities.Track(owner, "Shared");
            await SeedAsync(owner, guest, own, shared, Coauthor(shared, guest, CoauthorStatus.Accepted));
            var handler = new GetTracksByUserIdQueryHandler(Database);

            var library = await handler.Handle(new GetTracksByUserIdQuery(guest.Id, null, 1, 10, ViewerId: guest.Id), Ct);
            var withCoauthored = await handler.Handle(
                new GetTracksByUserIdQuery(guest.Id, null, 1, 10, ViewerId: guest.Id, IncludeCoauthored: true), Ct);

            Assert.Equal(["Own"], library.Value.Items.Select(track => track.Title).ToList());
            Assert.Equal(2, withCoauthored.Value.Items.Count);
        }

        [Fact]
        public async Task GetTracksByUserId_Invites_AreVisibleOnlyToTheOwner()
        {
            var owner = TestEntities.User(1, "owner");
            var accepted = TestEntities.User(2, "accepted");
            var pending = TestEntities.User(3, "pending");
            var track = TestEntities.Track(owner);
            await SeedAsync(owner, accepted, pending, track,
                Coauthor(track, accepted, CoauthorStatus.Accepted),
                Coauthor(track, pending, CoauthorStatus.Pending));
            var handler = new GetTracksByUserIdQueryHandler(Database);

            var asOwner = await handler.Handle(new GetTracksByUserIdQuery(owner.Id, null, 1, 10, ViewerId: owner.Id), Ct);
            var asStranger = await handler.Handle(new GetTracksByUserIdQuery(owner.Id, null, 1, 10, ViewerId: pending.Id), Ct);
            var anonymous = await handler.Handle(new GetTracksByUserIdQuery(owner.Id, null, 1, 10), Ct);

            var ownerItem = Assert.Single(asOwner.Value.Items);
            Assert.Equal(2, ownerItem.CoauthorInvites.Count);
            Assert.Contains(ownerItem.CoauthorInvites, invite => invite.Name == "pending" && invite.Status == CoauthorStatus.Pending);
            Assert.Equal(accepted.Id, Assert.Single(ownerItem.Coauthors).Id);
            Assert.Empty(Assert.Single(asStranger.Value.Items).CoauthorInvites);
            Assert.Empty(Assert.Single(anonymous.Value.Items).CoauthorInvites);
            Assert.Equal(accepted.Id, Assert.Single(Assert.Single(anonymous.Value.Items).Coauthors).Id);
        }

        [Fact]
        public async Task GetTracksByUserId_CoauthoredTrack_KeepsSharedListensCount()
        {
            var owner = TestEntities.User(1, "owner");
            var guest = TestEntities.User(2, "guest");
            var listener = TestEntities.User(3, "listener");
            var track = TestEntities.Track(owner);
            await SeedAsync(owner, guest, listener, track, Coauthor(track, guest, CoauthorStatus.Accepted),
                TestEntities.ListeningHistory(listener.Id, track.Id),
                TestEntities.ListeningHistory(listener.Id, track.Id));

            var result = await new GetTracksByUserIdQueryHandler(Database)
                .Handle(new GetTracksByUserIdQuery(guest.Id, null, 1, 10), Ct);

            Assert.Equal(2, Assert.Single(result.Value.Items).ListensCount);
        }
    }
}
