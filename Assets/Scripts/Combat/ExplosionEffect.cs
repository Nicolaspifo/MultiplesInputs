using UnityEngine;

namespace NetworkingLab.Combat
{
    /// <summary>Tiny one-shot visual: expands a sphere and removes itself.</summary>
    public sealed class ExplosionEffect : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float duration = 0.35f;
        [SerializeField, Min(0.05f)] private float startScale = 0.4f;
        [SerializeField, Min(0.1f)] private float endScale = 3f;

        private float elapsed;

        private void OnEnable()
        {
            transform.localScale = Vector3.one * startScale;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, progress);
            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
