using Unity.MLAgents.Actuators;
using UnityEngine;

public class RL_DefenseAgent : RLAgentBase
{
    [Header("����ġ")]
    public float successfulBlockReward = 0.10f;  // ��� ���� 1ȸ
    public float dodgeReward = 0.08f;  // ȸ�� ���� 1ȸ
    public float counterAttackReward = 0.05f;  // ���� �ָ� �߰�
    public float hpMaintainReward = 0.001f; // �� step �� HP ���

    bool wasBlocking = false;
    bool wasDodging = false;

    protected override void OnAttackSuccess()
    {
        // �ݰ� ���� ��
        AddReward(counterAttackReward);
    }

    public override void OnActionReceived(ActionBuffers act)
    {
        // ���� ���� ���
        bool blockingNow = ctrl.GetCurrentState() == AgentState.Defending;
        bool dodgingNow = ctrl.GetCurrentState() == AgentState.Dodging;

        base.OnActionReceived(act);  // ���� ó��

        // ��� ���� ����
        if (blockingNow && !wasBlocking && ctrl.enemy.GetCurrentState() == AgentState.Attacking)
            AddReward(successfulBlockReward);
        wasBlocking = blockingNow;

        if (dodgingNow && !wasDodging && ctrl.enemy.GetCurrentState() == AgentState.Attacking)
            AddReward(dodgeReward);
        wasDodging = dodgingNow;

        // ü�� ���� ����
        AddReward(hpMaintainReward * (ctrl.GetCurrentHP() / ctrl.GetMaxHP()));
    }
}
