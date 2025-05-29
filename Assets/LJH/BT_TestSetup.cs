using UnityEngine;
using BehaviorTree;
using System.Collections.Generic;

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
        
        // 경계 설정 (이미 있지 않으면 생성)
        SetupBorders();
        
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
            
        // 장애물 레이어 설정 (Obstacle 레이어가 있으면 사용, 없으면 기본값)
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        if (obstacleLayer != -1)
            agentComp.obstacleLayer = LayerMask.GetMask("Obstacle");
        else
            agentComp.obstacleLayer = 1 << 8; // 기본값 Layer 8
        
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
    
    void SetupBorders()
    {
        // Border 태그가 없으면 생성 (유니티 에디터에서만 가능)
        #if UNITY_EDITOR
        try
        {
            UnityEditor.SerializedObject tagManager = new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
            UnityEditor.SerializedProperty tagsProp = tagManager.FindProperty("tags");
            
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                UnityEditor.SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals("Border"))
                {
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                UnityEditor.SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
                newTag.stringValue = "Border";
                tagManager.ApplyModifiedProperties();
                Debug.Log("Border tag created");
            }
        }
        catch { }
        #endif
        
        // 기존 경계 찾기 - 태그 대신 이름으로 찾기
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        List<GameObject> borders = new List<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Border_") || obj.name.Contains("border") || obj.tag == "Border")
            {
                borders.Add(obj);
            }
        }
        
        // 경계가 4개 미만이면 새로 생성
        if (borders.Count < 4)
        {
            // 모든 기존 경계 삭제
            foreach (GameObject border in borders)
            {
                DestroyImmediate(border);
            }
            
            // 새 경계 생성
            CreateBorder("Border_North", new Vector3(0, 1, 10), new Vector3(20, 2, 1));
            CreateBorder("Border_South", new Vector3(0, 1, -10), new Vector3(20, 2, 1));
            CreateBorder("Border_East", new Vector3(10, 1, 0), new Vector3(1, 2, 20));
            CreateBorder("Border_West", new Vector3(-10, 1, 0), new Vector3(1, 2, 20));
            
            Debug.Log("Borders created");
        }
        else
        {
            // 기존 경계에 Obstacle 레이어 설정
            foreach (GameObject border in borders)
            {
                int obstacleLayer = LayerMask.NameToLayer("Obstacle");
                if (obstacleLayer != -1)
                {
                    border.layer = obstacleLayer;
                }
                else
                {
                    border.layer = 8; // 기본값
                }
            }
            
            Debug.Log("Existing borders configured");
        }
    }
    
    void CreateBorder(string name, Vector3 position, Vector3 scale)
    {
        GameObject border = GameObject.CreatePrimitive(PrimitiveType.Cube);
        border.name = name;
        
        // 태그 설정 (태그가 있으면)
        try
        {
            border.tag = "Border";
        }
        catch
        {
            Debug.LogWarning("Border tag not found. Please create 'Border' tag in Unity.");
        }
        
        border.transform.position = position;
        border.transform.localScale = scale;
        
        // 레이어 설정
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        if (obstacleLayer != -1)
        {
            border.layer = obstacleLayer;
        }
        else
        {
            border.layer = 8; // 기본값 Layer 8
        }
        
        // 시각적 설정
        Renderer renderer = border.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.5f, 0.5f, 0.5f, 0.8f); // 회색 반투명
            renderer.material = mat;
        }
        
        // Rigidbody 설정 (고정)
        Rigidbody rb = border.GetComponent<Rigidbody>();
        if (rb == null)
            rb = border.AddComponent<Rigidbody>();
        rb.isKinematic = true;
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
