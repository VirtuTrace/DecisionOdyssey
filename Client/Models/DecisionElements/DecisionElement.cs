using System.IO.Compression;
using System.Text;
using Client.Models.DecisionElements.DecisionMatrix;

namespace Client.Models.DecisionElements;

public abstract class DecisionElement
{
    public string Name { get; set; } = "";
    public Guid Guid { get; set; } = Guid.NewGuid();
    public DateTime CreationTime { get; set; } = DateTime.Now;
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    public virtual void Reset()
    {
        Name = "";
        Guid = Guid.NewGuid();
        CreationTime = DateTime.Now;
        LastUpdated = DateTime.Now;
    }
    
    public abstract (string, byte[]) Archive();
    
    protected static void ArchiveData(ZipArchive archive, MatrixCell cell, string header)
    {
        ArchiveMedia(cell.Image, archive, header, "image");
        ArchiveMedia(cell.Audio, archive, header, "audio");
        ArchiveMedia(cell.Video, archive, header, "video");
        ArchiveText(cell.Text, archive, header);
    }

    private static void ArchiveMedia(MediaData media, ZipArchive archive, string header, string mediaType)
    {
        if (!media.Present())
        {
            return;
        }
        
        var zipEntry = archive.CreateEntry($"{header}_{mediaType}{media.Extension}");
        var data = media.Data!;
        using var entryStream = zipEntry.Open();
        using var streamWriter = new BinaryWriter(entryStream);
        streamWriter.Write(data);
    }
    
    private static void ArchiveText(string text, ZipArchive archive, string header)
    {
        if (text == "")
        {
            return;
        }

        var zipEntry = archive.CreateEntry($"{header}_text.txt");
        var data = Encoding.UTF8.GetBytes(text);
        using var entryStream = zipEntry.Open();
        using var streamWriter = new BinaryWriter(entryStream);
        streamWriter.Write(data);
    }
}