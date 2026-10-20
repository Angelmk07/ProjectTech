using UnityEngine;

/// <summary>
/// Повесить на камеру рядом с CameraFollow2D.
/// Для каждой комнаты задаются свои границы камеры. Нужная комната выбирается автоматически
/// по позиции игрока (цели камеры), поэтому после телепорта камера сразу работает по границам новой комнаты.
/// Действует только при включённом «Учитывать комнаты» — иначе CameraFollow2D использует обычные границы уровня.
/// </summary>
[DisallowMultipleComponent]
public class CameraRooms : MonoBehaviour
{
    [System.Serializable]
    public class Room
    {
        public string name = "Комната";

        [Tooltip("Границы комнаты в мировых координатах: X, Y — левый нижний угол, W, H — ширина и высота (положительные). " +
                 "Камера не покажет ничего за этими границами. Если комната меньше экрана, камера встанет по её центру.")]
        public Rect bounds = new Rect(-10f, -5f, 20f, 10f);
    }

    [Header("Учитывать комнаты")]
    [SerializeField] private bool useRooms = true;

    [Header("Комнаты (например: холл, комната 1, комната 2, комната 3)")]
    [Tooltip("Комнаты не должны перекрываться. Если игрок вне всех комнат, берётся ближайшая.")]
    [SerializeField] private Room[] rooms;

    [Header("Гизмо")]
    [Tooltip("Рисовать рамки комнат в Scene всегда (иначе — только когда камера выбрана)")]
    [SerializeField] private bool drawGizmosAlways = true;

    private int currentIndex = -1;

    /// <summary>Индекс комнаты, по границам которой камера работает сейчас (-1, если ещё не определена).</summary>
    public int CurrentIndex => currentIndex;

    /// <summary>
    /// Границы комнаты, в которой сейчас игрок. false — комнаты выключены или не заданы
    /// (тогда камера использует обычные границы уровня).
    /// </summary>
    public bool TryGetBounds(Vector2 playerPosition, out Rect bounds)
    {
        bounds = default;

        if (!useRooms || rooms == null || rooms.Length == 0) return false;

        currentIndex = FindRoom(playerPosition);
        bounds = rooms[currentIndex].bounds;
        return true;
    }

    private int FindRoom(Vector2 point)
    {
        int nearest = 0;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < rooms.Length; i++)
        {
            Rect r = rooms[i].bounds;
            if (r.Contains(point)) return i;

            float dx = Mathf.Max(r.xMin - point.x, 0f, point.x - r.xMax);
            float dy = Mathf.Max(r.yMin - point.y, 0f, point.y - r.yMax);
            float distance = dx * dx + dy * dy;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = i;
            }
        }

        return nearest;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (drawGizmosAlways) DrawRooms();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmosAlways) DrawRooms();
    }

    // Каждая комната своим цветом: полупрозрачная заливка, рамка и подпись «индекс: имя».
    // В игре текущая комната (по которой сейчас работает камера) — белая, подпись с пометкой.
    private void DrawRooms()
    {
        if (rooms == null) return;

        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i] == null) continue;

            Rect r = rooms[i].bounds;
            bool isCurrent = Application.isPlaying && useRooms && i == currentIndex;
            Color color = isCurrent ? Color.white : Color.HSVToRGB((i * 0.19f) % 1f, 0.75f, 1f);

            if (!useRooms) color.a = 0.35f;

            Gizmos.color = new Color(color.r, color.g, color.b, isCurrent ? 0.12f : 0.06f);
            Gizmos.DrawCube(r.center, r.size);

            Gizmos.color = color;
            Gizmos.DrawWireCube(r.center, r.size);

            string label = $"{i}: {rooms[i].name}" + (isCurrent ? "  ◄ текущая" : "");
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.Label(new Vector3(r.xMin, r.yMax, 0f), label);
        }
    }
#endif
}