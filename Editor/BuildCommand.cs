using UnityEditor;
using System.Linq;
using System;

#if UNITY_EDITOR
static class BuildCommand {
    private const string IS_DEVELOPMENT_BUILD = "IS_DEVELOPMENT_BUILD";
    private const string BUILD_OPTIONS_ENV_VAR = "BUILD_OPTIONS_ENV_VAR";

    //////////////////// Main Method ///////////////////////
    public static void PerformBuild() {
        Console.WriteLine(":: Performing build");

        // Common for all Platforms
        var buildTarget = GetBuildTarget();
        var targetGroup = GetBuildTargetGroup(buildTarget);

        string xrPluginCommand = GetArgument("xrPlugin");
        if (xrPluginCommand != null) {
            var xrPlugin = GetXrPlugin(xrPluginCommand);
            foreach (XRPluginManagementSettings.Plugin plugin in Enum.GetValues(typeof(XRPluginManagementSettings.Plugin))) {
                XRPluginManagementSettings.DisablePlugin(targetGroup, plugin);
            }
            XRPluginManagementSettings.EnablePlugin(targetGroup, xrPlugin);
        }

        bool isDevelopmentBuild = IsDevelopmentType();
        HandleDevelopmentType(isDevelopmentBuild);

        var buildPath = GetBuildPath();
        var buildName = GetBuildName();
        PlayerSettings.productName = buildName;
        var buildOptions = GetBuildOptions();
        var scriptingBackend = GetScriptingBackend();
        PlayerSettings.SetScriptingBackend(targetGroup, scriptingBackend);

        Console.WriteLine($":: Ready to start build on {buildTarget}.");
        var fixedBuildPath = GetFixedBuildPath(buildTarget, buildPath, buildName);

        var buildReport = BuildPipeline.BuildPlayer(GetEnabledScenes(), fixedBuildPath, buildTarget, buildOptions);

        if (buildReport.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception($"Build ended with {buildReport.summary.result} status");

        Console.WriteLine(":: Done with build");
    }

    /////////////////////////////////////////////////////////

    ///////////////////// Utility Methods /////////////////////

    static bool IsDevelopmentType() {
        if (TryGetEnv(IS_DEVELOPMENT_BUILD, out string value)) {
            return bool.Parse(value);
        }

        Console.WriteLine($":: {IS_DEVELOPMENT_BUILD} env var not detected");
        throw new Exception($"{IS_DEVELOPMENT_BUILD} env var not detected");
    }

    private static void HandleDevelopmentType(bool isDevelopmentBuild) {
        EditorUserBuildSettings.development = isDevelopmentBuild;
        PlayerSettings.SplashScreen.show = isDevelopmentBuild;
        Debug.unityLogger.logEnabled = isDevelopmentBuild;

        Console.WriteLine(
            $":: {IS_DEVELOPMENT_BUILD} env var detected, setting \"Development Build\" to {isDevelopmentBuild}.");
    }

    static ScriptingImplementation GetScriptingBackend() {
        string scriptingBackendName = GetArgument("scriptingBackend");
        Console.WriteLine(":: Received scriptingBackend: " + scriptingBackendName);

        if (scriptingBackendName.TryConvertToEnum(out ScriptingImplementation scriptingBackend))
            return scriptingBackend;

        Console.WriteLine($":: {nameof(scriptingBackendName)} \"{scriptingBackendName}\" not defined on enum {nameof(ScriptingImplementation)}, using {nameof(ScriptingImplementation.Mono2x)} enum to build");

        return ScriptingImplementation.Mono2x;
    }

    static BuildTarget GetBuildTarget() {
        string buildTargetName = GetArgument("buildTarget");
        Console.WriteLine(":: Received buildTarget: " + buildTargetName);

        if (buildTargetName.TryConvertToEnum(out BuildTarget target))
            return target;

        Console.WriteLine($":: {nameof(buildTargetName)} \"{buildTargetName}\" not defined on enum {nameof(BuildTarget)}, using {nameof(BuildTarget.NoTarget)} enum to build");

        return BuildTarget.NoTarget;
    }

    static XRPluginManagementSettings.Plugin GetXrPlugin(string xrPlugin) {
        Console.WriteLine(":: Received xrPlugin: " + xrPlugin);

        if (xrPlugin.TryConvertToEnum(out XRPluginManagementSettings.Plugin plugin))
            return plugin;

        Console.WriteLine($":: {nameof(xrPlugin)} \"{xrPlugin}\" not defined on enum {nameof(XRPluginManagementSettings.Plugin)}, using {nameof(XRPluginManagementSettings.Plugin.Oculus)} enum to build");

        return XRPluginManagementSettings.Plugin.Oculus;
    }

    static BuildTargetGroup GetBuildTargetGroup(BuildTarget buildTarget) {
        string targetGroup;
        string platform = buildTarget.ToString();

        if (buildTarget.ToString().ToLower().Contains("standalone")) {
            targetGroup = "Standalone";
        }
        else if (buildTarget.ToString().ToLower().Contains("wsa")) {
            targetGroup = "WSA";
        }
        else {
            targetGroup = platform;
        }

        Console.WriteLine(":: Received targetGroup: " + targetGroup);

        if (targetGroup.TryConvertToEnum(out BuildTargetGroup target))
            return target;

        Console.WriteLine($":: {nameof(targetGroup)} \"{targetGroup}\" not defined on enum {nameof(BuildTargetGroup)}, using {nameof(BuildTargetGroup.Unknown)} enum to build");

        return BuildTargetGroup.Unknown;
    }

    static bool TryGetEnv(string key, out string value) {
        value = Environment.GetEnvironmentVariable(key);
        return !string.IsNullOrEmpty(value);
    }

    static string GetArgument(string name) {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++) {
            if (args[i].Contains(name)) {
                return args[i + 1];
            }
        }
        return null;
    }

    public static bool TryConvertToEnum<TEnum>(this string strEnumValue, out TEnum value) {
        if (!Enum.IsDefined(typeof(TEnum), strEnumValue)) {
            value = default;
            return false;
        }

        value = (TEnum)Enum.Parse(typeof(TEnum), strEnumValue);
        return true;
    }

    static string GetBuildPath() {
        string buildPath = GetArgument("customBuildPath");
        Console.WriteLine(":: Received customBuildPath " + buildPath);
        if (buildPath == "") {
            throw new Exception("customBuildPath argument is missing");
        }
        return buildPath;
    }

    static string GetBuildName() {
        string buildName = GetArgument("customBuildName");
        Console.WriteLine(":: Received customBuildName " + buildName);
        if (buildName == "") {
            throw new Exception("customBuildName argument is missing");
        }
        return buildName;
    }

    static string GetFixedBuildPath(BuildTarget buildTarget, string buildPath, string buildName) {
        if (buildTarget.ToString().ToLower().Contains("windows")) {
            buildName += ".exe";
        }
        else if (buildTarget == BuildTarget.Android) {
            buildName += EditorUserBuildSettings.buildAppBundle ? ".aab" : ".apk";
        }
        return buildPath + buildName;
    }

    static BuildOptions GetBuildOptions() {
        if (TryGetEnv(BUILD_OPTIONS_ENV_VAR, out string envVar)) {
            string[] allOptionVars = envVar.Split(',');
            BuildOptions allOptions = BuildOptions.None;
            BuildOptions option;
            string optionVar;
            int length = allOptionVars.Length;

            Console.WriteLine($":: Detecting {BUILD_OPTIONS_ENV_VAR} env var with {length} elements ({envVar})");

            for (int i = 0; i < length; i++) {
                optionVar = allOptionVars[i];

                if (optionVar.TryConvertToEnum(out option)) {
                    allOptions |= option;
                }
                else {
                    Console.WriteLine($":: Cannot convert {optionVar} to {nameof(BuildOptions)} enum, skipping it.");
                }
            }

            return allOptions;
        }

        return BuildOptions.None;
    }
    static string[] GetEnabledScenes() {
        return (
            from scene in EditorBuildSettings.scenes
            where scene.enabled
            where !string.IsNullOrEmpty(scene.path)
            select scene.path
        ).ToArray();
    }
    /////////////////////////////////////////////////////////////
}
#endif