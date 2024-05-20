namespace Client.Models.DecisionElements;

public class MediaData
{
    private string _extension = "";
    public byte[]? Data { get; set; }

    public string Extension
    {
        get => _extension;
        set => _extension = value[0] != '.' ? '.' + value : value;
    }

    public bool Present()
    {
        return Data is not null;
    }
}