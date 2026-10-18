using System;
using System.Collections.Generic;

public enum ResourceType
{
    LightA,
    LightB,
    LightC,
    LightD,
    LightE,
    LightF,
    LightG,
}

[Serializable]
public class ResourceEntry
{
    public ResourceType type;
    public int amount;
}

[Serializable]
public class SaveData
{
    public int levelBuildIndex = 0;
    public int playerHealth = -1; 
    public List<ResourceEntry> resources = new List<ResourceEntry>();
}
