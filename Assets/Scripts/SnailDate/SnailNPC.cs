using System.Collections;
using NUnit.Framework.Constraints;
using Pengi.Gameplay;
using Pengi.GameSystem;
using UnityEngine;
using UnityEngine.AI;

namespace SnailDate
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class SnailNPC : MonoBehaviour
    {
        public GameConfiguration gameConfiguration;
        public GameObject overworldSpritePrefab;
        public bool isNPC = false;
        public bool isMainPlayer = false;
        public float interactionSqrDistance = 1.5f;
        public string npcNodeStart = "Leo_Start";
        
        private NavMeshAgent _agent;
        private Transform _transform;

        void Start()
        {
            _transform = transform;
            _agent = GetComponent<NavMeshAgent>();

            if (isMainPlayer)
            {
                gameConfiguration.SetMainPlayer(this);
            }
            
            StartCoroutine(DelayedStart());
        }

        private IEnumerator DelayedStart()
        {
            var child = Instantiate(overworldSpritePrefab);
            var sprite = child.GetComponent<OverworldSprite>();
            sprite.transform.position = transform.position;
            sprite.reference = _transform;
            var clickableItem = child.GetComponent<ClickableItem>();
            clickableItem.onClickEvent.AddListener(OnClick);
            yield return new WaitForEndOfFrame();
        }

        public void SetTarget(Vector3 target)
        {
            _agent.destination = target;
        }

        public void OnClick()
        {
            if (isNPC && gameConfiguration.InputState == InputState.Overworld)
            {
                gameConfiguration.StartDialogue(npcNodeStart);
            }
        }
    }
}