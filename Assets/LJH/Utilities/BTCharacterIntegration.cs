using UnityEngine;
using System.Collections;

namespace BehaviorTree
{
    /// <summary>
    /// Integration helper to connect BT agents with character models and animations from team members
    /// </summary>
    [RequireComponent(typeof(BTAgent))]
    public class BTCharacterIntegration : MonoBehaviour
    {
        [Header("Character Model References")]
        [Tooltip("Reference to the character model from LYD folder")]
        public GameObject characterModel;
        
        [Header("Animation Settings")]
        [Tooltip("Animator component from the character model")]
        public Animator characterAnimator;
        
        [Header("Animation Parameter Names")]
        [Tooltip("Match these with LYD's AnimationController parameters")]
        public string moveSpeedParam = "IsMoving";  // LYD uses bool for movement state
        public string moveXParam = "MoveX";
        public string moveZParam = "MoveY";          // Note: LYD uses MoveY for Z axis
        public string attackTrigger = "Attack";
        public string defenseTrigger = "Guard";      // LYD uses 'Guard' instead of 'Defense'
        public string evasionTrigger = "Dodge";      // LYD uses 'Dodge' instead of 'Evasion'
        public string deathTrigger = "Death";
        
        [Header("LYD Animation Clip Names")]
        public string attackClipName = "HumanM@1HAttack01_R";
        public string guardClipName = "HumanM@ShieldAttack01";
        public string dodgeClipName = "HumanM@Combat_TakeDamage01";
        
        [Header("Character Components")]
        public Transform characterTransform;
        public Collider[] attackColliders;
        
        private BTAgent btAgent;
        private Vector3 lastPosition;
        private Vector3 currentVelocity;
        
        void Start()
        {
            btAgent = GetComponent<BTAgent>();
            
            // Auto-find components if not assigned
            if (characterModel == null)
            {
                characterModel = transform.Find("Character")?.gameObject;
            }
            
            if (characterAnimator == null && characterModel != null)
            {
                characterAnimator = characterModel.GetComponentInChildren<Animator>();
            }
            
            if (characterTransform == null && characterModel != null)
            {
                characterTransform = characterModel.transform;
            }
            
            // Find attack colliders if not assigned
            if (attackColliders == null || attackColliders.Length == 0)
            {
                Transform weaponTransform = characterModel?.transform.Find("Weapon");
                if (weaponTransform != null)
                {
                    attackColliders = weaponTransform.GetComponentsInChildren<Collider>();
                }
            }
            
            // Disable attack colliders initially
            SetAttackCollidersActive(false);
            
            lastPosition = transform.position;
        }
        
        void Update()
        {
            UpdateMovementAnimation();
            UpdateAttackColliders();
        }
        
        void UpdateMovementAnimation()
        {
            if (characterAnimator == null) return;
            
            // Calculate velocity
            currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
            lastPosition = transform.position;
            
            // Convert to local space
            Vector3 localVelocity = transform.InverseTransformDirection(currentVelocity);
            
            // Update animation parameters
            float moveSpeed = localVelocity.magnitude;
            
            // LYD uses IsMoving as bool, not float
            bool isMoving = moveSpeed > 0.1f;
            characterAnimator.SetBool(moveSpeedParam, isMoving);
            
            if (isMoving)
            {
                // Normalize for direction
                Vector3 normalizedVelocity = localVelocity.normalized;
                characterAnimator.SetFloat(moveXParam, normalizedVelocity.x);
                characterAnimator.SetFloat(moveZParam, normalizedVelocity.z); // LYD uses MoveY for Z
            }
            else
            {
                // Reset movement values when not moving
                characterAnimator.SetFloat(moveXParam, 0f);
                characterAnimator.SetFloat(moveZParam, 0f);
            }
        }
        
        void UpdateAttackColliders()
        {
            // Enable attack colliders only during attack animation
            if (btAgent.IsAttacking())
            {
                // Check if we're in the damage window of the attack animation
                AnimatorStateInfo stateInfo = characterAnimator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Attack") && stateInfo.normalizedTime > 0.3f && stateInfo.normalizedTime < 0.7f)
                {
                    SetAttackCollidersActive(true);
                }
                else
                {
                    SetAttackCollidersActive(false);
                }
            }
            else
            {
                SetAttackCollidersActive(false);
            }
        }
        
        void SetAttackCollidersActive(bool active)
        {
            if (attackColliders != null)
            {
                foreach (Collider col in attackColliders)
                {
                    if (col != null)
                        col.enabled = active;
                }
            }
        }
        
        // Called by BTAgent actions
        public void TriggerAttackAnimation()
        {
            if (characterAnimator != null)
            {
                characterAnimator.SetTrigger(attackTrigger);
                StartCoroutine(AttackAnimationCoroutine());
            }
        }
        
        public void TriggerDefenseAnimation()
        {
            if (characterAnimator != null)
            {
                characterAnimator.SetTrigger(defenseTrigger);
            }
        }
        
        public void TriggerEvasionAnimation()
        {
            if (characterAnimator != null)
            {
                characterAnimator.SetTrigger(evasionTrigger);
            }
        }
        
        public void TriggerDeathAnimation()
        {
            if (characterAnimator != null)
            {
                characterAnimator.SetTrigger(deathTrigger);
            }
        }
        
        IEnumerator AttackAnimationCoroutine()
        {
            yield return new WaitForSeconds(0.1f); // Wait for animation to start
            
            // Get the attack animation length
            AnimatorClipInfo[] clipInfo = characterAnimator.GetCurrentAnimatorClipInfo(0);
            float animationLength = clipInfo.Length > 0 ? clipInfo[0].clip.length : 1f;
            
            // Wait for animation to complete
            yield return new WaitForSeconds(animationLength);
            
            // Ensure colliders are disabled after attack
            SetAttackCollidersActive(false);
        }
        
        // Helper method to get animation state
        public bool IsInAnimationState(string stateName)
        {
            if (characterAnimator == null) return false;
            
            AnimatorStateInfo stateInfo = characterAnimator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(stateName);
        }
        
        // Helper method to check if animation is playing
        public bool IsAnimationPlaying(string stateName, float minNormalizedTime = 0f, float maxNormalizedTime = 1f)
        {
            if (characterAnimator == null) return false;
            
            AnimatorStateInfo stateInfo = characterAnimator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(stateName) && 
                   stateInfo.normalizedTime >= minNormalizedTime && 
                   stateInfo.normalizedTime <= maxNormalizedTime;
        }
    }
}
