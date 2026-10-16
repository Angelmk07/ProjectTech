using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static bool Exists => File.Exists(FilePath);

    public static void Save(SaveData data)
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"Не удалось сохранить игру: {e.Message}");
        }
    }

    // Возвращает null, если сохранения нет или файл повреждён.
    public static SaveData Load()
    {
        if (!Exists) return null;

        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"Не удалось загрузить сохранение: {e.Message}");
            return null;
        }
    }

    public static void Delete()
    {
        if (Exists) File.Delete(FilePath);
    }
}
