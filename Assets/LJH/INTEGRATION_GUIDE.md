# BT Agent Integration Guide

## Overview
This guide explains how to integrate the BT agent system with team members' implementations (LYD's character/animation system and DJS's UI system).

## Integration Steps

### 1. Character Model and Animation Integration (with LYD's code)

The team has implemented a PlayerController that uses similar animation parameters. Here's how to integrate:

#### A. Animation Parameter Mapping
LYD's PlayerController uses these animation parameters:
- **Bool**: `IsMoving` - For movement state
- **Float**: `MoveX`, `MoveY` - For directional movement
- **Trigger**: `Attack`, `Guard`, `Dodge` - For actions

Our BT system uses slightly different names. Update the `BTCharacterIntegration.cs` parameters:
```csharp
// In BTCharacterIntegration.cs, update these parameter names:
public string moveSpeedParam = "IsMoving";  // Changed from "MoveSpeed"
public string moveXParam = "MoveX";
public string moveZParam = "MoveY";          // Note: LYD uses MoveY for Z axis
public string attackTrigger = "Attack";
public string defenseTrigger = "Guard";      // Changed from "Defense"
public string evasionTrigger = "Dodge";      // Changed from "Evasion"
```

#### B. Animation Clip Names
LYD's actual animation clips have specific names:
- Attack: `HumanM@1HAttack01_R`
- Guard: `HumanM@ShieldAttack01`
- Dodge: `HumanM@Combat_TakeDamage01`

### 2. UI System Integration (with DJS's code)

DJS has implemented a HealthBar system. To integrate:

#### A. Use DJS's HealthBar Prefab
1. Find the healthbar prefab in `Assets/DJS/prefabs/`
2. Assign it to the `BTAgentUI` component:
   - `healthBarPrefab` field in BTAgentUI

#### B. Update Health System Integration
Create a bridge script to connect BT health with DJS's HealthBar:

```csharp
// Create this as BTHealthBridge.cs in the Utilities folder
using UnityEngine;

namespace BehaviorTree
{
    [RequireComponent(typeof(BTAgent))]
    public class BTHealthBridge : MonoBehaviour
    {
        private BTAgent btAgent;
        private HealthBar djsHealthBar;
        
        void Start()
        {
            btAgent = GetComponent<BTAgent>();
            djsHealthBar = GetComponentInChildren<HealthBar>();
            
            if (djsHealthBar != null)
            {
                djsHealthBar.maxHealth = btAgent.maxHealth;
                djsHealthBar.currentHealth = btAgent.currentHealth;
            }
        }
        
        void Update()
        {
            if (djsHealthBar != null && btAgent != null)
            {
                // Sync health values
                if (Mathf.Abs(djsHealthBar.currentHealth - btAgent.currentHealth) > 0.1f)
                {
                    djsHealthBar.ChangeHealth(btAgent.currentHealth - djsHealthBar.currentHealth);
                }
            }
        }
    }
}
```

### 3. Complete Agent Prefab Setup

Here's the step-by-step process to create a fully integrated agent prefab:

#### Step 1: Base GameObject Setup
1. Create empty GameObject named "BTAgent_Offensive" (or "BTAgent_Defensive")
2. Add Rigidbody component:
   - Freeze Rotation X and Z
   - Mass: 1
   - Drag: 1
   - Angular Drag: 5

#### Step 2: Add BT Components
Add these components in order:
1. `OffensiveAgent` or `DefensiveAgent` (from LJH/Agents/)
2. `BTCharacterIntegration` (from LJH/Utilities/)
3. `BTHealthBridge` (the new bridge script)
4. `BTAgentUI` (from LJH/Utilities/)
5. `BTDebugger` (optional, for debugging)

#### Step 3: Add Character Model
1. Find character model in `Assets/LYD/Character/`
2. Instantiate as child of the BTAgent GameObject
3. Name it "Character"
4. Ensure it has:
   - Animator component with LYD's AnimationController
   - Properly configured Avatar
   - All animation clips

#### Step 4: Configure Components
1. **BTCharacterIntegration**:
   - Character Model: Assign the character child
   - Character Animator: Auto-finds or manually assign
   - Update parameter names as shown above

2. **BTAgentUI**:
   - Health Bar Prefab: Use DJS's healthbar from `Assets/DJS/prefabs/`
   - Cooldown UI Prefab: Can use DJS's UI elements or leave empty for default

3. **BTAgent** (OffensiveAgent/DefensiveAgent):
   - Adjust parameters as needed:
     - Max Health: 100
     - Move Speed: 5
     - Attack Damage: 10
     - Attack Range: 2
     - Detection Range: 10

### 4. Scene Setup for Testing

#### A. Create Test Arena
1. Add `BTTestSceneSetup` to empty GameObject
2. Configure:
   - Offensive Agent Prefab: Your completed offensive agent prefab
   - Defensive Agent Prefab: Your completed defensive agent prefab
   - Arena Size: 20

#### B. Add Required Systems
1. **Camera Setup**:
   - Main Camera with appropriate position (e.g., 0, 10, -10)
   - Rotation to look at arena center

2. **Lighting**:
   - Directional Light for proper visibility

3. **Data Collection**:
   - Add `SimulationDataCollector` component to track combat data
   - Add `SimulationManager` for automated testing

### 5. Animation State Machine Adjustments

You may need to modify LYD's Animation Controller:

1. Open `Assets/LYD/AnimationController.controller`
2. Ensure these transitions exist:
   - Any State → Attack (via Attack trigger)
   - Any State → Guard (via Guard trigger)
   - Any State → Dodge (via Dodge trigger)
   - Idle ↔ Movement (via IsMoving bool)

3. Add exit time settings:
   - Attack: Exit time = 1.0
   - Guard: Exit time = 1.0
   - Dodge: Exit time = 1.0

### 6. Collision and Damage System

To properly handle combat:

1. **Add Colliders to Agents**:
   - Add CapsuleCollider to agent root
   - Height: 2, Radius: 0.5
   - Is Trigger: false

2. **Add Attack Detection**:
   - Create child GameObject "AttackZone"
   - Add BoxCollider as trigger
   - Position: (0, 1, 1) - in front of agent
   - Size: (1, 1, 2)
   - Is Trigger: true

3. **Update Attack Action**:
   Use collision detection or raycast for more accurate hit detection

### 7. Testing Checklist

Before running simulations:
- [ ] Character models properly display
- [ ] Animations play correctly
- [ ] Health bars appear above agents
- [ ] Cooldown displays work
- [ ] Agents move and rotate properly
- [ ] Combat actions trigger animations
- [ ] Damage is properly dealt and received
- [ ] Data is collected to CSV file

### 8. Common Issues and Solutions

**Issue**: Animations not playing
- Check AnimationController parameter names
- Verify animation clips are assigned
- Ensure Animator component is on character model

**Issue**: UI not visible
- Check Canvas render mode (should be World Space)
- Verify UI scale (typically 0.01 for world space)
- Ensure Main Camera is assigned

**Issue**: Agents not moving
- Check Rigidbody constraints
- Verify move speed is not zero
- Ensure no collider conflicts

**Issue**: No damage dealt
- Verify attack range settings
- Check defense/evasion states
- Ensure BTAgent components are properly referenced

## Final Notes

This integration allows the BT system to work seamlessly with:
- LYD's character models and animations
- DJS's UI system for health and cooldowns
- Full combat simulation with data collection

For any issues or questions about specific integrations, refer to the README.md files in each team member's folder or contact them directly.
