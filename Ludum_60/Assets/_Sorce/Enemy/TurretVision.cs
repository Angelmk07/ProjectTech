using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(DamageDealer))]
public class TurretVision : MonoBehaviour
{
    [Header("Поле обзора")]
    [SerializeField] private float viewRadius = 6f;
    [SerializeField, Range(1f, 360f)] private float viewAngle = 60f;
    [SerializeField, Range(4, 200)] private int rayCount = 40;
    [Tooltip("Смещение точки, из которой «смотрит» турель (локально)")]
    [SerializeField] private Vector2 originOffset = Vector2.zero;

    [Header("Слои")]
    [Tooltip("Что блокирует обзор (стены, земля)")]
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("Кого турель должна замечать (слой игрока)")]
    [SerializeField] private LayerMask targetMask;

    [Header("Урон")]
    [Tooltip("Как часто наносится урон, пока игрок виден (сек)")]
    [SerializeField] private float damageInterval = 0.5f;

    [Header("Визуал")]
    [SerializeField] private Color idleColor = new Color(1f, 0.9f, 0.1f, 0.25f);
    [SerializeField] private Color alertColor = new Color(1f, 0.1f, 0.1f, 0.4f);
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 5;

    public bool TargetVisible { get; private set; }
    public Transform Target { get; private set; }

    private Mesh mesh;
    private DamageDealer damageDealer;
    private MeshRenderer meshRenderer;
    private Material material;
    private Vector3[] vertices;
    private int[] triangles;
    private float nextDamageTime;

    private void Awake()
    {
        var meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        damageDealer = GetComponent<DamageDealer>();

        mesh = new Mesh { name = "TurretFOV" };
        meshFilter.mesh = mesh;
        material = new Material(Shader.Find("Sprites/Default"));
        meshRenderer.material = material;
        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        BuildBuffers();
    }

    private void OnValidate()
    {
        if (Application.isPlaying && mesh != null) BuildBuffers();
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (mesh != null) Destroy(mesh);
    }

    private void BuildBuffers()
    {
        vertices = new Vector3[rayCount + 2];   // центр + (rayCount + 1) точек по дуге
        triangles = new int[rayCount * 3];
    }

    private void LateUpdate()
    {
        UpdateVision();
        material.color = TargetVisible ? alertColor : idleColor;

        if (TargetVisible && Time.time >= nextDamageTime)
        {
            nextDamageTime = Time.time + damageInterval;
            damageDealer.DealDamage(Target);
        }
    }

    private void UpdateVision()
    {
        Vector2 origin = transform.TransformPoint(originOffset);
        float baseAngle = transform.eulerAngles.z;
        float startAngle = baseAngle - viewAngle * 0.5f;
        float step = viewAngle / rayCount;
        vertices[0] = transform.InverseTransformPoint(origin);

        int detectMask = obstacleMask | targetMask;
        Transform foundTarget = null;

        for (int i = 0; i <= rayCount; i++)
        {
            float angle = (startAngle + step * i) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            RaycastHit2D wallHit = Physics2D.Raycast(origin, dir, viewRadius, obstacleMask);
            Vector2 endPoint = wallHit.collider != null ? wallHit.point : origin + dir * viewRadius;
            vertices[i + 1] = transform.InverseTransformPoint(endPoint);

            if (foundTarget == null)
            {
                RaycastHit2D hit = Physics2D.Raycast(origin, dir, viewRadius, detectMask);
                if (hit.collider != null && ((1 << hit.collider.gameObject.layer) & targetMask) != 0)
                {
                    // Скрытого игрока не замечаем
                    var hideable = hit.collider.GetComponentInParent<IHideable>();
                    if (hideable == null || !hideable.IsHidden)
                    {
                        foundTarget = hit.collider.transform;
                    }
                }
            }
        }

        Target = foundTarget;
        TargetVisible = foundTarget != null;

        for (int i = 0; i < rayCount; i++)
        {
            int t = i * 3;
            triangles[t] = 0;
            triangles[t + 1] = i + 2;
            triangles[t + 2] = i + 1;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.TransformPoint(originOffset);
        float baseAngle = transform.eulerAngles.z;

        Gizmos.color = Color.yellow;
        for (int s = -1; s <= 1; s += 2)
        {
            float a = (baseAngle + s * viewAngle * 0.5f) * Mathf.Deg2Rad;
            Gizmos.DrawLine(origin, origin + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * viewRadius);
        }
    }
}