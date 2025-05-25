using UnityEngine;
using System.Collections.Generic;

public class BehaviorTreeRunner : MonoBehaviour
{
    public Transform enemy; // 인스펙터에서 연결

    private BTNode root;

    [Header("설정값")]
    public float detectRange = 10f;
    public float moveSpeed = 3f;
    public float stoppingDistance = 2f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;

    void Start()
    {
        // 노드 구성
        BTNode detect = new DetectEnemyNode(transform, enemy, detectRange);
        BTNode move = new MoveToEnemyNode(transform, enemy, stoppingDistance, moveSpeed);
        BTNode attack = new AttackNode(transform, enemy, attackRange, attackCooldown);

        BTNode approachAndAttack = new SequenceNode(new List<BTNode> { detect, move, attack });

        // (선택) 적이 없을 때 기본 행동 (예: 대기, 순찰)
        BTNode patrol = new PatrolNode(transform); // 아직 안 만들었지만 기본 구조상 자리 잡아줌

        root = new SelectorNode(new List<BTNode> { approachAndAttack, patrol });
    }

    void Update()
    {
        if (root != null)
        {
            root.Evaluate();
        }
    }
}
