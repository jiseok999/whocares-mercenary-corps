using UnityEngine;

/// <summary>
/// unit_011_attack: 0~7 프레임을 순서대로 재생한 뒤 마지막(7)에 고정한다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Unit11AttackFreezeVfx : MonoBehaviour
{
    [SerializeField] private float secondsPerFrame = 0.06f;

    private SpriteRenderer mSr;
    private float mLocalMul;
    private Vector3 mLocalFeet;
    private Sprite[] mFrames;
    private int mLastIndex = -1;
    private int mMaxIndex;
    private float mElapsed;
    private bool mStarted;

    public void Init(float localMul, Vector3 localFeet, Sprite[] sortedUpTo8)
    {
        mLocalMul = localMul;
        mLocalFeet = localFeet;
        mFrames = sortedUpTo8;
        mSr = GetComponent<SpriteRenderer>();
        if (mFrames == null || mFrames.Length == 0)
        {
            mMaxIndex = 0;
        }
        else
        {
            mMaxIndex = mFrames.Length - 1;
        }
        mStarted = true;
        mElapsed = 0f;
        mLastIndex = 0;
        if (mSr != null && mFrames != null && mFrames.Length > 0 && mFrames[0] != null)
        {
            mSr.sprite = mFrames[0];
            RepositionToFeet(mFrames[0]);
        }
    }

    void Update()
    {
        if (!mStarted || mSr == null || mFrames == null || mFrames.Length == 0)
        {
            return;
        }

        mElapsed += Time.deltaTime;
        float ft = Mathf.Max(1e-4f, secondsPerFrame);
        // mElapsed/ft: 0→0, 1→1, … , 이후 mMaxIndex에서 고정
        int idx = mFrames.Length < 2
            ? 0
            : Mathf.Min(mMaxIndex, Mathf.FloorToInt(mElapsed / ft));

        if (idx != mLastIndex)
        {
            mLastIndex = idx;
            Sprite sp = mFrames[idx];
            if (sp != null)
            {
                mSr.sprite = sp;
                RepositionToFeet(sp);
            }
        }
    }

    void RepositionToFeet(Sprite sp)
    {
        if (sp == null)
        {
            return;
        }
        transform.localPosition = new Vector3(
            -mLocalMul * sp.bounds.center.x + mLocalFeet.x,
            mLocalFeet.y - mLocalMul * sp.bounds.min.y,
            0f);
    }
}
