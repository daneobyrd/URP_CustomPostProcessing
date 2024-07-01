using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomPostProcessing.UniversalRP.Editor
{
    public static class CustomPostProcessVolumeQuery
    {
        // ReSharper disable Unity.PerformanceAnalysis
        // Disable performance analysis as these functions are editor-only
        
        // Based on VolumeManager.Update excerpts
        /// <seealso cref="VolumeManager.Update(UnityEngine.Rendering.VolumeStack, UnityEngine.Transform, UnityEngine.LayerMask)"/>
        private static Volume[] GetSceneVolumes()
        {
            var camera = Camera.main;
            if (camera is null)
            {
                return null;
            }

            // Start with full list
            var volumes = VolumeManager.instance
                                       .GetVolumes(camera.GetUniversalAdditionalCameraData().volumeLayerMask)
                                       .Where(v => v.sharedProfile != null).ToList();

            // Traverse all volumes
            foreach (var volume in volumes)
            {
                if (volume == null)
                {
                    volumes.Remove(volume);
                    continue;
                }
#if UNITY_EDITOR
                // Exclude volumes that aren't in the scene that is currently displayed in the scene view
                if (!IsVolumeRenderedByCamera(volume, camera))
                {
                    volumes.Remove(volume);
                }
#endif
            }

            return volumes.ToArray();
        }

        private static bool IsVolumeRenderedByCamera(Volume volume, Camera camera)
        {
            var method = typeof(VolumeManager).GetMethod("IsVolumeRenderedByCamera", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            var parameters = new object[] {volume, camera};
            return method != null && (bool) method.Invoke(typeof(VolumeManager), parameters);
        }

        private static bool IsComponentActiveForCamera<T>(Camera camera) where T : VolumeComponent
        {
            var universalCameraData = camera.GetUniversalAdditionalCameraData();
            var volumeLayerMask = universalCameraData.volumeLayerMask;

            return VolumeManager.instance.IsComponentActiveInMask<T>(volumeLayerMask);
        }

        public static bool TryGetVolumeProfilesInScene(out List<VolumeProfile> sceneVolumeProfiles)
        {
            var volumes = GetSceneVolumes();
            sceneVolumeProfiles = new List<VolumeProfile>();

            if (volumes == null)
            {
                return false;
            }

            foreach (var volume in volumes)
            {
                if (volume == null)
                    continue;

                // Skip disabled volumes and volumes without any data or weight
                if (!volume.enabled || volume.profile is null || volume.weight <= 0f)
                    continue;

                if (!sceneVolumeProfiles.Contains(volume.profile))
                {
                    sceneVolumeProfiles.Add(volume.profile);
                }
            }

            return sceneVolumeProfiles.Count > 0;
        }

        private static List<Type> GetAllCustomPPTypesInScene(List<VolumeProfile> volumeProfiles)
        {
            var allPostProcessTypesInScene = new List<Type>();

            if (volumeProfiles == null)
            {
                return allPostProcessTypesInScene;
            }

            foreach (var profile in volumeProfiles)
            {
                var profileComponents = new List<CustomPostProcessVolumeComponent>();

                if (!profile.TryGetAllSubclassOf(typeof(CustomPostProcessVolumeComponent), profileComponents)) continue;

                var profileComponentTypes = profileComponents.ObjectsToTypeList(allowDuplicateTypes: false);

                foreach (var type in profileComponentTypes)
                {
                    if (allPostProcessTypesInScene.Contains(type))
                        continue;

                    allPostProcessTypesInScene.Add(type);
                }
            }

            return allPostProcessTypesInScene;
        }

        private static bool TryGetAllCustomPPTypesInScene(this List<VolumeProfile> volumeProfiles, out List<Type> allPostProcessTypesInScene)
        {
            allPostProcessTypesInScene = GetAllCustomPPTypesInScene(volumeProfiles);

            return allPostProcessTypesInScene is {Count: > 0};
        }

        public static bool TryGetAllCustomPPComponentsInScene(this List<VolumeProfile> volumeProfiles, out List<CustomPostProcessVolumeComponent> ppVolumeComponentsInScene)
        {
            var allProfilePPVolumeComponents = new List<CustomPostProcessVolumeComponent>();

            if (!volumeProfiles.TryGetAllCustomPPTypesInScene(out var ppComponentTypesInScene))
            {
                ppVolumeComponentsInScene = null;
                return false;
            }

            // Get blended VolumeComponent from VolumeStack using types found in the scene.
            foreach (var type in ppComponentTypesInScene)
            {
                allProfilePPVolumeComponents.Add(CustomPostProcessCore.GetCustomPostProcessComponent(type));
            }

            ppVolumeComponentsInScene = allProfilePPVolumeComponents;
            return ppVolumeComponentsInScene.Count > 0;
        }

        public static bool TryGetAllCustomPPComponentsInScene(out List<CustomPostProcessVolumeComponent> ppVolumeComponentsInScene)
        {
            TryGetVolumeProfilesInScene(out List<VolumeProfile> volumeProfiles);
            return volumeProfiles.TryGetAllCustomPPComponentsInScene(out ppVolumeComponentsInScene);
        }
        
        // Editor only, used for debug view in CustomPostProcessRenderFeatureEditor
        public static void PopulateSceneDebugCustomPPComponentList(this CustomPostProcessVolumeComponentList list)
        {
            if (!TryGetVolumeProfilesInScene(out List<VolumeProfile> volumeProfiles))
            {
                return;
            }

            foreach (var profile in volumeProfiles)
            {
                var profileComponents = new List<CustomPostProcessVolumeComponent>();

                if (!profile.TryGetAllSubclassOf(typeof(CustomPostProcessVolumeComponent), profileComponents))
                    continue;

                list.AddRange(profileComponents);
            }
        }
        
        /*
        /// <seealso cref="UniversalRenderPipelineVolumeDebugSettings"/>
        /// <seealso cref="VolumeDebugSettings{T}"/>
        public class VolumeComponentGUIUtility
        {
            private Camera camera = Camera.main;

            /// <summary>Selected camera volume stack.</summary>
            public VolumeStack selectedCameraVolumeStack
            {
                get
                {
                    Camera cam = camera;
                    if (cam == null)
                        return null;

                    var additionalCameraData = camera.GetComponent<UniversalAdditionalCameraData>();
                    if (additionalCameraData == null)
                        return null;

                    var stack = additionalCameraData.volumeStack;
                    return stack ?? VolumeManager.instance.stack;
                }
            }

            /// <summary>Selected camera volume layer mask.</summary>
            public LayerMask selectedCameraLayerMask => camera != null ? camera.GetUniversalAdditionalCameraData().volumeLayerMask : (LayerMask) 0;

            /// <summary>Selected camera volume position.</summary>
            public Vector3 selectedCameraPosition => camera != null ? camera.transform.position : Vector3.zero;

            float[] weights = null;

            float ComputeWeight(Volume volume, Vector3 triggerPos)
            {
                if (volume == null) return 0;

                var profile = volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;

                if (!volume.gameObject.activeInHierarchy) return 0;
                if (!volume.enabled || profile == null || volume.weight <= 0f) return 0;
                if (!profile.TryGet(out CustomPostProcessVolumeComponent component)) return 0;
                if (!component.active) return 0;

                float weight = Mathf.Clamp01(volume.weight);
                if (!volume.isGlobal)
                {
                    var colliders = volume.GetComponents<Collider>();

                    // Find closest distance to volume, 0 means it's inside it
                    float closestDistanceSqr = float.PositiveInfinity;
                    foreach (var collider in colliders)
                    {
                        if (!collider.enabled)
                            continue;

                        var closestPoint = collider.ClosestPoint(triggerPos);
                        var d = (closestPoint - triggerPos).sqrMagnitude;

                        if (d < closestDistanceSqr)
                            closestDistanceSqr = d;
                    }

                    float blendDistSqr = volume.blendDistance * volume.blendDistance;
                    if (closestDistanceSqr > blendDistSqr)
                        weight = 0f;
                    else if (blendDistSqr > 0f)
                        weight *= 1f - (closestDistanceSqr / blendDistSqr);
                }

                return weight;
            }

            private Volume[] volumes;
            private CustomPostProcessVolumeComponent[] volumeComponents;

            /// <summary>Get an array of volumes on the <see cref="selectedCameraLayerMask"/></summary>
            /// <returns>An array of volumes sorted by influence.</returns>
            public Volume[] GetVolumes()
            {
                return VolumeManager.instance.GetVolumes(selectedCameraLayerMask)
                                    .Where(v => v.sharedProfile != null)
                                    .Reverse().ToArray();
            }

            /*
            /// <summary>
            /// Refreshes the volumes, fetches the stored volumes on the panel
            /// </summary>
            /// <param name="newVolumes">The list of <see cref="Volume"/> to refresh</param>
            /// <returns>If the volumes have been refreshed</returns>
            public bool RefreshVolumes(Volume[] newVolumes)
            {
                bool ret = false;
                if (volumes == null || !newVolumes.SequenceEqual(volumes))
                {
                    volumes     = (Volume[])newVolumes.Clone();
                    savedStates = GetStates();
                    ret         = true;
                }
                else
                {
                    var newStates = GetStates();
                    if (savedStates == null || ChangedStates(newStates))
                    {
                        savedStates = newStates;
                        ret         = true;
                    }
                }

                var triggerPos = selectedCameraPosition;
                weights = new float[volumes.Length];
                for (int i = 0; i < volumes.Length; i++)
                    weights[i] = ComputeWeight(volumes[i], triggerPos);

                return ret;
            }
            #1#

            /// <summary>
            /// Obtains the volume weight
            /// </summary>
            /// <param name="volume"><see cref="Volume"/></param>
            /// <returns>The weight of the volume</returns>
            public float GetVolumeWeight(Volume volume)
            {
                if (weights == null)
                    return 0;

                float total = 0f, weight = 0f;
                for (int i = 0; i < volumes.Length; i++)
                {
                    weight =  weights[i];
                    weight *= 1f - total;
                    total  += weight;

                    if (volumes[i] == volume)
                        return weight;
                }

                return 0f;
            }

            /// <summary>
            /// Return if the <see cref="Volume"/> has influence
            /// </summary>
            /// <param name="volume"><see cref="Volume"/> to check the influence</param>
            /// <returns>If the volume has influence</returns>
            public bool VolumeHasInfluence(Volume volume)
            {
                if (weights == null)
                    return false;

                int index = Array.IndexOf(volumes, volume);
                if (index == -1)
                    return false;

                return weights[index] != 0f;
            }
        }
        */
    }
}