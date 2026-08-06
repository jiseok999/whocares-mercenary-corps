using UnityEngine;

/// <summary>
/// 에너미2(mon_pig_2) 공격 시 좌측(−X) 직선으로 날아가며,
/// 같은 줄 앞쪽 적에 맞으면 해당 적을 1칸 전진(벽 방향)시키고 소멸합니다.
/// </summary>
public class Enemy2BallProjectile : MonoBehaviour
{
    static readonly Color BoostMessageColor = new Color(1f, 0.62f, 0.18f, 1f);

    public float speed = 16f;
    public float hitRadius = 0.38f;

    private Zombie shooter;
    private int shooterRow;
    private float shooterLaunchX;
    private float cellSize;
    private bool done;

    public void Init(Zombie owner, int row, float shooterWorldX, float laneCellSize)
    {
        shooter = owner;
        shooterRow = row;
        shooterLaunchX = shooterWorldX;
        cellSize = laneCellSize > 0.01f ? laneCellSize : 1f;
    }

    void Update()
    {
        if (done) return;
        if (GameManager.Instance != null && GameManager.Instance.IsCombatPaused()) return;

        transform.position += (Vector3)(Vector2.left * speed * Time.deltaTime);

        if (TryHitZombieAhead())
        {
            return;
        }

        if (transform.position.x < shooterLaunchX - cellSize * 24f)
        {
            Finish();
        }
    }

    bool TryHitZombieAhead()
    {
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        Zombie hitTarget = null;
        float bestX = float.MinValue;
        float laneTol = Mathf.Max(0.22f, cellSize * 0.55f);
        Vector3 projectilePos = transform.position;

        for (int i = 0; i < zombies.Length; i++)
        {
            Zombie zombie = zombies[i];
            if (zombie == null || zombie.IsDead || zombie == shooter) continue;
            if (zombie.currentRow != shooterRow) continue;
            if (zombie.transform.position.x >= shooterLaunchX - 0.05f) continue;
            if (Mathf.Abs(zombie.transform.position.y - projectilePos.y) > laneTol) continue;
            if (Mathf.Abs(projectilePos.x - zombie.transform.position.x) > hitRadius) continue;

            float zx = zombie.transform.position.x;
            if (zx > bestX)
            {
                bestX = zx;
                hitTarget = zombie;
            }
        }

        if (hitTarget == null)
        {
            return false;
        }

        hitTarget.ApplyEnemy2BoostForward(cellSize);
        hitTarget.ShowEnemy2BoostMessage(BoostMessageColor);
        Finish();
        return true;
    }

    void Finish()
    {
        done = true;
        Destroy(gameObject);
    }
}
