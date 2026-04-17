using UnityEngine;

namespace MorphSDF.Sample
{
    public class FpsCounter : MonoBehaviour
    {
        [SerializeField] private int _fontSize = 32;
        [SerializeField] private Color _color = Color.white;

        private GUIStyle _style;
        private Rect _rect;
        private float _deltaTime = 0.0f;

        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        }

        private void OnGUI()
        {
            float msec = _deltaTime * 1000.0f;
            float fps = 1f / _deltaTime;

            string text = $"{msec:0.00} ms ({fps:0.00} fps)";

            _rect = new Rect
            {
                x = Screen.width
            };

            _style = new GUIStyle
            {
                alignment = TextAnchor.UpperRight,
                fontSize = _fontSize,
                normal =
                {
                    textColor = _color
                }
            };

            GUI.Label(_rect, text, _style);
        }
    }
}