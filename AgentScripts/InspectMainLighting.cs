using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.SceneManagement;

public static class InspectMainLighting
{
    public static string Inspect()
    {
        var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var volumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var lines = new System.Collections.Generic.List<string>();
        lines.Add("scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + "; play=" + Application.isPlaying + "; frame=" + Time.frameCount);
        lines.Add("ambient=" + RenderSettings.ambientMode + "; sky=" + RenderSettings.ambientSkyColor + "; equator=" + RenderSettings.ambientEquatorColor + "; ground=" + RenderSettings.ambientGroundColor);
        foreach (var l in lights) lines.Add("Light " + Path(l.transform) + "; active=" + l.isActiveAndEnabled + "; type=" + l.type + "; intensity=" + l.intensity + "; range=" + l.range + "; shadows=" + l.shadows);
        foreach (var v in volumes)
        {
            lines.Add("Volume " + Path(v.transform) + "; active=" + v.isActiveAndEnabled + "; weight=" + v.weight + "; priority=" + v.priority);
            var p = v.HasInstantiatedProfile() ? v.profile : v.sharedProfile;
            if (p != null && p.TryGet<ColorAdjustments>(out var c)) lines.Add("exposure=" + c.postExposure.value + "; override=" + c.postExposure.overrideState);
        }
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) lines.Add("Camera " + Path(c.transform) + "; post=" + c.GetUniversalAdditionalCameraData().renderPostProcessing);
        return string.Join("\n", lines);
    }
    static string Path(Transform t) { return t.parent == null ? t.name : Path(t.parent) + "/" + t.name; }
}
