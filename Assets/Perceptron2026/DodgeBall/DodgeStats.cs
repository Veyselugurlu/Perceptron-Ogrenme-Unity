using System.Collections.Generic;
using UnityEngine;

namespace DodgeBall
{
    /// <summary>
    /// On the character: counts the thrown objects that hit it and shows what the perceptron decided
    /// for the last throw, how many decisions were wrong and the current weights.
    /// </summary>
    public class DodgeStats : MonoBehaviour
    {
        [SerializeField] private Perceptron perceptron;
        [SerializeField] private Material red;

        public int Throws { get; private set; }
        public int WrongDecisions { get; private set; }
        public int RedHits { get; private set; }
        public int OtherHits { get; private set; }
        /// <summary>Raised when a thrown object touches the character (true = red sphere).</summary>
        public event System.Action<GameObject, bool> Hit;

        private readonly HashSet<int> counted = new HashSet<int>();
        private string lastThrow = "-";
        private string lastDecision = "-";
        private GUIStyle style;

        private void OnEnable()
        {
            if (perceptron != null) perceptron.Reacted += OnReacted;
        }

        private void OnDisable()
        {
            if (perceptron != null) perceptron.Reacted -= OnReacted;
        }

        private void OnReacted(double shape, double colour, double desired, double result)
        {
            Throws++;
            if (result != desired) WrongDecisions++;
            lastThrow = (colour == 0 ? "red " : "green ") + (shape == 0 ? "sphere" : "cube");
            lastDecision = (result == 0 ? "duck" : "stand") + (result == desired ? " (right)" : " (WRONG)");
        }

        private void OnCollisionEnter(Collision collision)
        {
            GameObject other = collision.collider.gameObject;
            if (other.GetComponent<Lifetime>() == null || !counted.Add(other.GetInstanceID())) return;
            var r = other.GetComponent<Renderer>();
            bool redSphere = r != null && r.sharedMaterial == red && other.GetComponent<SphereCollider>() != null;
            if (redSphere) RedHits++;
            else OtherHits++;
            Hit?.Invoke(other, redSphere);
        }

        private void OnGUI()
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 17 };
                style.normal.textColor = Color.white;
            }
            GUI.Box(new Rect(12, 12, 360, 150), "Perceptron dodge ball");
            GUI.Label(new Rect(24, 36, 340, 24), $"Throws: {Throws}   wrong decisions: {WrongDecisions}", style);
            GUI.Label(new Rect(24, 60, 340, 24), $"Last: {lastThrow} -> {lastDecision}", style);
            GUI.Label(new Rect(24, 84, 340, 24), $"Hit by red sphere: {RedHits}   by others: {OtherHits}", style);
            if (perceptron != null)
                GUI.Label(new Rect(24, 108, 340, 24),
                    $"w1 {perceptron.Weight1:0.00}  w2 {perceptron.Weight2:0.00}  bias {perceptron.Bias:0.00}", style);
            GUI.Label(new Rect(24, 132, 340, 24), "Keys 1-4 throw, Space resets", style);
        }
    }
}
