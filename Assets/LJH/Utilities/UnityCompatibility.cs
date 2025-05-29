using UnityEngine;
using UnityEditor;

namespace BehaviorTree
{
    /// <summary>
    /// Unity compatibility helper for different Unity versions
    /// </summary>
    public static class UnityCompatibility
    {
        /// <summary>
        /// Wrapper for FindObjectsOfType that works with both old and new Unity versions
        /// </summary>
        public static T[] FindObjectsOfTypeCompat<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType<T>();
#endif
        }
        
        /// <summary>
        /// Wrapper for FindObjectOfType that works with both old and new Unity versions
        /// </summary>
        public static T FindFirstObjectOfTypeCompat<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>();
#else
            return Object.FindObjectOfType<T>();
#endif
        }
    }
}
