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
        private Transform _transform;

        void Start()
        {
            _transform = transform;
            _agent = GetComponent<NavMeshAgent>();
            StartCoroutine(DelayedStart());
        }

        private IEnumerator DelayedStart()
        {
            var sprite = Instantiate(overworldSpritePrefab).GetComponent<OverworldSprite>();
            sprite.transform.position = transform.position;
            sprite.reference = _transform;
            yield return new WaitForEndOfFrame();
        }

        public void SetTarget(Vector3 target)
        {
            _agent.destination = target;
        }
    }
}