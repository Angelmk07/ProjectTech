using System;
using System.Collections.Generic;

// Добавляйте новые ресурсы сюда (порядок не меняйте у уже существующих,
// иначе старые сохранения сломаются — добавляйте только в конец).
public enum ResourceType
{
    Coin,
    Gem,
    Key
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
    public int levelBuildIndex = 0;   // уровень, с которого продолжать
    public int playerHealth = -1;     // -1 = полное здоровье
    public List<ResourceEntry> resources = new List<ResourceEntry>();
}
