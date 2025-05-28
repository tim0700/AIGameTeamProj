using UnityEngine;

namespace BehaviorTree
{
    public class BehaviorTree : MonoBehaviour
    {
        private Node rootNode;
        protected Agent agent;

        private void Start()
        {
            agent = GetComponent<Agent>();
            if (agent == null)
            {
                Debug.LogError("BehaviorTree requires an Agent component!");
                return;
            }

            ConstructTree();
        }

        private void Update()
        {
            if (rootNode != null)
            {
                rootNode.Evaluate();
            }
        }

        protected virtual void ConstructTree()
        {
            // 상속받는 클래스에서 구현
        }

        protected void SetRootNode(Node node)
        {
            rootNode = node;
            rootNode.SetAgent(agent);
        }
    }
}
