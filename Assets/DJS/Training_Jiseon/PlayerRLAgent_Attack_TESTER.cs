// ✅ PlayerRLAgent_Attack.cs (암살자 보상로직)
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class PlayerRLAgent_Attack_TESTER : Agent
{
    public PlayerRL self;
    public AgentController opponent;
    public Rigidbody rb;

    private float arenaHalf = 15f;
    private float maxSpeed = 6f;

    private int totalActions = 0;
    private float totalDamage = 0f;
    private int consecutiveAttackFails = 0;
    public bool DidNotGetHitInLastAttack = false;

    private float lastAttackTime = -10f;
    private float lastSkillTime = -10f;
    private float closeTimeTracker = 0f;

    private float lastDodgeTime = -10f;  // 회피 시각 기록용

    private bool episodeEnded = false;

    private float lastDist = 0f;   // 이전 스텝에서의 거리



    public override void Initialize()
    {
        if (rb == null) rb = self.GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        self.ResetStatus();
        opponent.ResetAgent();

        self.transform.localPosition = new Vector3(-4, 0, 0);
        opponent.transform.localPosition = new Vector3(4, 0, 0);
        self.transform.rotation = Quaternion.identity;
        opponent.transform.rotation = Quaternion.identity;

        totalActions = 0;
        totalDamage = 0f;
        consecutiveAttackFails = 0;
        DidNotGetHitInLastAttack = false;
        lastAttackTime = -10f;
        lastSkillTime = -10f;
        closeTimeTracker = 0f;
        episodeEnded = false; // ✅ 에피소드 초기화
        lastDist = Vector3.Distance(self.transform.position,
                            opponent.transform.position);

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 본인 위치·속도
        sensor.AddObservation(self.transform.localPosition.x / arenaHalf);
        sensor.AddObservation(self.transform.localPosition.z / arenaHalf);
        sensor.AddObservation(rb.velocity.x / maxSpeed);
        sensor.AddObservation(rb.velocity.z / maxSpeed);

        // 본인 체력·쿨타임
        sensor.AddObservation(self.CurHP / self.maxHP);
        sensor.AddObservation(self.AttackCD / self.attackCooldownTime);
        sensor.AddObservation(self.GuardCD / self.guardCooldownTime);
        sensor.AddObservation(self.DodgeCD / self.dodgeCooldownTime);

        // 상대 위치·체력·쿨타임
        Vector3 rel = opponent.transform.localPosition - self.transform.localPosition;
        sensor.AddObservation(rel.x / arenaHalf);
        sensor.AddObservation(rel.z / arenaHalf);
        sensor.AddObservation(opponent.GetCurrentHP() / opponent.maxHP);
        sensor.AddObservation(opponent.attackCooldownTime / opponent.attackCooldownTime);
        sensor.AddObservation(opponent.defendCooldownTime / opponent.defendCooldownTime);
        sensor.AddObservation(opponent.dodgeCooldownTime / opponent.dodgeCooldownTime);


        // 상대 이동 속도
        Rigidbody oppRb = opponent.GetComponent<Rigidbody>();
        sensor.AddObservation(oppRb.velocity.x / maxSpeed);
        sensor.AddObservation(oppRb.velocity.z / maxSpeed);


        // 나와 상대 사이 거리
        float dist = Vector3.Distance(self.transform.position, opponent.transform.position);
        sensor.AddObservation(dist / arenaHalf);

        // 최근 1초 이내 스킬 사용 여부
        float timeSinceOpponentSkill = Mathf.Min(
            Time.time - opponent.attackCooldownTime,
            Time.time - opponent.defendCooldownTime,
            Time.time - opponent.dodgeCooldownTime
        );
        sensor.AddObservation(timeSinceOpponentSkill <= 1f ? 1f : 0f); 


        // 경계까지 거리
        sensor.AddObservation((arenaHalf - Mathf.Abs(self.transform.localPosition.x)) / arenaHalf);
        sensor.AddObservation((arenaHalf - Mathf.Abs(self.transform.localPosition.z)) / arenaHalf);
    }

    // Branch 0: 0정지 1앞 2뒤 3좌 4우
    // Branch 1: 0없음 1공격 2방어 3회피
    public override void OnActionReceived(ActionBuffers actions)
    {


        Vector3 lookDir = opponent.transform.position - self.transform.position;
        lookDir.y = 0f;                               // 수평 회전만
        if (lookDir.sqrMagnitude > 0.001f)
            self.transform.rotation = Quaternion.LookRotation(lookDir);

        int move = actions.DiscreteActions[0];
        int skill = actions.DiscreteActions[1];

        Vector3 dir = Vector3.zero;
        if (move == 1) dir = self.transform.forward;
        if (move == 2) dir = -self.transform.forward;
        if (move == 3) dir = -self.transform.right;
        if (move == 4) dir = self.transform.right;
        rb.AddForce(dir * 20f, ForceMode.Force);

        bool actionDone = false;
        if (skill == 1 && self.CanAttack())
        {
            self.RL_Attack();
            actionDone = true;
            lastAttackTime = Time.time;
            lastSkillTime = Time.time;
        }
        else if (skill == 2 && self.CanGuard())
        {
            self.RL_Guard();
            actionDone = true;
            lastSkillTime = Time.time;
        }
        else if (skill == 3 && self.CanDodge())
        {
            self.RL_Dodge();
            actionDone = true;
            lastSkillTime = Time.time;
        }

        if (!actionDone && skill != 0)
        {
            AddReward(-0.7f);
            Debug.Log("[Reward2] 쿨타임 중 스킬 시도 -0.7");
            var sr = Unity.MLAgents.Academy.Instance.StatsRecorder;
            sr.Add("Attack/Reward/Reward2", 1f, StatAggregationMethod.MostRecent); 
        }

        if (self.AttackCD > 0f && opponent != null)
        {
            float dist = Vector3.Distance(self.transform.position, opponent.transform.position);
            if (dist <= 2.0f)
            {
                AddReward(-0.5f);
                Debug.Log("[Reward5] 쿨타임 중 근접 감점 -0.5");
                var sr = Unity.MLAgents.Academy.Instance.StatsRecorder;
                sr.Add("Attack/Reward/Reward5", 1f, StatAggregationMethod.MostRecent); 
            }
        }

        totalActions++;

        // ----- 거리 보상 : 가까워지면 +, 멀어지면 - -----
        float dististinct = Vector3.Distance(self.transform.position, opponent.transform.position);
        AddReward((lastDist - dististinct) * 0.05f);   // 계수 0.05 → 필요시 0.02~0.1 사이로 조정
        lastDist = dististinct;
        // ----------------------------------------------


        // ✅ TensorBoard용 통계 기록 
        if (Unity.MLAgents.Academy.Instance.IsCommunicatorOn)
        {
            var sr = Unity.MLAgents.Academy.Instance.StatsRecorder;
            sr.Add("Attack/Agent/HP", self.CurHP / self.maxHP, StatAggregationMethod.MostRecent);
            sr.Add("Attack/Agent/AtkCD", self.AttackCD, StatAggregationMethod.MostRecent);
            sr.Add("Attack/Agent/CumReward", GetCumulativeReward(), StatAggregationMethod.MostRecent);
        }
    }

    void Update()
    {
        // 에피소드가 이미 끝났는지 확인 (중복 호출 방지)
        if (episodeEnded) return;

        if (self.CurHP <= 0f || opponent.GetCurrentHP() <= 0f)
        {
            EvaluateFinalPerformance(); // ✅ 성능 평가
            EndEpisode();               // ✅ 에피소드 종료
            episodeEnded = true;
        }
    }

    // ✅ 보상 처리 함수들

    public void RegisterAttack(float damageGiven)
    {
        totalDamage += damageGiven;
        DidNotGetHitInLastAttack = true;
        consecutiveAttackFails = 0;

        // 보상3. 회피 직후 2초 이내 공격 성공
        if (Time.time - lastDodgeTime <= 2f)
            AddReward(+1f);
        Debug.Log("[Reward3] 회피 후 2초 이내 공격 성공 +1");
        var sr = Unity.MLAgents.Academy.Instance.StatsRecorder;
        sr.Add("Attack/Reward/Reward3", 1f, StatAggregationMethod.MostRecent); 
    }

    public void RegisterDodge()
    {
        lastDodgeTime = Time.time;
    }

    public void EvaluateFinalPerformance()
    {
        float hitRate = (totalActions > 0) ? (totalDamage / totalActions) : 0f;
        //보상 4 공격 성공률(hitRate)에 따라 에피소드 종료 시 평가

        var sr = Unity.MLAgents.Academy.Instance.StatsRecorder;

        if (opponent.GetCurrentHP() <= 0f) // 승리했을 때만 평가
        {
            if (hitRate >= 0.8f)
            {
                AddReward(+2.0f);
                Debug.Log($"[Reward4] 고효율 전투! HitRate: {hitRate:F2} → +2");
                sr.Add("Attack/Reward/Reward4HitRate_High", 1f, StatAggregationMethod.MostRecent);
            }
            else if (hitRate >= 0.5f)
            {
                AddReward(+1.0f);
                Debug.Log($"[Reward4] 중간 효율 전투. HitRate: {hitRate:F2} → +1");
                sr.Add("Attack/Reward/Reward4HitRate_Mid", 1f, StatAggregationMethod.MostRecent);
            }
            else
            {
                AddReward(-1.0f);
                Debug.Log($"[Reward4] 공격 남발. HitRate: {hitRate:F2} → -1");
                sr.Add("Attack/Reward/Reward4HitRate_Low", 1f, StatAggregationMethod.MostRecent);
            }
        }
    }
}

