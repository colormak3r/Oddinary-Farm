using UnityEditor;
using System;
using UnityEditor.Build.Reporting;

public static class BuildScript
{
    public static string[] SCENES_PATHS = {
        "Assets/5. Scenes/Main Menu.unity",
        "Assets/5. Scenes/Main Game.unity"
    };

    public static string BUILD_PATH = "Build/Oddinary Farm.exe";

    // This method will be called via the command line
    public static void PerformBuildWin64()
    {
        PerformBuild(BuildTarget.StandaloneWindows64);
    }

    private static void PerformBuild(BuildTarget buildTarget)
    {
        // Perform the build
        BuildReport report = BuildPipeline.BuildPlayer(SCENES_PATHS, BUILD_PATH, buildTarget, BuildOptions.None);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Console.WriteLine("Build succeeded: " + summary.totalSize + " bytes");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Console.WriteLine("Build failed");
            // Optionally, exit with an error code so your CI can detect the failure.
            Environment.Exit(1);
        }
    }
}
