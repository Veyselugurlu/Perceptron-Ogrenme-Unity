using UnityEngine;

namespace DodgeBall
{
    /// <summary>Removes a thrown object after a few seconds so the floor does not fill up.</summary>
    public class Lifetime : MonoBehaviour
    {
        public float seconds = 3f;

        private void Start() => Destroy(gameObject, seconds);
    }
}
