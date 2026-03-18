using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Contracts.Application;

namespace Musify.Application.Pictures.Commands.ResizePicture
{
    public class ResizeCommandHandler(IPictureService pictureService)
        : IRequestHandler<ResizeCommand, Task<Result<Guid> >>
    {
        public async Task<Result<Guid>> Handle(ResizeCommand request, CancellationToken cancellationToken)
        {
            var result = await pictureService.ResizePictureAsync(request.KeyName, request.ContentType, request.PictureStream, request.PictureResizes, cancellationToken);

            return result;
        }
    }
}