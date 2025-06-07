using Unity.MLAgents.Actuators;
using UnityEngine;

public class RL_DefenseAgent : RLAgentBase
{
    [Header("가중치")]
    public float successfulBlockReward = 0.10f;  // 방어 성공 1회
    public float dodgeReward = 0.08f;  // 회피 성공 1회
    public float counterAttackReward = 0.05f;  // 피해 주면 추가
    public float hpMaintainReward = 0.001f; // 매 step 당 HP 비례

    bool wasBlocking = false;
    bool wasDodging = false;

    protected override void OnAttackSuccess()
    {
        // 반격 성공 시
        AddReward(counterAttackReward);
    }

    public override void OnActionReceived(ActionBuffers act)
    {
        // 사전 상태 기록
        bool blockingNow = ctrl.GetCurrentState() == AgentState.Defending;
        bool dodgingNow = ctrl.GetCurrentState() == AgentState.Dodging;

        base.OnActionReceived(act);  // 공통 처리

        // 방어 성공 판정
        if (blockingNow && !wasBlocking && ctrl.enemy.GetCurrentState() == AgentState.Attacking)
            AddReward(successfulBlockReward);
        wasBlocking = blockingNow;

        if (dodgingNow && !wasDodging && ctrl.enemy.GetCurrentState() == AgentState.Attacking)
            AddReward(dodgeReward);
        wasDodging = dodgingNow;

        // 체력 유지 보상
        AddReward(hpMaintainReward * (ctrl.GetCurrentHP() / ctrl.GetMaxHP()));
    }
}
