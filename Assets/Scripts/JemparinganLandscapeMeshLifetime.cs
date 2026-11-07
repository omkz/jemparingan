using System.Collections.Generic;
using UnityEngine;

// Separate Unity component script so its MonoBehaviour class matches the
// filename. Owns runtime-generated meshes and releases native allocations.
[DisallowMultipleComponent]
public sealed class JemparinganLandscapeMeshLifetime : MonoBehaviour
{
    private readonly List<Mesh> ownedMeshes = new List<Mesh>();

    public void Own(Mesh mesh)
    {
        if (mesh != null) ownedMeshes.Add(mesh);
    }

    private void OnDestroy()
    {
        foreach (Mesh mesh in ownedMeshes)
            if (mesh != null) Destroy(mesh);
        ownedMeshes.Clear();
    }
}
