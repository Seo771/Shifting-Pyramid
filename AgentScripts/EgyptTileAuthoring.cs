using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Run through Unity Pipeline, outside Assets so this does not join the game assembly.
public static class EgyptTileAuthoring
{
    const string TilePath = "Assets/Prefab/Maze_Prefab/Default_Tile.prefab";
    const string Egypt = "Assets/Assets/Egypt_Vol1/";
    const string Output = "Assets/Prefab/Maze_Prefab/EgyptTileVisuals";
    const string Evidence = "Temp/EgyptTileAuthoring";
    const string Backup = "AgentScripts/Backups/Default_Tile.before.prefab";
    const string ExitPath = "Assets/Prefab/Maze_Prefab/Exit_Tile.prefab";

    public static string Inspect()
    {
        var text = new StringBuilder();
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        text.AppendLine("Stage: " + (stage == null ? "none" : stage.assetPath + "; dirty=" + stage.scene.isDirty));
        text.AppendLine("Pipeline: " + UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline);
        foreach (var path in new[] { TilePath, Egypt + "Prefabs/SM_Wall.prefab", Egypt + "Prefabs/SM_Floor.prefab", Egypt + "Prefabs/SM_Pillar1.prefab", Egypt + "Prefabs/SM_Arch.prefab" })
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            text.AppendLine(path);
            foreach (var filter in asset.GetComponentsInChildren<MeshFilter>(true))
                text.AppendLine("  " + filter.name + ": mesh=" + filter.sharedMesh.name + "; bounds=" + filter.sharedMesh.bounds + "; vertices=" + filter.sharedMesh.vertexCount + "; scale=" + filter.transform.localScale);
            foreach (var renderer in asset.GetComponentsInChildren<Renderer>(true))
                text.AppendLine("  material " + renderer.name + ": " + string.Join(", ", renderer.sharedMaterials.Select(m => m == null ? "NULL" : m.name + " / " + m.shader.name)));
        }
        return text.ToString();
    }

    public static string Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Author the prefab outside Play Mode.");
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath == TilePath && stage.scene.isDirty)
            throw new InvalidOperationException("Default_Tile has unsaved Prefab Mode edits. Save those before authoring.");
        Directory.CreateDirectory(Evidence);
        if (!File.Exists(Evidence + "/Default_Tile.before.prefab"))
            File.Copy(TilePath, Evidence + "/Default_Tile.before.prefab");
        Directory.CreateDirectory("AgentScripts/Backups");
        if (!File.Exists(Backup)) File.Copy(TilePath, Backup);
        if (!AssetDatabase.IsValidFolder(Output))
            AssetDatabase.CreateFolder("Assets/Prefab/Maze_Prefab", "EgyptTileVisuals");

        var wall = FitMesh("SM_Wall", "Wall", Vector3.one, -Vector3.one * 0.5f);
        var wallEastWest = FitMesh("SM_Wall", "WallEastWest", Vector3.one, -Vector3.one * 0.5f, Quaternion.Euler(0f, 90f, 0f));
        var wallSouth = FitMesh("SM_Wall", "WallSouth", Vector3.one, -Vector3.one * 0.5f, Quaternion.Euler(0f, 180f, 0f));
        var wallWest = FitMesh("SM_Wall", "WallWest", Vector3.one, -Vector3.one * 0.5f, Quaternion.Euler(0f, 270f, 0f));
        var floor = FitMesh("SM_Floor", "Floor", new Vector3(10f, 0.18f, 10f), new Vector3(-5f, -0.18f, -5f));
        var ceiling = FitMesh("SM_Floor", "Ceiling", Vector3.one, -Vector3.one * 0.5f, Quaternion.Euler(180f, 0f, 0f));
        var pillar = FitMesh("SM_Pillar1", "Pillar", Vector3.one, -Vector3.one * 0.5f);
        var wallMat = MakeMaterial("Wall", "MTL_Wall", Color.white, 0.22f);
        var floorMat = MakeMaterial("Floor", "MTL_Floor", Color.white, 0.28f);
        var ceilingMat = MakeMaterial("Ceiling", "MTL_Floor", new Color(0.72f, 0.70f, 0.66f, 1f), 0.18f);
        var pillarMat = MakeMaterial("Pillar", "MTL_Pillar1", Color.white, 0.22f);

        var root = PrefabUtility.LoadPrefabContents(TilePath);
        try
        {
            string physics = PhysicsSignature(root);
            var tileView = root.GetComponent<ShiftingPyramid.Maze.View.MazeTileView>();
            string wallReferences = EditorJsonUtility.ToJson(tileView);
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                string name = filter.name.Trim();
                var renderer = filter.GetComponent<MeshRenderer>();
                if (name == "Floor") { filter.sharedMesh = floor; renderer.sharedMaterial = floorMat; }
                else if (name == "ceiling") { filter.sharedMesh = ceiling; renderer.sharedMaterial = ceilingMat; }
                else if (name.StartsWith("Wall_", StringComparison.Ordinal))
                {
                    filter.sharedMesh = name == "Wall_East" ? wallEastWest
                        : name == "Wall_West" ? wallWest : name == "Wall_South" ? wallSouth : wall;
                    renderer.sharedMaterial = wallMat;
                }
                else if (filter.transform.parent != null && filter.transform.parent.name.Trim() == "Pillar")
                { filter.sharedMesh = pillar; renderer.sharedMaterial = pillarMat; }
            }
            // The demo's irregular blocks have intentional gaps. Keep a recessed visual backing
            // inside the existing collider envelope so a closed maze wall never reveals the void.
            foreach (var side in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.Trim().StartsWith("Wall_", StringComparison.Ordinal)).ToArray())
            {
                var backing = side.Find("EgyptStoneBacking");
                if (backing == null)
                {
                    var go = new GameObject("EgyptStoneBacking", typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(side, false);
                    backing = go.transform;
                }
                bool eastWest = side.name.Trim() == "Wall_East" || side.name.Trim() == "Wall_West";
                backing.localScale = eastWest ? new Vector3(0.55f, 1f, 1f) : new Vector3(1f, 1f, 0.55f);
                backing.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                backing.GetComponent<MeshRenderer>().sharedMaterial = wallMat;
            }
            SealCorners(root);
            if (physics != PhysicsSignature(root) || wallReferences != EditorJsonUtility.ToJson(tileView))
                throw new InvalidOperationException("Authoring changed physics or maze wall references.");
            PrefabUtility.SaveAsPrefabAsset(root, TilePath);
            File.WriteAllText(Evidence + "/physics-and-wall-references.txt", "PASS: collider settings, transforms and four maze-wall references unchanged.\n" + physics);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        // Save only authored assets; do not save the user's other dirty materials or scenes.
        AssetDatabase.ImportAsset(TilePath, ImportAssetOptions.ForceUpdate);
        return Validate();
    }

    public static string FixCorners()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Author the prefab outside Play Mode.");
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath == TilePath && stage.scene.isDirty)
            throw new InvalidOperationException("Save the current Default_Tile Prefab Mode edits first.");
        var root = PrefabUtility.LoadPrefabContents(TilePath);
        try
        {
            string physics = PhysicsSignature(root);
            var view = root.GetComponent<ShiftingPyramid.Maze.View.MazeTileView>();
            string bindings = EditorJsonUtility.ToJson(view);
            SealCorners(root);
            if (physics != PhysicsSignature(root) || bindings != EditorJsonUtility.ToJson(view))
                throw new InvalidOperationException("Sealing changed physics or wall references.");
            PrefabUtility.SaveAsPrefabAsset(root, TilePath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.ImportAsset(TilePath, ImportAssetOptions.ForceUpdate);
        return Validate();
    }

    static void SealCorners(GameObject root)
    {
        var wallMaterial = AssetDatabase.LoadAssetAtPath<Material>(Output + "/EgyptTile_Wall.mat");
        var floorMaterial = AssetDatabase.LoadAssetAtPath<Material>(Output + "/EgyptTile_Floor.mat");
        var ceilingMaterial = AssetDatabase.LoadAssetAtPath<Material>(Output + "/EgyptTile_Ceiling.mat");
        var posts = root.GetComponentsInChildren<Transform>(true)
            .Where(t => t.parent != null && t.parent.name.Trim() == "Pillar" && t.GetComponent<Collider>() != null).ToArray();
        if (posts.Length != 4) throw new InvalidOperationException("Expected four original corner posts.");
        foreach (var post in posts)
        {
            // The tapered demo column does not fill its bounds at mid-height. A full-height
            // core closes the corner within the pre-existing collider; a tiny overlap hides seams.
            AddBacking(post, "EgyptCornerCore", Vector3.zero, new Vector3(1.002f, 1f, 1.002f), wallMaterial);
        }
        var floor = root.GetComponentsInChildren<Transform>(true).Single(t => t.name.Trim() == "Floor");
        AddBacking(floor, "EgyptFloorBacking", new Vector3(0f, -0.205f, 0f), new Vector3(10f, 0.05f, 10f), floorMaterial);
        var ceiling = root.GetComponentsInChildren<Transform>(true).Single(t => t.name.Trim() == "ceiling");
        AddBacking(ceiling, "EgyptCeilingBacking", Vector3.zero, new Vector3(1f, 0.55f, 1f), ceilingMaterial);
    }

    static void AddBacking(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var child = parent.Find(name);
        if (child == null)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            child = go.transform;
        }
        child.localPosition = position;
        child.localRotation = Quaternion.identity;
        child.localScale = scale;
        child.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        child.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    public static string StyleExit()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Author the prefab outside Play Mode.");
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath == ExitPath && stage.scene.isDirty)
            throw new InvalidOperationException("Save the current Exit_Tile Prefab Mode edits first.");
        const string backup = "AgentScripts/Backups/Exit_Tile.before.prefab";
        Directory.CreateDirectory("AgentScripts/Backups");
        if (!File.Exists(backup)) File.Copy(ExitPath, backup);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(TilePath);
        var sources = source.GetComponentsInChildren<MeshFilter>(true).Where(f =>
            f.name.Trim() == "Floor" || f.name.Trim() == "ceiling" || f.name.Trim().StartsWith("Wall_", StringComparison.Ordinal)
            || f.transform.parent != null && f.transform.parent.name.Trim() == "Pillar")
            .ToDictionary(f => f.name.Trim());
        var root = PrefabUtility.LoadPrefabContents(ExitPath);
        try
        {
            string physics = PhysicsSignature(root);
            var view = root.GetComponent<ShiftingPyramid.Maze.View.MazeTileView>();
            var gate = root.GetComponent<ExitGate>();
            if (gate == null || !gate.GetComponent<Collider>().isTrigger)
                throw new InvalidOperationException("ExitGate and its trigger must exist before styling.");
            string bindings = EditorJsonUtility.ToJson(view);
            string rules = EditorJsonUtility.ToJson(gate);
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!sources.TryGetValue(filter.name.Trim(), out var reference)) continue;
                filter.sharedMesh = reference.sharedMesh;
                filter.GetComponent<MeshRenderer>().sharedMaterials = reference.GetComponent<MeshRenderer>().sharedMaterials;
            }
            var wallMaterial = AssetDatabase.LoadAssetAtPath<Material>(Output + "/EgyptTile_Wall.mat");
            foreach (var side in root.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.Trim().StartsWith("Wall_", StringComparison.Ordinal)).ToArray())
            {
                bool eastWest = side.name.Trim() == "Wall_East" || side.name.Trim() == "Wall_West";
                AddBacking(side, "EgyptStoneBacking", Vector3.zero,
                    eastWest ? new Vector3(0.55f, 1f, 1f) : new Vector3(1f, 1f, 0.55f), wallMaterial);
            }
            SealCorners(root);
            if (physics != PhysicsSignature(root) || bindings != EditorJsonUtility.ToJson(view) || rules != EditorJsonUtility.ToJson(gate))
                throw new InvalidOperationException("Styling changed exit rules, trigger, physics or wall bindings.");
            VerifyWalls(root);
            PrefabUtility.SaveAsPrefabAsset(root, ExitPath);
            return "PASS: Exit_Tile styled; original ExitGate, trigger, collider data and four wall bindings preserved; colliders="
                + root.GetComponentsInChildren<Collider>(true).Length;
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void VerifyWalls(GameObject root)
    {
        var view = root.GetComponent<ShiftingPyramid.Maze.View.MazeTileView>();
        var serialized = new SerializedObject(view);
        var grid = new ShiftingPyramid.Maze.Core.MazeGrid(1, 1);
        var tile = grid.GetTile(ShiftingPyramid.Maze.Core.MazeCoordinate.Zero);
        // Exercise all exit orientations. Each case leaves exactly its outward wall open.
        foreach (ShiftingPyramid.Maze.Core.MazeDirection outward in Enum.GetValues(typeof(ShiftingPyramid.Maze.Core.MazeDirection)))
        {
            foreach (ShiftingPyramid.Maze.Core.MazeDirection side in Enum.GetValues(typeof(ShiftingPyramid.Maze.Core.MazeDirection)))
                grid.SetConnection(tile.Coordinate, side, side == outward);
            view.Apply(tile);
            int index = 0;
            foreach (var field in new[] { "northWall", "eastWall", "southWall", "westWall" })
            {
                var wall = (GameObject)serialized.FindProperty(field).objectReferenceValue;
                if (wall == null || wall.activeSelf != (index != (int)outward))
                    throw new InvalidOperationException("Exit wall switching failed: " + field);
                index++;
            }
        }
        // Validation must not persist the last test orientation into the prefab.
        foreach (ShiftingPyramid.Maze.Core.MazeDirection side in Enum.GetValues(typeof(ShiftingPyramid.Maze.Core.MazeDirection)))
            grid.SetConnection(tile.Coordinate, side, false);
        view.Apply(tile);
    }

    static Mesh FitMesh(string prefabName, string assetName, Vector3 size, Vector3 min, Quaternion? rotation = null)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Egypt + "Prefabs/" + prefabName + ".prefab");
        var source = prefab.GetComponentInChildren<MeshFilter>().sharedMesh;
        var mesh = UnityEngine.Object.Instantiate(source);
        mesh.name = "EgyptTile_" + assetName;
        var bounds = source.bounds;
        var scale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
        var turn = rotation ?? Quaternion.identity;
        var center = min + size * 0.5f;
        mesh.vertices = source.vertices.Select(v => center + turn * (min + Vector3.Scale(v - bounds.min, scale) - center)).ToArray();
        mesh.normals = source.normals.Select(n => turn * new Vector3(n.x / scale.x, n.y / scale.y, n.z / scale.z).normalized).ToArray();
        mesh.tangents = source.tangents.Select(t =>
        {
            var direction = turn * Vector3.Scale(new Vector3(t.x, t.y, t.z), scale).normalized;
            return new Vector4(direction.x, direction.y, direction.z, t.w);
        }).ToArray();
        mesh.RecalculateBounds();
        var path = Output + "/" + mesh.name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) AssetDatabase.CreateAsset(mesh, path);
        else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; }
        AssetDatabase.SaveAssetIfDirty(mesh);
        return mesh;
    }

    static Material MakeMaterial(string name, string sourceName, Color tint, float smoothness)
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>(Egypt + "3D/Materials/" + sourceName + ".mat");
        var path = Output + "/EgyptTile_" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
        material.SetColor("_BaseColor", tint);
        material.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
        material.SetFloat("_BumpScale", 0.65f);
        material.EnableKeyword("_NORMALMAP");
        material.SetTexture("_OcclusionMap", source.GetTexture("_OcclusionMap"));
        material.SetFloat("_OcclusionStrength", 0.75f);
        material.EnableKeyword("_OCCLUSIONMAP");
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", smoothness);
        // Demo wall modules are authored for one viewing side; maze walls are viewed from both.
        material.SetFloat("_Cull", 0f);
        material.doubleSidedGI = true;
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        return material;
    }

    static string PhysicsSignature(GameObject root)
    {
        return string.Join("\n", root.GetComponentsInChildren<Collider>(true).Select(c => c.transform).Select(t =>
            t.name + ": " + t.localPosition + "; " + t.localRotation + "; " + t.localScale)) + "\n"
            + string.Join("\n", root.GetComponentsInChildren<Collider>(true).Select(c => EditorJsonUtility.ToJson(c))) + "\n"
            + string.Join("\n", root.GetComponentsInChildren<MonoBehaviour>(true).Select(c => c.GetType().FullName));
    }

    public static string Validate()
    {
        var root = PrefabUtility.LoadPrefabContents(TilePath);
        try
        {
            var view = root.GetComponent<ShiftingPyramid.Maze.View.MazeTileView>();
            var serialized = new SerializedObject(view);
            foreach (var direction in new[] { "northWall", "eastWall", "southWall", "westWall" })
                if (serialized.FindProperty(direction).objectReferenceValue == null)
                    throw new InvalidOperationException("Missing " + direction);
            var grid = new ShiftingPyramid.Maze.Core.MazeGrid(1, 1);
            var tile = grid.GetTile(ShiftingPyramid.Maze.Core.MazeCoordinate.Zero);
            foreach (ShiftingPyramid.Maze.Core.MazeDirection direction in Enum.GetValues(typeof(ShiftingPyramid.Maze.Core.MazeDirection)))
            {
                grid.SetConnection(tile.Coordinate, direction, true);
                view.Apply(tile);
            }
            foreach (var field in new[] { "northWall", "eastWall", "southWall", "westWall" })
                if (((GameObject)serialized.FindProperty(field).objectReferenceValue).activeSelf)
                    throw new InvalidOperationException("Opening failed: " + field);
            foreach (ShiftingPyramid.Maze.Core.MazeDirection direction in Enum.GetValues(typeof(ShiftingPyramid.Maze.Core.MazeDirection)))
                grid.SetConnection(tile.Coordinate, direction, false);
            view.Apply(tile);
            foreach (var field in new[] { "northWall", "eastWall", "southWall", "westWall" })
                if (!((GameObject)serialized.FindProperty(field).objectReferenceValue).activeSelf)
                    throw new InvalidOperationException("Closing failed: " + field);
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
                if (r.sharedMaterial == null || !r.sharedMaterial.shader.isSupported
                    || !AssetDatabase.GetAssetPath(r.sharedMaterial).StartsWith(Output, StringComparison.Ordinal))
                    throw new InvalidOperationException("Invalid tile material: " + r.name);
            return "PASS: four walls open/close, tile materials supported; renderers="
                + root.GetComponentsInChildren<MeshRenderer>(true).Length
                + "; colliders=" + root.GetComponentsInChildren<Collider>(true).Length;
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static string PreviewBefore() { return Preview("before"); }
    public static string PreviewAfter() { return Preview("after"); }
    public static string PreviewExit() { return Preview("exit", ExitPath); }
    public static string FocusExit()
    {
        AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(ExitPath));
        return "Opened Exit_Tile in Prefab Mode.";
    }
    public static string FocusTile()
    {
        AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(TilePath));
        return "Opened Default_Tile in Prefab Mode.";
    }

    public static string VerifyMaze() { return VerifyMazeInternal(false); }
    public static string VerifyExitMaze() { return VerifyMazeInternal(true); }

    static string VerifyMazeInternal(bool includeExit)
    {
        var previousScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var root = new GameObject("EgyptTileNavigationCheck");
        ShiftingPyramid.Maze.Settings.MazeBalanceSettings settings = null;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.position = new Vector3(10000f, 0f, 10000f);
        try
        {
            var renderer = root.AddComponent<ShiftingPyramid.Maze.View.MazeRenderer>();
            var data = new SerializedObject(renderer);
            data.FindProperty("tilePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(TilePath)
                .GetComponent<ShiftingPyramid.Maze.View.MazeTileView>();
            data.ApplyModifiedPropertiesWithoutUndo();
            var grid = new ShiftingPyramid.Maze.Generation.DepthFirstMazeGenerator().Generate(5, 5, 42);
            if (includeExit)
            {
                new ShiftingPyramid.Maze.Generation.MazeExitPlacer().Place(grid);
                settings = ScriptableObject.CreateInstance<ShiftingPyramid.Maze.Settings.MazeBalanceSettings>();
                var balance = new SerializedObject(settings);
                balance.FindProperty("exitRoomPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(ExitPath)
                    .GetComponent<ShiftingPyramid.Maze.View.MazeTileView>();
                balance.ApplyModifiedPropertiesWithoutUndo();
            }
            renderer.Build(grid, settings);
            if (includeExit && root.GetComponentsInChildren<ExitGate>(true).Length != 1)
                throw new InvalidOperationException("The generated maze must contain one real Exit_Tile.");
            var surface = root.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
            surface.collectObjects = Unity.AI.Navigation.CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes;
            Physics.SyncTransforms();
            surface.BuildNavMesh();
            int before = CountCompletePaths(renderer, grid);
            var changer = new ShiftingPyramid.Maze.RuntimeChange.MazeRegionChanger();
            bool changed = changer.TryChange(grid, 3, ShiftingPyramid.Maze.Core.MazeCoordinate.Zero, 1,
                null, 0, out var origin, 42);
            if (!changed) throw new InvalidOperationException("No corridor change found in navigation check.");
            renderer.Refresh(grid);
            surface.BuildNavMesh();
            int after = CountCompletePaths(renderer, grid);
            string result = "PASS: real MazeRenderer + modified prefab + render-mesh NavMesh, start to "
                + before + "/25 cells before and " + after + "/25 after a 3x3 corridor change at " + origin
                + (includeExit ? "; includes the styled Exit_Tile." : ".");
            File.WriteAllText(Evidence + "/navigation-check.txt", result);
            return result;
        }
        finally
        {
            var surface = root.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
            if (surface != null) surface.RemoveData();
            UnityEngine.Object.DestroyImmediate(root);
            if (settings != null) UnityEngine.Object.DestroyImmediate(settings);
            if (previousScene.IsValid() && previousScene.isLoaded)
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousScene);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    static int CountCompletePaths(ShiftingPyramid.Maze.View.MazeRenderer renderer, ShiftingPyramid.Maze.Core.MazeGrid grid)
    {
        renderer.TryGetTileWorldPosition(ShiftingPyramid.Maze.Core.MazeCoordinate.Zero, out var position);
        if (!UnityEngine.AI.NavMesh.SamplePosition(position, out var start, 2f, UnityEngine.AI.NavMesh.AllAreas))
            throw new InvalidOperationException("Start floor has no NavMesh.");
        int count = 0;
        foreach (var tile in grid.Tiles)
        {
            renderer.TryGetTileWorldPosition(tile.Coordinate, out var target);
            var path = new UnityEngine.AI.NavMeshPath();
            if (!UnityEngine.AI.NavMesh.SamplePosition(target, out var end, 2f, UnityEngine.AI.NavMesh.AllAreas)
                || !UnityEngine.AI.NavMesh.CalculatePath(start.position, end.position, UnityEngine.AI.NavMesh.AllAreas, path)
                || path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete)
                throw new InvalidOperationException("Physical route failed at " + tile.Coordinate);
            count++;
        }
        return count;
    }

    static string Preview(string label, string prefabPath = TilePath)
    {
        Directory.CreateDirectory(Evidence);
        var preview = new PreviewRenderUtility();
        try
        {
            var instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
            preview.AddSingleGO(instance);
            preview.camera.fieldOfView = 85f;
            preview.camera.nearClipPlane = 0.05f;
            preview.camera.farClipPlane = 100f;
            preview.camera.transform.position = new Vector3(0f, 4.5f, 3.6f);
            preview.camera.transform.LookAt(new Vector3(0f, 2f, -3f));
            preview.ambientColor = new Color(0.2f, 0.18f, 0.15f);
            preview.lights[0].intensity = 1.6f;
            preview.lights[0].color = new Color(1f, 0.72f, 0.42f);
            preview.lights[0].transform.rotation = Quaternion.Euler(40f, 25f, 0f);
            preview.lights[1].intensity = 0.6f;
            preview.lights[1].color = new Color(0.58f, 0.68f, 1f);
            preview.lights[1].transform.rotation = Quaternion.Euler(15f, 210f, 0f);
            preview.BeginStaticPreview(new Rect(0, 0, 1280, 900));
            preview.Render(true);
            var texture = preview.EndStaticPreview();
            var path = Evidence + "/Default_Tile-" + label + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            return Path.GetFullPath(path);
        }
        finally { preview.Cleanup(); }
    }
}
