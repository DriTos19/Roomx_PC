using UnityEngine;
using Autodesk.Fbx;
using System.IO;

/// <summary>
/// FBX Loader - Loads FBX files at runtime and converts them to Unity GameObjects
/// </summary>
public class FBXLoader : MonoBehaviour
{
    /// <summary>
    /// Loads an FBX file from the specified file path and returns it as a GameObject.
    /// </summary>
    public static GameObject LoadFBX(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("FBX file does not exist: " + filePath);
            return null;
        }

        try
        {
            var manager = FbxManager.Create();
            if (manager == null)
            {
                Debug.LogError("Failed to create FBX Manager");
                return null;
            }

            var importer = FbxImporter.Create(manager, "");
            if (importer == null)
            {
                Debug.LogError("Failed to create FBX Importer");
                manager.Destroy();
                return null;
            }

            if (!importer.Initialize(filePath, -1, manager.GetIOSettings()))
            {
                Debug.LogError("Failed to initialize FBX importer for: " + filePath);
                importer.Destroy();
                manager.Destroy();
                return null;
            }

            var scene = FbxScene.Create(manager, "");
            if (!importer.Import(scene))
            {
                Debug.LogError("Failed to import FBX scene from: " + filePath);
                importer.Destroy();
                scene.Destroy();
                manager.Destroy();
                return null;
            }

            // Convert FBX scene to Unity GameObject
            GameObject rootObject = ConvertFbxSceneToGameObject(scene);

            // Cleanup
            importer.Destroy();
            scene.Destroy();
            manager.Destroy();

            if (rootObject != null)
            {
                Debug.Log("Successfully loaded FBX: " + filePath);
            }

            return rootObject;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Exception while loading FBX file: " + e.Message + "\n" + e.StackTrace);
            return null;
        }
    }

    private static GameObject ConvertFbxSceneToGameObject(FbxScene scene)
    {
        GameObject root = new GameObject(scene.GetName());

        // Traverse the scene and create GameObjects
        FbxNode rootNode = scene.GetRootNode();
        if (rootNode != null)
        {
            for (int i = 0; i < rootNode.GetChildCount(); i++)
            {
                FbxNode childNode = rootNode.GetChild(i);
                GameObject childObj = ProcessFbxNode(childNode);
                if (childObj != null)
                {
                    childObj.transform.SetParent(root.transform, false);
                }
            }
        }

        // Center the imported model around the origin
        CenterModelOnOrigin(root);

        return root;
    }

    private static GameObject ProcessFbxNode(FbxNode node)
    {
        if (node == null)
            return null;

        GameObject obj = new GameObject(node.GetName());

        // Set transform
        FbxDouble3 translation = node.LclTranslation.Get();
        FbxDouble3 rotation = node.LclRotation.Get();
        FbxDouble3 scaling = node.LclScaling.Get();

        obj.transform.localPosition = new Vector3((float)translation.X, (float)translation.Y, (float)translation.Z);
        obj.transform.localRotation = Quaternion.Euler((float)rotation.X, (float)rotation.Y, (float)rotation.Z);
        obj.transform.localScale = new Vector3((float)scaling.X, (float)scaling.Y, (float)scaling.Z);

        // Process mesh
        FbxMesh mesh = node.GetMesh();
        if (mesh != null && mesh.GetPolygonCount() > 0)
        {
            try
            {
                Mesh unityMesh = ConvertFbxMeshToUnityMesh(mesh);
                if (unityMesh != null)
                {
                    MeshFilter filter = obj.AddComponent<MeshFilter>();
                    filter.mesh = unityMesh;

                    MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
                    // Assign a default material
                    renderer.material = new Material(Shader.Find("Standard"));

                    // Add collider for placement interaction
                    obj.AddComponent<BoxCollider>();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Failed to process mesh for node " + node.GetName() + ": " + e.Message);
            }
        }

        // Process children
        for (int i = 0; i < node.GetChildCount(); i++)
        {
            FbxNode childNode = node.GetChild(i);
            GameObject childObj = ProcessFbxNode(childNode);
            if (childObj != null)
            {
                childObj.transform.SetParent(obj.transform, false);
            }
        }

        return obj;
    }

    private static Mesh ConvertFbxMeshToUnityMesh(FbxMesh fbxMesh)
    {
        if (fbxMesh == null)
            return null;

        Mesh unityMesh = new Mesh();
        unityMesh.name = fbxMesh.GetName();

        // Get vertices
        int vertexCount = fbxMesh.GetControlPointsCount();
        if (vertexCount == 0)
        {
            Debug.LogWarning("FBX mesh has no vertices");
            return null;
        }

        Vector3[] vertices = new Vector3[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            FbxVector4 point = fbxMesh.GetControlPointAt(i);
            vertices[i] = new Vector3((float)point.X, (float)point.Y, (float)point.Z);
        }
        unityMesh.vertices = vertices;

        // Get triangles
        int polygonCount = fbxMesh.GetPolygonCount();
        int triangleCount = 0;

        // Count triangles first
        for (int i = 0; i < polygonCount; i++)
        {
            triangleCount += (fbxMesh.GetPolygonSize(i) - 2) * 3;
        }

        int[] triangles = new int[triangleCount];
        int index = 0;

        // Fill triangles - handle polygons with more than 3 vertices by triangulating
        for (int i = 0; i < polygonCount; i++)
        {
            int polygonSize = fbxMesh.GetPolygonSize(i);
            for (int j = 1; j < polygonSize - 1; j++)
            {
                triangles[index++] = fbxMesh.GetPolygonVertex(i, 0);
                triangles[index++] = fbxMesh.GetPolygonVertex(i, j);
                triangles[index++] = fbxMesh.GetPolygonVertex(i, j + 1);
            }
        }

        unityMesh.triangles = triangles;

        // Recalculate normals for proper lighting
        unityMesh.RecalculateNormals();
        unityMesh.RecalculateBounds();

        return unityMesh;
    }

    private static void CenterModelOnOrigin(GameObject model)
    {
        if (model == null || model.transform.childCount == 0)
            return;

        // Calculate the center of all child local positions
        Vector3 totalPosition = Vector3.zero;
        int childCount = 0;

        foreach (Transform child in model.transform)
        {
            totalPosition += child.localPosition;
            childCount++;
        }

        if (childCount == 0)
            return;

        Vector3 center = totalPosition / childCount;

        // Offset all child local positions so the center is at origin
        foreach (Transform child in model.transform)
        {
            child.localPosition -= center;
        }

        // Also scale the model to a reasonable size
        ScaleModelToReasonableSize(model);

        Debug.Log($"Centered FBX model '{model.name}' by offsetting local positions by {-center}");
    }

    private static void ScaleModelToReasonableSize(GameObject model)
    {
        if (model == null)
            return;

        // Calculate the bounds of the model
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
            return;

        // Get the maximum dimension
        float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        // Scale so the largest dimension is 1.0 units (good size for furniture)
        float targetSize = 1.0f;
        float scaleFactor = targetSize / maxDimension;

        // Apply scale to the root object
        model.transform.localScale *= scaleFactor;

        Debug.Log($"Scaled FBX model '{model.name}' by factor {scaleFactor} (max dimension was {maxDimension}, now {targetSize})");
    }
}
