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

    public StairState State { get; private set; } = StairState.Descending;

    private void Awake()
    {
        upperWalk.enabled = false;
    }

    public bool IsPlayer(Collider2D other)
    {
        return (playerLayer.value & (1 << other.gameObject.layer)) != 0;
    }

    // Нижняя зона: приоритетная, всегда включает режим подъёма
    public void OnBottomTouched()
    {
        State = StairState.Ascending;
        upperWalk.enabled = false;
    }

    // Вход в зону площадки без нажатой клавиши: решает состояние
    public void OnLandingEntered()
    {
        if (State == StairState.Ascending)
        {
            upperWalk.enabled = true;
            State = StairState.Descending; // сброс на дефолт
        }
        else
        {
            upperWalk.enabled = false;
        }
    }

    // Игрок на площадке явно выбрал направление клавишей
    public void OnLandingChoice(bool wantUp)
    {
        upperWalk.enabled = wantUp;
        State = StairState.Descending;
    }

    // Верхняя зона: игнорируется, пока игрок поднимается снизу
    public void OnTopTouched()
    {
        if (State == StairState.Ascending) return;

        upperWalk.enabled = true;
    }
}