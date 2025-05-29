using UnityEngine;

namespace BehaviorTree
{
    /// <summary>
    /// Bridge component to connect BTAgent health system with DJS's HealthBar UI
    /// </summary>
    [RequireComponent(typeof(BTAgent))]
    public class BTHealthBridge : MonoBehaviour
    {
        private BTAgent btAgent;
        private HealthBar djsHealthBar;
        private float lastSyncedHealth;
        
        void Start()
        {
            btAgent = GetComponent<BTAgent>();
            
            // Try to find DJS's HealthBar component in children
            djsHealthBar = GetComponentInChildren<HealthBar>();
            
            // If not found in children, try to find in UI canvas
            if (djsHealthBar == null)
            {
                Canvas[] canvases = GetComponentsInChildren<Canvas>();
                foreach (Canvas canvas in canvases)
                {
                    djsHealthBar = canvas.GetComponentInChildren<HealthBar>();
                    if (djsHealthBar != null) break;
                }
            }
            
            // Initialize health bar if found
            if (djsHealthBar != null)
            {
                djsHealthBar.maxHealth = btAgent.maxHealth;
                // Force update the health bar to current health
                djsHealthBar.healthBar.fillAmount = btAgent.currentHealth / btAgent.maxHealth;
                lastSyncedHealth = btAgent.currentHealth;
                
                Debug.Log($"[BTHealthBridge] Connected to DJS HealthBar for {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[BTHealthBridge] Could not find DJS HealthBar component for {gameObject.name}. Using default UI.");
            }
        }
        
        void Update()
        {
            if (djsHealthBar != null && btAgent != null)
            {
                // Check if health has changed
                if (Mathf.Abs(lastSyncedHealth - btAgent.currentHealth) > 0.01f)
                {
                    // Calculate the health change
                    float healthDelta = btAgent.currentHealth - lastSyncedHealth;
                    
                    // Update DJS health bar using its animation system
                    // Note: DJS's ChangeHealth expects delta, not absolute value
                    UpdateHealthBar(healthDelta);
                    
                    lastSyncedHealth = btAgent.currentHealth;
                }
            }
        }
        
        private void UpdateHealthBar(float delta)
        {
            // Since DJS's HealthBar might have private methods,
            // we'll update it directly if ChangeHealth is not accessible
            try
            {
                // Try to use DJS's method if it exists
                var changeHealthMethod = djsHealthBar.GetType().GetMethod("ChangeHealth");
                if (changeHealthMethod != null)
                {
                    changeHealthMethod.Invoke(djsHealthBar, new object[] { delta });
                }
                else
                {
                    // Fallback: directly update the fill amount
                    float targetFill = btAgent.currentHealth / btAgent.maxHealth;
                    djsHealthBar.healthBar.fillAmount = targetFill;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTHealthBridge] Error updating health bar: {e.Message}");
                // Fallback: directly update the fill amount
                float targetFill = btAgent.currentHealth / btAgent.maxHealth;
                djsHealthBar.healthBar.fillAmount = targetFill;
            }
        }
        
        /// <summary>
        /// Manually sync the health bar with current agent health
        /// </summary>
        public void ForceSync()
        {
            if (djsHealthBar != null && btAgent != null)
            {
                float healthDelta = btAgent.currentHealth - lastSyncedHealth;
                UpdateHealthBar(healthDelta);
                lastSyncedHealth = btAgent.currentHealth;
            }
        }
        
        /// <summary>
        /// Get the connected DJS HealthBar component
        /// </summary>
        public HealthBar GetHealthBar()
        {
            return djsHealthBar;
        }
    }
}
