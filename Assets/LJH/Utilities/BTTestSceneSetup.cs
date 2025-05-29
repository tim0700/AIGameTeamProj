using UnityEngine;

namespace BehaviorTree
{
    public class BTTestSceneSetup : MonoBehaviour
    {
        [Header("Prefab References")]
        public GameObject offensiveAgentPrefab;
        public GameObject defensiveAgentPrefab;
        
        [Header("Spawn Settings")]
        public Vector3 offensiveSpawnPosition = new Vector3(-5, 0, 0);
        public Vector3 defensiveSpawnPosition = new Vector3(5, 0, 0);
        
        [Header("Arena Settings")]
        public float arenaSize = 20f;
        
        void Start()
        {
            SetupArena();
            SpawnAgents();
        }
        
        void SetupArena()
        {
            // Create floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Arena Floor";
            floor.transform.localScale = new Vector3(arenaSize / 10f, 1, arenaSize / 10f);
            floor.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.3f);
            
            // Create walls
            CreateWall("North Wall", new Vector3(0, 1, arenaSize/2), new Vector3(arenaSize, 2, 0.5f));
            CreateWall("South Wall", new Vector3(0, 1, -arenaSize/2), new Vector3(arenaSize, 2, 0.5f));
            CreateWall("East Wall", new Vector3(arenaSize/2, 1, 0), new Vector3(0.5f, 2, arenaSize));
            CreateWall("West Wall", new Vector3(-arenaSize/2, 1, 0), new Vector3(0.5f, 2, arenaSize));
        }
        
        void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().material.color = new Color(0.5f, 0.5f, 0.5f);
        }
        
        void SpawnAgents()
        {
            // Spawn offensive agent
            if (offensiveAgentPrefab != null)
            {
                GameObject offensiveAgent = Instantiate(offensiveAgentPrefab, offensiveSpawnPosition, Quaternion.identity);
                offensiveAgent.AddComponent<BTDebugger>();
                offensiveAgent.AddComponent<BTAgentUI>();
                
                // Set offensive agent color to red
                Renderer renderer = offensiveAgent.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.red;
                }
            }
            else
            {
                CreateTestAgent<OffensiveAgent>(offensiveSpawnPosition, Color.red);
            }
            
            // Spawn defensive agent
            if (defensiveAgentPrefab != null)
            {
                GameObject defensiveAgent = Instantiate(defensiveAgentPrefab, defensiveSpawnPosition, Quaternion.identity);
                defensiveAgent.AddComponent<BTDebugger>();
                defensiveAgent.AddComponent<BTAgentUI>();
                
                // Set defensive agent color to blue
                Renderer renderer = defensiveAgent.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.blue;
                }
            }
            else
            {
                CreateTestAgent<DefensiveAgent>(defensiveSpawnPosition, Color.blue);
            }
        }
        
        GameObject CreateTestAgent<T>(Vector3 position, Color color) where T : BTAgent
        {
            // Create a simple capsule as the agent
            GameObject agent = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            agent.transform.position = position;
            
            // Add components
            agent.AddComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            agent.AddComponent<T>();
            agent.AddComponent<BTDebugger>();
            agent.AddComponent<BTAgentUI>();
            
            // Set color
            agent.GetComponent<Renderer>().material.color = color;
            
            return agent;
        }
    }
}
