#if UNITY_EDITOR
using System;
using UnityEditor;

// Configure UBA pre-export method: OctopusS1CloudBuild.PreExport
// Set OCTOPUS_CHANNEL, OCTOPUS_VERSION and OCTOPUS_VERSION_CODE as target environment variables.
// UBA owns the export/build step. Do not recursively call BuildPlayer here.
public static class OctopusS1CloudBuild
{
    public static void PreExport()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("This cloud target must select Android.");
        OctopusS1Build.PrepareAndroid(OctopusS1Release.FromCommandLine(false));
    }
}
#endif
