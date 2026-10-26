#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Release channel contract (docs/09_DEV_LIVE_RELEASES_AR.md).
/// One source tree, two Android identities that install side by side.
/// </summary>
public sealed class OctopusS1Channel
{
    public const string BaseApplicationId = "com.evento.octopuss1";
    public const string BaseProductName = "Octopus S1";

    public string Name { get; private set; }
    public string ApplicationId { get; private set; }
    public string ProductName { get; private set; }
    public bool Development { get; private set; }

    public static readonly OctopusS1Channel Dev = new OctopusS1Channel {
        Name = "Dev", ApplicationId = BaseApplicationId + ".dev", ProductName = BaseProductName + " Dev", Development = true };
    public static readonly OctopusS1Channel LiveCandidate = new OctopusS1Channel {
        Name = "LiveCandidate", ApplicationId = BaseApplicationId, ProductName = BaseProductName, Development = false };

    public static OctopusS1Channel Parse(string value)
    {
        if (value == Dev.Name) return Dev;
        if (value == LiveCandidate.Name) return LiveCandidate;
        throw new ArgumentException("Unknown or missing release channel '" + value + "'. Use Dev or LiveCandidate.");
    }
}

public sealed class OctopusS1Release
{
    public OctopusS1Channel Channel;
    public string Version;
    public int VersionCode;
    public string OutputPath;

    // Reads -octopusChannel/-octopusVersion/-octopusVersionCode/-octopusOutput,
    // falling back to OCTOPUS_* environment variables (Unity Build Automation).
    public static OctopusS1Release FromCommandLine(bool requireOutput)
    {
        var release = new OctopusS1Release {
            Channel = OctopusS1Channel.Parse(Read("octopusChannel", "OCTOPUS_CHANNEL")),
            Version = Read("octopusVersion", "OCTOPUS_VERSION"),
            OutputPath = Read("octopusOutput", "OCTOPUS_OUTPUT")
        };
        if (string.IsNullOrEmpty(release.Version) || !Regex.IsMatch(release.Version, @"^\d+\.\d+\.\d+$"))
            throw new ArgumentException("A semantic version such as 0.1.0 is required.");
        int code;
        if (!int.TryParse(Read("octopusVersionCode", "OCTOPUS_VERSION_CODE"), out code) || code < 1)
            throw new ArgumentException("A positive integer versionCode is required.");
        release.VersionCode = code;
        if (requireOutput && string.IsNullOrEmpty(release.OutputPath))
            throw new ArgumentException("-octopusOutput is required for a local build.");
        return release;
    }

    private static string Read(string argument, string environment)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "-" + argument) return args[i + 1];
        return Environment.GetEnvironmentVariable(environment);
    }
}

public static class OctopusS1Build
{
    public const string UnityVersion = "6000.6.4f1";
    private const string ScenePath = "Assets/Scenes/OctopusS1Prototype.unity";

    // Shared by the local batch build and the cloud pre-export hook. Never calls BuildPlayer.
    public static void PrepareAndroid(OctopusS1Release release)
    {
        if (Application.unityVersion != UnityVersion)
            throw new InvalidOperationException("Expected Unity " + UnityVersion + ", running " + Application.unityVersion);
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Install Android Build Support with SDK, NDK and OpenJDK.");
        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Could not switch to Android.");
        if (!File.Exists(ScenePath)) OctopusS1SceneBuilder.BuildPrototypeScene();
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        EditorUserBuildSettings.buildAppBundle = false;

        // Channel identity is applied last so scene generation cannot overwrite it.
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, release.Channel.ApplicationId);
        PlayerSettings.productName = release.Channel.ProductName;
        PlayerSettings.bundleVersion = release.Version;
        PlayerSettings.Android.bundleVersionCode = release.VersionCode;
        EditorUserBuildSettings.development = release.Channel.Development;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.connectProfiler = false;
        ApplySigning(release.Channel);
    }

    // Dev: the build host's Android debug key (documented test policy).
    // LiveCandidate: the owner's persistent keystore, supplied only through environment variables.
    private static void ApplySigning(OctopusS1Channel channel)
    {
        if (channel.Development)
        {
            PlayerSettings.Android.useCustomKeystore = false;
            return;
        }
        var keystore = Environment.GetEnvironmentVariable("OCTOPUS_LIVE_KEYSTORE");
        var storePass = Environment.GetEnvironmentVariable("OCTOPUS_LIVE_KEYSTORE_PASS");
        var alias = Environment.GetEnvironmentVariable("OCTOPUS_LIVE_KEY_ALIAS");
        var aliasPass = Environment.GetEnvironmentVariable("OCTOPUS_LIVE_KEY_PASS");
        if (string.IsNullOrEmpty(keystore) || !File.Exists(keystore) || string.IsNullOrEmpty(storePass) ||
            string.IsNullOrEmpty(alias) || string.IsNullOrEmpty(aliasPass))
            throw new InvalidOperationException("LiveCandidate requires the owner's Live keystore (OCTOPUS_LIVE_KEYSTORE, " +
                "OCTOPUS_LIVE_KEYSTORE_PASS, OCTOPUS_LIVE_KEY_ALIAS, OCTOPUS_LIVE_KEY_PASS). Debug signing is refused.");
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystore;
        PlayerSettings.Android.keystorePass = storePass;
        PlayerSettings.Android.keyaliasName = alias;
        PlayerSettings.Android.keyaliasPass = aliasPass;
    }

    // Restores the committed base identity so a build never leaves host paths or channel values in ProjectSettings.
    private static void RestoreBaseSettings()
    {
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, OctopusS1Channel.BaseApplicationId);
        PlayerSettings.productName = OctopusS1Channel.BaseProductName;
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.useCustomKeystore = false;
        PlayerSettings.Android.keystoreName = "";
        PlayerSettings.Android.keyaliasName = "";
        PlayerSettings.Android.keystorePass = "";
        PlayerSettings.Android.keyaliasPass = "";
        AssetDatabase.SaveAssets();
    }

    public static void Android()
    {
        var release = OctopusS1Release.FromCommandLine(true);
        try
        {
            PrepareAndroid(release);
            Directory.CreateDirectory(Path.GetDirectoryName(release.OutputPath));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath },
                locationPathName = release.OutputPath,
                target = BuildTarget.Android,
                options = release.Channel.Development ? BuildOptions.Development : BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Android build failed: " + report.summary.result);
            Debug.Log("Octopus S1 " + release.Channel.Name + " " + release.Version + " (" + release.VersionCode + ") built at " + release.OutputPath);
        }
        finally
        {
            RestoreBaseSettings();
        }
    }
}
#endif
