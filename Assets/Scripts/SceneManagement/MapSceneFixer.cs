using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif
#if UNITY_2019_3_OR_NEWER
using UnityEngine.Rendering;
#endif

public class MapSceneFixer : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== MapSceneFixer Start ===");

        // log scenes
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            Debug.Log($"Loaded scene {i}: {s.name}, active={s == SceneManager.GetActiveScene()}");
        }

        // log quality
        int q = QualitySettings.GetQualityLevel();
        Debug.Log("Quality level: " + QualitySettings.names[q]);

        // enforce a known quality level if needed
        // QualitySettings.SetQualityLevel(3, true);   // pick the index you use when testing the map directly

        // keep only the main camera
        var mainCam = Camera.main;
        foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (cam != mainCam)
            {
                Debug.Log("Disabling extra camera: " + cam.name);
                cam.enabled = false;
            }
        }

        // disable all post-processing volumes
#if UNITY_POST_PROCESSING_STACK_V2
        foreach (var v in FindObjectsByType<PostProcessVolume>(FindObjectsSortMode.None))
        {
            Debug.Log(
                "Disabling PostProcessVolume: "
                    + v.name
                    + " (scene "
                    + v.gameObject.scene.name
                    + ")"
            );
            v.enabled = false;
        }

        foreach (var l in FindObjectsByType<PostProcessLayer>(FindObjectsSortMode.None))
        {
            Debug.Log("Disabling PostProcessLayer on: " + l.gameObject.name);
            l.enabled = false;
        }
#endif

        // disable URP/HDRP Volumes if present
#if UNITY_2019_3_OR_NEWER
        foreach (var v in FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            Debug.Log("Disabling Volume: " + v.name + " (scene " + v.gameObject.scene.name + ")");
            v.enabled = false;
        }
#endif

        // optional hard reset of environment lighting
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.reflectionIntensity = 1f;
    }
}
