using FluentValidation.TestHelper;
using WebApi.DataTransferObjects.PlayLists;
using WebApi.DataTransferObjects.Tracks;
using WebApi.DataTransferObjects.Users;
using WebApi.Validators.PlayLists;
using WebApi.Validators.Tracks;
using WebApi.Validators.Users;
using Xunit;

namespace Musify.WebApi.Tests.Validators;

public sealed class CreateTrackRequestValidatorTests
{
    private readonly CreateTrackRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var model = new CreateTrackRequest("Song", Guid.NewGuid(), Guid.NewGuid());
        _validator.TestValidate(model).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Empty_title_fails(string? title)
    {
        var model = new CreateTrackRequest(title!, Guid.NewGuid(), Guid.NewGuid());
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_longer_than_50_fails()
    {
        var model = new CreateTrackRequest(new string('a', 51), Guid.NewGuid(), Guid.NewGuid());
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Empty_intent_ids_fail()
    {
        var model = new CreateTrackRequest("Song", Guid.Empty, Guid.Empty);
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.PictureIntentId);
        result.ShouldHaveValidationErrorFor(x => x.AudioIntentId);
    }
}

public sealed class RequestTrackUploadUrlsRequestValidatorTests
{
    private readonly RequestTrackUploadUrlsRequestValidator _validator = new();

    private static RequestTrackUploadUrlsRequest Valid() =>
        new("png", "image/png", "mp3", "audio/mpeg");

    [Fact]
    public void Valid_request_passes()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Missing_required_fields_fail()
    {
        var model = new RequestTrackUploadUrlsRequest("", "", "", "");
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.PictureFileType);
        result.ShouldHaveValidationErrorFor(x => x.PictureContentType);
        result.ShouldHaveValidationErrorFor(x => x.AudioFileType);
        result.ShouldHaveValidationErrorFor(x => x.AudioContentType);
    }

    [Fact]
    public void Non_positive_expected_sizes_fail_when_provided()
    {
        var model = Valid() with { ExpectedPictureSizeBytes = 0, ExpectedAudioSizeBytes = -1 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ExpectedPictureSizeBytes);
        result.ShouldHaveValidationErrorFor(x => x.ExpectedAudioSizeBytes);
    }

    [Fact]
    public void Null_expected_sizes_are_allowed()
    {
        var model = Valid() with { ExpectedPictureSizeBytes = null, ExpectedAudioSizeBytes = null };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.ExpectedPictureSizeBytes);
        result.ShouldNotHaveValidationErrorFor(x => x.ExpectedAudioSizeBytes);
    }
}

public sealed class CreatePlayListRequestValidatorTests
{
    private readonly CreatePlayListRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var model = new CreatePlayListRequest("My List", "A description", null);
        _validator.TestValidate(model).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_name_and_description_fail()
    {
        var model = new CreatePlayListRequest("", "", null);
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Over_length_name_and_description_fail()
    {
        var model = new CreatePlayListRequest(new string('n', 51), new string('d', 257), null);
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}

public sealed class UpdatePlayListRequestValidatorTests
{
    private readonly UpdatePlayListRequestValidator _validator = new();

    [Fact]
    public void All_null_is_valid()
    {
        _validator.TestValidate(new UpdatePlayListRequest(null, null, null))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Over_length_values_fail_only_when_provided()
    {
        var model = new UpdatePlayListRequest(new string('n', 51), new string('d', 257), null);
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.NewName);
        result.ShouldHaveValidationErrorFor(x => x.NewDescription);
    }

    [Fact]
    public void Empty_string_name_is_allowed_by_length_rule()
    {
        var model = new UpdatePlayListRequest("", null, null);
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.NewName);
    }
}

public sealed class RequestPlayListPictureUploadRequestValidatorTests
{
    private readonly RequestPlayListPictureUploadRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        _validator.TestValidate(new RequestPlayListPictureUploadRequest("webp", "image/webp"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Missing_filetype_and_contenttype_fail()
    {
        var result = _validator.TestValidate(new RequestPlayListPictureUploadRequest("", ""));
        result.ShouldHaveValidationErrorFor(x => x.FileType);
        result.ShouldHaveValidationErrorFor(x => x.ContentType);
    }

    [Fact]
    public void Non_positive_size_fails_when_provided()
    {
        var result = _validator.TestValidate(new RequestPlayListPictureUploadRequest("webp", "image/webp", 0));
        result.ShouldHaveValidationErrorFor(x => x.ExpectedSizeBytes);
    }
}

public sealed class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var model = new CreateUserRequest(Guid.NewGuid(), "Alice", "Al", "Ice");
        _validator.TestValidate(model).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_id_and_name_fail()
    {
        var model = new CreateUserRequest(Guid.Empty, "", null, null);
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Id);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Over_length_optional_names_fail_only_when_provided()
    {
        var model = new CreateUserRequest(Guid.NewGuid(), "Alice", new string('f', 49), new string('s', 49));
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
        result.ShouldHaveValidationErrorFor(x => x.SecondName);
    }

    [Fact]
    public void Null_optional_names_are_allowed()
    {
        var model = new CreateUserRequest(Guid.NewGuid(), "Alice", null, null);
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
        result.ShouldNotHaveValidationErrorFor(x => x.SecondName);
    }
}
