using UnityEngine;
using System.Text;

namespace BehaviorTree
{
    public class BTDebugger : MonoBehaviour
    {
        private BTAgent agent;
        private Node rootNode;
        
        [Header("Debug Settings")]
        public bool showDebugInfo = true;
        public KeyCode toggleDebugKey = KeyCode.F1;
        
        private StringBuilder debugText = new StringBuilder();
        
        void Start()
        {
            agent = GetComponent<BTAgent>();
            // We'll need to modify BTAgent to expose the root node for debugging
        }
        
        void Update()
        {
            if (Input.GetKeyDown(toggleDebugKey))
            {
                showDebugInfo = !showDebugInfo;
            }
        }
        
        void OnGUI()
        {
            if (!showDebugInfo || agent == null) return;
            
            // Create debug info box
            GUI.Box(new Rect(10, 10, 300, 200), $"BT Debug: {gameObject.name}");
            
            debugText.Clear();
            debugText.AppendLine($"Agent Type: {agent.GetType().Name}");
            debugText.AppendLine($"Health: {agent.currentHealth:F0}/{agent.maxHealth:F0}");
            debugText.AppendLine($"State: {(agent.IsAttacking() ? "Attacking" : agent.IsDefending() ? "Defending" : agent.IsEvading() ? "Evading" : "Idle")}");
            
            debugText.AppendLine("\nCooldowns:");
            float attackCD = Mathf.Max(0, agent.attackCooldown - (Time.time - agent.lastAttackTime));
            float defenseCD = Mathf.Max(0, agent.defenseCooldown - (Time.time - agent.lastDefenseTime));
            float evasionCD = Mathf.Max(0, agent.evasionCooldown - (Time.time - agent.lastEvasionTime));
            
            debugText.AppendLine($"  Attack: {(attackCD > 0 ? attackCD.ToString("F1") + "s" : "Ready")}");
            debugText.AppendLine($"  Defense: {(defenseCD > 0 ? defenseCD.ToString("F1") + "s" : "Ready")}");
            debugText.AppendLine($"  Evasion: {(evasionCD > 0 ? evasionCD.ToString("F1") + "s" : "Ready")}");
            
            if (agent.GetEnemy() != null)
            {
                float distance = Vector3.Distance(transform.position, agent.GetEnemy().transform.position);
                debugText.AppendLine($"\nEnemy Distance: {distance:F1}");
            }
            
            GUI.Label(new Rect(15, 30, 290, 165), debugText.ToString());
        }
    }
}
