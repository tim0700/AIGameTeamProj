using UnityEngine;

namespace BehaviorTree
{
    public abstract class BTAgent : MonoBehaviour
    {
        protected Node root = null;
        public Node Root => root; // Expose root for debugging

        // Agent properties
        [Header("Agent Stats")]
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public float moveSpeed = 5f;
        public float attackDamage = 10f;
        public float attackRange = 2f;
        public float detectionRange = 10f;

        // Cooldown timers
        [Header("Cooldowns")]
        public float attackCooldown = 2.5f;
        public float defenseCooldown = 2.5f;
        public float evasionCooldown = 5f;

        public float lastAttackTime = -10f;
        public float lastDefenseTime = -10f;
        public float lastEvasionTime = -10f;

        // State flags
        public bool isDefending = false;
        public bool isEvading = false;
        public bool isAttacking = false;

        // References
        protected GameObject enemy;
        protected Animator animator;
        protected Rigidbody rb;

        // Movement
        protected Vector3 moveDirection;

        protected virtual void Start()
        {
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody>();
            currentHealth = maxHealth;
            SetupTree();
        }

        protected virtual void Update()
        {
            if (root != null)
            {
                root.Evaluate();
            }

            // Update state flags
            UpdateStateFlags();
        }

        protected abstract void SetupTree();

        protected void UpdateStateFlags()
        {
            // Reset defense state after duration
            if (isDefending && Time.time - lastDefenseTime > 1.0f)
            {
                isDefending = false;
            }

            // Reset evasion state after duration
            if (isEvading && Time.time - lastEvasionTime > 0.8f)
            {
                isEvading = false;
            }

            // Reset attack state after duration
            if (isAttacking && Time.time - lastAttackTime > 0.5f)
            {
                isAttacking = false;
            }
        }

        public void TakeDamage(float damage)
        {
            if (isDefending || isEvading)
            {
                Debug.Log($"{gameObject.name} blocked/evaded the attack!");
                
                // Record successful defense
                if (isDefending && SimulationDataCollector.Instance != null)
                {
                    SimulationDataCollector.Instance.RecordDefenseSuccess(this);
                }
                
                return;
            }

            currentHealth -= damage;
            Debug.Log($"{gameObject.name} took {damage} damage! Health: {currentHealth}/{maxHealth}");
            
            // Record damage taken
            if (SimulationDataCollector.Instance != null)
            {
                SimulationDataCollector.Instance.RecordDamageTaken(this, damage);
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            Debug.Log($"{gameObject.name} has been defeated!");
            // Implement death logic
            enabled = false;
        }

        public GameObject FindEnemy()
        {
            // Find the closest enemy agent
            BTAgent[] allAgents = FindObjectsOfType<BTAgent>();
            GameObject closestEnemy = null;
            float closestDistance = float.MaxValue;

            foreach (BTAgent agent in allAgents)
            {
                if (agent != this && agent.isActiveAndEnabled)
                {
                    float distance = Vector3.Distance(transform.position, agent.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = agent.gameObject;
                    }
                }
            }

            return closestEnemy;
        }

        // Cooldown check methods
        public bool IsAttackReady() => Time.time - lastAttackTime >= attackCooldown;
        public bool IsDefenseReady() => Time.time - lastDefenseTime >= defenseCooldown;
        public bool IsEvasionReady() => Time.time - lastEvasionTime >= evasionCooldown;

        // State check methods
        public bool IsDefending() => isDefending;
        public bool IsEvading() => isEvading;
        public bool IsAttacking() => isAttacking;

        // Get methods
        public float GetHealthPercentage() => currentHealth / maxHealth;
        public GameObject GetEnemy() => enemy;
    }
}
