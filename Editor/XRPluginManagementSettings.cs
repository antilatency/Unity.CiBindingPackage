using System;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;

#if UNITY_EDITOR
public static class XRPluginManagementSettings {
    // ref. https://docs.unity3d.com/Packages/com.unity.xr.management@4.1/manual/EndUser.html
    public enum Plugin {
        OpenXR,
        Oculus,
        OpenVR,
        Pico
    }

    public static void EnablePlugin(BuildTargetGroup buildTargetGroup, Plugin plugin) {
        var buildTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
        var pluginsSettings = buildTargetSettings.AssignedSettings;
        var success = XRPackageMetadataStore.AssignLoader(pluginsSettings, GetLoaderName(plugin), buildTargetGroup);
        if (success) {
            Console.WriteLine($":: XR Plug-in Management: Enabled {plugin} plugin on {buildTargetGroup}");
        }
    }

    public static void DisablePlugin(BuildTargetGroup buildTargetGroup, Plugin plugin) {
        var buildTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
        var pluginsSettings = buildTargetSettings.AssignedSettings;
        var success = XRPackageMetadataStore.RemoveLoader(pluginsSettings, GetLoaderName(plugin), buildTargetGroup);
        if (success) {
            Console.WriteLine($":: XR Plug-in Management: Disabled {plugin} plugin on {buildTargetGroup}");
        }
    }

    static string GetLoaderName(Plugin plugin) => plugin switch {
        Plugin.OpenXR => "UnityEngine.XR.OpenXR.OpenXRLoader",
        Plugin.Oculus => "Unity.XR.Oculus.OculusLoader",
        Plugin.OpenVR => "Unity.XR.OpenVR.OpenVRLoader",
        Plugin.Pico => "Unity.XR.PXR.PXR_Loader",
        _ => throw new NotImplementedException()
    };
}
#endif