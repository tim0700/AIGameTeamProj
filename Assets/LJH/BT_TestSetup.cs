using UnityEngine;
using BehaviorTree;

public class BT_TestSetup : MonoBehaviour
{
    [Header("Test Settings")]
    public Vector3 attackerSpawnPos = new Vector3(-5, 0, 0);
    public Vector3 defenderSpawnPos = new Vector3(5, 0, 0);
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    private GameObject attackingAgent;
    private GameObject defensiveAgent;
    
    void Start()
    {
        SetupTestScene();
    }
    
    void SetupTestScene()
    {
        // 기존 에이전트들 삭제
        CleanupExistingAgents();
        
        // 공격형 에이전트 생성
        attackingAgent = CreateAgent("AttackingAgent", attackerSpawnPos, "Player", "Enemy", true);
        
        // 수비형 에이전트 생성  
        defensiveAgent = CreateAgent("DefensiveAgent", defenderSpawnPos, "Enemy", "Player", false);
        
        Debug.Log("BT Test Scene Setup Complete!");
        Debug.Log("Attacking Agent: " + attackingAgent.name);
        Debug.Log("Defensive Agent: " + defensiveAgent.name);
    }
    
    void CleanupExistingAgents()
    {
        // 기존 에이전트 찾기 및 삭제
        Agent[] allAgents = FindObjectsOfType<Agent>();
        foreach (Agent agent in allAgents)
        {
            if (Application.isPlaying)
                DestroyImmediate(agent.gameObject);
            else
                DestroyImmediate(agent.gameObject);
        }
        
        // AttackingAgent와 DefensiveAgent 이름을 가진 오브젝트 모두 삭제
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("AttackingAgent") || obj.name.Contains("DefensiveAgent"))
            {
                if (Application.isPlaying)
                    DestroyImmediate(obj);
                else
                    DestroyImmediate(obj);
            }
        }
        
        // 변수 초기화
        attackingAgent = null;
        defensiveAgent = null;
    }
    
    GameObject CreateAgent(string name, Vector3 position, string layerName, string enemyLayerName, bool isAttacking)
    {
        // 게임 오브젝트 생성
        GameObject agent = new GameObject(name);
        agent.transform.position = position;
        
        // 레이어 설정 (레이어가 없으면 기본값 사용)
        int layer = LayerMask.NameToLayer(layerName);
        if (layer != -1)
            agent.layer = layer;
        
        // Rigidbody 추가
        Rigidbody rb = agent.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        // Collider 추가
        CapsuleCollider col = agent.AddComponent<CapsuleCollider>();
        col.height = 2f;
        col.radius = 0.5f;
        col.center = new Vector3(0, 1f, 0);
        
        // Agent 컴포넌트 추가
        Agent agentComp = agent.AddComponent<Agent>();
        
        // 적 레이어 설정 (레이어가 없으면 기본값 사용)
        int enemyLayer = LayerMask.NameToLayer(enemyLayerName);
        if (enemyLayer != -1)
            agentComp.enemyLayer = LayerMask.GetMask(enemyLayerName);
        else
            agentComp.enemyLayer = ~0; // 모든 레이어
        
        // BT 컴포넌트 추가
        if (isAttacking)
        {
            agent.AddComponent<BehaviorTree.Strategies.AttackingAgentBT>();
        }
        else
        {
            agent.AddComponent<BehaviorTree.Strategies.DefensiveAgentBT>();
        }
        
        // 간단한 시각적 표현 (큐브)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.transform.SetParent(agent.transform);
        visual.transform.localPosition = new Vector3(0, 1f, 0);
        visual.transform.localScale = new Vector3(0.8f, 1.8f, 0.8f);
        
        // 색상으로 구분
        Renderer renderer = visual.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = isAttacking ? Color.red : Color.blue;
        renderer.material = mat;
        
        // Collider 제거 (Agent의 Collider만 사용)
        DestroyImmediate(visual.GetComponent<Collider>());
        
        return agent;
    }
    
    void Update()
    {
        // 디버그 정보 업데이트
        if (showDebugInfo && attackingAgent != null && defensiveAgent != null)
        {
            Agent attacker = attackingAgent.GetComponent<Agent>();
            Agent defender = defensiveAgent.GetComponent<Agent>();
            
            if (attacker != null && defender != null)
            {
                // 화면에 정보 표시 (영어로 변경)
                debugText = string.Format("Attacker HP: {0:F0}/{1:F0}{2}Defender HP: {3:F0}/{4:F0}{5}Distance: {6:F1}{7}Attacker: {8}{9}Defender: {10}",
                    attacker.currentHealth, attacker.maxHealth, System.Environment.NewLine,
                    defender.currentHealth, defender.maxHealth, System.Environment.NewLine,
                    Vector3.Distance(attacker.transform.position, defender.transform.position), System.Environment.NewLine,
                    GetAgentStatus(attacker), System.Environment.NewLine,
                    GetAgentStatus(defender));
            }
        }
        
        // R 키로 재시작
        if (Input.GetKeyDown(KeyCode.R))
        {
            SetupTestScene();
        }
    }
    
    string GetAgentStatus(Agent agent)
    {
        if (agent.isDead) return "Dead";
        if (agent.isAttacking) return "Attacking";
        if (agent.isDefending) return "Defending";
        if (agent.isDodging) return "Dodging";
        return "Idle";
    }
    
    private string debugText = "";
    
    void OnGUI()
    {
        if (showDebugInfo && !string.IsNullOrEmpty(debugText))
        {
            // UI 크기 조정
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 12;
            style.alignment = TextAnchor.UpperLeft;
            style.wordWrap = true;
            
            // 메인 정보 박스
            GUI.Box(new Rect(10, 10, 300, 120), debugText, style);
            
            // 추가 정보 - 각 줄을 따로 박스로
            string feedbackInfo = string.Format("Visual Feedback:{0}Yellow = Attack{1}Cyan = Defense{2}Green = Block Success{3}Magenta = Dodge{4}Red = Damage",
                System.Environment.NewLine, System.Environment.NewLine, 
                System.Environment.NewLine, System.Environment.NewLine, System.Environment.NewLine);
                
            GUI.Box(new Rect(10, 140, 300, 120), feedbackInfo, style);
            
            // 콘트롤 가이드
            GUI.Box(new Rect(10, 270, 300, 60), 
                "Press R to restart test", style);
        }
    }
}
