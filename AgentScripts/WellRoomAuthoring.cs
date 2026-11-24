using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ShiftingPyramid.Maze.View;
using ShiftingPyramid.Maze.Core;

public static class WellRoomAuthoring
{
    const string Target = "Assets/Prefab/Maze_Prefab/Well_Room.prefab";
    static string Path(Transform t, Transform root) { return t == root ? "" : Path(t.parent, root) + "/" + t.name; }
    static string Signature(GameObject root)
    {
        var b = new StringBuilder();
        foreach (var c in root.GetComponentsInChildren<Component>(true))
        {
            if (!(c is Collider) && !(c is Rigidbody) && !(c is MonoBehaviour)) continue;
            if (c.GetComponentInParent<TorchLight>(true) != null) continue;
            var t = c.transform;
            b.AppendLine(Path(t, root.transform) + ";" + t.localPosition.ToString("F5") + ";" + t.localRotation.ToString("F5") + ";" + t.localScale.ToString("F5") + ";" + EditorJsonUtility.ToJson(c));
        }
        return b.ToString();
    }
    static void CopySurface(Transform source, Transform target)
    {
        target.GetComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
        var r = target.GetComponent<MeshRenderer>(); var sr = source.GetComponent<MeshRenderer>();
        r.sharedMaterials = sr.sharedMaterials; r.enabled = sr.enabled;
        foreach (Transform child in source)
        {
            if (!child.name.StartsWith("Egypt", StringComparison.Ordinal)) continue;
            if (child.GetComponentsInChildren<Collider>(true).Length != 0) throw new Exception("Visual must not add colliders.");
            var old = target.Find(child.name);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var clone = UnityEngine.Object.Instantiate(child.gameObject, target, false); clone.name = child.name;
        }
    }
    public static string Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode required.");
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        bool open = stage != null && stage.assetPath == Target;
        Directory.CreateDirectory("AgentScripts/Backups");
        const string backup = "AgentScripts/Backups/Well_Room.before-Egypt.prefab";
        if (!File.Exists(backup)) File.Copy(Target, backup);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maze_Prefab/CurseJar_Tile.prefab");
        var root = open ? stage.prefabContentsRoot : PrefabUtility.LoadPrefabContents(Target);
        try
        {
            string before = Signature(root);
            foreach (var t in root.GetComponentsInChildren<Transform>(true).ToArray())
            {
                bool shell = t.name == "Floor" || t.name == "ceiling"
                    || t.parent != null && (t.parent.name == "Walls" || t.parent.name.Trim() == "Pillar");
                if (!shell || t.GetComponent<MeshFilter>() == null) continue;
                var match = source.transform.Find(Path(t, root.transform).TrimStart('/'));
                if (match == null) throw new Exception("Missing reference surface: " + t.name);
                CopySurface(match, t);
            }
            foreach (var old in root.GetComponentsInChildren<TorchLight>(true).ToArray()) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var torchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maze_Prefab/EgyptWallTorch.prefab");
            var directions = new[] { Vector3.right, Vector3.left, Vector3.back };
            var names = new[] { "East", "West", "South" };
            for (int i = 0; i < directions.Length; i++)
            {
                var torch = ((GameObject)PrefabUtility.InstantiatePrefab(torchPrefab, root.transform)).transform;
                torch.name = "EgyptTorch_" + names[i];
                torch.localPosition = directions[i] * 13.95f + Vector3.up * 7f;
                torch.localRotation = Quaternion.LookRotation(directions[i], Vector3.up);
                torch.localScale = Vector3.one * 6.6f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(torch);
            }
            if (before != Signature(root)) throw new Exception("Well, entrance, collider or gameplay settings changed.");
            Check(root);
            PrefabUtility.SaveAsPrefabAsset(root, Target);
            return "PASS: repeated stone room shell; 3 Egypt torches scale 6.6; original well, heal values, entrance and non-torch physics unchanged.";
        }
        finally { if (!open) PrefabUtility.UnloadPrefabContents(root); }
    }
    static string Check(GameObject root)
    {
        root.GetComponent<MazeRoomView>().CreateTemplate(MazeTileType.SpecialRoom, 10f);
        if (root.GetComponentsInChildren<HealingWell>(true).Length != 1) throw new Exception("Expected one well.");
        var torches = root.GetComponentsInChildren<TorchLight>(true);
        if (torches.Length != 3) throw new Exception("Expected three torches.");
        foreach (var torch in torches)
        {
            if (Mathf.Abs(torch.transform.localScale.x - 6.6f) > 0.01f || torch.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new Exception("Wrong torch scale or blocking collider.");
            var nav = torch.GetComponent<Unity.AI.Navigation.NavMeshModifier>();
            if (nav == null || !nav.ignoreFromBuild || !nav.applyToChildren) throw new Exception("Torch NavMesh exclusion missing.");
            foreach (var ps in torch.GetComponentsInChildren<ParticleSystem>(true))
            { ps.Simulate(1f, false, true, true); if (ps.particleCount == 0) throw new Exception("No flame/smoke particles."); }
        }
        return "PASS: entrance template, one healing well, three scale-6.6 nonblocking torches, emitting fire/smoke and NavMesh exclusions.";
    }
    public static string Validate()
    {
        var root = PrefabUtility.LoadPrefabContents(Target);
        try { return Check(root); } finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    public static string PoseLive()
    {
        var well = UnityEngine.Object.FindFirstObjectByType<HealingWell>();
        bool generated = well != null;
        if (well == null)
        {
            var preview = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Target), new Vector3(1000f, 0f, 1000f), Quaternion.identity);
            preview.name = "TemporaryWellRoomReview";
            well = preview.GetComponentInChildren<HealingWell>();
        }
        var room = well.GetComponentInParent<MazeRoomView>();
        var camera = Camera.main;
        camera.transform.position = room.transform.TransformPoint(new Vector3(0f, 5f, 10f));
        camera.transform.LookAt(room.transform.TransformPoint(new Vector3(0f, 4f, -8f)));
        camera.fieldOfView = 95f;
        return "Live well room=" + room.name + "; generatedByMaze=" + generated + "; torches=" + room.GetComponentsInChildren<TorchLight>().Length + "; frame=" + Time.frameCount;
    }
}
