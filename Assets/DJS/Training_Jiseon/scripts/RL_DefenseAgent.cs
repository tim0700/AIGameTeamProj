using Unity.MLAgents.Actuators;
using Unity.MLAgents;
using UnityEngine;
using System.IO;


public class RL_DefenseAgent : RLAgentBase
{
    [Header("보상 설정")]
    public float successfulBlockReward = 0.7f;      // ① 타이밍 방어
    public float dodgeReward = 0.3f;                // ② 회피 성공
    public float counterAttackReward = 0.4f;        // ③ 일반 반격
    public float counterAfterDodgeReward = 0.5f;    // ④ 회피 후 반격
    public float hpMaintainReward = 0.001f;          // ⑤ 체력 유지
    public float counterAfterSkillReward = 0.4f;       // ⑥ 적 스킬 직후 반격

    public float passiveDistancePenalty = -0.2f;  // ⑦ 일정 거리 유지 시 감점

    public float consecutiveBlockReward = 0.7f; // ⑧ 연속 방어

    public float approachRewardBase = 0.0001f;        // ⑨ 적에게 접근 시 보상



    private float lastDistanceToEnemy = Mathf.Infinity;



    bool wasBlocking = false;
    bool wasDodging = false;

    // 회피 후 반격 판단용
    bool dodgedRecently = false;
    float postDodgeTimer = 0f;

    //⑧ 연속 방어 성공
    int blockStreak = 0;
    string lastDefenseType = "";

    //  일정 시간 거리만 유지할 경우 패널티용 타이머
    float distantTimer = 0f;

    // 🔽 CSV 기록용 변수
    private int episodeCount = 0;
    private int attackSuccessCount = 0;
    private int defenseSuccessCount = 0;
    private float cumulativeReward = 0f;
    private bool didWin = false;

    private string cachedPath = null;
    
    private int timingBlockCount = 0;
    private int dodgeSuccessCount = 0;
    private int counterCount = 0;
    private int counterAfterDodgeCount = 0;
    private int counterAfterSkillCount = 0;
    private int passivePenaltyCount = 0;
    private float hpMaintainRewardSum = 0f;
    private int approachCount = 0;



    public override void OnEpisodeBegin()
    {
        Debug.Log($"Reward [CSV] 에피소드 시작: {episodeCount} — CSV 저장 시도됨");

        if (episodeCount > 0)
            SaveEpisodeResultToCSV();  // 직전 에피소드 저장

        episodeCount++;
        attackSuccessCount = 0;
        defenseSuccessCount = 0;
        cumulativeReward = 0f;
        didWin = false;
        lastDistanceToEnemy = Mathf.Infinity;
        timingBlockCount = 0;
        dodgeSuccessCount = 0;
        counterCount = 0;
        counterAfterDodgeCount = 0;
        counterAfterSkillCount = 0;
        passivePenaltyCount = 0;
        hpMaintainRewardSum = 0f;
        approachCount = 0;
    }

    private string GetNextCSVFilePath()
{
    string baseDir = Application.dataPath + "/Results/";
    string baseName = "rl_defense_results";
    string extension = ".csv";

    int version = 1;
    string finalPath;

    do
    {
        finalPath = Path.Combine(baseDir, $"{baseName}{version}{extension}");
        version++;
    } while (File.Exists(finalPath));

    return finalPath;
}



    protected override void OnAttackSuccess()
    {
        var sr = Academy.Instance.StatsRecorder;

        // ✅ ③ 반격 상황에서만 일반 반격 보상 
        if (wasBlocking || dodgedRecently)
        {
            AddReward(counterAttackReward);
            cumulativeReward += counterAttackReward;
            attackSuccessCount++;
            counterCount++;

            sr.Add("Defense/Reward/03_CounterAttack", counterAttackReward, StatAggregationMethod.MostRecent);
            Debug.Log("[Reward③] 반격 성공!");
        }

        // ✅ ④ 회피 후 반격 성공
        if (dodgedRecently && postDodgeTimer <= 3f)
        {
            AddReward(counterAfterDodgeReward);
            cumulativeReward += counterAfterDodgeReward;
            attackSuccessCount++;
            counterAfterDodgeCount++;

            sr.Add("Defense/Reward/04_CounterAfterDodge", counterAfterDodgeReward, StatAggregationMethod.MostRecent);
            Debug.Log("[Reward④] 회피 후 반격 성공!");
            dodgedRecently = false;
        }

        // ✅ ⑥ 적 스킬 직후 반격 성공 
        var enemyCd = ctrl.enemy.GetCooldownState();
        float attackRatio = enemyCd.attackCooldown / enemyCd.attackMaxTime;
        if (attackRatio > 0.90f && attackRatio <= 1.0f)
        {
            AddReward(counterAfterSkillReward);
            cumulativeReward += counterAfterSkillReward;
            attackSuccessCount++;
            counterAfterSkillCount++;

            sr.Add("Defense/Reward/06_CounterAfterSkill", counterAfterSkillReward, StatAggregationMethod.MostRecent);
            Debug.Log("[Reward⑥] 스킬 직후 반격 성공!");
        }
    }

    public void OnDefenseSuccess()
    {
        if (lastDefenseType == "Block")
            blockStreak++;
        else
            blockStreak = 1;

        lastDefenseType = "Block";

        if (blockStreak >= 2)
        {
            AddReward(consecutiveBlockReward);
            cumulativeReward += consecutiveBlockReward;
            defenseSuccessCount++;

            var sr = Academy.Instance.StatsRecorder;
            sr.Add("Defense/Reward/08_ConsecutiveBlocks", consecutiveBlockReward, StatAggregationMethod.MostRecent);
            Debug.Log("[Reward⑧] 연속 방어 성공!");
        }
    }


    public override void OnActionReceived(ActionBuffers act)
    {
        bool blockingNow = ctrl.GetCurrentState() == AgentState.Defending;
        bool dodgingNow = ctrl.GetCurrentState() == AgentState.Dodging;

        base.OnActionReceived(act);

        var sr = Academy.Instance.StatsRecorder;
        var enemyCd = ctrl.enemy.GetCooldownState();
        float cooldownRatio = enemyCd.attackCooldown / enemyCd.attackMaxTime;
        float dist = Vector3.Distance(transform.position, ctrl.enemy.transform.position);



        if (blockingNow && !wasBlocking) //방어 버튼을 막 누른 순간
        {
            if (ctrl.enemy.GetCurrentState() == AgentState.Attacking &&
                cooldownRatio > 0.90f && cooldownRatio <= 1.0f &&
                dist < 2f) //상대가 공격하는 도중 && 적이 공격 스킬을 방금 막 시전해서 쿨타임이 이제 막 시작됨 && 나와 적 사이의 거리2 이하 → 근접 범위에 있음
            {
                AddReward(successfulBlockReward);
                cumulativeReward += consecutiveBlockReward;
                defenseSuccessCount++;
                timingBlockCount++;

                sr.Add("Defense/Reward/01_TimingBlock", successfulBlockReward, StatAggregationMethod.MostRecent);
                Debug.Log("[Reward①] 타이밍 방어 성공!");

                // ✅ 연속 방어 성공 체크
                OnDefenseSuccess();
            }
        }


        // ② 회피 성공
        if (dodgingNow && !wasDodging &&
            ctrl.enemy.GetCurrentState() == AgentState.Attacking)
        {
            AddReward(dodgeReward);
            cumulativeReward += dodgeReward;
            defenseSuccessCount++;
            dodgeSuccessCount++; 

            sr.Add("Defense/Reward/02_Dodge", dodgeReward, StatAggregationMethod.MostRecent);
            Debug.Log("[Reward②] 회피 성공!");
            dodgedRecently = true;
            postDodgeTimer = 0f;
        }

        // 회피 후 반격 유효 시간 추적
        if (dodgedRecently)
        {
            postDodgeTimer += Time.fixedDeltaTime;
            if (postDodgeTimer > 3f)
                dodgedRecently = false;
        }

        // ⑤ 체력 유지 보상
        float hpReward = hpMaintainReward * (ctrl.GetCurrentHP() / ctrl.GetMaxHP());
        AddReward(hpReward);
        cumulativeReward += hpReward;
        hpMaintainRewardSum += hpReward;

        sr.Add("Defense/Reward/05_HPMaintain", hpReward, StatAggregationMethod.MostRecent);

        // ⑦ 일정 시간 이상 거리만 유지할 경우 패널티
        float distToEnemy = Vector3.Distance(transform.position, ctrl.enemy.transform.position);

        if (distToEnemy > 3.0f)
        {
            distantTimer += Time.fixedDeltaTime;

            if (distantTimer >= 5.0f)
            {
                AddReward(passiveDistancePenalty);
                cumulativeReward += passiveDistancePenalty;
                passivePenaltyCount++;

                sr.Add("Defense/Reward/07_PassiveDistancing", passiveDistancePenalty, StatAggregationMethod.MostRecent);
                Debug.Log("[Reward⑦] 5초간 거리 유지 → 소극적 행동 감점 -0.2");
                distantTimer = 0f;
            }
        }
        else
        {
            // 붙었을 경우 타이머 리셋
            distantTimer = 0f;
        }

        // ⑨ 적에게 가까이 접근했을 때 보상
        float currentDistance = Vector3.Distance(transform.position, ctrl.enemy.transform.position);
        if (currentDistance > 1.5f && currentDistance < 8.0f) // 일정 거리 범위 내에서만 계산
        {
            float delta = lastDistanceToEnemy - currentDistance;
            if (delta > 0.1f) // 이전보다 충분히 가까워졌을 때만 보상
            {
                AddReward(approachRewardBase);
                cumulativeReward += approachRewardBase;
                approachCount++;

                sr.Add("Defense/Reward/09_ApproachEnemy", approachRewardBase, StatAggregationMethod.MostRecent);
                Debug.Log("[Reward⑨] 적에게 접근! +" + approachRewardBase);
            }
        }
        lastDistanceToEnemy = currentDistance; // 다음 프레임 비교용


        // 상태 갱신
        wasBlocking = blockingNow;
        wasDodging = dodgingNow;

        // ✅ 승패 판단 추가
        if (ctrl.GetCurrentHP() <= 0)
        {
            didWin = false;
            EndEpisode();
        }
        if (ctrl.enemy.GetCurrentHP() <= 0)
        {
            didWin = true;
            EndEpisode();
        }
    }

    private void SaveEpisodeResultToCSV()
    {
            Debug.Log($"Reward [CSV] 저장 시도: atk={attackSuccessCount}, def={defenseSuccessCount}, reward={cumulativeReward}");

            // ✅ 모든 값이 0이고 패배일 경우 저장 안 함
        if (attackSuccessCount == 0 &&
            defenseSuccessCount == 0 &&
            Mathf.Approximately(cumulativeReward, 0f))
            {
                Debug.Log("[CSV] 무의미한 에피소드이므로 저장 생략됨");
                return;
            }
        try
        {
            if (cachedPath == null)
            {
                cachedPath = GetNextCSVFilePath();
                Debug.Log("[CSV] 새로운 파일 생성됨: " + cachedPath);
            }

            bool fileExists = File.Exists(cachedPath);

            using (StreamWriter writer = new StreamWriter(cachedPath, true))
            {
                if (!fileExists)
                    writer.WriteLine("Episode,AttackSuccess,DefenseSuccess,TimingBlock,DodgeSuccess,Counter,CounterAfterDodge,CounterAfterSkill,PassivePenaltyCount,HPMaintainTotal,ApproachCount,CumulativeReward,Result");

                string result = didWin ? "Win" : "Lose";
                    writer.WriteLine($"{episodeCount},{attackSuccessCount},{defenseSuccessCount},{timingBlockCount},{dodgeSuccessCount},{counterCount},{counterAfterDodgeCount},{counterAfterSkillCount},{passivePenaltyCount},{hpMaintainRewardSum:F3},{approachCount},{cumulativeReward:F3},{result}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("CSV 저장 중 오류 발생: " + e.Message);
        }
    }
}