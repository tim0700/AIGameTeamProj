using UnityEngine;

namespace BehaviorTree
{
    /// <summary>
    /// Visualizes agent ranges and arena boundaries for debugging
    /// </summary>
    public class BTVisualizationHelper : MonoBehaviour
    {
        [Header("Visualization Settings")]
        public bool showDetectionRange = true;
        public bool showAttackRange = true;
        public bool showOptimalRange = true;
        public bool showAgentConnections = true;
        public bool showMovementVectors = true;
        
        [Header("Colors")]
        public Color detectionRangeColor = new Color(0, 1, 0, 0.2f);
        public Color attackRangeColor = new Color(1, 0, 0, 0.3f);
        public Color optimalRangeColor = new Color(0, 0, 1, 0.2f);
        public Color connectionLineColor = Color.yellow;
        public Color movementVectorColor = Color.cyan;
        
        private BTAgent[] agents;
        
        void Start()
        {
            // Find all agents in scene
            agents = FindObjectsByType<BTAgent>(FindObjectsSortMode.None);
        }
        
        void OnDrawGizmos()
        {
            if (agents == null || agents.Length == 0)
            {
                agents = FindObjectsByType<BTAgent>(FindObjectsSortMode.None);
            }
            
            foreach (BTAgent agent in agents)
            {
                if (agent == null || !agent.enabled) continue;
                
                Vector3 agentPos = agent.transform.position;
                
                // Draw detection range
                if (showDetectionRange)
                {
                    Gizmos.color = detectionRangeColor;
                    DrawCircle(agentPos, agent.detectionRange, 32);
                }
                
                // Draw attack range
                if (showAttackRange)
                {
                    Gizmos.color = attackRangeColor;
                    DrawCircle(agentPos, agent.attackRange, 16);
                }
                
                // Draw optimal range for defensive agents
                if (showOptimalRange && agent is DefensiveAgent)
                {
                    DefensiveAgent defAgent = agent as DefensiveAgent;
                    Gizmos.color = optimalRangeColor;
                    DrawCircle(agentPos, defAgent.optimalDistance, 24);
                }
                
                // Draw connection to enemy
                if (showAgentConnections && agent.GetEnemy() != null)
                {
                    Gizmos.color = connectionLineColor;
                    Vector3 enemyPos = agent.GetEnemy().transform.position;
                    Gizmos.DrawLine(agentPos + Vector3.up * 0.5f, enemyPos + Vector3.up * 0.5f);
                    
                    // Draw distance text
                    float distance = Vector3.Distance(agentPos, enemyPos);
                    Vector3 midPoint = (agentPos + enemyPos) / 2f + Vector3.up * 2f;
                    DrawText(midPoint, $"{distance:F1}m", Color.white);
                }
                
                // Draw movement vector
                if (showMovementVectors)
                {
                    Rigidbody rb = agent.GetComponent<Rigidbody>();
                    if (rb != null && rb.velocity.magnitude > 0.1f)
                    {
                        Gizmos.color = movementVectorColor;
                        Gizmos.DrawRay(agentPos + Vector3.up * 0.5f, rb.velocity.normalized * 2f);
                    }
                }
                
                // Draw agent status
                DrawAgentStatus(agent);
            }
        }
        
        void DrawCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
        
        void DrawAgentStatus(BTAgent agent)
        {
            Vector3 statusPos = agent.transform.position + Vector3.up * 3f;
            
            // Draw health bar
            float healthPercent = agent.GetHealthPercentage();
            Color healthColor = Color.Lerp(Color.red, Color.green, healthPercent);
            
            Gizmos.color = Color.black;
            Gizmos.DrawLine(statusPos - Vector3.right * 0.5f, statusPos + Vector3.right * 0.5f);
            
            Gizmos.color = healthColor;
            float healthBarLength = healthPercent - 0.5f;
            Gizmos.DrawLine(statusPos - Vector3.right * 0.5f, statusPos + Vector3.right * healthBarLength);
            
            // Draw state indicator
            string state = "Idle";
            Color stateColor = Color.white;
            
            if (agent.IsAttacking())
            {
                state = "ATK";
                stateColor = Color.red;
            }
            else if (agent.IsDefending())
            {
                state = "DEF";
                stateColor = Color.blue;
            }
            else if (agent.IsEvading())
            {
                state = "EVD";
                stateColor = Color.green;
            }
            
            DrawText(statusPos + Vector3.up * 0.5f, state, stateColor);
        }
        
        void DrawText(Vector3 position, string text, Color color)
        {
            // This is a placeholder - in actual Unity, you'd use handles or UI
            // For now, we'll use a small colored cube as indicator
            Gizmos.color = color;
            Gizmos.DrawCube(position, Vector3.one * 0.1f);
        }
        
        void OnGUI()
        {
            if (!Application.isPlaying) return;
            
            // Draw legend
            int y = 10;
            DrawLegendItem(10, y, "Detection Range", detectionRangeColor); y += 20;
            DrawLegendItem(10, y, "Attack Range", attackRangeColor); y += 20;
            DrawLegendItem(10, y, "Optimal Range (Def)", optimalRangeColor); y += 20;
            DrawLegendItem(10, y, "Enemy Connection", connectionLineColor); y += 20;
            DrawLegendItem(10, y, "Movement Vector", movementVectorColor);
        }
        
        void DrawLegendItem(int x, int y, string label, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, 15, 15), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 20, y, 150, 20), label);
        }
    }
}
