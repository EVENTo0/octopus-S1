using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

/// <summary>
/// Review R3: a channel build must leave every setting it touches exactly as it found it,
/// after success and after failure, including keystore name/alias and diagnostic flags.
/// These tests change settings only in memory and restore them; no real keystore is used.
/// </summary>
public class BuildStateTests
{
    private static readonly string[] LiveVariables =
        { "OCTOPUS_LIVE_KEYSTORE", "OCTOPUS_LIVE_KEYSTORE_PASS", "OCTOPUS_LIVE_KEY_ALIAS", "OCTOPUS_LIVE_KEY_PASS" };
    private string fakeKeystore;
    private string before;

    [SetUp]
    public void Capture()
    {
        before = OctopusS1BuildState.Capture().Describe();
        foreach (var name in LiveVariables) Environment.SetEnvironmentVariable(name, null);
    }

    [TearDown]
    public void Cleanup()
    {
        foreach (var name in LiveVariables) Environment.SetEnvironmentVariable(name, null);
        if (fakeKeystore != null && File.Exists(fakeKeystore)) File.Delete(fakeKeystore);
        Assert.AreEqual(before, OctopusS1BuildState.Capture().Describe(), "Settings must be unchanged after each test.");
    }

    private static OctopusS1Release Release(OctopusS1Channel channel, int code) =>
        new OctopusS1Release { Channel = channel, Version = "9.8.7", VersionCode = code, OutputPath = "unused.apk" };

    [Test]
    public void DevSettingsAreAppliedThenRestored()
    {
        OctopusS1Build.WithRestoredSettings(() =>
        {
            OctopusS1Build.ApplyChannelSettings(Release(OctopusS1Channel.Dev, 4321));
            Assert.AreEqual("com.evento.octopuss1.dev", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            Assert.AreEqual("Octopus S1 Dev", PlayerSettings.productName);
            Assert.AreEqual(4321, PlayerSettings.Android.bundleVersionCode);
            Assert.IsTrue(EditorUserBuildSettings.development);
            Assert.IsFalse(PlayerSettings.Android.useCustomKeystore);
        });
        Assert.AreEqual(before, OctopusS1BuildState.Capture().Describe());
    }

    [Test]
    public void LiveKeystoreAndFlagsAreRestoredAfterSuccess()
    {
        fakeKeystore = Path.GetTempFileName();
        Environment.SetEnvironmentVariable("OCTOPUS_LIVE_KEYSTORE", fakeKeystore);
        Environment.SetEnvironmentVariable("OCTOPUS_LIVE_KEYSTORE_PASS", "test-only");
        Environment.SetEnvironmentVariable("OCTOPUS_LIVE_KEY_ALIAS", "test-alias");
        Environment.SetEnvironmentVariable("OCTOPUS_LIVE_KEY_PASS", "test-only");
        OctopusS1Build.WithRestoredSettings(() =>
        {
            OctopusS1Build.ApplyChannelSettings(Release(OctopusS1Channel.LiveCandidate, 4322));
            Assert.AreEqual("com.evento.octopuss1", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            Assert.IsTrue(PlayerSettings.Android.useCustomKeystore);
            Assert.AreEqual(fakeKeystore.Replace('\\', '/'), PlayerSettings.Android.keystoreName.Replace('\\', '/'));
            Assert.AreEqual("test-alias", PlayerSettings.Android.keyaliasName);
            Assert.IsFalse(EditorUserBuildSettings.development);
            Assert.IsFalse(EditorUserBuildSettings.allowDebugging);
            Assert.IsFalse(EditorUserBuildSettings.connectProfiler);
        });
        var after = OctopusS1BuildState.Capture().Describe();
        Assert.AreEqual(before, after);
        StringAssert.DoesNotContain(Path.GetFileName(fakeKeystore), after, "The host keystore path must not remain in project settings.");
    }

    [Test]
    public void SettingsAreRestoredWhenTheBuildFails()
    {
        Assert.Throws<InvalidOperationException>(() => OctopusS1Build.WithRestoredSettings(() =>
        {
            OctopusS1Build.ApplyChannelSettings(Release(OctopusS1Channel.Dev, 4323));
            EditorUserBuildSettings.connectProfiler = true;
            throw new InvalidOperationException("simulated build failure");
        }));
        Assert.AreEqual(before, OctopusS1BuildState.Capture().Describe());
    }

    [Test]
    public void LiveWithoutOwnerKeystoreIsRefusedAndRestored()
    {
        var error = Assert.Throws<InvalidOperationException>(() => OctopusS1Build.WithRestoredSettings(() =>
            OctopusS1Build.ApplyChannelSettings(Release(OctopusS1Channel.LiveCandidate, 4324))));
        StringAssert.Contains("Debug signing is refused", error.Message);
        Assert.AreEqual(before, OctopusS1BuildState.Capture().Describe());
    }

    [Test]
    public void UnknownChannelIsRejected()
    {
        Assert.Throws<ArgumentException>(() => OctopusS1Channel.Parse("Live"));
        Assert.Throws<ArgumentException>(() => OctopusS1Channel.Parse(null));
    }
}
