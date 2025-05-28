using UnityEngine;
using BehaviorTree;

/// <summary>
/// Simple test script to verify visual feedback is working correctly
/// Attach this to an agent to test color changes with keyboard inputs
/// </summary>
public class VisualFeedbackTest : MonoBehaviour
{
    private Agent agent;
    
    void Start()
    {
        agent = GetComponent<Agent>();
        if (agent == null)
        {
            Debug.LogError("VisualFeedbackTest: No Agent component found!");
            enabled = false;
        }
    }
    
    void Update()
    {
        // Test visual feedback with number keys
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("Testing Attack Feedback (Yellow)");
            agent.ShowAttackFeedback();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Testing Defense Feedback (Cyan)");
            agent.ShowDefenseFeedback();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("Testing Dodge Feedback (Magenta)");
            agent.ShowDodgeFeedback();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log("Testing Damage Feedback (Red)");
            agent.ShowDamageFeedback();
        }
        
        // Display instructions
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("=== Visual Feedback Test Controls ===");
            Debug.Log("1 - Test Attack Feedback (Yellow)");
            Debug.Log("2 - Test Defense Feedback (Cyan)");
            Debug.Log("3 - Test Dodge Feedback (Magenta)");
            Debug.Log("4 - Test Damage Feedback (Red)");
            Debug.Log("H - Show this help");
        }
    }
    
    void OnGUI()
    {
        // Show test controls on screen
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 12;
        
        string info = "Visual Feedback Test Controls:\n" +
                     "1 - Attack (Yellow)\n" +
                     "2 - Defense (Cyan)\n" +
                     "3 - Dodge (Magenta)\n" +
                     "4 - Damage (Red)\n" +
                     "H - Help in Console";
                     
        GUI.Box(new Rect(Screen.width - 210, 10, 200, 100), info, style);
    }
}
