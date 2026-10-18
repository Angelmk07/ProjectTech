using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class StairZone : MonoBehaviour
{
    public enum ZoneType
    {
        Bottom,
        Landing,
        Top
    }

    [SerializeField] private StairController stair;
    [SerializeField] private ZoneType type;

    // сколько коллайдеров игрока сейчас внутри зоны
    private int insideCount;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnDisable()
    {
        insideCount = 0;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!stair.IsPlayer(other)) return;

        insideCount++;
        if (insideCount != 1) return; // реагируем один раз за заход

        switch (type)
        {
            case ZoneType.Bottom: stair.OnBottomTouched(other); break;
            case ZoneType.Landing: stair.OnLandingEntered(other); break;
            case ZoneType.Top: stair.OnTopTouched(other); break;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!stair.IsPlayer(other)) return;

        insideCount = Mathf.Max(0, insideCount - 1);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (type != ZoneType.Landing) return;
        if (!stair.IsPlayer(other)) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        bool up = kb.wKey.isPressed || kb.upArrowKey.isPressed;
        bool down = kb.sKey.isPressed || kb.downArrowKey.isPressed;

        if (up == down) return; // обе или ни одной: не трогаем

        stair.OnLandingChoice(other, up);
    }
}