#if UNITY_EDITOR
using System;
using UnityEditor;

// Configure UBA pre-export method: OctopusS1CloudBuild.PreExport
// UBA owns the export/build step. Do not recursively call BuildPlayer here.
public static class OctopusS1CloudBuild
{
    public static void PreExport()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("This cloud target must select Android.");
        OctopusS1Build.PrepareAndroid();
        EditorUserBuildSettings.development = true;
        EditorUserBuildSettings.allowDebugging = false;
    }
}
#endif
