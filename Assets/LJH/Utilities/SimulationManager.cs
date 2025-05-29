using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BehaviorTree
{
    public class SimulationManager : MonoBehaviour
    {
        [Header("Simulation Settings")]
        public int numberOfSimulations = 10;
        public float maxSimulationTime = 300f; // 5 minutes max per simulation
        public bool autoRestartOnEnd = true;
        
        [Header("Agent Prefabs")]
        public GameObject offensiveAgentPrefab;
        public GameObject defensiveAgentPrefab;
        
        [Header("Spawn Settings")]
        public Vector3 offensiveSpawnPosition = new Vector3(-5, 0, 0);
        public Vector3 defensiveSpawnPosition = new Vector3(5, 0, 0);
        
        private int currentSimulation = 0;
        private float simulationStartTime;
        private bool simulationRunning = false;
        
        void Start()
        {
            StartSimulation();
        }
        
        void StartSimulation()
        {
            currentSimulation++;
            Debug.Log($"Starting Simulation {currentSimulation}/{numberOfSimulations}");
            
            simulationStartTime = Time.time;
            simulationRunning = true;
            
            // Spawn agents
            SpawnAgents();
            
            // Ensure data collector is present
            if (SimulationDataCollector.Instance == null)
            {
                GameObject dataCollector = new GameObject("SimulationDataCollector");
                dataCollector.AddComponent<SimulationDataCollector>();
            }
            
            // Start checking for simulation end
            StartCoroutine(CheckSimulationEnd());
        }
        
        void SpawnAgents()
        {
            // Spawn offensive agent
            GameObject offensiveAgent = Instantiate(offensiveAgentPrefab, offensiveSpawnPosition, Quaternion.identity);
            offensiveAgent.name = $"OffensiveAgent_Sim{currentSimulation}";
            
            // Spawn defensive agent  
            GameObject defensiveAgent = Instantiate(defensiveAgentPrefab, defensiveSpawnPosition, Quaternion.identity);
            defensiveAgent.name = $"DefensiveAgent_Sim{currentSimulation}";
            
            // Make them face each other
            Vector3 dirToDefensive = (defensiveAgent.transform.position - offensiveAgent.transform.position).normalized;
            offensiveAgent.transform.rotation = Quaternion.LookRotation(dirToDefensive);
            
            Vector3 dirToOffensive = (offensiveAgent.transform.position - defensiveAgent.transform.position).normalized;
            defensiveAgent.transform.rotation = Quaternion.LookRotation(dirToOffensive);
        }
        
        IEnumerator CheckSimulationEnd()
        {
            while (simulationRunning)
            {
                yield return new WaitForSeconds(1f);
                
                // Check if simulation time exceeded
                if (Time.time - simulationStartTime > maxSimulationTime)
                {
                    Debug.Log("Simulation timed out!");
                    EndSimulation();
                    yield break;
                }
                
                // Check if only one agent remains
                BTAgent[] agents = FindObjectsByType<BTAgent>(FindObjectsSortMode.None);
                int aliveCount = 0;
                
                foreach (BTAgent agent in agents)
                {
                    if (agent.enabled && agent.currentHealth > 0)
                    {
                        aliveCount++;
                    }
                }
                
                if (aliveCount <= 1)
                {
                    Debug.Log($"Simulation ended! Alive agents: {aliveCount}");
                    EndSimulation();
                    yield break;
                }
            }
        }
        
        void EndSimulation()
        {
            simulationRunning = false;
            
            // Wait a bit before cleanup
            StartCoroutine(CleanupAndRestart());
        }
        
        IEnumerator CleanupAndRestart()
        {
            yield return new WaitForSeconds(2f);
            
            // Clean up agents
            BTAgent[] agents = FindObjectsByType<BTAgent>(FindObjectsSortMode.None);
            foreach (BTAgent agent in agents)
            {
                Destroy(agent.gameObject);
            }
            
            // Check if we should run another simulation
            if (currentSimulation < numberOfSimulations && autoRestartOnEnd)
            {
                yield return new WaitForSeconds(1f);
                
                // Reset data collector for new combat
                if (SimulationDataCollector.Instance != null)
                {
                    SimulationDataCollector.Instance.ResetForNewCombat();
                }
                
                StartSimulation();
            }
            else
            {
                Debug.Log($"All {numberOfSimulations} simulations completed!");
                Debug.Log("Data saved to CSV file in project root.");
                
                // Optional: Reload scene for manual restart
                if (Input.GetKeyDown(KeyCode.R))
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
            }
        }
        
        void OnGUI()
        {
            // Display simulation info
            GUI.Box(new Rect(Screen.width - 210, 10, 200, 100), "Simulation Info");
            GUI.Label(new Rect(Screen.width - 200, 30, 180, 20), $"Simulation: {currentSimulation}/{numberOfSimulations}");
            
            if (simulationRunning)
            {
                float elapsedTime = Time.time - simulationStartTime;
                GUI.Label(new Rect(Screen.width - 200, 50, 180, 20), $"Time: {elapsedTime:F1}s / {maxSimulationTime:F0}s");
            }
            
            if (!simulationRunning && currentSimulation >= numberOfSimulations)
            {
                GUI.Label(new Rect(Screen.width - 200, 70, 180, 20), "Press R to restart");
            }
        }
    }
}
