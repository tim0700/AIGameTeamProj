using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using System.IO;

/*──────────────────────────────────────────────
 *  RL_AttackAgent  (PPO + CSV 로깅)
 *    · EndEpisode 는 손대지 않는다
 *    · 직전 결과를 OnEpisodeBegin() 시점에 1줄 기록
 *──────────────────────────────────────────────*/
public class RL_AttackAgent : RLAgentBase
{
    /*── 1. Inspector 가중치 ──*/
    [Header("보상 가중치")]
    public float damageReward = 0.08f;
    public float getHitPenalty = -0.06f;
    public float approachReward = 0.01f;
    public float retreatPenalty = -0.008f;
    public float outOfRangePenalty = -0.20f;

    /*── 2. 내부 상태 ──*/
    float prevDist, prevEnemyHP;

    /*── 3. 결과 Enum ──*/
    enum EpResult { None, Win, Lose, Draw }
    EpResult lastResult = EpResult.None;      // 방금 끝난 회차의 결과

    /*── 4. CSV 통계 ──*/
    static readonly string CSV =
        Path.Combine(Application.dataPath, "Results", "rl_attack_results.csv");
    static bool headerDone = false;

    int epIdx = 0;
    int atkSucc = 0;
    int defSucc = 0;          // 공격형이라 0 유지
    float cumRew = 0f;
    /*────────────────── 5. Awake ──────────────────*/
    void Awake()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CSV));

        // ① 파일이 없을 때만 헤더 작성
        if (!File.Exists(CSV))
            File.WriteAllText(CSV,
              "Episode,AttackSuccess,DefenseSuccess,CumReward,Outcome\n");

    }

    /*────────────────── 6. Episode 시작 ──────────────────*/
    public override void OnEpisodeBegin()
    {
        /* ① 방금 끝난 에피소드 결과를 한 줄 기록 */
        if (epIdx > 0) WriteCSV();     // 첫 회차(0)에는 쓰지 않음

        /* ② 새 회차 초기화 */
        ++epIdx;
        atkSucc = defSucc = 0;
        cumRew = 0f;
        lastResult = EpResult.Draw;    // 기본값 = 무승부

        base.OnEpisodeBegin();

        if (ctrl.enemy)
        {
            prevDist = Vector3.Distance(transform.position, ctrl.enemy.transform.position);
            prevEnemyHP = ctrl.enemy.GetCurrentHP();
        }
        prevSelfHP = ctrl.GetCurrentHP();
    }

    /*────────────────── 7. 공격 성공 ──────────────────*/
    protected override void OnAttackSuccess()
    {
        atkSucc++;

        float eHP = ctrl.enemy.GetCurrentHP();
        float dmg = prevEnemyHP - eHP;
        if (dmg > 0) AddTrReward(damageReward * dmg);
        prevEnemyHP = eHP;
    }

    /*────────────────── 8. 매 Step 처리 ──────────────────*/
    public override void OnActionReceived(ActionBuffers act)
    {
        base.OnActionReceived(act);           // 시간 패널티 포함

        /* (간단) 피격 페널티 */
        float lost = prevSelfHP - ctrl.GetCurrentHP();
        if (lost > 0) AddTrReward(getHitPenalty * lost);
        prevSelfHP = ctrl.GetCurrentHP();

        /* (요약) 거리 shaping & 헛손질 패널티 */
        if (ctrl.enemy)
        {
            Vector3 self = transform.position;
            Vector3 enemy = ctrl.enemy.transform.position;
            float dist = Vector3.Distance(self, enemy);

            float d = prevDist - dist;
            AddTrReward(d * (d >= 0 ? approachReward : -retreatPenalty) * 10f);
            prevDist = dist;

            if (act.DiscreteActions[0] == 5 && dist > 1.2f)
                AddTrReward(outOfRangePenalty);
        }

        /* 승/패 판정 플래그만 세팅 → EndEpisode 호출은 RLEnvironmentManager */
        if (ctrl.enemy && ctrl.enemy.GetCurrentHP() <= 0) lastResult = EpResult.Win;
        else if (ctrl.GetCurrentHP() <= 0) lastResult = EpResult.Lose;
    }

    /*────────────────── 9. CSV 기록 헬퍼 ──────────────────*/
    void WriteCSV()
    {
        /* ❸ ElapsedSec, StepCount 열 제거 */
        string line =
            $"{epIdx},{atkSucc},{defSucc},{cumRew:F3},{lastResult}";
        File.AppendAllText(CSV, line + "\n");

        Debug.Log($"[CSV] Ep{epIdx} {lastResult}  R:{cumRew:F2}");
    }

    void AddTrReward(float r) { AddReward(r); cumRew += r; }
}
