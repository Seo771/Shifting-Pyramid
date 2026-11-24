using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MainSceneLighting
{
    const string MainPath = "Assets/Scenes/MainScene.unity";
    const string TorchPath = "Assets/Prefab/Maze_Prefab/EgyptWallTorch.prefab";
    const string ProfilePath = "Assets/Data/MainMazeAtmosphere.asset";
    const string Evidence = "Temp/MainSceneLighting";
    [Serializable] public class SceneRecord { public string path; public bool active; public bool loaded; }
    [Serializable] public class Layout { public SceneRecord[] scenes; public string prefab; }
    [Serializable] public class EnvironmentState
    {
        public AmbientMode ambientMode;
        public Color sky, equator, ground, fogColor, lightColor;
        public float ambientIntensity, lightIntensity, fogDensity;
        public bool fog;
    }

    static T InScene<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(true)).FirstOrDefault();
    }

    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before authoring.");
        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline == null) throw new InvalidOperationException("URP must be active.");
        Directory.CreateDirectory(Evidence);
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.scene.isDirty) throw new InvalidOperationException("Save the current prefab edits first.");
        var layout = new Layout
        {
            scenes = EditorSceneManager.GetSceneManagerSetup().Select(s => new SceneRecord { path = s.path, active = s.isActive, loaded = s.isLoaded }).ToArray(),
            prefab = stage == null ? "" : stage.assetPath
        };
        if (!File.Exists(Evidence + "/editor-layout.json"))
            File.WriteAllText(Evidence + "/editor-layout.json", JsonUtility.ToJson(layout));
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Save unsaved scenes before the lighting review.");
        const string backup = "AgentScripts/Backups/MainScene.before-lighting.unity";
        Directory.CreateDirectory("AgentScripts/Backups");
        if (!File.Exists(backup)) File.Copy(MainPath, backup);
        EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Single);
        var scene = SceneManager.GetActiveScene();
        var sun = InScene<Light>(scene);
        if (!File.Exists(Evidence + "/before.json"))
            File.WriteAllText(Evidence + "/before.json", JsonUtility.ToJson(new EnvironmentState
            {
                ambientMode = RenderSettings.ambientMode, sky = RenderSettings.ambientSkyColor,
                equator = RenderSettings.ambientEquatorColor, ground = RenderSettings.ambientGroundColor,
                ambientIntensity = RenderSettings.ambientIntensity, fog = RenderSettings.fog,
                fogDensity = RenderSettings.fogDensity, fogColor = RenderSettings.fogColor,
                lightColor = sun.color, lightIntensity = sun.intensity
            }));
        if (!pipeline.supportsHDR)
        {
            pipeline.supportsHDR = true;
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssetIfDirty(pipeline);
        }
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, ProfilePath); }
        var tone = Effect<Tonemapping>(profile); tone.mode.Override(TonemappingMode.ACES);
        var bloom = Effect<Bloom>(profile);
        bloom.intensity.Override(0.22f); bloom.threshold.Override(1.15f); bloom.scatter.Override(0.55f); bloom.clamp.Override(8f);
        var color = Effect<ColorAdjustments>(profile);
        color.postExposure.Override(-0.10f); color.contrast.Override(8f); color.saturation.Override(-6f);
        var balance = Effect<WhiteBalance>(profile); balance.temperature.Override(7f);
        var vignette = Effect<Vignette>(profile); vignette.intensity.Override(0.18f); vignette.smoothness.Override(0.45f);
        foreach (var effect in profile.components) { EditorUtility.SetDirty(effect); AssetDatabase.SaveAssetIfDirty(effect); }
        EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile);

        var atmosphere = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "MazeAtmosphere");
        if (atmosphere == null) atmosphere = new GameObject("MazeAtmosphere");
        var volume = atmosphere.GetComponent<Volume>() ?? atmosphere.AddComponent<Volume>();
        volume.isGlobal = true; volume.priority = 20f; volume.weight = 1f; volume.sharedProfile = profile;
        foreach (var camera in scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Camera>(true)))
        {
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true; data.volumeLayerMask |= 1;
            camera.allowHDR = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(data);
            PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
        }
        var placement = InScene<ShiftingPyramid.Game.Torch.MazeTorchPlacementController>(scene);
        var placementData = new SerializedObject(placement);
        placementData.FindProperty("torchScale").floatValue = 6.6f;
        placementData.FindProperty("lightRange").floatValue = 20f;
        placementData.ApplyModifiedPropertiesWithoutUndo();
        SetMood();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        var torch = PrefabUtility.LoadPrefabContents(TorchPath);
        try
        {
            var light = torch.GetComponent<TorchLight>();
            light.lightIntensity = 4f; light.lightRange = 20f;
            var lamp = torch.GetComponentInChildren<Light>(true);
            lamp.intensity = 4f; lamp.range = 20f;
            lamp.shadows = LightShadows.Soft;
            lamp.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Low;
            lamp.shadowCustomResolution = 256;
            foreach (var renderer in torch.GetComponentsInChildren<MeshRenderer>(true))
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            PrefabUtility.SaveAsPrefabAsset(torch, TorchPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(torch); }
        return "Applied: torch scale 2.5 -> 6.6, warm realtime torch lighting, cool low fill, gradient ambient, subtle fog, ACES/Bloom/color/vignette; MainScene camera post-processing enabled.";
    }

    public static string TuneDarker()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != MainPath || scene.isDirty) throw new InvalidOperationException("A saved MainScene must be active.");
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null || !profile.TryGet<ColorAdjustments>(out var color)) throw new InvalidOperationException("Missing atmosphere profile.");
        const string sceneBackup = "AgentScripts/Backups/MainScene.before-darker-lighting.unity";
        const string profileBackup = "AgentScripts/Backups/MainMazeAtmosphere.before-darker-lighting.asset";
        if (!File.Exists(sceneBackup)) File.Copy(MainPath, sceneBackup);
        if (!File.Exists(profileBackup)) File.Copy(ProfilePath, profileBackup);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.35f, 0.38f, 0.44f);
        RenderSettings.ambientEquatorColor = new Color(0.265f, 0.245f, 0.215f);
        RenderSettings.ambientGroundColor = new Color(0.13f, 0.11f, 0.09f);
        RenderSettings.fogColor = new Color(0.045f, 0.037f, 0.028f);
        color.postExposure.Override(-0.10f); color.contrast.Override(8f);
        EditorUtility.SetDirty(color); AssetDatabase.SaveAssetIfDirty(color);
        EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile);
        DynamicGI.UpdateEnvironment();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Saved darker ambient/fog, exposure -0.10 and contrast 8. Torch brightness and scale preserved.";
    }

    static T Effect<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet<T>(out var component))
        {
            component = profile.Add<T>(true);
            AssetDatabase.AddObjectToAsset(component, profile);
        }
        component.active = true;
        return component;
    }

    static void SetMood()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.35f, 0.38f, 0.44f);
        RenderSettings.ambientEquatorColor = new Color(0.265f, 0.245f, 0.215f);
        RenderSettings.ambientGroundColor = new Color(0.13f, 0.11f, 0.09f);
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.006f; RenderSettings.fogColor = new Color(0.045f, 0.037f, 0.028f);
        foreach (var lamp in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (lamp.type != LightType.Directional) continue;
            lamp.intensity = 0.15f; lamp.color = new Color(0.65f, 0.72f, 0.85f); lamp.shadows = LightShadows.Soft;
        }
        DynamicGI.UpdateEnvironment();
    }

    public static async Task<string> PoseReview()
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        ShiftingPyramid.Maze.View.MazeTestBootstrapper bootstrap;
        do
        {
            await Task.Delay(100);
            bootstrap = UnityEngine.Object.FindFirstObjectByType<ShiftingPyramid.Maze.View.MazeTestBootstrapper>();
            if (DateTime.UtcNow > deadline) throw new InvalidOperationException("Maze readiness timed out.");
        } while (bootstrap == null || !bootstrap.IsMazeReady);
        // Only the temporary review run is parked; no gameplay component settings are saved.
        foreach (var movement in UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None)) movement.enabled = false;
        foreach (var look in UnityEngine.Object.FindObjectsByType<MouseLook>(FindObjectsSortMode.None)) look.enabled = false;
        foreach (var curse in UnityEngine.Object.FindObjectsByType<Mummification>(FindObjectsSortMode.None)) curse.enabled = false;
        foreach (var monster in UnityEngine.Object.FindObjectsByType<MonsterAI>(FindObjectsSortMode.None)) { monster.StopActions(); monster.enabled = false; }
        bootstrap.GetComponent<ShiftingPyramid.Maze.View.MazeRuntimeChangeController>().enabled = false;
        var torches = GeneratedTorches();
        var torch = torches.OrderBy(t => t.transform.position.sqrMagnitude).First();
        var camera = Camera.main;
        camera.transform.position = torch.transform.position - torch.transform.forward * 6f - Vector3.up * 1.5f;
        camera.transform.LookAt(torch.transform.position + Vector3.up);
        return "Review ready: frame=" + Time.frameCount + "; torches=" + torches.Length + "; camera=" + camera.name;
    }

    public static string Before()
    {
        var before = JsonUtility.FromJson<EnvironmentState>(File.ReadAllText(Evidence + "/before.json"));
        RenderSettings.ambientMode = before.ambientMode; RenderSettings.ambientSkyColor = before.sky;
        RenderSettings.ambientEquatorColor = before.equator; RenderSettings.ambientGroundColor = before.ground;
        RenderSettings.ambientIntensity = before.ambientIntensity; RenderSettings.fog = before.fog;
        foreach (var volume in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
            if (volume.name == "MazeAtmosphere") volume.weight = 0f;
        foreach (var lamp in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (lamp.type == LightType.Directional) { lamp.color = before.lightColor; lamp.intensity = before.lightIntensity; }
        foreach (var torch in GeneratedTorches())
        {
            torch.transform.localScale = Vector3.one * 2.5f;
            torch.enabled = false;
            var lamp = torch.GetComponentInChildren<Light>();
            lamp.shadows = LightShadows.None; lamp.intensity = 2f; lamp.range = 16f;
        }
        DynamicGI.UpdateEnvironment();
        return "Baseline lighting applied to temporary review run.";
    }

    public static string After()
    {
        SetMood();
        foreach (var volume in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
            if (volume.name == "MazeAtmosphere") volume.weight = 1f;
        foreach (var torch in GeneratedTorches())
        {
            torch.transform.localScale = Vector3.one * 6.6f;
            torch.enabled = true;
            var lamp = torch.GetComponentInChildren<Light>();
            lamp.shadows = LightShadows.Soft; lamp.range = 20f;
        }
        return Validate();
    }

    public static string Validate()
    {
        var camera = Camera.main;
        if (camera == null || !camera.GetUniversalAdditionalCameraData().renderPostProcessing || !camera.allowHDR)
            throw new InvalidOperationException("Camera HDR/post processing is not enabled.");
        var volume = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Single(v => v.name == "MazeAtmosphere");
        if (!volume.isGlobal || volume.weight != 1f || volume.sharedProfile.components.Count < 5
            || (camera.GetUniversalAdditionalCameraData().volumeLayerMask.value & (1 << volume.gameObject.layer)) == 0)
            throw new InvalidOperationException("Atmosphere volume is not reaching the camera.");
        var torches = GeneratedTorches();
        if (torches.Length == 0 || torches.Any(t => Mathf.Abs(t.transform.localScale.x - 6.6f) > 0.01f))
            throw new InvalidOperationException("Torch scaling not applied.");
        return "PASS: live MainScene, " + torches.Length + " enlarged torches, HDR camera + 5 active atmosphere effects; frame=" + Time.frameCount;
    }

    static TorchLight[] GeneratedTorches()
    {
        return UnityEngine.Object.FindObjectsByType<TorchLight>(FindObjectsSortMode.None)
            .Where(t => t.name.StartsWith("Torch_", StringComparison.Ordinal)).ToArray();
    }

    public static string RestoreEditor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop review Play Mode first.");
        var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(Evidence + "/editor-layout.json"));
        EditorSceneManager.RestoreSceneManagerSetup(layout.scenes.Select(s => new SceneSetup { path = s.path, isActive = s.active, isLoaded = s.loaded }).ToArray());
        if (!string.IsNullOrEmpty(layout.prefab)) AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(layout.prefab));
        return "Restored original editor scene/prefab layout.";
    }
}
