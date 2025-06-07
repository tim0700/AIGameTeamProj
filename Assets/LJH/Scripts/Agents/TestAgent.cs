using UnityEngine;

public class TestAgent : MonoBehaviour, IBattleAgent
{
    [Header("�׽�Ʈ ����")]
    public float actionDelay = 1f;
    public float aggressionLevel = 0.7f; // 0: ������, 1: ������

    protected AgentController controller;
    protected float lastActionTime;
    protected string agentName;

    void Start()
    {
        agentName = $"TestAgent_{Random.Range(1000, 9999)}";
    }

    public virtual void Initialize(AgentController agentController)
    {
        controller = agentController;
        lastActionTime = Time.time;
        Debug.Log($"{GetAgentName()} �ʱ�ȭ �Ϸ�");
    }

    public virtual AgentAction DecideAction(GameObservation obs)
    {
        // �ൿ ������
        if (Time.time - lastActionTime < actionDelay)
            return AgentAction.Idle;

        lastActionTime = Time.time;

        // ü�� ��� �ൿ ����
        float healthRatio = obs.selfHP / 100f;

        // ü���� ������ ȸ�� �켱
        if (healthRatio < 0.3f && obs.cooldowns.CanDodge)
        {
            return AgentAction.Dodge;
        }

        // �Ÿ� ��� �ൿ ����
        float distance = obs.distanceToEnemy;

        if (distance < 2.5f) // �ٰŸ�
        {
            // ���� �����ϸ� ����
            if (obs.cooldowns.CanAttack && Random.value < aggressionLevel)
            {
                return AgentAction.Attack;
            }
            // ��� �����ϸ� ���
            else if (obs.cooldowns.CanDefend && Random.value < 0.5f)
            {
                return AgentAction.Defend;
            }
            // �ڷ� ��������
            else
            {
                Vector3 retreatDir = (obs.selfPosition - obs.enemyPosition).normalized;
                return AgentAction.Move(retreatDir);
            }
        }
        else if (distance > 8f) // ���Ÿ�
        {
            // ������ ����
            Vector3 approachDir = (obs.enemyPosition - obs.selfPosition).normalized;
            return AgentAction.Move(approachDir);
        }
        else // �߰Ÿ�
        {
            // ���� �ൿ
            float action = Random.value;

            if (action < 0.4f) // 40% Ȯ���� ����
            {
                Vector3 approachDir = (obs.enemyPosition - obs.selfPosition).normalized;
                return AgentAction.Move(approachDir);
            }
            else if (action < 0.7f) // 30% Ȯ���� ���� �̵�
            {
                Vector3 sideDir = Vector3.Cross(Vector3.up, (obs.enemyPosition - obs.selfPosition).normalized);
                if (Random.value < 0.5f) sideDir = -sideDir;
                return AgentAction.Move(sideDir);
            }
            else // 30% Ȯ���� ���
            {
                return AgentAction.Idle;
            }
        }
    }

    public virtual void OnActionResult(ActionResult result)
    {
        if (result.success && result.damage > 0)
        {
            Debug.Log($"{GetAgentName()}: {result.actionType} ����! ������: {result.damage}");
        }
    }

    public virtual void OnEpisodeEnd(EpisodeResult result)
    {
        string status = result.won ? "�¸�" : "�й�";
        Debug.Log($"{GetAgentName()} ���� ����: {status} (HP: {result.finalHP})");
    }

    public virtual string GetAgentName() => agentName;
    public virtual string GetAgentType() => "TestAI";
}