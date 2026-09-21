using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
using UnityEngine;

public static class StarterBuild
{
    public static void BuildWindows()
    {
        StarterExampleBuilder.Create();
        const string tone = "Assets/Res/Starter/VerificationTone.wav";
        if (!File.Exists(tone))
        {
            using (var writer = new BinaryWriter(File.Create(tone)))
            {
                int samples = 22050;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
                writer.Write(44100); writer.Write(88200); writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
                for (int i = 0; i < samples; i++) writer.Write((short)(Math.Sin(2 * Math.PI * 440 * i / 44100) * 3000));
            }
            AssetDatabase.ImportAsset(tone);
        }
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(tone), settings.DefaultGroup);
        entry.address = "Audios/VerificationTone";
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult addressables);
        if (!string.IsNullOrEmpty(addressables.Error)) throw new InvalidOperationException(addressables.Error);
        string output = Path.GetFullPath("../TestResults/Windows/FeatherVerification.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        string product = PlayerSettings.productName;
        var backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone);
        try
        {
            // Keep verification saves separate from the game's existing player data.
            PlayerSettings.productName = "FeatherFrameworkVerification";
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { StarterExampleBuilder.MenuPath, StarterExampleBuilder.GamePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Player build failed: " + report.summary.result);
            Debug.Log("FEATHER_BUILD_PASS " + output);
        }
        finally
        {
            PlayerSettings.productName = product;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, backend);
            AssetDatabase.SaveAssets();
        }
    }
}
