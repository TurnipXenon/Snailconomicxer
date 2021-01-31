using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace SnailDate
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class SnailNPC : MonoBehaviour
    {
        public GameObject overworldSpritePrefab;
        
        private NavMeshAgent _agent;

        void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            StartCoroutine(DelayedStart());
        }

        private IEnumerator DelayedStart()
        {
            var sprite = Instantiate(overworldSpritePrefab).GetComponent<OverworldSprite>();
            sprite.transform.position = transform.position;
            sprite.reference = transform;
            yield return new WaitForEndOfFrame();
        }

        public void SetTarget(Vector3 target)
        {
            _agent.destination = target;
        }
    }
}