using UnityEngine;

namespace PixelDefense.Gameplay
{
    /// <summary>Pushes the shared toy-lighting globals (tint, rim, wrap, specular) from the visual config.</summary>
    [ExecuteAlways]
    public sealed class ToyLook : MonoBehaviour
    {
        private static readonly int ShadowTintId = Shader.PropertyToID("_PD_ShadowTint");
        private static readonly int RimColorId = Shader.PropertyToID("_PD_RimColor");
        private static readonly int LightParamsId = Shader.PropertyToID("_PD_LightParams");

        [SerializeField] private VisualConfig _config;

        public void Init(VisualConfig config)
        {
            _config = config;
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        public void Apply()
        {
            if (_config == null)
            {
                return;
            }

            Color tint = _config.ShadowTint.linear;
            Shader.SetGlobalVector(ShadowTintId, new Vector4(tint.r, tint.g, tint.b, _config.ShadowTintStrength));
            Color rim = _config.RimColor.linear;
            Shader.SetGlobalVector(RimColorId, new Vector4(rim.r, rim.g, rim.b, _config.RimStrength));
            Shader.SetGlobalVector(LightParamsId, new Vector4(_config.Wrap, _config.SpecularPower, _config.SpecularStrength, _config.AmbientMultiplier));
        }
    }
}
