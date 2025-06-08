using UnityEngine;

/// <summary>
/// RL-Agent(A)  vs  BT-Defensive(B)
///  ‣ A : PPO 학습 대상 — 에피소드마다 리셋
///  ‣ B : BT 룰 에이전트 — 게임 시작 때만 한 번 세팅, 그 뒤로는 건드리지 않음
/// </summary>
public class RLEnvironmentManager : MonoBehaviour
{
    [Header("참조")]
    public RLAgentBase rlAgent;   // 공격형 RL
    public AgentController btAgent;   // 방어형 BT (DefensiveBTAgent 내장)

    [Header("스폰 포인트 (0 = RL, 1 = BT)")]
    public Transform[] spawnPoints;

    [Header("에피소드 제한 시간")]
    public float episodeTime = 45f;
    private float timer;

    /* ───────────────── 초기 세팅 ───────────────── */
    void Awake()
    {
        // Enemy 링크 (서로의 AgentController 교환)
        var rlCtrl = rlAgent.GetComponent<AgentController>();
        rlCtrl.SetEnemy(btAgent);
        btAgent.SetEnemy(rlCtrl);

        // 첫 위치 배치 (BT는 이후 그대로 둠)
        rlAgent.transform.position = spawnPoints[0].position;
        btAgent.transform.position = spawnPoints[1].position;
    }

    void Start() => ResetEpisode();   // RL 첫 에피소드 시작

    /* ───────────────── 매 프레임 루프 ───────────────── */
    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)                     // 시간 초과 → RL 패배 처리
            EndMatch(btAgent);

        // 승패 체크
        if (!rlAgent.GetComponent<AgentController>().IsAlive())
            EndMatch(btAgent);               // BT 승
        else if (!btAgent.IsAlive())
            EndMatch(rlAgent);               // RL 승
    }

    /* ───────────────── RL 쪽만 리셋 ───────────────── */
    void ResetEpisode()
    {
        btAgent.ResetAgent();
        rlAgent.transform.position = spawnPoints[0].position;
        rlAgent.SetReady();          // Ready 플래그 ON
        rlAgent.EndEpisode();        // End → Begin 자동 호출
        timer = episodeTime;
    }

    /* ───────────────── 승패 처리 ───────────────── */
    void EndMatch(Object winner)
    {
        if (winner == rlAgent) rlAgent.Win();
        else rlAgent.Lose();  // BT가 이기거나 시간 초과

        ResetEpisode();          // BT는 그대로, RL만 새 에피소드
    }
}
