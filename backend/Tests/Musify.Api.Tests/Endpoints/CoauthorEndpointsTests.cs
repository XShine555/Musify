using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Musify.Api.DataTransferObjects.Tracks;
using Musify.Api.Tests.TestSupport;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.Persistence;
using Xunit;

namespace Musify.Api.Tests.Endpoints
{
    [Collection(ApiCollection.Name)]
    public sealed class CoauthorEndpointsTests(ApiTestFixture fixture)
    {
        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private async Task<Guid> SeedTrackAsync(long ownerId, string title, bool isPublished = true)
        {
            using var scope = fixture.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<Database>();
            var track = new Track
            {
                Title = title,
                NormalizedTitle = title.ToUpperInvariant(),
                OwnerUserId = ownerId,
                IsPublished = isPublished,
                Audio = new TrackAudio { TranscodeStatus = ProcessingStatus.Completed, FolderName = "folder" }
            };
            database.Tracks.Add(track);
            await database.SaveChangesAsync(Ct);
            return track.Id;
        }

        private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            var text = await response.Content.ReadAsStringAsync(Ct);
            return JsonDocument.Parse(text).RootElement.Clone();
        }

        private static string Id(long id) => id.ToString(CultureInfo.InvariantCulture);

        private async Task<Guid> InviteAndGetInvitationIdAsync(long ownerId, Guid trackId, long guestId)
        {
            var invite = await fixture.CreateAuthenticatedClient(ownerId)
                .PostAsJsonAsync($"/tracks/{trackId}/coauthors", new InviteCoauthorsRequest([guestId]), Ct);
            Assert.Equal(HttpStatusCode.OK, invite.StatusCode);

            var invitations = await ReadJsonAsync(await fixture.CreateAuthenticatedClient(guestId)
                .GetAsync("/users/me/coauthor-invitations?pageNumber=1&pageSize=10", Ct));
            return invitations.GetProperty("items").EnumerateArray()
                .Single(item => item.GetProperty("trackId").GetGuid() == trackId)
                .GetProperty("id").GetGuid();
        }

        [Fact]
        public async Task Coauthors_FullFlow_ExposesOnlyAcceptedCoauthorsPublicly()
        {
            var owner = await fixture.SeedUserAsync(name: "coauthor-owner");
            var guest = await fixture.SeedUserAsync(name: "coauthor-guest");
            var trackId = await SeedTrackAsync(owner, "Shared song");
            var ownerClient = fixture.CreateAuthenticatedClient(owner);
            var guestClient = fixture.CreateAuthenticatedClient(guest);

            var invitationId = await InviteAndGetInvitationIdAsync(owner, trackId, guest);

            var beforeAccept = await ReadJsonAsync(await fixture.CreateAnonymousClient().GetAsync($"/tracks/{trackId}", Ct));
            var ownerListBefore = await ReadJsonAsync(await ownerClient.GetAsync($"/tracks/users/{Id(owner)}", Ct));
            var duplicate = await ownerClient.PostAsJsonAsync($"/tracks/{trackId}/coauthors", new InviteCoauthorsRequest([guest]), Ct);
            var accept = await guestClient.PostAsync($"/coauthor-invitations/{invitationId}/accept", null, Ct);
            var acceptAgain = await guestClient.PostAsync($"/coauthor-invitations/{invitationId}/reject", null, Ct);
            var afterAccept = await ReadJsonAsync(await fixture.CreateAnonymousClient().GetAsync($"/tracks/{trackId}", Ct));
            var ownerListAfter = await ReadJsonAsync(await ownerClient.GetAsync($"/tracks/users/{Id(owner)}", Ct));
            var guestProfile = await ReadJsonAsync(await fixture.CreateAnonymousClient().GetAsync($"/tracks/users/{Id(guest)}", Ct));
            var guestLibrary = await ReadJsonAsync(await guestClient.GetAsync($"/tracks/users/{Id(guest)}", Ct));

            Assert.Equal(0, beforeAccept.GetProperty("coauthors").GetArrayLength());
            var pendingInvite = ownerListBefore.GetProperty("items")[0].GetProperty("coauthorInvites")[0];
            Assert.Equal("Pending", pendingInvite.GetProperty("status").GetString());
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, accept.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, acceptAgain.StatusCode);
            Assert.Equal("coauthor-guest", afterAccept.GetProperty("coauthors")[0].GetProperty("name").GetString());
            Assert.Equal("Accepted", ownerListAfter.GetProperty("items")[0].GetProperty("coauthorInvites")[0].GetProperty("status").GetString());
            Assert.Equal(trackId, guestProfile.GetProperty("items").EnumerateArray().Single().GetProperty("id").GetGuid());
            Assert.Equal(0, guestProfile.GetProperty("items")[0].GetProperty("coauthorInvites").GetArrayLength());
            Assert.Equal(0, guestLibrary.GetProperty("items").GetArrayLength());
        }

        [Fact]
        public async Task Coauthors_Permissions_AreEnforced()
        {
            var owner = await fixture.SeedUserAsync();
            var guest = await fixture.SeedUserAsync();
            var stranger = await fixture.SeedUserAsync();
            var trackId = await SeedTrackAsync(owner, "Guarded");
            var invitationId = await InviteAndGetInvitationIdAsync(owner, trackId, guest);

            var strangerInvites = await fixture.CreateAuthenticatedClient(stranger)
                .PostAsJsonAsync($"/tracks/{trackId}/coauthors", new InviteCoauthorsRequest([stranger]), Ct);
            var self = await fixture.CreateAuthenticatedClient(owner)
                .PostAsJsonAsync($"/tracks/{trackId}/coauthors", new InviteCoauthorsRequest([owner]), Ct);
            var strangerAccepts = await fixture.CreateAuthenticatedClient(stranger)
                .PostAsync($"/coauthor-invitations/{invitationId}/accept", null, Ct);
            var strangerRemoves = await fixture.CreateAuthenticatedClient(stranger)
                .DeleteAsync($"/tracks/{trackId}/coauthors/{Id(guest)}", Ct);
            var anonymous = await fixture.CreateAnonymousClient()
                .GetAsync("/users/me/coauthor-invitations?pageNumber=1&pageSize=10", Ct);

            Assert.Equal(HttpStatusCode.Forbidden, strangerInvites.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, self.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, strangerAccepts.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, strangerRemoves.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        }

        [Fact]
        public async Task Coauthors_OwnerRemovesAndReinvitesRejected_Works()
        {
            var owner = await fixture.SeedUserAsync();
            var guest = await fixture.SeedUserAsync();
            var trackId = await SeedTrackAsync(owner, "Again");
            var ownerClient = fixture.CreateAuthenticatedClient(owner);
            var guestClient = fixture.CreateAuthenticatedClient(guest);
            var invitationId = await InviteAndGetInvitationIdAsync(owner, trackId, guest);

            var reject = await guestClient.PostAsync($"/coauthor-invitations/{invitationId}/reject", null, Ct);
            var reinvite = await ownerClient.PostAsJsonAsync($"/tracks/{trackId}/coauthors", new InviteCoauthorsRequest([guest]), Ct);
            var accept = await guestClient.PostAsync($"/coauthor-invitations/{invitationId}/accept", null, Ct);
            var withdraw = await guestClient.DeleteAsync($"/tracks/{trackId}/coauthors/{Id(guest)}", Ct);
            var track = await ReadJsonAsync(await fixture.CreateAnonymousClient().GetAsync($"/tracks/{trackId}", Ct));

            Assert.Equal(HttpStatusCode.NoContent, reject.StatusCode);
            Assert.Equal(HttpStatusCode.OK, reinvite.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, accept.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, withdraw.StatusCode);
            Assert.Equal(0, track.GetProperty("coauthors").GetArrayLength());
        }
    }
}
