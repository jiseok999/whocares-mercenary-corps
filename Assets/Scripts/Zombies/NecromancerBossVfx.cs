using UnityEngine;

/// <summary>네크로맨서 패턴2: 아군 유닛을 향해 날아가 맞으면 헥스(초록 + 행동 정지)</summary>
public class NecromancerHexOrb : MonoBehaviour
{
    public float speed = 14f;
    public float hitRadius = 0.35f;
    public float hexDuration = 2.5f;
    public Character target;

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused()) return;
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 tp = target.transform.position;
        transform.position = Vector3.MoveTowards(transform.position, tp, speed * Time.deltaTime);
        if (Vector2.Distance(transform.position, tp) <= hitRadius)
        {
            if (target != null && target.gameObject != null)
            {
                target.ApplyNecromancerHex(hexDuration);
            }
            Destroy(gameObject);
        }
    }
}

/// <summary>네크로맨서 패턴3: 벽(아군 방어막)에 2 데미지</summary>
public class NecromancerBarrierSlash : MonoBehaviour
{
    public float speed = 18f;
    public int damage = 2;

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused()) return;
        Barrier b = Barrier.Instance;
        if (b == null || !b.IsAlive())
        {
            Destroy(gameObject);
            return;
        }

        Bounds bb = b.GetWorldBounds();
        float tx = bb.min.x;
        transform.position += Vector3.left * (speed * Time.deltaTime);
        if (transform.position.x <= tx + 0.08f)
        {
            b.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
