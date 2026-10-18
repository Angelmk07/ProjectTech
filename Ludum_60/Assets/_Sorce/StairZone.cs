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

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!stair.IsPlayer(other)) return;

        switch (type)
        {
            case ZoneType.Bottom: stair.OnBottomTouched(); break;
            case ZoneType.Landing: stair.OnLandingEntered(); break;
            case ZoneType.Top: stair.OnTopTouched(); break;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (type != ZoneType.Landing) return;
        if (!stair.IsPlayer(other)) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        bool up = kb.wKey.isPressed || kb.upArrowKey.isPressed;
        bool down = kb.sKey.isPressed || kb.downArrowKey.isPressed;

        if (up == down) return;

        stair.OnLandingChoice(up);
    }
}