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

/// <summary>
/// Every project/editor setting a channel build touches, captured before and restored after
/// the build (success or failure). Keystore name/alias are read and written as raw serialized
/// values: the public setter turns "" into "{inproject}: ".
/// </summary>
public sealed class OctopusS1BuildState
{
    private string applicationId, productName, bundleVersion, keystoreName, keyaliasName, keystorePass, keyaliasPass;
    private int versionCode;
    private bool useCustomKeystore, development, allowDebugging, connectProfiler, buildAppBundle, deepProfiling;
    private AndroidSdkVersions minSdk;
    private ScriptingImplementation backend;
    private AndroidArchitecture architectures;
    private UIOrientation orientation;
    private EditorBuildSettingsScene[] scenes;

    public static OctopusS1BuildState Capture()
    {
        var player = PlayerSettingsObject();
        return new OctopusS1BuildState {
            applicationId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
            productName = PlayerSettings.productName,
            bundleVersion = PlayerSettings.bundleVersion,
            versionCode = PlayerSettings.Android.bundleVersionCode,
            useCustomKeystore = PlayerSettings.Android.useCustomKeystore,
            keystoreName = player.FindProperty("AndroidKeystoreName").stringValue,
            keyaliasName = player.FindProperty("AndroidKeyaliasName").stringValue,
            keystorePass = PlayerSettings.Android.keystorePass,
            keyaliasPass = PlayerSettings.Android.keyaliasPass,
            minSdk = PlayerSettings.Android.minSdkVersion,
            backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android),
            architectures = PlayerSettings.Android.targetArchitectures,
            orientation = PlayerSettings.defaultInterfaceOrientation,
            development = EditorUserBuildSettings.development,
            allowDebugging = EditorUserBuildSettings.allowDebugging,
            connectProfiler = EditorUserBuildSettings.connectProfiler,
            buildAppBundle = EditorUserBuildSettings.buildAppBundle,
            deepProfiling = EditorUserBuildSettings.buildWithDeepProfilingSupport,
            scenes = EditorBuildSettings.scenes
        };
    }

    public void Restore()
    {
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, applicationId);
        PlayerSettings.productName = productName;
        PlayerSettings.bundleVersion = bundleVersion;
        PlayerSettings.Android.bundleVersionCode = versionCode;
        PlayerSettings.Android.keystorePass = keystorePass;
        PlayerSettings.Android.keyaliasPass = keyaliasPass;
        PlayerSettings.Android.useCustomKeystore = useCustomKeystore;
        var player = PlayerSettingsObject();
        player.FindProperty("AndroidKeystoreName").stringValue = keystoreName;
        player.FindProperty("AndroidKeyaliasName").stringValue = keyaliasName;
        player.ApplyModifiedPropertiesWithoutUndo();
        PlayerSettings.Android.minSdkVersion = minSdk;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, backend);
        PlayerSettings.Android.targetArchitectures = architectures;
        PlayerSettings.defaultInterfaceOrientation = orientation;
        EditorUserBuildSettings.development = development;
        EditorUserBuildSettings.allowDebugging = allowDebugging;
        EditorUserBuildSettings.connectProfiler = connectProfiler;
        EditorUserBuildSettings.buildAppBundle = buildAppBundle;
        EditorUserBuildSettings.buildWithDeepProfilingSupport = deepProfiling;
        EditorBuildSettings.scenes = scenes;
    }

    /// <summary>Comparable description. Passwords are reported only as set or unset.</summary>
    public string Describe()
    {
        var sceneList = new System.Text.StringBuilder();
        foreach (var scene in scenes) sceneList.Append(scene.path).Append(scene.enabled ? "+" : "-").Append(';');
        return string.Join("|", new[] {
            applicationId, productName, bundleVersion, versionCode.ToString(), useCustomKeystore.ToString(),
            keystoreName, keyaliasName, (!string.IsNullOrEmpty(keystorePass)).ToString(), (!string.IsNullOrEmpty(keyaliasPass)).ToString(),
            minSdk.ToString(), backend.ToString(), architectures.ToString(), orientation.ToString(), development.ToString(),
            allowDebugging.ToString(), connectProfiler.ToString(), buildAppBundle.ToString(), deepProfiling.ToString(), sceneList.ToString() });
    }

    private static SerializedObject PlayerSettingsObject() =>
        new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
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
        ApplyChannelSettings(release);
    }

    // Channel identity is applied after scene generation so it cannot be overwritten.
    public static void ApplyChannelSettings(OctopusS1Release release)
    {
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, release.Channel.ApplicationId);
        PlayerSettings.productName = release.Channel.ProductName;
        PlayerSettings.bundleVersion = release.Version;
        PlayerSettings.Android.bundleVersionCode = release.VersionCode;
        EditorUserBuildSettings.development = release.Channel.Development;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.connectProfiler = false;
        EditorUserBuildSettings.buildWithDeepProfilingSupport = false;
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

    /// <summary>Runs a build step and always restores the settings it may have changed.</summary>
    public static void WithRestoredSettings(Action buildStep)
    {
        var before = OctopusS1BuildState.Capture();
        try
        {
            buildStep();
        }
        finally
        {
            before.Restore();
            AssetDatabase.SaveAssets();
        }
    }

    public static void Android()
    {
        var release = OctopusS1Release.FromCommandLine(true);
        WithRestoredSettings(() =>
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
        });
    }
}
#endif
