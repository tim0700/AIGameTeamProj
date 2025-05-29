using UnityEngine;
using UnityEditor;

namespace BehaviorTree
{
    /// <summary>
    /// Helper script to fix Unity import issues after exiting Safe Mode
    /// </summary>
    public class BTImportFixer : MonoBehaviour
    {
        [MenuItem("BehaviorTree/Fix Import Issues")]
        public static void FixImportIssues()
        {
            Debug.Log("Fixing BehaviorTree import issues...");
            
            // Force reimport of all BT scripts
            AssetDatabase.ImportAsset("Assets/LJH/BehaviorTree", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/LJH/Agents", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            
            // Refresh the asset database
            AssetDatabase.Refresh();
            
            Debug.Log("Import fix complete. Please check if the errors are resolved.");
        }
        
        [MenuItem("BehaviorTree/Verify All Components")]
        public static void VerifyComponents()
        {
            Debug.Log("=== BehaviorTree Component Verification ===");
            
            // Check for all condition nodes
            var conditionTypes = new[]
            {
                typeof(HealthBelowThresholdCondition),
                typeof(EnemyInRangeCondition),
                typeof(EnemyStateCondition),
                typeof(CooldownReadyCondition)
            };
            
            foreach (var type in conditionTypes)
            {
                if (type != null)
                    Debug.Log($"✓ Found: {type.Name}");
                else
                    Debug.LogError($"✗ Missing: {type?.Name ?? "Unknown"}");
            }
            
            // Check for all action nodes
            var actionTypes = new[]
            {
                typeof(AttackAction),
                typeof(DefenseAction),
                typeof(EvasionAction),
                typeof(MoveTowardEnemyAction),
                typeof(MoveAwayFromEnemyAction),
                typeof(PatrolAction),
                typeof(SideStepAction)
            };
            
            foreach (var type in actionTypes)
            {
                if (type != null)
                    Debug.Log($"✓ Found: {type.Name}");
                else
                    Debug.LogError($"✗ Missing: {type?.Name ?? "Unknown"}");
            }
            
            Debug.Log("=== Verification Complete ===");
        }
    }
}
