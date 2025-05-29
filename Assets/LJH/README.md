# Behavior Tree (BT) Agent Implementation

## Overview
This folder contains the complete Behavior Tree implementation for the AI Game Programming term project. The implementation includes both offensive and defensive agents with full integration support for Unity ML-Agents.

## Project Structure

```
LJH/
├── Agents/
│   ├── OffensiveAgent.cs    # Offensive strategy BT agent
│   └── DefensiveAgent.cs     # Defensive strategy BT agent
├── BehaviorTree/
│   ├── Core/
│   │   ├── Node.cs          # Base node class
│   │   └── BTAgent.cs       # Base agent class with health, cooldowns, etc.
│   └── Nodes/
│       ├── Composite Nodes/
│       │   ├── Selector.cs   # OR logic node
│       │   ├── Sequence.cs   # AND logic node
│       │   └── Parallel.cs   # Parallel execution node
│       ├── Decorator Nodes/
│       │   ├── Inverter.cs           # Inverts child result
│       │   ├── Repeater.cs           # Repeats child execution
│       │   └── CooldownDecorator.cs  # Adds cooldown to child
│       ├── Action Nodes/
│       │   ├── AttackAction.cs
│       │   ├── DefenseAction.cs
│       │   ├── EvasionAction.cs
│       │   ├── MoveTowardEnemyAction.cs
│       │   ├── MoveAwayFromEnemyAction.cs
│       │   ├── PatrolAction.cs
│       │   └── SideStepAction.cs
│       └── Condition Nodes/
│           ├── EnemyInRangeCondition.cs
│           ├── EnemyStateCondition.cs
│           ├── HealthBelowThresholdCondition.cs
│           └── CooldownReadyCondition.cs
└── Utilities/
    ├── BTTestSceneSetup.cs         # Quick arena setup for testing
    ├── BTDebugger.cs               # Debug UI for agents
    ├── BTAgentUI.cs                # Health bars and cooldown UI
    ├── BTCharacterIntegration.cs   # Integration with character models
    ├── SimulationDataCollector.cs  # Collects combat data to CSV
    └── SimulationManager.cs        # Runs multiple simulations

```

## Integration with Team Components

### Character Models (from LYD folder)
1. Add `BTCharacterIntegration` component to your agent GameObject
2. Assign the character model reference from `Assets/LYD/` 
3. The integration script will automatically find and connect:
   - Animator component
   - Character transform
   - Attack colliders (if present)

### UI System (from DJS folder)
1. The `BTAgentUI` component creates world-space UI for each agent
2. If custom UI prefabs exist in `Assets/DJS/`, assign them to:
   - `healthBarPrefab`
   - `cooldownUIPrefab`

## Setup Instructions

### 1. Create Agent Prefabs

1. Create an empty GameObject
2. Add components in this order:
   - `Rigidbody` (freeze X and Z rotation)
   - Either `OffensiveAgent` or `DefensiveAgent`
   - `BTDebugger` (optional, for debugging)
   - `BTAgentUI` (for health/cooldown display)
   - `BTCharacterIntegration` (for animation)

3. Add character model as child:
   - Drag character model from `Assets/LYD/` as child
   - Ensure it has Animator component
   - Set up animation parameters

### 2. Animation Setup

Required animation parameters:
- **Float**: `MoveSpeed` (0-1)
- **Float**: `MoveX` (-1 to 1)
- **Float**: `MoveZ` (-1 to 1)
- **Trigger**: `Attack`
- **Trigger**: `Defense`
- **Trigger**: `Evasion`
- **Trigger**: `Death`

### 3. Running Simulations

1. Create empty scene
2. Add `SimulationManager` component to empty GameObject
3. Assign agent prefabs to:
   - `offensiveAgentPrefab`
   - `defensiveAgentPrefab`
4. Set number of simulations (default: 10)
5. Play scene - simulations will run automatically

### 4. Data Collection

The system automatically collects:
- Attack attempts and successes
- Defense attempts and successes
- Evasion attempts
- Damage dealt/taken
- Combat duration
- Winner determination

Data is saved to: `[ProjectRoot]/bt_simulation_data.csv`

## Agent Strategies

### Offensive Agent
- **Priority 1**: Emergency escape when health < 30%
- **Priority 2**: Combat behavior
  - Attack when in range and cooldown ready
  - Approach enemy when out of range
  - Circle strafe when too close
- **Priority 3**: Patrol when no enemy detected

### Defensive Agent
- **Priority 1**: Defend/evade against incoming attacks
- **Priority 2**: Counter-attack when enemy is vulnerable
- **Priority 3**: Position management
  - Retreat when too close
  - Approach when too far
  - Maintain optimal distance (3 units)
- **Priority 4**: Defensive patrol

## Key Parameters

### Agent Stats
- **Max Health**: 100
- **Move Speed**: 5 units/sec
- **Attack Damage**: 10
- **Attack Range**: 2 units
- **Detection Range**: 10 units

### Cooldowns
- **Attack**: 2.5 seconds
- **Defense**: 2.5 seconds
- **Evasion**: 5 seconds

### Defensive Agent Specific
- **Optimal Distance**: 3 units
- **Max Distance**: 5 units

## Testing

### Quick Test Setup
1. Use `BTTestSceneSetup` for instant arena creation
2. Creates floor and walls automatically
3. Spawns one of each agent type
4. Press F1 to toggle debug info

### Debug Controls
- **F1**: Toggle debug overlay
- **R**: Restart simulations (when complete)

## Troubleshooting

### Agents not moving
- Check if character model has Rigidbody
- Ensure arena has proper boundaries
- Verify no collider conflicts

### Animations not playing
- Verify Animator component is assigned
- Check animation parameter names match
- Ensure animation clips are properly set up

### Data not collecting
- Check if `SimulationDataCollector` is in scene
- Verify write permissions for project folder
- Look for CSV file in project root (not Assets)

## Implementation Status

### ✅ Completed Components
- **Core BT System**: All base node types (Composite, Decorator, Action, Condition)
- **Agent Implementations**: Both OffensiveAgent and DefensiveAgent with full BT structures
- **Action Nodes**: All 7 action types (Attack, Defense, Evasion, Movement, Patrol, etc.)
- **Condition Nodes**: All 4 condition types (Range, State, Health, Cooldown)
- **Decorator Nodes**: Inverter, Parallel, Repeater, CooldownDecorator
- **Utility Systems**: Debug UI, Agent UI, Data Collection, Simulation Manager
- **Integration Components**: BTCharacterIntegration, BTHealthBridge for team code integration

### 📋 Next Steps
1. **Create Agent Prefabs**: Follow the INTEGRATION_GUIDE.md to create prefabs
2. **Test with Team Assets**: Integrate LYD's character models and DJS's UI prefabs
3. **Run Simulations**: Use SimulationManager to collect combat data
4. **Fine-tune Parameters**: Adjust attack ranges, cooldowns, and AI behaviors based on test results
5. **Prepare for RL Integration**: Ensure BT agents work correctly as they'll be used to train RL agents

### 🔧 Quick Testing
1. Create new scene
2. Add BTTestSceneSetup component to empty GameObject
3. Assign your agent prefabs (or leave empty for default capsules)
4. Press Play - agents will spawn and begin combat
5. Press F1 to toggle debug information

## Contact
For questions about BT implementation: [Your contact info]
For character/animation issues: Contact LYD team member
For UI issues: Contact DJS team member
