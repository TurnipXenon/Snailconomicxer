using System;
using UnityEngine;

namespace SnailDate
{
    public class CameraFocus : MonoBehaviour
    {
        public Transform focus;
        private float maxDistanceDelta = 1f;

        [Header("Limits")] 
        public Vector2 lowerRightLimit = new Vector2(25.32f, -0.14f);

        public Vector2 upperLeftLimit = new Vector2(5.1f, 12.8f);
        private Transform _transform;

        private void Start()
        {
            _transform = transform;
        }
            
        private void Update()
        {
            var position = focus.position;
            var target = new Vector3(
                Mathf.Clamp(position.x, upperLeftLimit.x, lowerRightLimit.x),
            Mathf.Clamp(position.y, lowerRightLimit.y, upperLeftLimit.y));
            if (target != transform.position)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, maxDistanceDelta);
            }
        }
    }
}