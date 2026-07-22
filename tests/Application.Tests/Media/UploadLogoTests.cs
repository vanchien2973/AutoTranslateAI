using System.Text;
using Application.Features.Media.UploadLogo;
using Application.Interfaces;

namespace Application.Tests.Media;

public class UploadLogoTests
{
    private static UploadLogoCommand Command(
        string contentType = "image/png",
        long length = 1024) =>
        new(new MemoryStream(Encoding.UTF8.GetBytes("png-bytes")), "logo.png", contentType, length);

    [Fact]
    public async Task Given_Image_When_Handle_Then_UploadsUnderLogosPrefix()
    {
        var storage = Substitute.For<IStorageService>();
        var handler = new UploadLogoCommandHandler(storage);

        var response = await handler.Handle(Command(), CancellationToken.None);

        response.StorageKey.Should().StartWith("logos/").And.EndWith(".png");
        await storage.Received(1).UploadAsync(
            Arg.Any<Stream>(), response.StorageKey, "image/png", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/webp", ".webp")]
    [InlineData("image/png", ".png")]
    public async Task Given_ContentType_When_Handle_Then_KeyUsesMatchingExtension(string type, string extension)
    {
        var handler = new UploadLogoCommandHandler(Substitute.For<IStorageService>());

        var response = await handler.Handle(Command(type), CancellationToken.None);

        response.StorageKey.Should().EndWith(extension);
    }

    [Fact]
    public async Task Given_TwoUploads_When_Handle_Then_KeysDiffer()
    {
        var handler = new UploadLogoCommandHandler(Substitute.For<IStorageService>());

        var first = await handler.Handle(Command(), CancellationToken.None);
        var second = await handler.Handle(Command(), CancellationToken.None);

        second.StorageKey.Should().NotBe(first.StorageKey);
    }

    [Theory]
    [InlineData("image/png", 1024, true)]
    [InlineData("image/gif", 1024, false)] // unsupported by the ffmpeg overlay path
    [InlineData("text/plain", 1024, false)]
    [InlineData("image/png", 0, false)] // empty file
    [InlineData("image/png", 3 * 1024 * 1024, false)] // over the 2 MB cap
    public void Given_File_When_Validate_Then_MatchesExpectation(string type, long length, bool expected)
    {
        var result = new UploadLogoCommandValidator().Validate(Command(type, length));

        result.IsValid.Should().Be(expected);
    }
}
