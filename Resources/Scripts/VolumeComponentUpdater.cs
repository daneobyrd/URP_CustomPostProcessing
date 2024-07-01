// Original from: https://tryfinally.dev/unity-custom-srp-volume-components

using System;
using System.Collections.Generic;
using static System.Reflection.BindingFlags;

namespace UnityEngine.Rendering.Universal
{
    /// <summary> An interface for volume components that can be updated. </summary>
    public interface IUpdatableVolumeComponent
    {
        /// <summary> Called every frame to update the volume component. </summary>
        void Update();

        /// <summary> Indicates whether the component should be updated in edit mode. </summary>
        bool ExecuteInEditMode => true;
    }

    /// <summary>
    /// Updates volume components that implement <see cref="IUpdatableVolumeComponent"/>.
    /// Make sure there's a single instance of this component somewhere in the scene, eg. on the main camera.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class VolumeComponentUpdater : MonoBehaviour
    {
        VolumeStack previousStack;
        Dictionary<Type, VolumeComponent> cachedVolumeStackComponents;

        [SerializeField] private Camera _MainCamera;

        private Camera mainCamera
        {
            get
            {
                if (!_MainCamera || _MainCamera == null)
                {
                    if (!TryGetComponent(out _MainCamera))
                    {
                        _MainCamera = Camera.main;
                    }
                }

#if UNITY_EDITOR
                else
                {
                    if (!_MainCamera.CompareTag("MainCamera"))
                    {
                        Debug.LogWarning($"{_MainCamera} is not tagged MainCamera");
                    }
                }
#endif

                return _MainCamera;
            }
        }

        private UniversalAdditionalCameraData AdditionalCameraData => mainCamera.GetUniversalAdditionalCameraData();
        
        // Executes after gameplay/animation update, but before rendering.
        void LateUpdate()
        {
            // This is fast now!
            // https://blog.unity.com/technology/new-performance-improvements-in-unity-2020-2
            var main = Camera.main;

            if (!main)
                return;

            // in HDRP, get the VolumeStack from the HDCamera associated with the main camera
            // var stack = UnityEngine.Rendering.HighDefinition.HDCamera
            // .GetOrCreate(camera)
            // .volumeStack;

            // in URP, obtain the VolumeStack from the UniversalAdditionalCameraData instead
            var stack = AdditionalCameraData.volumeStack;

            if (stack == null)
                return;

            // invalidate cache if stack changed
            if (stack != previousStack)
                cachedVolumeStackComponents = null;

            previousStack = stack;

            // get components from the VolumeStack using reflection because the API is private :(
            // we cache the result to avoid doing this every frame
            // (note: this is not future-proof and is likely to break in future versions of Unity)
            cachedVolumeStackComponents ??= typeof(VolumeStack)
                                            .GetField("components", NonPublic | Instance)
                                            ?.GetValue(stack) as Dictionary<Type, VolumeComponent>;

            if (cachedVolumeStackComponents == null)
            {
                return;
            }

            // update components that implement IUpdatableVolumeComponent
            foreach (var component in cachedVolumeStackComponents.Values)
            {
                if (component is not IUpdatableVolumeComponent updatable)
                {
                    continue;
                }
                
                if (updatable.ExecuteInEditMode || Application.isPlaying)
                    updatable.Update();
            }
        }
    }
}