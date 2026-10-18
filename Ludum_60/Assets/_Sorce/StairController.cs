using UnityEngine;

public class StairController : MonoBehaviour
{
    public enum StairState
    {
        Descending, // по умолчанию
        Ascending
    }

    [Header("Коллайдер ходьбы верхнего пролёта (EdgeCollider2D)")]
    [SerializeField] private Collider2D upperWalk;

    [Header("Слой игрока")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Sorting Order игрока")]
    [SerializeField] private int orderBehindUpper = 2;
    [SerializeField] private int orderFrontOfUpper = 8;

    [Header("Через сколько секунд режим подъёма сбрасывается сам")]
    [SerializeField] private float ascendTimeout = 6f;

    private StairState state = StairState.Descending;
    private float ascendingSince;

    // Состояние с учётом тайм-аута
    private bool IsAscending
    {
        get
        {
            if (state == StairState.Ascending && Time.time - ascendingSince > ascendTimeout)
                state = StairState.Descending;
            return state == StairState.Ascending;
        }
    }

    private void Awake()
    {
        upperWalk.enabled = false;
    }

    public bool IsPlayer(Collider2D other)
    {
        return (playerLayer.value & (1 << other.gameObject.layer)) != 0;
    }

    private void SetOrder(Collider2D player, int order)
    {
        var sprite = player.GetComponentInChildren<SpriteRenderer>();
        if (sprite != null)
            sprite.sortingOrder = order;
    }

    // Низ: приоритетная зона
    public void OnBottomTouched(Collider2D player)
    {
        state = StairState.Ascending;
        ascendingSince = Time.time;
        upperWalk.enabled = false;
        SetOrder(player, orderFrontOfUpper);
    }

    // Центр без нажатой клавиши: решает намерение
    public void OnLandingEntered(Collider2D player)
    {
        if (IsAscending)
        {
            // шёл снизу: взбирается
            upperWalk.enabled = true;
            state = StairState.Descending;
            SetOrder(player, orderBehindUpper);
        }
        else
        {
            // пришёл сверху (или без намерения): спускается
            upperWalk.enabled = false;
            SetOrder(player, orderFrontOfUpper);
        }
    }

    // Центр с нажатой клавишей: игрок передумал
    public void OnLandingChoice(Collider2D player, bool wantUp)
    {
        upperWalk.enabled = wantUp;
        state = StairState.Descending;
        SetOrder(player, wantUp ? orderBehindUpper : orderFrontOfUpper);
    }

    // Верх: игнорируется, пока игрок явно поднимается снизу
    public void OnTopTouched(Collider2D player)
    {
        if (IsAscending) return;

        upperWalk.enabled = true;
        SetOrder(player, orderBehindUpper);
    }
}