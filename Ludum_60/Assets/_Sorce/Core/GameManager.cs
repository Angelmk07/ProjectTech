using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Хранит ресурсы, сохранение и переходы между уровнями.
/// Создавать на сцене не нужно: появится сам при первом обращении
/// (GameManager.Instance) и переживёт загрузку сцен.
/// </summary>
public class GameManager : MonoBehaviour
{
    static GameManager instance;
    static bool quitting;

    public static bool HasInstance => instance != null;

    public static GameManager Instance
    {
        get
        {
            if (instance == null && !quitting)
                new GameObject("GameManager").AddComponent<GameManager>();
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        quitting = false;
    }

    // Тип ресурса, новое значение
    public event Action<ResourceType, int> ResourceChanged;

    SaveData data = new SaveData();
    readonly Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();

    // Здоровье игрока, сохранённое при входе на уровень (-1 = полное).
    public int SavedPlayerHealth => data.playerHealth;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromDisk();
    }

    void OnApplicationQuit() => quitting = true;

    // ---------- Ресурсы ----------

    public int GetResource(ResourceType type)
    {
        return resources.TryGetValue(type, out int value) ? value : 0;
    }

    public void AddResource(ResourceType type, int amount)
    {
        resources[type] = Mathf.Max(0, GetResource(type) + amount);
        ResourceChanged?.Invoke(type, resources[type]);
    }

    public bool TrySpendResource(ResourceType type, int amount)
    {
        if (GetResource(type) < amount) return false;
        AddResource(type, -amount);
        return true;
    }

    // ---------- Сохранение ----------

    void LoadFromDisk()
    {
        data = SaveSystem.Load() ?? new SaveData();

        resources.Clear();
        foreach (var entry in data.resources)
            resources[entry.type] = entry.amount;

        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            ResourceChanged?.Invoke(type, GetResource(type));
    }

    void SaveToDisk()
    {
        data.resources.Clear();
        foreach (var pair in resources)
            data.resources.Add(new ResourceEntry { type = pair.Key, amount = pair.Value });

        SaveSystem.Save(data);
    }

    // ---------- Уровни ----------

    /// <summary>
    /// Вызывается при выходе с уровня: сохраняет прогресс и грузит следующую сцену.
    /// targetScene = -1 → следующая по порядку в Build Settings (после последней — первая).
    /// </summary>
    public void CompleteLevel(int playerHealth, int targetScene = -1)
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        int next = targetScene >= 0
            ? targetScene
            : (current + 1) % SceneManager.sceneCountInBuildSettings;

        data.levelBuildIndex = next;
        data.playerHealth = playerHealth;
        SaveToDisk();

        SceneManager.LoadScene(next);
    }

    /// <summary>
    /// Смерть игрока: откатываем ресурсы и здоровье к последнему сохранению
    /// (состоянию на входе в уровень) и перезагружаем сцену.
    /// </summary>
    public void RestartLevel()
    {
        LoadFromDisk();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Для главного меню
    public bool HasSave => SaveSystem.Exists;

    public void ContinueGame()
    {
        LoadFromDisk();
        SceneManager.LoadScene(data.levelBuildIndex);
    }

    public void NewGame(int firstScene = 0)
    {
        SaveSystem.Delete();
        LoadFromDisk();
        SceneManager.LoadScene(firstScene);
    }
}
