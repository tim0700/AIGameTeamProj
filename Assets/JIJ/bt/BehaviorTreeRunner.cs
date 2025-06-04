using UnityEngine;
using System.Collections.Generic;

public class BehaviorTreeRunner : MonoBehaviour //에이전트가 어떤 행동을 할지 매 프레임마다 결정해주는 관리자
{
    public Transform enemy; 

    private BTNode root;

    [Header("설정값")]
    public float detectRange = 10f;
    public float moveSpeed = 3f;
    public float stoppingDistance = 2f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;

    void Start() // 행동 트리 구성
    {
        // 노드 구성
        BTNode detect = new DetectEnemyNode(transform, enemy, detectRange);
        BTNode move = new MoveToEnemyNode(transform, enemy, stoppingDistance, moveSpeed);
        BTNode attack = new AttackNode(transform, enemy, attackRange, attackCooldown);

        BTNode approachAndAttack = new SequenceNode(new List<BTNode> { detect, move, attack }); // 이 3개 노드를 순서대로 실행하는 시퀀스 노드로 묶음

        // (선택) 적이 없을 때 기본 행동 (예: 대기, 순찰)
        BTNode patrol = new PatrolNode(transform); 

        root = new SelectorNode(new List<BTNode> { approachAndAttack, patrol }); //approachAndAttack이 실패하면 → patrol 실행
    }

    void Update() // 매 프레임 행동 트리 실행
    {
        if (root != null)
        {
            root.Evaluate();
        }
    }
}
