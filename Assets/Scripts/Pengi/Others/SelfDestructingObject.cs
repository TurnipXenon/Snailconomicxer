using UnityEngine;

namespace Pengi.Others
{
    /// <summary>
    /// SelfDestructingObject is a component attached to an object you only want to see in the editor.
    /// The object self destroys during play.
    /// </summary>
    /// Notes:
    /// (1) We don't want to destroy exactly on Start because that's gonna be bad for our performance.
    /// We can delay the destruction time, and maybe do it randomly for each item.
    public class SelfDestructingObject : MonoBehaviour
    {
        public float latestDestructionTime = 5f;
        public float destructionTimeWindow = 10f;
        
        private void Start()
        {
            Destroy(gameObject, latestDestructionTime + Random.value * destructionTimeWindow);
        }
    }
}