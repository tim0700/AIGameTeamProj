using Unity.MLAgents.Actuators;
using UnityEngine;

/// <summary>
/// ● 공격형 PPO 학습 에이전트
///   – 공격 성공 / 접근 보상  ↑
///   – 피격 / 후퇴 페널티    ↓
/// </summary>
public class RL_AttackAgent : RLAgentBase
{
    /* ─────────── 인스펙터 가중치 ─────────── */
    [Header("보상 가중치")]
    [Tooltip("적 HP 1 줄일 때 가산")]
    public float damageReward = 0.08f;

    [Tooltip("내 HP 1 잃을 때 감산")]
    public float getHitPenalty = -0.06f;

    [Tooltip("거리 1m 가까워질 때 가산")]
    public float approachReward = 0.01f;

    [Tooltip("거리 1m 멀어질 때 감산")]
    public float retreatPenalty = -0.008f;

    [Tooltip("사거리 밖에서 공격 시 감산")]
    public float outOfRangePenalty = -0.20f;          // ⬅ 인스펙터에서 조절

    /* 인스펙터에 노출할 가중치 추가 */
    [Tooltip("1m 이내로 붙었을 때 프레임당 보상")]
    public float inRangeHoldReward = 0.02f;


    /* ─────────── 내부 상태 ─────────── */
    private float prevDist;          // 직전 프레임 적과의 거리 스냅
    private float prevEnemyHP;        // 부모에서 셋업하지만 여기서도 갱신

    /* --------------------------------------------------------------------- */
    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();       // RLAgentBase 초기화(HP 스냅 등)

        // 거리 스냅샷
        if (ctrl.enemy != null)
            prevDist = Vector3.Distance(transform.position,
                                        ctrl.enemy.transform.position);

        // 내 체력 스냅도 로컬 보관
        prevEnemyHP = ctrl.GetCurrentHP();
    }

    /* --------------------------------------------------------------------- */
    protected override void OnAttackSuccess()
    {
        /* 데미지 기반 보상 */
        float enemyHP = ctrl.enemy.GetCurrentHP();
        float damageDone = prevEnemyHP - enemyHP;

        if (damageDone > 0f)
            AddReward(damageReward * damageDone);

        prevEnemyHP = enemyHP;
    }



    /* --------------------------------------------------------------------- */
    public override void OnActionReceived(ActionBuffers act)
    {
        base.OnActionReceived(act);                 // 시간 패널티, 피격 패널티 등

        /* ───────── 1) 맞았는지 체크 ───────── */
        float selfHP = ctrl.GetCurrentHP();
        float lostHP = prevSelfHP - selfHP;
        if (lostHP > 0f) AddReward(getHitPenalty * lostHP);
        prevSelfHP = selfHP;

        if (ctrl.enemy != null)
        {
            Vector3 selfPos = transform.position;
            Vector3 enemyPos = ctrl.enemy.transform.position;

            float currDist = Vector3.Distance(selfPos, enemyPos);

            float delta = prevDist - currDist;          // + : 가까워짐, – : 멀어짐
            if (Mathf.Abs(delta) > 0.01f)
            {
                float stepReward = delta > 0          // 접근
                    ? 0.10f * delta                  // +0.10 × 줄어든 m
                    : -0.10f * (-delta);              // 후퇴엔 동등 패널티
                AddReward(stepReward);
            }
            prevDist = currDist;

            /* ② 초근접 지수 보상 */
            AddReward(Mathf.Exp(-6f * currDist) * 0.08f);

            if (currDist > 0.8f)           // 0.8 m 초과 구간만
            {
                float distPenalty = (currDist - 0.8f) * -0.02f;
                AddReward(distPenalty);    // 멀수록 계속 손해
            }

            /* 2) 헛손질 패널티 */
            if (act.DiscreteActions[0] == 5 && currDist > 1.2f)
                AddReward(outOfRangePenalty);

            /* 3) 가까이 보정된 facing 보상(기존 0.005 → 0.02) */
            Vector3 dir = (enemyPos - selfPos).normalized;
            float facing = 1f - Vector3.Angle(transform.forward, dir) / 180f;
            AddReward(0.04f * facing);

            /* 4) Idle 패널티 -- 1 m 밖에서만 손해 */
            if (act.DiscreteActions[0] == 0 && currDist > 1.0f)
                AddReward(-0.08f);    // 붙었는데 가만있으면 0, 멀리 Idle 은 –0.05
        }
    }
}
