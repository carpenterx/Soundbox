using System;
using System.Collections.Generic;

[Serializable]
public class SoundList
{
    public List<ExternalSound> Sounds;

    public SoundList()
    {
        Sounds = new List<ExternalSound>();
    }
}
