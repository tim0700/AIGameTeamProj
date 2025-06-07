// ✅ PlayerRLAgent_Defense.cs (철벽 방어자 보상 로직 - 강화학습 포함)
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class PlayerRLAgent_Defense : Agent
{
    public PlayerRL self;
    public PlayerRL opponent;
    // [SerializeField] private PlayerRL opponent;

    public Rigidbody rb;


    private float arenaHalf = 15f;
    private float maxSpeed = 6f;

    private int guardCount = 0;
    private int dodgeCount = 0;
    private string lastDefense = "";

    private float lastDodgeTime = -999f;
    private float opponentSkillTime = -999f;
    private int hitCount = 0;
    private bool episodeEnded = false;

    //보상 1,4를 위한 추가
    private float lastOpponentAttackCD = -1f;


    public override void Initialize()
    {
        if (rb == null) rb = self.GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        self.ResetStatus();
        if (opponent != null)
        {
            opponent.ResetStatus();
            opponent.transform.localPosition = new Vector3(-4, 0, 0);
        }
        self.transform.localPosition = new Vector3(4, 0, 0);

        guardCount = 0;
        dodgeCount = 0;
        lastDefense = "";
        lastDodgeTime = -999f;
        opponentSkillTime = -999f;
        hitCount = 0;
        episodeEnded = false;
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

        // 상대 위치·체력·상태
        Vector3 rel = opponent.transform.localPosition - self.transform.localPosition;
        sensor.AddObservation(rel.x / arenaHalf);
        sensor.AddObservation(rel.z / arenaHalf);

        sensor.AddObservation(opponent.CurHP / opponent.maxHP);

        sensor.AddObservation(opponent.AttackCD / opponent.attackCooldownTime);
        sensor.AddObservation(opponent.GuardCD / opponent.guardCooldownTime);
        sensor.AddObservation(opponent.DodgeCD / opponent.dodgeCooldownTime);

        // 상대 이동 속도
        Rigidbody oppRb = opponent.GetComponent<Rigidbody>();
        sensor.AddObservation(oppRb.velocity.x / maxSpeed);
        sensor.AddObservation(oppRb.velocity.z / maxSpeed);

        // 상대 쿨타임 시간
        sensor.AddObservation(opponent.AttackCD / opponent.attackCooldownTime); // 이미 있음 👍

        // 상대 최근 공격 여부 (타이밍 인식용)
        float timeSinceOpponentAttack = Time.time - opponent.LastAttackTime;
        sensor.AddObservation(timeSinceOpponentAttack <= 1f ? 1f : 0f);

        // 최근 1초 이내 스킬 사용 여부
        float timeSinceOpponentSkill = Mathf.Min(
            Time.time - opponent.LastAttackTime,
            Time.time - opponent.LastGuardTime,
            Time.time - opponent.LastDodgeTime
        );
        sensor.AddObservation(timeSinceOpponentSkill <= 1f ? 1f : 0f);


        // 경계까지 거리
        sensor.AddObservation((arenaHalf - Mathf.Abs(self.transform.localPosition.x)) / arenaHalf);
        sensor.AddObservation((arenaHalf - Mathf.Abs(self.transform.localPosition.z)) / arenaHalf);

        float dist = Vector3.Distance(self.transform.position, opponent.transform.position);
        sensor.AddObservation(dist / 15f);
    }


    public override void OnActionReceived(ActionBuffers actions)
    {
        int move = actions.DiscreteActions[0];
        int skill = actions.DiscreteActions[1];

        Vector3 dir = Vector3.zero;
        if (move == 1) dir = self.transform.forward;
        if (move == 2) dir = -self.transform.forward;
        if (move == 3) dir = -self.transform.right;
        if (move == 4) dir = self.transform.right;
        // rb.AddForce(dir * 20f, ForceMode.Force);
        Debug.Log("Move: " + move + ", dir: " + dir);  // ← 이 줄 추가

        rb.AddForce(dir * 5f, ForceMode.VelocityChange);


        bool actionDone = false;
        if (skill == 1 && self.CanAttack())
        {
            self.RL_Attack();
            actionDone = true;

        }
        else if (skill == 2 && self.CanGuard())
        {
            self.RL_Guard();
            actionDone = true;

        }
        else if (skill == 3 && self.CanDodge())
        {
            self.RL_Dodge();
            actionDone = true;

        }

        if (!actionDone && skill != 0)
        {
            AddReward(-0.7f);
            Debug.Log("[Reward5] 쿨타임 중 스킬 시도 -0.7");
            var sr = Unity.MLAgents.Academy.Instance.StatsRecorder;
            sr.Add("Defense/Reward/Reward5", 1f, StatAggregationMethod.MostRecent);
        }

        // ✅ TensorBoard용 통계 기록 
        if (Unity.MLAgents.Academy.Instance.IsCommunicatorOn)
        {
            var sr = Unity.MLAgents.Academy.Instance.StatsRecorder;
            sr.Add("Defense/Agent/CumReward", GetCumulativeReward(), StatAggregationMethod.MostRecent);
        }
    }

    public void RegisterSuccessfulDefense(string type)
    {
        if (Time.time - opponentSkillTime <= 0.5f && type == "Guard")
        {
            AddReward(+2.0f);
            Debug.Log("[Reward1] 타이밍 방어 성공 +2.0");
            var sr = Unity.MLAgents.Academy.Instance.StatsRecorder;
            sr.Add("Defense/Reward/Reward1", 1f, StatAggregationMethod.MostRecent);
        }

        if (Time.time - opponentSkillTime <= 1.0f && type == "Dodge")
        {
            AddReward(+1.5f);
            Debug.Log("[Reward4] 정밀 회피 성공 +1.5");
            var sr = Unity.MLAgents.Academy.Instance.StatsRecorder;
            sr.Add("Defense/Reward/Reward4", 1f, StatAggregationMethod.MostRecent);
        }

        if (type == lastDefense) //👉 이번에 사용한 수비 스킬(type)이 이전과 같으면 사용한 종류에 따라 해당 카운터(guardCount 또는 dodgeCount)를 증가
        {
            if (type == "Guard") guardCount++;
            if (type == "Dodge") dodgeCount++;
        }
        else
        {
            guardCount = (type == "Guard") ? 1 : 0;
            dodgeCount = (type == "Dodge") ? 1 : 0;
        }
        lastDefense = type;

        if (guardCount >= 3 || dodgeCount >= 3)
        {
            AddReward(-0.5f);
            Debug.Log("[Reward3] 반복 수비 감점 -0.5");
            guardCount = 0;
            dodgeCount = 0;
            var sr = Unity.MLAgents.Academy.Instance.StatsRecorder;
            sr.Add("Defense/Reward/Reward3", 1f, StatAggregationMethod.MostRecent);
        }

        if (type == "Dodge")
            lastDodgeTime = Time.time;
    }

    public void RegisterCounterAttackSuccess()
    {
        if (Time.time - lastDodgeTime <= 2.0f)
        {
            AddReward(+2.5f);
            Debug.Log("[Reward2] 회피 후 반격 성공 +2.5");
            var sr = Unity.MLAgents.Academy.Instance.StatsRecorder;
            sr.Add("Defense/Reward/Reward2", 1f, StatAggregationMethod.MostRecent);
        }
    }

    public void RegisterDamage(float amount)
    {
        hitCount++;
    }

    public void RegisterOpponentAttack()
    {
        opponentSkillTime = Time.time;
    }

    public void EvaluateFinalPerformance() { }

    // [System.Obsolete]
    void Update()
    {
        if (episodeEnded) return;

        if (self.CurHP <= 0f || opponent.CurHP <= 0f)
        {
            EvaluateFinalPerformance();
            EndEpisode();
            episodeEnded = true;
        }

        // ✅ 맵 아래로 떨어졌을 때 패널티 + 종료
        if (self.transform.position.y < -1f)
        {
            AddReward(-5.0f);
            Debug.Log("[패널티] 맵 아래로 떨어짐 → 에피소드 종료");
            EndEpisode();
            episodeEnded = true;
        }

        // // ✅ 자동 전투 재시작 시도
        // var uiManager = FindObjectOfType<BattleUIManager>();
        // if (uiManager != null)
        // {
        //     uiManager.AutoRestartBattle();
        // }
    }
    
    void LateUpdate()
{
    if (episodeEnded || opponent == null) return;

    // 쿨다운이 줄었다면 공격했다고 간주
    float currentCD = opponent.AttackCD;
    if (lastOpponentAttackCD > 0.1f && currentCD < lastOpponentAttackCD - 0.3f)
    {
        opponentSkillTime = Time.time;
        Debug.Log("[RL] 상대 공격 감지됨 (쿨타임 감소 기반)");
    }

    lastOpponentAttackCD = currentCD;
}
}