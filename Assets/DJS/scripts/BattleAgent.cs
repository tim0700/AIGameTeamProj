using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class BattleAgent : Agent
{
    // 수정된 컴포넌트 참조
    private Rigidbody rb;
    private Animator animator;
    private PlayerRL playerRL; // HealthSystem → PlayerRL로 변경

    [SerializeField] private Transform enemy;
    [SerializeField] private Transform arena;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        playerRL = GetComponent<PlayerRL>(); // HealthSystem → PlayerRL

        if (arena == null)
            arena = transform.parent;
    }

    public override void OnEpisodeBegin()
    {
        // PlayerRL의 체력 초기화 메서드 호출
        // playerRL.ResetHealth();
        transform.localPosition = new Vector3(Random.Range(-5f, 5f), 0.5f, Random.Range(-5f, 5f));
        rb.velocity = Vector3.zero;

        enemy.localPosition = new Vector3(Random.Range(-5f, 5f), 0.5f, Random.Range(-5f, 5f));
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // PlayerRL의 현재 체력 직접 참조
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(enemy.localPosition);
        sensor.AddObservation(rb.velocity);
        // sensor.AddObservation(playerRL.currentHP); // CurrentHealth → currentHP
        sensor.AddObservation(animator.GetCurrentAnimatorStateInfo(0).normalizedTime);

        Vector3 toEnemy = enemy.position - transform.position;
        sensor.AddObservation(toEnemy.magnitude);
        sensor.AddObservation(Vector3.Dot(transform.forward, toEnemy.normalized));
        sensor.AddObservation(Vector3.Angle(transform.forward, toEnemy));
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        Vector3 movement = new Vector3(moveX, 0f, moveZ) * 5f;
        rb.AddForce(movement);

        int combatAction = actions.DiscreteActions[0];
        switch (combatAction)
        {
            case 0:
                animator.SetTrigger("Defend");
                AddReward(-0.01f);
                break;

            case 1:
                if (CheckAttackRange())
                {
                    // 적의 HealthSystem → PlayerRL로 변경 필요
                    enemy.GetComponent<PlayerRL>().TakeDamage(10);
                    AddReward(0.2f);
                    animator.SetTrigger("Attack");
                }
                else
                {
                    AddReward(-0.1f);
                }
                break;

            case 2:
                if (CheckAttackRange()) // CurrentHealth → currentHP && playerRL.currentHP > 50
                {
                    enemy.GetComponent<PlayerRL>().TakeDamage(20);
                    playerRL.TakeDamage(10); // HealthSystem → PlayerRL 메서드 호출
                    AddReward(0.3f);
                    animator.SetTrigger("StrongAttack");
                }
                else
                {
                    AddReward(-0.2f);
                }
                break;
        }

        // AddReward(playerRL.currentHP * 0.001f); // 체력 보상 계산
        // if (playerRL.currentHP <= 0) EndEpisode(); // IsDead → 직접 체크
    }

    private bool CheckAttackRange()
    {
        return Vector3.Distance(transform.position, enemy.position) < 2f;
    }

    // Heuristic() 메서드는 동일 유지
}
