using UnityEngine;
using UnityEngine.Rendering.Universal;

// Stacks a URP overlay camera so flying score tiles draw after world-space game UI.
public sealed class ScoreFxOverlay : MonoBehaviour
{
    public const string LayerName = "ScoreFx";

    Camera overlayCamera;
    Camera sourceCamera;

    static ScoreFxOverlay instance;

    public static int Layer => LayerMask.NameToLayer(LayerName);

    public static void Prepare(GameObject subject)
    {
        if (subject == null)
        {
            return;
        }

        int layer = Layer;
        if (layer < 0)
        {
            return;
        }

        SetLayerRecursively(subject, layer);
        EnsureInstance();
    }

    static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        Camera source = Camera.main;
        if (source == null)
        {
            return;
        }

        int layer = Layer;
        if (layer < 0)
        {
            return;
        }

        GameObject overlayObject = new GameObject("ScoreFx Overlay Camera");
        overlayObject.transform.SetParent(source.transform, false);

        Camera overlayCamera = overlayObject.AddComponent<Camera>();
        overlayCamera.enabled = true;
        overlayCamera.orthographic = source.orthographic;
        overlayCamera.orthographicSize = source.orthographicSize;
        overlayCamera.fieldOfView = source.fieldOfView;
        overlayCamera.nearClipPlane = source.nearClipPlane;
        overlayCamera.farClipPlane = source.farClipPlane;
        overlayCamera.rect = source.rect;
        overlayCamera.depth = source.depth + 1;
        overlayCamera.clearFlags = CameraClearFlags.Nothing;
        overlayCamera.cullingMask = 1 << layer;
        overlayCamera.allowHDR = source.allowHDR;
        overlayCamera.allowMSAA = source.allowMSAA;
        overlayCamera.allowDynamicResolution = false;
        overlayCamera.useOcclusionCulling = false;
        overlayCamera.targetTexture = null;

        UniversalAdditionalCameraData overlayData = overlayObject.AddComponent<UniversalAdditionalCameraData>();
        overlayData.renderType = CameraRenderType.Overlay;
        overlayData.renderShadows = false;
        overlayData.renderPostProcessing = false;
        overlayData.requiresColorOption = CameraOverrideOption.Off;
        overlayData.requiresDepthOption = CameraOverrideOption.Off;

        UniversalAdditionalCameraData sourceData = source.GetUniversalAdditionalCameraData();
        if (sourceData != null && !sourceData.cameraStack.Contains(overlayCamera))
        {
            sourceData.cameraStack.Add(overlayCamera);
        }

        source.cullingMask &= ~(1 << layer);

        ScoreFxOverlay overlay = overlayObject.AddComponent<ScoreFxOverlay>();
        overlay.overlayCamera = overlayCamera;
        overlay.sourceCamera = source;
        instance = overlay;
    }

    void LateUpdate()
    {
        if (overlayCamera == null || sourceCamera == null)
        {
            return;
        }

        overlayCamera.orthographic = sourceCamera.orthographic;
        overlayCamera.orthographicSize = sourceCamera.orthographicSize;
        overlayCamera.fieldOfView = sourceCamera.fieldOfView;
        overlayCamera.nearClipPlane = sourceCamera.nearClipPlane;
        overlayCamera.farClipPlane = sourceCamera.farClipPlane;
        overlayCamera.rect = sourceCamera.rect;
        overlayCamera.lensShift = sourceCamera.lensShift;
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (sourceCamera == null || overlayCamera == null)
        {
            return;
        }

        UniversalAdditionalCameraData sourceData = sourceCamera.GetUniversalAdditionalCameraData();
        if (sourceData != null)
        {
            sourceData.cameraStack.Remove(overlayCamera);
        }
    }

    static void SetLayerRecursively(GameObject subject, int layer)
    {
        subject.layer = layer;
        Transform transform = subject.transform;
        for (int i = 0; i < transform.childCount; i++)
        {
            SetLayerRecursively(transform.GetChild(i).gameObject, layer);
        }
    }
}
