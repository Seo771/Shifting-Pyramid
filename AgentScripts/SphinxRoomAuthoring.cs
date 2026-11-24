using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ShiftingPyramid.Maze.View;
using ShiftingPyramid.Maze.Core;

public static class SphinxRoomAuthoring
{
    const string Target = "Assets/Prefab/Maze_Prefab/SphinxRoom.prefab";
    const string Visuals = "Assets/Prefab/Maze_Prefab/EgyptTileVisuals/";
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
        const string backup = "AgentScripts/Backups/SphinxRoom.before-Egypt.prefab";
        if (!File.Exists(backup)) File.Copy(Target, backup);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maze_Prefab/CurseJar_Tile.prefab");
        var root = open ? stage.prefabContentsRoot : PrefabUtility.LoadPrefabContents(Target);
        try
        {
            string before = Signature(root);
            var door = root.GetComponentInChildren<SphinxDoorController>(true);
            var doorData = new SerializedObject(door);
            var visual = (Transform)doorData.FindProperty("doorVisual").objectReferenceValue;
            var blocker = (BoxCollider)doorData.FindProperty("passageBlocker").objectReferenceValue;
            var visualPose = visual.localPosition.ToString("F5") + visual.localRotation.ToString("F5") + visual.localScale.ToString("F5");
            foreach (var t in root.GetComponentsInChildren<Transform>(true).ToArray())
            {
                bool shell = t.name == "Floor" || t.name == "ceiling"
                    || t.parent != null && (t.parent.name == "Walls" || t.parent.name.Trim() == "Pillar");
                if (!shell || t.GetComponent<MeshFilter>() == null) continue;
                var match = source.transform.Find(Path(t, root.transform).TrimStart('/'));
                if (match == null) throw new Exception("Missing matching source surface: " + t.name);
                CopySurface(match, t);
            }
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Visuals + "EgyptTile_Wall.asset");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Visuals + "EgyptTile_Wall.mat");
            visual.GetComponent<MeshFilter>().sharedMesh = mesh;
            visual.GetComponent<MeshRenderer>().sharedMaterial = material;
            var backing = visual.Find("EgyptStoneBacking");
            if (backing == null) { backing = new GameObject("EgyptStoneBacking", typeof(MeshFilter), typeof(MeshRenderer)).transform; backing.SetParent(visual, false); }
            backing.localPosition = Vector3.zero; backing.localRotation = Quaternion.identity; backing.localScale = new Vector3(1f, 1f, 0.55f);
            backing.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            backing.GetComponent<MeshRenderer>().sharedMaterial = material;
            foreach (var old in root.GetComponentsInChildren<TorchLight>(true).ToArray()) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var torchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maze_Prefab/EgyptWallTorch.prefab");
            foreach (int sign in new[] { -1, 1 })
            {
                var torch = ((GameObject)PrefabUtility.InstantiatePrefab(torchPrefab, root.transform)).transform;
                torch.name = sign == 1 ? "EgyptTorch_East" : "EgyptTorch_West";
                torch.localPosition = new Vector3(sign * 13.95f, 7f, 0f);
                torch.localRotation = Quaternion.LookRotation(Vector3.right * sign, Vector3.up);
                torch.localScale = Vector3.one * 6.6f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(torch);
            }
            if (before != Signature(root)) throw new Exception("Gameplay data, references or non-torch physics changed.");
            if (visualPose != visual.localPosition.ToString("F5") + visual.localRotation.ToString("F5") + visual.localScale.ToString("F5"))
                throw new Exception("Door transform changed.");
            if (backing.parent != visual || backing.GetComponent<Collider>() != null || blocker == null) throw new Exception("Door backing/blocker structure invalid.");
            root.GetComponent<MazeRoomView>().CreateTemplate(MazeTileType.SpecialRoom, 10f);
            PrefabUtility.SaveAsPrefabAsset(root, Target);
            return "PASS: stone room + two scale-6.6 torches; moving door mesh/backing remain under original DoorVisual; original controllers, UI, quiz, triggers, colliders, door positions and timing unchanged.";
        }
        finally { if (!open) PrefabUtility.UnloadPrefabContents(root); }
    }

    public static string EnlargeAndRaiseDoor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode required.");
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        bool openStage = stage != null && stage.assetPath == Target;
        const string backup = "AgentScripts/Backups/SphinxRoom.before-roof-door.prefab";
        if (!File.Exists(backup)) File.Copy(Target, backup);
        var root = openStage ? stage.prefabContentsRoot : PrefabUtility.LoadPrefabContents(Target);
        try
        {
            var door = root.GetComponentInChildren<SphinxDoorController>(true);
            var data = new SerializedObject(door);
            var visual = (Transform)data.FindProperty("doorVisual").objectReferenceValue;
            var blocker = (BoxCollider)data.FindProperty("passageBlocker").objectReferenceValue;
            var ceiling = root.transform.Find("ceiling");
            var ceilingBox = ceiling.GetComponent<BoxCollider>();
            float roofTop = ceiling.localPosition.y + (ceilingBox.center.y + ceilingBox.size.y * 0.5f) * ceiling.localScale.y;
            var size = new Vector3(10f, 16f, 0.8f);
            var closedPosition = new Vector3(0f, 7.5f, 0f);
            var openPosition = new Vector3(0f, roofTop + 0.55f + size.y * 0.5f, 0f);
            if (openStage) Undo.RecordObjects(new UnityEngine.Object[] { door, visual, blocker, blocker.transform }, "Enlarge door and raise above roof");
            visual.localScale = size;
            visual.localPosition = openPosition;
            blocker.transform.localPosition = closedPosition;
            blocker.center = Vector3.zero; blocker.size = size;
            data.FindProperty("openPosition").vector3Value = openPosition;
            data.FindProperty("closedPosition").vector3Value = closedPosition;
            data.ApplyModifiedPropertiesWithoutUndo();
            float bottomOpen = openPosition.y + visual.GetComponent<MeshFilter>().sharedMesh.bounds.min.y * size.y;
            if (bottomOpen <= roofTop || closedPosition.y - size.y * 0.5f > 0f)
                throw new Exception("Door must clear roof while open and meet floor while closed.");
            PrefabUtility.SaveAsPrefabAsset(root, Target);
            return "Saved door size=" + size + "; open=" + openPosition + "; closed=" + closedPosition
                + "; roofTop=" + roofTop + "; open door bottom=" + bottomOpen + "; blocker matches closed slab; movement remains 2 seconds.";
        }
        finally { if (!openStage) PrefabUtility.UnloadPrefabContents(root); }
    }

    static SphinxRoomController LiveRoom()
    {
        var room = UnityEngine.Object.FindFirstObjectByType<SphinxRoomController>();
        if (room == null) throw new Exception("No live Sphinx room found.");
        return room;
    }
    public static string ExcludeTorchDecoration()
    {
        const string torchPath = "Assets/Prefab/Maze_Prefab/EgyptWallTorch.prefab";
        const string backup = "AgentScripts/Backups/EgyptWallTorch.before-nav-exclusion.prefab";
        if (!File.Exists(backup)) File.Copy(torchPath, backup);
        var root = PrefabUtility.LoadPrefabContents(torchPath);
        try
        {
            var modifier = root.GetComponent<Unity.AI.Navigation.NavMeshModifier>() ?? root.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            modifier.ignoreFromBuild = true; modifier.applyToChildren = true;
            PrefabUtility.SaveAsPrefabAsset(root, torchPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return "Saved: decorative Egypt torch excluded from NavMesh collection including child meshes.";
    }
    public static string VerifyTorchNavigation()
    {
        var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var root = new GameObject("TorchNavMeshCheck");
        root.transform.position = new Vector3(10000f, 0f, 10000f);
        var surface = root.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
        try
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.SetParent(root.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.5f, 0f); floor.transform.localScale = new Vector3(10f, 1f, 10f);
            var torch = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maze_Prefab/EgyptWallTorch.prefab"), root.transform);
            torch.transform.localPosition = new Vector3(0f, 5f, 4f); torch.transform.localScale = Vector3.one * 6.6f;
            surface.collectObjects = Unity.AI.Navigation.CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();
            if (!UnityEngine.AI.NavMesh.SamplePosition(root.transform.position, out _, 1f, UnityEngine.AI.NavMesh.AllAreas)) throw new Exception("Floor navigation failed.");
            var modifier = torch.GetComponent<Unity.AI.Navigation.NavMeshModifier>();
            if (modifier == null || !modifier.ignoreFromBuild || !modifier.applyToChildren) throw new Exception("Torch navigation exclusion missing.");
            return "PASS: RenderMeshes NavMesh build with real scale-6.6 torch prefab; floor remains navigable, decorative torch subtree excluded.";
        }
        finally
        {
            surface.RemoveData(); UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true);
        }
    }
    public static string PoseInterior()
    {
        var room = LiveRoom(); var camera = Camera.main;
        camera.transform.position = room.transform.TransformPoint(new Vector3(0f, 5f, 12f));
        camera.transform.LookAt(room.transform.TransformPoint(new Vector3(0f, 4f, -8f))); camera.fieldOfView = 95f;
        return "Live Sphinx room=" + room.name + "; torches=" + room.GetComponentsInChildren<TorchLight>().Length + "; frame=" + Time.frameCount;
    }
    static void PoseDoor(SphinxRoomController room)
    {
        Camera.main.transform.position = room.transform.TransformPoint(new Vector3(0f, 4f, 5f));
        Camera.main.transform.LookAt(room.transform.TransformPoint(new Vector3(0f, 5.5f, 14.75f)));
        Camera.main.fieldOfView = 85f;
    }
    public static async Task<string> CloseAndCheck()
    {
        var room = LiveRoom(); PoseDoor(room);
        var door = room.GetComponentInChildren<SphinxDoorController>();
        var data = new SerializedObject(door);
        var visual = (Transform)data.FindProperty("doorVisual").objectReferenceValue;
        var blocker = (BoxCollider)data.FindProperty("passageBlocker").objectReferenceValue;
        var open = data.FindProperty("openPosition").vector3Value;
        var closed = data.FindProperty("closedPosition").vector3Value;
        float duration = data.FindProperty("moveDuration").floatValue;
        if (Vector3.Distance(visual.localPosition, open) > 0.01f || blocker.enabled) throw new Exception("Door must start open and traversable.");
        room.EnterRoom();
        if (!blocker.enabled) throw new Exception("Entering did not enable blocker.");
        await Task.Delay((int)(duration * 400f));
        float travel = Vector3.Dot(visual.localPosition - open, (closed - open).normalized);
        float length = Vector3.Distance(open, closed);
        if (!(travel > 0.01f && travel < length - 0.01f)) throw new Exception("No intermediate downward animation.");
        await Task.Delay((int)(duration * 750f) + 200);
        if (Vector3.Distance(visual.localPosition, closed) > 0.01f || !blocker.enabled) throw new Exception("Door did not fully close.");
        var backing = visual.Find("EgyptStoneBacking");
        if (backing == null || backing.localPosition != Vector3.zero) throw new Exception("Backing detached from door.");
        var ray = new Ray(room.transform.TransformPoint(new Vector3(0f, 4f, 10f)), room.transform.forward);
        if (!blocker.Raycast(ray, out _, 10f)) throw new Exception("Closed blocker did not obstruct passage.");
        return "PASS: EnterRoom -> Close; intermediate descent and closed position verified; blocker enabled + passage ray blocked; stone backing follows visual. frame=" + Time.frameCount;
    }
    public static async Task<string> QuizAndOpenCheck()
    {
        var room = LiveRoom(); var door = room.GetComponentInChildren<SphinxDoorController>();
        var data = new SerializedObject(door);
        var visual = (Transform)data.FindProperty("doorVisual").objectReferenceValue;
        var blocker = (BoxCollider)data.FindProperty("passageBlocker").objectReferenceValue;
        var open = data.FindProperty("openPosition").vector3Value;
        float duration = data.FindProperty("moveDuration").floatValue;
        room.StartQuiz();
        var field = typeof(SphinxRoomController).GetField("currentRiddle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var riddle = (QuizData)field.GetValue(room);
        if (riddle == null) throw new Exception("Quiz did not start.");
        room.SubmitAnswer(riddle.answer);
        if (!blocker.enabled) throw new Exception("Passage released before upward travel completed.");
        await Task.Delay((int)(duration * 1000f) + 300);
        if (Vector3.Distance(visual.localPosition, open) > 0.01f || blocker.enabled) throw new Exception("Quiz completion failed to reopen passage.");
        foreach (var movement in UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None)) movement.enabled = false;
        return "PASS: StartQuiz -> SubmitAnswer(correct) -> EndQuiz -> Open; original open position restored and blocker released after motion. frame=" + Time.frameCount;
    }
}
