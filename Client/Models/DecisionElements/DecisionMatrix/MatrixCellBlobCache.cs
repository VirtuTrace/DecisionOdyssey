using Client.JSWrappers;

namespace Client.Models.DecisionElements.DecisionMatrix;

public class MatrixCellBlobCache
{
    public string? ImageBlobUrl { get; set; }
    public string? AudioBlobUrl { get; set; }
    public string? VideoBlobUrl { get; set; }

    public async Task RevokeUrl(BlobCreator blobCreator, MatrixDataType type)
    {
        switch (type)
        {
            case MatrixDataType.None:
                break;
            case MatrixDataType.Image:
                if (ImageBlobUrl is not null)
                {
                    await blobCreator.RevokeBlobUrl(ImageBlobUrl);
                    ImageBlobUrl = null;
                }
                break;
            case MatrixDataType.Audio:
                if (AudioBlobUrl is not null)
                {
                    await blobCreator.RevokeBlobUrl(AudioBlobUrl);
                    AudioBlobUrl = null;
                }
                break;
            case MatrixDataType.Video:
                if (VideoBlobUrl is not null)
                {
                    await blobCreator.RevokeBlobUrl(VideoBlobUrl);
                    VideoBlobUrl = null;
                }
                break;
            case MatrixDataType.Text:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
    
    public async Task RevokeUrls(BlobCreator blobCreator)
    {
        if (ImageBlobUrl is not null)
        {
            await blobCreator.RevokeBlobUrl(ImageBlobUrl);
        }
        if (AudioBlobUrl is not null)
        {
            await blobCreator.RevokeBlobUrl(AudioBlobUrl);
        }
        if (VideoBlobUrl is not null)
        {
            await blobCreator.RevokeBlobUrl(VideoBlobUrl);
        }
    }
}