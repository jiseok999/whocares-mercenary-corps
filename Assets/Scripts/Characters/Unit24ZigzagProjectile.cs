using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 24 전용 지그재그 관통 투사체
/// </summary>
public class Unit24ZigzagProjectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 3;
    public int sourceUnitNumber;
    public float zigzagSpeed = 0.33f;
    public float zigzagMinY = -8f; // Y -800 (월드 좌표 기준)
    public float zigzagMaxY = -2f; // Y -200 (월드 좌표 기준)
    public float initialLaunchAngle = 60f;
    public float forwardDirection = 1f;

    private float lifeTime = 8f;
    private float expireTime;
    private readonly HashSet<Zombie> hitTargets = new HashSet<Zombie>();
    private CircleCollider2D hitCollider;
    private float verticalDirection = -1f;
    private bool usedInitialLaunch = false;
    private float initialVerticalSpeed = 0f;
    private Vector2 initialDirection = Vector2.right;

    void Start()
    {
        Unit1ProjectileAfterimage.Attach(gameObject);
        expireTime = Time.time + lifeTime;
        float radians = initialLaunchAngle * Mathf.Deg2Rad;
        initialDirection = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
        if (initialDirection.sqrMagnitude <= 0.001f)
        {
            initialDirection = Vector2.right;
        }
        verticalDirection = initialDirection.y >= 0f ? 1f : -1f;
        initialVerticalSpeed = speed * Mathf.Tan(Mathf.Abs(initialLaunchAngle) * Mathf.Deg2Rad);

        BoardManager boardManager = FindFirstObjectByType<BoardManager>();
        if (boardManager != null && boardManager.TryGetLaneBounds(out float laneMinY, out float laneMaxY))
        {
            zigzagMinY = laneMinY;
            zigzagMaxY = laneMaxY;
        }

        hitCollider = gameObject.AddComponent<CircleCollider2D>();
        hitCollider.isTrigger = true;
        hitCollider.radius = Mathf.Max(transform.localScale.x * 0.5f, 0.6f);

        Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused())
        {
            return;
        }

        if (Time.time >= expireTime)
        {
            Destroy(gameObject);
            return;
        }

        if (!usedInitialLaunch)
        {
            transform.position += (Vector3)(initialDirection * speed * Time.deltaTime);
            usedInitialLaunch = true;
            return;
        }

        float xMove = speed * Time.deltaTime;
        float ySpeed = zigzagSpeed;
        transform.position += new Vector3(xMove, ySpeed * verticalDirection * Time.deltaTime, 0f);

        Vector3 pos = transform.position;
        if (pos.y <= zigzagMinY)
        {
            pos.y = zigzagMinY;
            verticalDirection = 1f;
        }
        else if (pos.y >= zigzagMaxY)
        {
            pos.y = zigzagMaxY;
            verticalDirection = -1f;
        }
        transform.position = pos;

        Camera cam = Camera.main;
        if (cam != null)
        {
            float cameraRight = cam.transform.position.x + (cam.orthographicSize * (Screen.width / (float)Screen.height));
            if (transform.position.x > cameraRight + 5f)
            {
                Destroy(gameObject);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Zombie zombie = other.GetComponent<Zombie>();
        if (zombie == null) return;
        if (hitTargets.Contains(zombie)) return;

        hitTargets.Add(zombie);
        zombie.TakeDamage(damage, sourceUnitNumber);
        AttackHitEffectFire.Spawn(zombie.transform.position, Vector3.one * 2f);
    }

    public void SetLaunchAngle(float angle)
    {
        initialLaunchAngle = angle;
        forwardDirection = 1f;
    }
}
