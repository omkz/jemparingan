using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Newtonsoft.Json;
using System.Security.Cryptography;

public static class QuestVrBootstrap
{
    public static object Run()
    {
        Directory.CreateDirectory("docs/validation/quest-vr-v1");
        Directory.CreateDirectory("Logs");
        File.WriteAllBytes("Logs/quest-vr-desktop-before.unity", File.ReadAllBytes(
            "Assets/Jemparingan/Scenes/JemparinganPlayableEnvironment_v05.unity"));
        string hash;
        using (var sha = SHA256.Create())
            hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes("Logs/quest-vr-desktop-before.unity"))).Replace("-", "").ToLowerInvariant();
        string normalizedHash;
        using (var sha = SHA256.Create())
            normalizedHash = BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText("Logs/quest-vr-desktop-before.unity").Replace("\r\n", "\n")))).Replace("-", "").ToLowerInvariant();
        if (!File.Exists("docs/validation/quest-vr-v1/desktop-baseline.json"))
            File.WriteAllText("docs/validation/quest-vr-v1/desktop-baseline.json", JsonConvert.SerializeObject(new { sha256 = hash, sha256Lf = normalizedHash }, Formatting.Indented));
        foreach (var target in new[] { NamedBuildTarget.Android, NamedBuildTarget.Standalone })
        {
            var defines = PlayerSettings.GetScriptingDefineSymbols(target).Split(';').Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (!defines.Contains("META_XR_SDK")) defines.Add("META_XR_SDK");
            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
        }
        AssetDatabase.Refresh();
        return new { version = Application.unityVersion, defines = "META_XR_SDK: Android + Standalone", desktopBackup = "Logs/quest-vr-desktop-before.unity" };
    }
}
