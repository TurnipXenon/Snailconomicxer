using System;
using UnityEngine;

namespace SnailDate
{
    public class OverworldSprite : MonoBehaviour
    {
        public Transform reference;
        private Vector3 _offset;

        private void Start()
        {
            _offset = reference.transform.position - transform.position;
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