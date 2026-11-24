using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ShiftingPyramid.Game.Torch;

public static class TorchSizeAdjustment
{
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode required.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/MainScene.unity");
        if (!scene.isLoaded) throw new Exception("MainScene must be loaded.");
        const string roomPath = "Assets/Prefab/Maze_Prefab/CurseJar_Tile.prefab";
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        bool useOpenStage = stage != null && stage.assetPath == roomPath;
        Directory.CreateDirectory("AgentScripts/Backups");
        foreach (var path in new[] { scene.path, roomPath })
        {
            var backup = "AgentScripts/Backups/" + System.IO.Path.GetFileName(path) + ".before-large-torches";
            if (!File.Exists(backup)) File.Copy(path, backup);
        }
        var placement = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MazeTorchPlacementController>(true)).Single();
        Undo.RecordObject(placement, "Enlarge torches to double size");
        var data = new SerializedObject(placement);
        data.FindProperty("torchScale").floatValue = 6.6f;
        data.FindProperty("wallInset").floatValue = 0.8f;
        data.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        var root = useOpenStage ? stage.prefabContentsRoot : PrefabUtility.LoadPrefabContents(roomPath);
        try
        {
            var torches = root.GetComponentsInChildren<TorchLight>(true);
            if (torches.Length != 2) throw new Exception("Expected two room torches.");
            foreach (var torch in torches)
            {
                torch.transform.localScale = Vector3.one * 6.6f;
                var pos = torch.transform.localPosition;
                pos.x = Mathf.Sign(pos.x) * 13.95f;
                torch.transform.localPosition = pos;
                PrefabUtility.RecordPrefabInstancePropertyModifications(torch.transform);
            }
            PrefabUtility.SaveAsPrefabAsset(root, roomPath);
        }
        finally { if (!useOpenStage) PrefabUtility.UnloadPrefabContents(root); }
        return "Saved corridor + two curse-room torches: 3.3 -> 6.6; offset 0.3 further from wall; light settings unchanged.";
    }
    public static string VerifyLive()
    {
        var torches = UnityEngine.Object.FindObjectsByType<TorchLight>(FindObjectsSortMode.None)
            .Where(t => t.name.StartsWith("Torch_") || t.name.StartsWith("EgyptTorch_")).ToArray();
        if (torches.Length < 17) throw new Exception("Missing generated or curse-room torches.");
        foreach (var torch in torches)
        {
            if (Mathf.Abs(torch.transform.localScale.x - 6.6f) > 0.01f) throw new Exception("Unexpected scale: " + torch.name);
            if (torch.GetComponentsInChildren<Collider>().Length != 0) throw new Exception("Blocking torch.");
            foreach (var renderer in torch.GetComponentsInChildren<MeshRenderer>())
            {
                // All eight world AABB corners must remain behind the inner wall face.
                var b = renderer.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    if (Vector3.Dot(corner - torch.transform.position, torch.transform.forward) > 0.31f)
                        throw new Exception("Torch mesh extends into the wall: " + torch.name + "/" + renderer.name);
                }
            }
        }
        return "PASS: " + torches.Length + " live torches at scale 6.6, nonblocking and clear of inner wall face; frame=" + Time.frameCount;
    }
}
