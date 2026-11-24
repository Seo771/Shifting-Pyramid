using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class EgyptTorchAuthoring
{
    const string Source = "Assets/Assets/Egypt_Vol1/Prefabs/SM_Marshall.prefab";
    const string Target = "Assets/Prefab/Maze_Prefab/EgyptWallTorch.prefab";

    public static string Inspect()
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Source);
        var text = new StringBuilder();
        foreach (var t in asset.GetComponentsInChildren<Transform>(true))
            text.AppendLine(t.name + ": pos=" + t.localPosition + "; scale=" + t.localScale + "; components="
                + string.Join(",", t.GetComponents<Component>().Select(c => c == null ? "MISSING" : c.GetType().Name)));
        foreach (var f in asset.GetComponentsInChildren<MeshFilter>(true))
            text.AppendLine("Mesh " + f.name + ": " + f.sharedMesh.bounds);
        foreach (var r in asset.GetComponentsInChildren<Renderer>(true))
            text.AppendLine("Material " + r.name + ": " + string.Join(",", r.sharedMaterials.Select(m => m == null ? "NULL" : m.name + "/" + m.shader.name + "/supported=" + m.shader.isSupported)));
        foreach (var ps in asset.GetComponentsInChildren<ParticleSystem>(true))
            text.AppendLine("Particles " + ps.name + ": loop=" + ps.main.loop + "; autoplay=" + ps.main.playOnAwake);
        return text.ToString();
    }

    public static string Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Create the torch outside Play Mode.");
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("EgyptWallTorch");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            var body = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source), root.transform, false);
            if (PrefabUtility.IsPartOfPrefabInstance(body))
                PrefabUtility.UnpackPrefabInstance(body, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            body.name = "EgyptTorchModel";
            body.transform.localPosition = new Vector3(0f, 0f, -0.11f);
            // Placement's +Z faces the wall. Turn the demo flame toward the corridor.
            body.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            foreach (var collider in body.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
            foreach (var t in body.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);

            var lamp = body.GetComponentInChildren<Light>(true);
            lamp.transform.SetParent(root.transform, true);
            lamp.name = "TorchFire"; // Existing TorchLight expects this direct child.
            lamp.type = LightType.Point;
            lamp.lightmapBakeType = LightmapBakeType.Realtime;
            lamp.intensity = 2f;
            lamp.range = 16f;
            lamp.shadows = LightShadows.None;
            var light = root.AddComponent<TorchLight>();
            light.lightColor = lamp.color;
            light.lightIntensity = 2f;
            light.lightRange = 16f;
            light.flickerAmount = 0.2f;
            foreach (var ps in body.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                bool smoke = ps.name == "Smoke";
                var main = ps.main;
                main.playOnAwake = true;
                main.loop = true;
                main.prewarm = true;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.startLifetime = new ParticleSystem.MinMaxCurve(smoke ? 0.7f : 0.35f, smoke ? 1.2f : 0.65f);
                main.startSize3D = false;
                main.startSize = new ParticleSystem.MinMaxCurve(smoke ? 0.6f : 0.45f, smoke ? 1f : 0.8f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, smoke ? 0.9f : 1.1f);
                main.startColor = Color.white;
                main.gravityModifier = 0f;
                main.maxParticles = smoke ? 12 : 48;
                ps.transform.localPosition = new Vector3(0f, smoke ? 0.52f : 0.37f, 0.04f);
                var emission = ps.emission;
                emission.rateOverTime = smoke ? 5f : 28f;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 8f;
                shape.radius = 0.04f;
                shape.rotation = new Vector3(-90f, 0f, 0f);
                var velocity = ps.velocityOverLifetime; velocity.enabled = false;
                var force = ps.forceOverLifetime; force.enabled = false;
                var noise = ps.noise; noise.enabled = false;
                var size = ps.sizeOverLifetime;
                size.enabled = true;
                size.separateAxes = false;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, smoke ? 1.5f : 0f));
                var gradient = new Gradient();
                gradient.SetKeys(smoke
                    ? new[] { new GradientColorKey(new Color(0.18f, 0.15f, 0.12f), 0f), new GradientColorKey(new Color(0.08f, 0.07f, 0.06f), 1f) }
                    : new[] { new GradientColorKey(new Color(1f, 0.85f, 0.25f), 0f), new GradientColorKey(new Color(1f, 0.28f, 0.025f), 0.65f) },
                    new[] { new GradientAlphaKey(smoke ? 0.12f : 0.9f, 0f), new GradientAlphaKey(0f, 1f) });
                var color = ps.colorOverLifetime; color.enabled = true; color.color = gradient;
                var sheet = ps.textureSheetAnimation;
                sheet.enabled = !smoke;
                sheet.numTilesX = 2; sheet.numTilesY = 2;
                sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial = ParticleMaterial(smoke);
            }
            PrefabUtility.SaveAsPrefabAsset(root, Target);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(scene);
        }
        var saved = AssetDatabase.LoadAssetAtPath<GameObject>(Target);
        if (saved.GetComponent<TorchLight>() == null || saved.transform.Find("TorchFire") == null
            || saved.GetComponentsInChildren<ParticleSystem>(true).Length != 2
            || saved.GetComponentsInChildren<Light>(true).Length != 1
            || saved.GetComponentsInChildren<Collider>(true).Length != 0)
            throw new InvalidOperationException("Saved torch components are invalid.");
        foreach (var renderer in saved.GetComponentsInChildren<Renderer>(true))
            if (renderer.sharedMaterial == null || !renderer.sharedMaterial.shader.isSupported)
                throw new InvalidOperationException("Unsupported torch material: " + renderer.name);
        return "PASS: EgyptWallTorch saved; flame + smoke, one realtime flickering light, supported materials and no blocking colliders.";
    }

    static Material ParticleMaterial(bool smoke)
    {
        const string shaderPath = "Assets/Prefab/Maze_Prefab/EgyptTileVisuals/EgyptTorchParticles.shader";
        AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceUpdate);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
        if (shader == null || ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("Torch particle shader has errors.");
        string path = "Assets/Prefab/Maze_Prefab/EgyptTileVisuals/EgyptTorch_" + (smoke ? "Smoke" : "Fire") + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(smoke
            ? "Assets/Assets/Egypt_Vol1/FX_Procedural fire_Hovl Studio/FireSphere1.png"
            : "Assets/Assets/Object/Piloto Studio/Textures/FireQuad_Stylized.png"));
        material.SetColor("_BaseColor", smoke ? Color.white : new Color(2f, 2f, 2f, 1f));
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)(smoke ? UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : UnityEngine.Rendering.BlendMode.One));
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        return material;
    }

    public static string ApplyToMainScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Apply to MainScene outside Play Mode.");
        const string path = "Assets/Scenes/MainScene.unity";
        const string backup = "AgentScripts/Backups/MainScene.before-EgyptTorch.unity";
        Directory.CreateDirectory("AgentScripts/Backups");
        if (!File.Exists(backup)) File.Copy(path, backup);
        var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            var placement = scene.GetRootGameObjects().SelectMany(go =>
                go.GetComponentsInChildren<ShiftingPyramid.Game.Torch.MazeTorchPlacementController>(true)).Single();
            var serialized = new SerializedObject(placement);
            serialized.FindProperty("torchPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Target);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "PASS: MainScene torchPrefab now uses EgyptWallTorch; placement counts, spacing, height and scale retained.";
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded)
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }

    public static string Preview()
    {
        var preview = new PreviewRenderUtility();
        try
        {
            var root = new GameObject("EgyptTorchPreview");
            preview.AddSingleGO(root);
            var tile = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maze_Prefab/Default_Tile.prefab"), root.transform, false);
            var torch = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Target), root.transform, false);
            torch.transform.localScale = Vector3.one * 2.5f;
            torch.transform.position = new Vector3(0f, 7f, -4.5f);
            torch.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            foreach (var ps in torch.GetComponentsInChildren<ParticleSystem>(true))
                ps.Simulate(2f, false, true, true);
            preview.camera.fieldOfView = 50f;
            preview.camera.nearClipPlane = 0.05f;
            preview.camera.farClipPlane = 60f;
            preview.camera.transform.position = new Vector3(1.6f, 7.3f, 1.2f);
            preview.camera.transform.LookAt(new Vector3(0f, 7.5f, -4.5f));
            preview.ambientColor = new Color(0.12f, 0.1f, 0.08f);
            preview.lights[0].color = new Color(1f, 0.7f, 0.4f);
            preview.lights[0].intensity = 0.6f;
            preview.lights[0].transform.rotation = Quaternion.Euler(20f, 10f, 0f);
            preview.lights[1].intensity = 0.15f;
            preview.BeginStaticPreview(new Rect(0, 0, 1100, 850));
            preview.Render(true);
            var texture = preview.EndStaticPreview();
            Directory.CreateDirectory("Temp/EgyptTorchAuthoring");
            var path = "Temp/EgyptTorchAuthoring/EgyptWallTorch.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            return Path.GetFullPath(path);
        }
        finally { preview.Cleanup(); }
    }

    public static string VerifyPlacement()
    {
        var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var root = new GameObject("EgyptTorchPlacementCheck");
        ShiftingPyramid.Game.Torch.MazeTorchPlacementController placement = null;
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        try
        {
            var renderer = root.AddComponent<ShiftingPyramid.Maze.View.MazeRenderer>();
            var rendererData = new SerializedObject(renderer);
            rendererData.FindProperty("tilePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Maze_Prefab/Default_Tile.prefab")
                .GetComponent<ShiftingPyramid.Maze.View.MazeTileView>();
            rendererData.ApplyModifiedPropertiesWithoutUndo();
            var bootstrap = root.AddComponent<ShiftingPyramid.Maze.View.MazeTestBootstrapper>();
            var bootData = new SerializedObject(bootstrap);
            bootData.FindProperty("mazeRenderer").objectReferenceValue = renderer;
            bootData.ApplyModifiedPropertiesWithoutUndo();
            var grid = new ShiftingPyramid.Maze.Generation.DepthFirstMazeGenerator().Generate(5, 5, 42);
            typeof(ShiftingPyramid.Maze.View.MazeTestBootstrapper).GetField("currentGrid", flags).SetValue(bootstrap, grid);
            renderer.Build(grid, null);
            placement = root.AddComponent<ShiftingPyramid.Game.Torch.MazeTorchPlacementController>();
            var data = new SerializedObject(placement);
            data.FindProperty("bootstrapper").objectReferenceValue = bootstrap;
            data.FindProperty("torchPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Target);
            data.ApplyModifiedPropertiesWithoutUndo();
            typeof(ShiftingPyramid.Game.Torch.MazeTorchPlacementController).GetMethod("OnEnable", flags).Invoke(placement, null);
            bootstrap.Updates.Apply(grid, ShiftingPyramid.Maze.Core.MazeChangeSet.Full(grid));
            int before = CheckTorches(root, grid);
            var snapshot = grid.Clone();
            if (!new ShiftingPyramid.Maze.RuntimeChange.MazeRegionChanger().TryChange(grid, 3,
                ShiftingPyramid.Maze.Core.MazeCoordinate.Zero, 1, null, 0, out _, 42))
                throw new InvalidOperationException("No test corridor change found.");
            renderer.Refresh(grid);
            bootstrap.Updates.Apply(grid, ShiftingPyramid.Maze.Core.MazeChangeSet.Between(snapshot, grid));
            int after = CheckTorches(root, grid);
            string result = "PASS: actual torch placement/update pipeline; " + before + " torches before and " + after
                + " after maze change, all on closed walls; realtime lights + live simulated flame/smoke; no colliders.";
            File.WriteAllText("Temp/EgyptTorchAuthoring/placement-check.txt", result);
            return result;
        }
        finally
        {
            if (placement != null)
                typeof(ShiftingPyramid.Game.Torch.MazeTorchPlacementController).GetMethod("OnDisable", flags).Invoke(placement, null);
            UnityEngine.Object.DestroyImmediate(root);
            if (previous.IsValid() && previous.isLoaded) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    static int CheckTorches(GameObject root, ShiftingPyramid.Maze.Core.MazeGrid grid)
    {
        var torches = root.GetComponentsInChildren<TorchLight>(true);
        if (torches.Length == 0) throw new InvalidOperationException("No torch was placed.");
        foreach (var torch in torches)
        {
            var parts = torch.name.Split('_');
            var coordinate = new ShiftingPyramid.Maze.Core.MazeCoordinate(int.Parse(parts[1]), int.Parse(parts[2]));
            var side = (ShiftingPyramid.Maze.Core.MazeDirection)Enum.Parse(typeof(ShiftingPyramid.Maze.Core.MazeDirection), parts[3]);
            if (grid.GetTile(coordinate).IsOpen(side)) throw new InvalidOperationException("Torch covers an open passage.");
            if (torch.GetComponentsInChildren<Collider>(true).Length != 0) throw new InvalidOperationException("Torch blocks navigation.");
            var lamp = torch.GetComponentInChildren<Light>(true);
            if (lamp == null || lamp.lightmapBakeType != LightmapBakeType.Realtime) throw new InvalidOperationException("Missing realtime light.");
            foreach (var ps in torch.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Simulate(1f, false, true, true);
                if (ps.particleCount == 0) throw new InvalidOperationException("Particle system did not emit: " + ps.name);
            }
        }
        return torches.Length;
    }

    public static string Focus()
    {
        AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(Target));
        return "Opened EgyptWallTorch prefab.";
    }
}
