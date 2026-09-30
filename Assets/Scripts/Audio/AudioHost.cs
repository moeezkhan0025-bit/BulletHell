using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Audio
{
    /// <summary>Gives the AudioService a frame tick (crossfades, ducking). Lives on the Services object; unscaled time, so music keeps moving while paused.</summary>
    public sealed class AudioHost : MonoBehaviour
    {
        public AudioService Service { get; set; }

        private void Update() => Service?.Tick(Time.unscaledDeltaTime);
    }
}
