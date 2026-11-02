using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Меню Tools → MOBA → «Собрать для телефона (WebGL)».
// Собирает браузерную версию игры в папку Builds/WebGL рядом с проектом.
public static class WebBuilder
{
    const string OutDir = "Builds/WebGL";

    [MenuItem("Tools/MOBA/Собрать для телефона (WebGL)")]
    public static void Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            EditorUtility.DisplayDialog("Нет модуля WebGL",
                "Сначала поставь модуль WebGL Build Support:\n\nUnity Hub → Установки → шестерёнка у версии → Добавить модули → WebGL Build Support.\n\nПотом перезапусти Unity.",
                "Понятно");
            return;
        }

        var scenes = new List<string>();
        foreach (var sc in EditorBuildSettings.scenes)
            if (sc.enabled && File.Exists(sc.path)) scenes.Add(sc.path);
        if (scenes.Count == 0)
        {
            EditorUtility.DisplayDialog("Нет сцен", "Сначала создай карту и главное меню через Tools → MOBA.", "Ок");
            return;
        }

        // Настройки, чтобы игру можно было выложить по простой ссылке
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;  // распаковка в браузере, без настроек сервера
        PlayerSettings.WebGL.dataCaching = false;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);
        PlayerSettings.runInBackground = true;

        if (Directory.Exists(OutDir)) Directory.Delete(OutDir, true);

        var opts = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = OutDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
        {
            float mb = report.summary.totalSize / (1024f * 1024f);
            EditorUtility.DisplayDialog("Готово!",
                $"Браузерная версия собрана ({mb:0.0} МБ) в папку {OutDir}.\n\nНапиши Claude «выложи», и он сделает ссылку для телефона.",
                "Ура");
            EditorUtility.RevealInFinder(OutDir);
        }
        else
        {
            EditorUtility.DisplayDialog("Ошибка сборки",
                $"Сборка не получилась: {report.summary.result}.\nОткрой Console и пришли скриншот Claude.",
                "Ок");
        }
    }
}
