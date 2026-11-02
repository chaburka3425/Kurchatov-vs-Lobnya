using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Меню Tools → MOBA → «Создать главное меню».
// Создаёт сцену MainMenu и ставит её первой, чтобы игра начиналась с меню.
public static class MenuBuilder
{
    const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    const string GameScenePath = "Assets/Scenes/KurchatovVsLobnya.unity";

    [MenuItem("Tools/MOBA/Создать главное меню")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Assets/Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        var menu = new GameObject("MainMenu").AddComponent<MainMenu>();
        menu.leftPortrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Portrait_Kurchatov.png");
        menu.rightPortrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Portrait_Lobnya.png");
        menu.ogrFaceLeft = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Face_Left.png");
        menu.ogrFaceRight = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Face_Right.png");

        EditorSceneManager.SaveScene(scene, MenuScenePath);

        // Build Settings: сначала меню, потом карта.
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == MenuScenePath || s.path == GameScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(MenuScenePath, true));
        if (File.Exists(GameScenePath))
            scenes.Insert(1, new EditorBuildSettingsScene(GameScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();

        EditorUtility.DisplayDialog("Готово",
            "Главное меню «МПКА 2» создано!\n\nЖми Play — игра начнётся с меню.\nВ игре Esc — пауза.",
            "Поехали");
    }
}
