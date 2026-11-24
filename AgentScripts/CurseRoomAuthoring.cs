using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ShiftingPyramid.Maze.View;
using ShiftingPyramid.Maze.Core;

public static class CurseRoomAuthoring
{
    const string Target = "Assets/Prefab/Maze_Prefab/CurseJar_Tile.prefab";
    const string Visuals = "Assets/Prefab/Maze_Prefab/EgyptTileVisuals/";
    const string TorchPath = "Assets/Prefab/Maze_Prefab/EgyptWallTorch.prefab";
    static bool OldTorch(Transform t) { return t.GetComponentInParent<TorchLight>(true) != null; }
    static string Path(Transform t, Transform root) { return t == root ? "" : Path(t.parent, root) + "/" + t.name; }
    static string Signature(GameObject root)
    {
        var b = new StringBuilder();
        foreach (var c in root.GetComponentsInChildren<Collider>(true).Where(c => !OldTorch(c.transform)))
            b.AppendLine(Path(c.transform, root.transform) + ";" + c.transform.localPosition.ToString("F5") + ";" + c.transform.localRotation.ToString("F5") + ";" + c.transform.localScale.ToString("F5") + ";" + EditorJsonUtility.ToJson(c));
        foreach (var m in root.GetComponentsInChildren<MonoBehaviour>(true).Where(m => !OldTorch(m.transform)))
            b.AppendLine(Path(m.transform, root.transform) + ";" + m.GetType().FullName + ";" + EditorJsonUtility.ToJson(m));
        return b.ToString();
    }
    static Mesh Mesh(string name) { return AssetDatabase.LoadAssetAtPath<Mesh>(Visuals + "EgyptTile_" + name + ".asset"); }
    static Material Mat(string name) { return AssetDatabase.LoadAssetAtPath<Material>(Visuals + "EgyptTile_" + name + ".mat"); }
    static Mesh Repeat(Mesh source, string name, int countX, int countZ)
    {
        var combines = new CombineInstance[countX * countZ];
        bool floor = name == "Floor";
        float width = floor ? 10f : 1f;
        int i = 0;
        for (int z = 0; z < countZ; z++) for (int x = 0; x < countX; x++)
            combines[i++] = new CombineInstance {
                mesh = source,
                transform = Matrix4x4.TRS(new Vector3(width * ((x + 0.5f) / countX - 0.5f), 0f,
                    countZ == 1 ? 0f : width * ((z + 0.5f) / countZ - 0.5f)), Quaternion.identity,
                    new Vector3(1f / countX, 1f, 1f / countZ))
            };
        var result = new Mesh { name = "EgyptCurseRoom_" + name };
        result.CombineMeshes(combines, true, true); result.RecalculateBounds();
        string assetPath = Visuals + result.name + ".asset";
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        if (saved == null) { AssetDatabase.CreateAsset(result, assetPath); saved = result; }
        else { EditorUtility.CopySerialized(result, saved); UnityEngine.Object.DestroyImmediate(result); }
        AssetDatabase.SaveAssetIfDirty(saved);
        return saved;
    }
    static Transform Visual(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Vector3 scale)
    {
        var t = parent.Find(name);
        if (t == null) { t = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)).transform; t.SetParent(parent, false); }
        t.localPosition = pos; t.localRotation = Quaternion.identity; t.localScale = scale;
        t.GetComponent<MeshFilter>().sharedMesh = mesh; t.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return t;
    }
    public static string Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath == Target) throw new InvalidOperationException("Close CurseJar_Tile Prefab Mode first.");
        Directory.CreateDirectory("AgentScripts/Backups");
        const string backup = "AgentScripts/Backups/CurseJar_Tile.before-Egypt.prefab";
        if (!File.Exists(backup)) File.Copy(Target, backup);
        var north = Repeat(Mesh("Wall"), "NorthWall", 3, 1);
        var south = Repeat(Mesh("WallSouth"), "SouthWall", 3, 1);
        var floor = Repeat(Mesh("Floor"), "Floor", 3, 3);
        var ceiling = Repeat(Mesh("Ceiling"), "Ceiling", 3, 3);
        var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        var root = PrefabUtility.LoadPrefabContents(Target);
        try
        {
            var before = Signature(root);
            var room = root.GetComponent<MazeRoomView>();
            room.CreateTemplate(MazeTileType.CurseJarRoom, 10f);
            foreach (var f in root.GetComponentsInChildren<MeshFilter>(true).ToArray())
            {
                var t = f.transform; var r = f.GetComponent<MeshRenderer>();
                if (t.parent != null && t.parent.name == "Walls")
                {
                    bool full = t.name == "Wall(1)" || t.name == "Wall(2)" || t.name == "Wall(3)";
                    f.sharedMesh = full ? (t.name == "Wall(2)" ? north : south) : Mesh("Wall");
                    r.sharedMaterial = Mat("Wall");
                    Visual(t, "EgyptStoneBacking", cube, Mat("Wall"), Vector3.zero, new Vector3(1f, 1f, 0.55f));
                }
                else if (t.name == "Floor")
                {
                    f.sharedMesh = floor; r.sharedMaterial = Mat("Floor");
                    Visual(t, "EgyptFloorBacking", cube, Mat("Floor"), new Vector3(0f, -0.205f, 0f), new Vector3(10f, 0.05f, 10f));
                }
                else if (t.name == "ceiling")
                {
                    f.sharedMesh = ceiling; r.sharedMaterial = Mat("Ceiling");
                    Visual(t, "EgyptCeilingBacking", cube, Mat("Ceiling"), Vector3.zero, new Vector3(1f, 0.55f, 1f));
                }
                else if (t.parent != null && t.parent.name.Trim() == "Pillar")
                {
                    r.enabled = false;
                    Visual(t, "EgyptPillar", Mesh("Pillar"), Mat("Pillar"), new Vector3(0f, 0.225f, 0f), new Vector3(1f, 1.5f, 1f));
                    Visual(t, "EgyptCornerCore", cube, Mat("Wall"), new Vector3(0f, 0.225f, 0f), new Vector3(1.002f, 1.5f, 1.002f));
                }
            }
            foreach (var old in root.GetComponentsInChildren<TorchLight>(true).Where(t => t.name != "EgyptTorch_East" && t.name != "EgyptTorch_West").ToArray())
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TorchPath);
            foreach (int sign in new[] { -1, 1 })
            {
                string name = sign == 1 ? "EgyptTorch_East" : "EgyptTorch_West";
                var torch = root.transform.Find(name);
                if (torch == null) torch = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform)).transform;
                torch.name = name; torch.localPosition = new Vector3(sign * 13.95f, 7f, 0f);
                torch.localRotation = Quaternion.LookRotation(Vector3.right * sign, Vector3.up);
                torch.localScale = Vector3.one * 6.6f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(torch);
            }
            if (before != Signature(root)) throw new InvalidOperationException("Room colliders, jar or gameplay component data changed.");
            room.CreateTemplate(MazeTileType.CurseJarRoom, 10f);
            Check(root);
            PrefabUtility.SaveAsPrefabAsset(root, Target);
            return "PASS: 30x30 room styled with repeated stone modules; 2 nested Egypt torches at scale 6.6; all non-torch colliders, jar data and entrance settings unchanged.";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static string Check(GameObject root)
    {
        root.GetComponent<MazeRoomView>().CreateTemplate(MazeTileType.CurseJarRoom, 10f);
        if (root.GetComponentsInChildren<CurseJar>(true).Length != 1) throw new Exception("Expected one CurseJar.");
        var torches = root.GetComponentsInChildren<TorchLight>(true);
        if (torches.Length != 2) throw new Exception("Expected two torches.");
        foreach (var torch in torches)
        {
            if (torch.GetComponentsInChildren<Collider>(true).Length != 0) throw new Exception("Torch blocks movement.");
            if (torch.transform.Find("TorchFire").GetComponent<Light>() == null) throw new Exception("Missing torch light.");
            foreach (var ps in torch.GetComponentsInChildren<ParticleSystem>(true))
            { ps.Simulate(1f, false, true, true); if (ps.particleCount == 0) throw new Exception("No particles: " + ps.name); }
        }
        return "PASS: entrance template, one jar, two nonblocking torches with emitting fire/smoke.";
    }
    public static string Validate()
    {
        var root = PrefabUtility.LoadPrefabContents(Target);
        try { return Check(root); } finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    public static string Preview()
    {
        var preview = new PreviewRenderUtility();
        try
        {
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Target));
            preview.AddSingleGO(root);
            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true)) ps.Simulate(1f, false, true, true);
            preview.camera.fieldOfView = 95f; preview.camera.nearClipPlane = 0.1f; preview.camera.farClipPlane = 100f;
            preview.camera.transform.position = new Vector3(0f, 5f, 13f);
            preview.camera.transform.LookAt(new Vector3(0f, 4f, -8f));
            preview.ambientColor = new Color(0.15f, 0.13f, 0.11f);
            preview.lights[0].intensity = 0.9f; preview.lights[0].color = new Color(1f, 0.7f, 0.42f);
            preview.lights[0].transform.rotation = Quaternion.Euler(40f, 25f, 0f);
            preview.lights[1].intensity = 0.25f;
            preview.BeginStaticPreview(new Rect(0, 0, 1280, 900)); preview.Render(true);
            var texture = preview.EndStaticPreview();
            Directory.CreateDirectory("Temp/CurseRoomAuthoring");
            var path = "Temp/CurseRoomAuthoring/CurseJar_Tile.png";
            File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            return System.IO.Path.GetFullPath(path);
        }
        finally { preview.Cleanup(); }
    }
}
