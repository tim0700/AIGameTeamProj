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

    /* ─────────── 내부 상태 ─────────── */
    private float prevDist;          // 직전 프레임 적과의 거리 스냅
    private float prevSelfHP;        // 부모에서 셋업하지만 여기서도 갱신

    /* --------------------------------------------------------------------- */
    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();       // RLAgentBase 초기화(HP 스냅 등)

        // 거리 스냅샷
        if (ctrl.enemy != null)
            prevDist = Vector3.Distance(transform.position,
                                        ctrl.enemy.transform.position);

        // 내 체력 스냅도 로컬 보관
        prevSelfHP = ctrl.GetCurrentHP();
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
        base.OnActionReceived(act);   // 시간 패널티·공격 성공 콜백 등 공통 처리

        /* ─── 1) 맞았는지 확인 ─── */
        float selfHP = ctrl.GetCurrentHP();
        float lostHP = prevSelfHP - selfHP;

        if (lostHP > 0f)
            AddReward(getHitPenalty * lostHP);

        prevSelfHP = selfHP;

        /* ─── 2) 거리 변화 보상 ─── */
        if (ctrl.enemy != null)
        {
            float currDist = Vector3.Distance(transform.position,
                                              ctrl.enemy.transform.position);
            float delta = prevDist - currDist;    // +면 접근, -면 후퇴

            if (Mathf.Abs(delta) > 0.01f)            // 1cm 이상 움직였을 때만
            {
                float rew = delta > 0
                            ? delta * approachReward
                            : -delta * retreatPenalty;   // delta < 0 ⇒ 후퇴
                AddReward(rew);
            }
            prevDist = currDist;
        }
    }
}
