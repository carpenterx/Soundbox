using System;

[Serializable]
public class ExternalSound
{
    public string Name;
    public int TilePosition;
    //public int HorizontalSpan;
    //public int VerticalSpan;
    public string SoundPath;

    public ExternalSound(string name, int tilePosition, string soundPath)
    {
        Name = name;
        TilePosition = tilePosition;
        SoundPath = soundPath;
    }
}
