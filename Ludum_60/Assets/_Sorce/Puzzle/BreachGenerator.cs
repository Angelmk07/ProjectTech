using System.Collections.Generic;
using UnityEngine;

public enum BreachMoveRule
{
    CyberpunkAlternating,
    SameRowOrColumn
}

public class BreachData
{
    public int size;
    public string[,] grid;
    public string[] key; 
    public Vector2Int[] solutionPath;
}

public static class BreachGenerator
{
    public static bool IsLegalStep(BreachMoveRule rule, int step, Vector2Int last, Vector2Int candidate)
    {
        if (step == 0)
            return rule == BreachMoveRule.CyberpunkAlternating ? candidate.y == 0 : true;

        if (rule == BreachMoveRule.CyberpunkAlternating)
            return step % 2 == 1 ? candidate.x == last.x : candidate.y == last.y;

        return candidate.x == last.x || candidate.y == last.y;
    }

    public static BreachData Generate(int size, int keyLength, IList<string> pool, BreachMoveRule rule)
    {
        size = Mathf.Max(2, size);
        int requested = Mathf.Clamp(keyLength, 2, size * size);
        int len = requested;
        Vector2Int[] path = null;

        while (len >= 2 && path == null)
        {
            for (int attempt = 0; attempt < 30 && path == null; attempt++)
                path = TryBuildPath(size, len, rule);
            if (path == null) len--;
        }

        if (path == null)
        {
            len = 2;
            path = TryBuildPath(size, 2, rule);
        }

        if (len < requested)
            Debug.LogWarning($"[Breach] Ключ длиной {requested} не поместился в поле, использована длина {len}.");

        var key = new string[len];
        for (int i = 0; i < len; i++)
            key[i] = pool[Random.Range(0, pool.Count)];


        var grid = new string[size, size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                grid[y, x] = pool[Random.Range(0, pool.Count)];

        for (int i = 0; i < len; i++)
            grid[path[i].y, path[i].x] = key[i];

        return new BreachData { size = size, grid = grid, key = key, solutionPath = path };
    }

    static Vector2Int[] TryBuildPath(int size, int len, BreachMoveRule rule)
    {
        var path = new Vector2Int[len];
        var used = new bool[size, size];
        int budget = 20000;

        return Extend(0) ? path : null;

        bool Extend(int step)
        {
            if (step == len) return true;
            if (--budget < 0) return false;

            var candidates = new List<Vector2Int>();
            Vector2Int last = step > 0 ? path[step - 1] : default;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (used[y, x]) continue;
                    var c = new Vector2Int(x, y);
                    if (IsLegalStep(rule, step, last, c)) candidates.Add(c);
                }
            }

            Shuffle(candidates);

            foreach (var c in candidates)
            {
                used[c.y, c.x] = true;
                path[step] = c;
                if (Extend(step + 1)) return true;
                used[c.y, c.x] = false;
            }
            return false;
        }
    }

    static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
