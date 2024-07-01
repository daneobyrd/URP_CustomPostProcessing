using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CustomPostProcessing.UniversalRP
{
    /// <summary>
    /// An injection point for the full screen pass. This is similar to <see cref="RenderPassEvent"/> enum but limits to only supported events.
    /// </summary>
    public enum CustomPostProcessInjectionPoint
    {
        [InspectorName("Before Transparents")]
        BeforeTransparents = RenderPassEvent.BeforeRenderingTransparents,
        [InspectorName("Before Post Process")]
        BeforePostProcess = RenderPassEvent.BeforeRenderingPostProcessing,
        [InspectorName("After Post Process")]
        AfterPostProcess = RenderPassEvent.AfterRenderingPostProcessing,
    }
}