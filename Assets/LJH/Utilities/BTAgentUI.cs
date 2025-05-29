using UnityEngine;
using UnityEngine.UI;

namespace BehaviorTree
{
    public class BTAgentUI : MonoBehaviour
    {
        private BTAgent agent;
        
        [Header("UI References")]
        public GameObject healthBarPrefab; // Assign in inspector
        public GameObject cooldownUIPrefab; // Assign in inspector
        
        private Slider healthBar;
        private Text healthText;
        private Text[] cooldownTexts;
        
        private Camera mainCamera;
        private Canvas worldCanvas;
        
        void Start()
        {
            agent = GetComponent<BTAgent>();
            mainCamera = Camera.main;
            
            // Create world space canvas for this agent
            CreateWorldCanvas();
            
            // Create health bar
            if (healthBarPrefab != null)
            {
                GameObject healthBarObj = Instantiate(healthBarPrefab, worldCanvas.transform);
                healthBar = healthBarObj.GetComponentInChildren<Slider>();
                healthText = healthBarObj.GetComponentInChildren<Text>();
            }
            else
            {
                CreateDefaultHealthBar();
            }
            
            // Create cooldown display
            if (cooldownUIPrefab != null)
            {
                GameObject cooldownObj = Instantiate(cooldownUIPrefab, worldCanvas.transform);
                cooldownTexts = cooldownObj.GetComponentsInChildren<Text>();
            }
            else
            {
                CreateDefaultCooldownDisplay();
            }
        }
        
        void CreateWorldCanvas()
        {
            GameObject canvasObj = new GameObject("AgentCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = Vector3.up * 2f;
            
            worldCanvas = canvasObj.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.worldCamera = mainCamera;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;
            
            canvasObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 100);
            canvasObj.transform.localScale = Vector3.one * 0.01f;
        }
        
        void CreateDefaultHealthBar()
        {
            // Create health bar container
            GameObject healthBarContainer = new GameObject("HealthBar");
            healthBarContainer.transform.SetParent(worldCanvas.transform);
            RectTransform containerRect = healthBarContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0.5f);
            containerRect.anchorMax = new Vector2(1, 0.5f);
            containerRect.anchoredPosition = new Vector2(0, 30);
            containerRect.sizeDelta = new Vector2(180, 20);
            
            // Create background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(healthBarContainer.transform);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = Vector2.zero;
            
            // Create fill area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(healthBarContainer.transform);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.anchoredPosition = Vector2.zero;
            fillAreaRect.sizeDelta = new Vector2(-10, -10);
            
            // Create fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = Color.green;
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;
            
            // Create slider component
            healthBar = healthBarContainer.AddComponent<Slider>();
            healthBar.fillRect = fillRect;
            healthBar.targetGraphic = fillImage;
            healthBar.direction = Slider.Direction.LeftToRight;
            healthBar.minValue = 0;
            healthBar.maxValue = 1;
            healthBar.value = 1;
            
            // Create health text
            GameObject textObj = new GameObject("HealthText");
            textObj.transform.SetParent(healthBarContainer.transform);
            healthText = textObj.AddComponent<Text>();
            healthText.text = "100/100";
            healthText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            healthText.fontSize = 12;
            healthText.color = Color.white;
            healthText.alignment = TextAnchor.MiddleCenter;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
        }
        
        void CreateDefaultCooldownDisplay()
        {
            // Create cooldown container
            GameObject cooldownContainer = new GameObject("CooldownDisplay");
            cooldownContainer.transform.SetParent(worldCanvas.transform);
            RectTransform containerRect = cooldownContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 0);
            containerRect.anchoredPosition = new Vector2(0, -20);
            containerRect.sizeDelta = new Vector2(180, 20);
            
            cooldownTexts = new Text[3];
            string[] labels = { "ATK", "DEF", "EVD" };
            
            for (int i = 0; i < 3; i++)
            {
                GameObject textObj = new GameObject($"Cooldown{labels[i]}");
                textObj.transform.SetParent(cooldownContainer.transform);
                Text text = textObj.AddComponent<Text>();
                text.text = $"{labels[i]}: Ready";
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.fontSize = 10;
                text.color = Color.green;
                text.alignment = TextAnchor.MiddleCenter;
                
                RectTransform textRect = textObj.GetComponent<RectTransform>();
                float xPos = -60 + (i * 60);
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.anchoredPosition = new Vector2(xPos, 0);
                textRect.sizeDelta = new Vector2(50, 20);
                
                cooldownTexts[i] = text;
            }
        }
        
        void Update()
        {
            // Update UI to face camera
            if (worldCanvas != null && mainCamera != null)
            {
                worldCanvas.transform.LookAt(worldCanvas.transform.position + mainCamera.transform.rotation * Vector3.forward,
                    mainCamera.transform.rotation * Vector3.up);
            }
            
            // Update health bar
            if (healthBar != null)
            {
                healthBar.value = agent.GetHealthPercentage();
                if (healthText != null)
                {
                    healthText.text = $"{agent.currentHealth:F0}/{agent.maxHealth:F0}";
                }
            }
            
            // Update cooldown displays
            if (cooldownTexts != null && cooldownTexts.Length >= 3)
            {
                float attackCD = Mathf.Max(0, agent.attackCooldown - (Time.time - agent.lastAttackTime));
                float defenseCD = Mathf.Max(0, agent.defenseCooldown - (Time.time - agent.lastDefenseTime));
                float evasionCD = Mathf.Max(0, agent.evasionCooldown - (Time.time - agent.lastEvasionTime));
                
                cooldownTexts[0].text = $"ATK: {(attackCD > 0 ? attackCD.ToString("F1") : "Ready")}";
                cooldownTexts[1].text = $"DEF: {(defenseCD > 0 ? defenseCD.ToString("F1") : "Ready")}";
                cooldownTexts[2].text = $"EVD: {(evasionCD > 0 ? evasionCD.ToString("F1") : "Ready")}";
                
                // Color code based on ready state
                cooldownTexts[0].color = attackCD > 0 ? Color.red : Color.green;
                cooldownTexts[1].color = defenseCD > 0 ? Color.red : Color.green;
                cooldownTexts[2].color = evasionCD > 0 ? Color.red : Color.green;
            }
        }
    }
}
