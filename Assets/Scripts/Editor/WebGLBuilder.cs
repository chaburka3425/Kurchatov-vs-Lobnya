using UnityEditor;
using UnityEngine;

// Сборка игры для браузера (WebGL) — потом выкладывается на GitHub Pages.
public static class WebGLBuilder
{
    const string OutDir = "Builds/WebGL";

    [MenuItem("Tools/MOBA/Собрать для браузера (WebGL)")]
    public static void Build()
    {
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-skipMap") < 0)
            MapBuilder.Build();   // свежая карта со всеми правками билдеров

        // GitHub Pages не отдаёт сжатые .gz/.br с нужными заголовками — собираем без сжатия
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.template = "PROJECT:Mpka";   // своя страница: на весь экран, удобно на телефоне
        // без этого линковщик не находит _embind_* (bindings.cpp из рантайма)
        PlayerSettings.WebGL.emscriptenArgs = "-lembind";
        // недостающие _emval_* для libAppUITextMateLib.a — заглушки в Assets/Plugins/WebGL/EmvalStubs.jslib
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Medium);

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/MainMenu.unity", MapBuilder.ScenePath },
            locationPathName = OutDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("WEBGL BUILD RESULT: " + report.summary.result + ", size " + report.summary.totalSize / (1024 * 1024) + " MB");
        if (Application.isBatchMode)
            EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }
}
