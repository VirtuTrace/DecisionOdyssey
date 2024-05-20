namespace Client.Models.DecisionElements.DecisionMatrix;

public class MatrixCell
{
    public MediaData Image { get; set; } = new();
    public MediaData Audio { get; set; } = new();
    public MediaData Video { get; set; } = new();
    public string Text { get; set; } = "";
    
    public bool IsEmpty()
    {
        return !Image.Present() && !Audio.Present() && !Video.Present() && Text == "";
    }
    
    public bool Contains(MatrixDataType type)
    {
        var result = true;
        if((type & MatrixDataType.Image) == MatrixDataType.Image)
        {
            result &= Image.Present();
        }
        if((type & MatrixDataType.Audio) == MatrixDataType.Audio)
        {
            result &= Audio.Present();
        }
        if((type & MatrixDataType.Video) == MatrixDataType.Video)
        {
            result &= Video.Present();
        }
        if((type & MatrixDataType.Text) == MatrixDataType.Text)
        {
            result &= Text != "";
        }
        return result;
    }
}