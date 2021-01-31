using System;
using UnityEngine;

namespace SnailDate
{
    public class OverworldSprite : MonoBehaviour
    {
        public Transform reference;
        public float forwardMulti = 1f;
        
        private Vector3 _offset;

        private void Start()
        {
            _offset = reference.transform.position - transform.position + (Vector3.forward * forwardMulti);
        }

        private void Update()
        {
            var target = reference.transform.position + _offset;
            if (target != transform.position)
            {
                transform.position = target;
            }
        }
    }
}