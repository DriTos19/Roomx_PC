using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

public static class GLBLoader
{
    public static async Task<GameObject> LoadGLB(string filePath)
    {
        var root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(filePath));

        var gltf = new GltfImport();
        bool success = await gltf.Load(filePath);

        if (!success)
        {
            Object.Destroy(root);
            Debug.LogError($"GLBLoader: Failed to load file: {filePath}");
            return null;
        }

        bool instantiated = await gltf.InstantiateMainSceneAsync(root.transform);
        if (!instantiated)
        {
            Object.Destroy(root);
            Debug.LogError($"GLBLoader: Failed to instantiate scene from: {filePath}");
            return null;
        }

        NormalizeModel(root);
        root.SetActive(false); // hide from scene; used only as a source for Instantiate
        return root;
    }

    static void NormalizeModel(GameObject model)
    {
        // Center all children around origin
        var renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);

        Vector3 offset = bounds.center - model.transform.position;
        foreach (Transform child in model.transform)
            child.position -= offset;

        // Scale so largest dimension equals 1 unit
        float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxDim > 0f)
            model.transform.localScale = Vector3.one * (1f / maxDim);
    }
}
