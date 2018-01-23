using System;
using System.Collections.Generic;

[Serializable]
public class CampusJson
{
    public string BuildingsJson;
    public SerializableDictionaryIntString Floors;
    public SerializableDictionaryIntString Rooms;
}
