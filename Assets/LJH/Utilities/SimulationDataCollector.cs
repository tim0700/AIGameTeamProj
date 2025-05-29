using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace BehaviorTree
{
    [System.Serializable]
    public class CombatData
    {
        public string timestamp;
        public string agentName;
        public string agentType;
        public int attacksAttempted;
        public int attacksSuccessful;
        public int defensesAttempted;
        public int defensesSuccessful;
        public int evasionsAttempted;
        public float damageDealt;
        public float damageTaken;
        public float healthRemaining;
        public float combatDuration;
        public bool isWinner;
    }

    public class SimulationDataCollector : MonoBehaviour
    {
        private static SimulationDataCollector instance;
        public static SimulationDataCollector Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SimulationDataCollector>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("SimulationDataCollector");
                        instance = go.AddComponent<SimulationDataCollector>();
                    }
                }
                return instance;
            }
        }

        [Header("Data Collection Settings")]
        public bool collectData = true;
        public string csvFileName = "bt_simulation_data.csv";
        public float dataCollectionInterval = 0.5f;

        private List<CombatData> combatDataList = new List<CombatData>();
        private Dictionary<BTAgent, CombatData> currentCombatData = new Dictionary<BTAgent, CombatData>();
        private float combatStartTime;
        private bool combatActive = false;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            // Find all BT agents and start tracking
            BTAgent[] agents = FindObjectsByType<BTAgent>(FindObjectsSortMode.None);
            foreach (BTAgent agent in agents)
            {
                StartTrackingAgent(agent);
            }

            combatStartTime = Time.time;
            combatActive = true;

            // Start periodic data collection
            InvokeRepeating(nameof(CollectPeriodicData), dataCollectionInterval, dataCollectionInterval);
        }

        public void StartTrackingAgent(BTAgent agent)
        {
            if (!currentCombatData.ContainsKey(agent))
            {
                CombatData data = new CombatData
                {
                    timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    agentName = agent.gameObject.name,
                    agentType = agent.GetType().Name,
                    attacksAttempted = 0,
                    attacksSuccessful = 0,
                    defensesAttempted = 0,
                    defensesSuccessful = 0,
                    evasionsAttempted = 0,
                    damageDealt = 0,
                    damageTaken = 0,
                    healthRemaining = agent.currentHealth,
                    combatDuration = 0,
                    isWinner = false
                };
                currentCombatData[agent] = data;
            }
        }

        public void RecordAttackAttempt(BTAgent agent)
        {
            if (currentCombatData.ContainsKey(agent))
            {
                currentCombatData[agent].attacksAttempted++;
            }
        }

        public void RecordAttackSuccess(BTAgent agent, float damage)
        {
            if (currentCombatData.ContainsKey(agent))
            {
                currentCombatData[agent].attacksSuccessful++;
                currentCombatData[agent].damageDealt += damage;
            }
        }

        public void RecordDefenseAttempt(BTAgent agent)
        {
            if (currentCombatData.ContainsKey(agent))
            {
                currentCombatData[agent].defensesAttempted++;
            }
        }

        public void RecordDefenseSuccess(BTAgent agent)
        {
            if (currentCombatData.ContainsKey(agent))
            {
                currentCombatData[agent].defensesSuccessful++;
            }
        }

        public void RecordEvasionAttempt(BTAgent agent)
        {
            if (currentCombatData.ContainsKey(agent))
            {
                currentCombatData[agent].evasionsAttempted++;
            }
        }

        public void RecordDamageTaken(BTAgent agent, float damage)
        {
            if (currentCombatData.ContainsKey(agent))
            {
                currentCombatData[agent].damageTaken += damage;
                currentCombatData[agent].healthRemaining = agent.currentHealth;
            }
        }

        void CollectPeriodicData()
        {
            if (!combatActive) return;

            foreach (var kvp in currentCombatData)
            {
                kvp.Value.combatDuration = Time.time - combatStartTime;
                kvp.Value.healthRemaining = kvp.Key.currentHealth;
            }

            // Check for combat end
            BTAgent[] activeAgents = FindObjectsByType<BTAgent>(FindObjectsSortMode.None);
            int aliveCount = 0;
            BTAgent winner = null;

            foreach (BTAgent agent in activeAgents)
            {
                if (agent.enabled && agent.currentHealth > 0)
                {
                    aliveCount++;
                    winner = agent;
                }
            }

            if (aliveCount <= 1)
            {
                EndCombat(winner);
            }
        }

        public void EndCombat(BTAgent winner)
        {
            if (!combatActive) return;

            combatActive = false;
            CancelInvoke(nameof(CollectPeriodicData));

            // Mark winner
            if (winner != null && currentCombatData.ContainsKey(winner))
            {
                currentCombatData[winner].isWinner = true;
            }

            // Add all combat data to list
            foreach (var data in currentCombatData.Values)
            {
                combatDataList.Add(data);
            }

            // Save to CSV
            SaveToCSV();

            // Log summary
            LogCombatSummary();
        }

        void SaveToCSV()
        {
            StringBuilder csv = new StringBuilder();
            
            // Header
            csv.AppendLine("Timestamp,AgentName,AgentType,AttacksAttempted,AttacksSuccessful,DefensesAttempted,DefensesSuccessful,EvasionsAttempted,DamageDealt,DamageTaken,HealthRemaining,CombatDuration,IsWinner");
            
            // Data
            foreach (CombatData data in combatDataList)
            {
                csv.AppendLine($"{data.timestamp},{data.agentName},{data.agentType},{data.attacksAttempted},{data.attacksSuccessful},{data.defensesAttempted},{data.defensesSuccessful},{data.evasionsAttempted},{data.damageDealt:F2},{data.damageTaken:F2},{data.healthRemaining:F2},{data.combatDuration:F2},{data.isWinner}");
            }

            string path = Path.Combine(Application.dataPath, "..", csvFileName);
            File.WriteAllText(path, csv.ToString());
            Debug.Log($"Combat data saved to: {path}");
        }

        void LogCombatSummary()
        {
            Debug.Log("=== Combat Summary ===");
            foreach (var kvp in currentCombatData)
            {
                CombatData data = kvp.Value;
                Debug.Log($"{data.agentName} ({data.agentType}):");
                Debug.Log($"  - Attacks: {data.attacksSuccessful}/{data.attacksAttempted}");
                Debug.Log($"  - Defenses: {data.defensesSuccessful}/{data.defensesAttempted}");
                Debug.Log($"  - Evasions: {data.evasionsAttempted}");
                Debug.Log($"  - Damage Dealt: {data.damageDealt:F2}");
                Debug.Log($"  - Damage Taken: {data.damageTaken:F2}");
                Debug.Log($"  - Final Health: {data.healthRemaining:F2}");
                Debug.Log($"  - Winner: {data.isWinner}");
            }
            Debug.Log($"Combat Duration: {Time.time - combatStartTime:F2} seconds");
        }

        public void ResetForNewCombat()
        {
            currentCombatData.Clear();
            combatStartTime = Time.time;
            combatActive = true;
            
            BTAgent[] agents = FindObjectsByType<BTAgent>(FindObjectsSortMode.None);
            foreach (BTAgent agent in agents)
            {
                StartTrackingAgent(agent);
            }
            
            InvokeRepeating(nameof(CollectPeriodicData), dataCollectionInterval, dataCollectionInterval);
        }
    }
}
