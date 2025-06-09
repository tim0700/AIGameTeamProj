using UnityEngine;

/// <summary>
/// 기본 테스트 에이전트 클래스
/// 간단한 규칙 기반 AI로 동작하며, 다른 에이전트들의 베이스 클래스로 사용됨
/// </summary>
public class TestAgent : MonoBehaviour, IBattleAgent
{
    [Header("테스트 설정")]
    public float actionDelay = 1f; // 행동 간격
    public float aggressionLevel = 0.7f; // 0: 방어적, 1: 공격적

    protected AgentController controller;
    protected float lastActionTime;
    protected string agentName;

    void Start()
    {
        agentName = $"TestAgent_{Random.Range(1000, 9999)}";
    }

    /// <summary>
    /// 에이전트 초기화
    /// </summary>
    public virtual void Initialize(AgentController agentController)
    {
        controller = agentController;
        lastActionTime = Time.time;
        Debug.Log($"{GetAgentName()} 초기화 완료");
    }

    /// <summary>
    /// 현재 상황을 분석하여 다음 행동 결정
    /// </summary>
    public virtual AgentAction DecideAction(GameObservation obs)
    {
        // 행동 딜레이
        if (Time.time - lastActionTime < actionDelay)
            return AgentAction.Idle;

        lastActionTime = Time.time;

        // 체력 기반 행동 결정
        float healthRatio = obs.selfHP / 100f;

        // 체력이 낮으면 회피 우선
        if (healthRatio < 0.3f && obs.cooldowns.CanDodge)
        {
            return AgentAction.Dodge;
        }

        // 거리 기반 행동 결정
        float distance = obs.distanceToEnemy;

        if (distance < 2.5f) // 근거리
        {
            // 공격 가능하면 공격
            if (obs.cooldowns.CanAttack && Random.value < aggressionLevel)
            {
                return AgentAction.Attack;
            }
            // 방어 가능하면 방어
            else if (obs.cooldowns.CanDefend && Random.value < 0.5f)
            {
                return AgentAction.Defend;
            }
            // 뒤로 후퇴하기
            else
            {
                Vector3 retreatDir = (obs.selfPosition - obs.enemyPosition).normalized;
                return AgentAction.Move(retreatDir);
            }
        }
        else if (distance > 8f) // 원거리
        {
            // 적에게 접근
            Vector3 approachDir = (obs.enemyPosition - obs.selfPosition).normalized;
            return AgentAction.Move(approachDir);
        }
        else // 중거리
        {
            // 랜덤 행동
            float action = Random.value;

            if (action < 0.4f) // 40% 확률로 접근
            {
                Vector3 approachDir = (obs.enemyPosition - obs.selfPosition).normalized;
                return AgentAction.Move(approachDir);
            }
            else if (action < 0.7f) // 30% 확률로 옆으로 이동
            {
                Vector3 sideDir = Vector3.Cross(Vector3.up, (obs.enemyPosition - obs.selfPosition).normalized);
                if (Random.value < 0.5f) sideDir = -sideDir;
                return AgentAction.Move(sideDir);
            }
            else // 30% 확률로 대기
            {
                return AgentAction.Idle;
            }
        }
    }

    /// <summary>
    /// 행동 결과 처리
    /// </summary>
    public virtual void OnActionResult(ActionResult result)
    {
        if (result.success && result.damage > 0)
        {
            Debug.Log($"{GetAgentName()}: {result.actionType} 성공! 데미지: {result.damage}");
        }
    }

    /// <summary>
    /// 에피소드 종료 시 호출
    /// </summary>
    public virtual void OnEpisodeEnd(EpisodeResult result)
    {
        string status = result.won ? "승리" : "패배";
        Debug.Log($"{GetAgentName()} 전투 결과: {status} (HP: {result.finalHP})");
    }

    public virtual string GetAgentName() => agentName;
    public virtual string GetAgentType() => "TestAI";
}
