using UnityEngine;

namespace MorphSDF.Sample
{
    public class FpsSetter : MonoBehaviour
    {
        [SerializeField] private int _targetFps = 10000;

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = _targetFps;
        }
    }
}