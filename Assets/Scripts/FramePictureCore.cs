using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Shared placement logic used by both the FramePicture component (drag an image into the
// Inspector) and the Tools > Frame Picture menu item. Lives in a normal script assembly (not
// Editor/) so FramePicture can call it; the Editor-only menu tool can still reach it fine since
// editor assemblies are allowed to reference the normal one. Editor-only in practice (everything
// here needs AssetDatabase/EditorUtility), hence the #if guard around the whole body.
public static class FramePictureCore
{
#if UNITY_EDITOR
    public static GameObject PlaceImage(GameObject frame, Texture2D tex, string texAssetPath, Vector3? roomInteriorHint = null, string narration = null, float marginFactor = 0.85f, float brightness = 0.8f, Vector2? focusPoint = null, bool showFullImage = false)
    {
        var mf = frame.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogError(frame.name + " has no mesh to measure -- can't fit a frame to it.");
            return null;
        }

        Mesh mesh = mf.sharedMesh;
        Vector3[] verts = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector3 roomInterior = roomInteriorHint ?? GuessRoomInterior(frame);

        // Find the dominant flat surface facing the room (not just the nearest coordinate axis --
        // a standing frame leaning back has a face normal with real Y tilt in it, which a simple
        // "thinnest local axis" check misses entirely). Cluster vertices by normal direction and
        // keep the largest cluster whose average normal faces toward the room interior.
        var clusterKeyToIndices = new Dictionary<Vector3Int, List<int>>();
        for (int i = 0; i < normals.Length; i++)
        {
            Vector3 n = normals[i].normalized;
            var key = new Vector3Int(Mathf.RoundToInt(n.x * 2), Mathf.RoundToInt(n.y * 2), Mathf.RoundToInt(n.z * 2));
            if (!clusterKeyToIndices.TryGetValue(key, out var list)) { list = new List<int>(); clusterKeyToIndices[key] = list; }
            list.Add(i);
        }

        // The real picture face -- front or back -- is always the mesh's single LARGEST flat
        // surface by vertex count (the flat paper/canvas), never a bevel or edge sliver. Find
        // that one first, purely by size; direction only decides which of its two sides (the
        // surface itself, or its mirror on the opposite face) is the one facing the room.
        List<int> largestCluster = null;
        Vector3 largestLocalNormal = Vector3.forward;
        foreach (var kv in clusterKeyToIndices)
        {
            if (largestCluster == null || kv.Value.Count > largestCluster.Count)
            {
                Vector3 sum = Vector3.zero;
                foreach (var idx in kv.Value) sum += normals[idx];
                largestLocalNormal = sum.normalized;
                largestCluster = kv.Value;
            }
        }

        // Find whichever OTHER cluster has a normal most opposite to the largest one -- that is
        // the same flat surface's other side (back of the paper), not an unrelated bevel.
        List<int> oppositeCluster = null;
        Vector3 oppositeLocalNormal = -largestLocalNormal;
        float mostOpposite = -1f;
        foreach (var kv in clusterKeyToIndices)
        {
            if (kv.Value == largestCluster) continue;
            Vector3 sum = Vector3.zero;
            foreach (var idx in kv.Value) sum += normals[idx];
            Vector3 avgLocalNormal = sum.normalized;
            float opposition = Vector3.Dot(avgLocalNormal, -largestLocalNormal);
            if (opposition > mostOpposite) { mostOpposite = opposition; oppositeCluster = kv.Value; oppositeLocalNormal = avgLocalNormal; }
        }

        List<int> bestCluster = largestCluster;
        Vector3 bestLocalNormal = largestLocalNormal;
        if (oppositeCluster != null && mostOpposite > 0.5f)
        {
            // Two real candidates for the picture plane (front and back of the same surface) --
            // pick whichever one actually faces the room.
            Vector3 toInterior = (roomInterior - frame.transform.position).normalized;
            float largestFacing = Vector3.Dot(frame.transform.TransformDirection(largestLocalNormal), toInterior);
            float oppositeFacing = Vector3.Dot(frame.transform.TransformDirection(oppositeLocalNormal), toInterior);
            if (oppositeFacing > largestFacing) { bestCluster = oppositeCluster; bestLocalNormal = oppositeLocalNormal; }
        }

        // Build a 2D coordinate frame on that true (possibly tilted) picture plane: world up
        // projected onto the plane, and right = normal x up.
        Vector3 worldNormal = frame.transform.TransformDirection(bestLocalNormal).normalized;
        Vector3 worldUp = Vector3.up - Vector3.Dot(Vector3.up, worldNormal) * worldNormal;
        if (worldUp.sqrMagnitude < 0.01f) worldUp = Vector3.ProjectOnPlane(frame.transform.up, worldNormal);
        worldUp.Normalize();
        Vector3 worldRight = Vector3.Cross(worldUp, worldNormal).normalized;

        // First pass: measure bounds strictly from bestCluster, exactly as before.
        float minR = float.MaxValue, maxR = float.MinValue, minU = float.MaxValue, maxU = float.MinValue;
        foreach (var idx in bestCluster)
        {
            Vector3 wp = frame.transform.TransformPoint(verts[idx]);
            float r = Vector3.Dot(wp, worldRight);
            float u = Vector3.Dot(wp, worldUp);
            if (r < minR) minR = r;
            if (r > maxR) maxR = r;
            if (u < minU) minU = u;
            if (u > maxU) maxU = u;
        }

        // Second pass: widen that strict box using every vertex whose normal is reasonably close
        // to bestLocalNormal, not just the ones that happened to land in bestCluster's exact
        // rounded bucket -- smooth-shaded bevels right at the opening's rim blend their normals
        // toward the bevel direction, so the true corner vertices can round into a neighboring
        // bucket and get excluded from bestCluster entirely, undershooting the measured opening
        // on whichever side that happened and leaving a visible gap between picture and frame.
        // The angle match alone is too loose, though -- on a frame with a steep ornate bevel it
        // can sweep in moulding or side-wall vertices that face a similar direction but belong to
        // a completely different part of the mesh. Gate the sweep spatially too, to only pull in
        // vertices within a modest margin of the strict box: close enough to be the same opening's
        // rounded-off rim, too far to be unrelated geometry.
        // A bevel rim is a few centimeters wide regardless of how big the picture itself is, so
        // the slack has to be a small fixed distance, not a fraction of the face size -- a
        // proportional slack let the sweep nearly double the measured box on a large frame.
        const float slack = 0.035f;
        float gateMinR = minR - slack, gateMaxR = maxR + slack;
        float gateMinU = minU - slack, gateMaxU = maxU + slack;

        List<int> boundsVerts = new List<int>();
        Vector3 worldSum = Vector3.zero;
        for (int i = 0; i < normals.Length; i++)
        {
            if (Vector3.Dot(normals[i].normalized, bestLocalNormal) <= 0.4f) continue;
            Vector3 wp = frame.transform.TransformPoint(verts[i]);
            float r = Vector3.Dot(wp, worldRight);
            float u = Vector3.Dot(wp, worldUp);
            if (r < gateMinR || r > gateMaxR || u < gateMinU || u > gateMaxU) continue;
            boundsVerts.Add(i);
            worldSum += wp;
            if (r < minR) minR = r;
            if (r > maxR) maxR = r;
            if (u < minU) minU = u;
            if (u > maxU) maxU = u;
        }
        Vector3 faceCenter = boundsVerts.Count > 0 ? worldSum / boundsVerts.Count : frame.transform.TransformPoint(verts[bestCluster[0]]);
        float faceW = maxR - minR;
        float faceH = maxU - minU;

        float w = faceW * marginFactor;
        float h = faceH * marginFactor;

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        string matPath = "Assets/Textures/DreamExhibits/Mat_" + frame.name + "_Image.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.shader = shader;
        }
        mat.name = "Mat_" + frame.name + "_Image";
        mat.SetTexture("_BaseMap", tex);
        mat.SetFloat("_Cull", 0f);
        float b = Mathf.Clamp01(brightness);
        mat.SetColor("_BaseColor", new Color(b, b, b, 1f));
        EditorUtility.SetDirty(mat);

        float frameAspect = w / h;
        float imgAspect = (float)tex.width / tex.height;
        float uMin, uMax, vMin, vMax;
        if (showFullImage)
        {
            // The source photo's own aspect is too far from the frame opening's for a crop to
            // make sense -- a portrait group photo in a landscape frame, or a landscape photo in
            // a portrait frame, where cropping to fill always cuts someone's head or half the
            // group off, and shifting the focus point only changes who gets cut off, not whether.
            // Show the whole photo instead: shrink the canvas to the image's own aspect so nothing
            // is cropped (full 0..1 UVs), leaving a little of the opening showing at the narrow
            // sides rather than hiding part of the picture.
            uMin = 0f; uMax = 1f; vMin = 0f; vMax = 1f;
            if (imgAspect > frameAspect) h = w / imgAspect;
            else w = h * imgAspect;
        }
        else
        {
            // Cover-crop the image into the frame's (w,h) opening without distortion. Crop is
            // baked directly into the overlay mesh's own UVs below -- no Material texture
            // scale/offset, no dependency on any primitive's built-in UV layout or Quaternion axis
            // semantics. focusPoint (default image-center) lets the crop window be shifted for an
            // off-center subject.
            Vector2 focus = focusPoint ?? new Vector2(0.5f, 0.5f);
            float fx = Mathf.Clamp01(focus.x);
            float fy = Mathf.Clamp01(focus.y);
            float cropW, cropH;
            if (imgAspect > frameAspect) { cropW = frameAspect / imgAspect; cropH = 1f; }
            else { cropW = 1f; cropH = imgAspect / frameAspect; }
            uMin = Mathf.Clamp(fx - cropW * 0.5f, 0f, Mathf.Max(0f, 1f - cropW));
            uMax = uMin + cropW;
            vMin = Mathf.Clamp(fy - cropH * 0.5f, 0f, Mathf.Max(0f, 1f - cropH));
            vMax = vMin + cropH;
        }

        // Explicit world-space quad: corners placed directly from our own (worldRight, worldUp)
        // basis, UVs matched 1:1 to the same corners by construction. No intermediate coordinate
        // system (no primitive mesh, no transform rotation) for an axis mismatch to hide in.
        float halfW = w * 0.5f;
        float halfH = h * 0.5f;
        Vector3 nudge = worldNormal * 0.015f;
        Vector3 bl = faceCenter - worldRight * halfW - worldUp * halfH + nudge;
        Vector3 brC = faceCenter + worldRight * halfW - worldUp * halfH + nudge;
        Vector3 tr = faceCenter + worldRight * halfW + worldUp * halfH + nudge;
        Vector3 tl = faceCenter - worldRight * halfW + worldUp * halfH + nudge;

        var canvasMesh = new Mesh();
        canvasMesh.name = frame.name + "_ImageCanvasMesh";
        canvasMesh.vertices = new[] { bl, brC, tr, tl };
        canvasMesh.uv = new[]
        {
            new Vector2(uMin, vMin),
            new Vector2(uMax, vMin),
            new Vector2(uMax, vMax),
            new Vector2(uMin, vMax),
        };
        canvasMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        canvasMesh.normals = new[] { worldNormal, worldNormal, worldNormal, worldNormal };
        canvasMesh.RecalculateBounds();

        string canvasName = frame.name + "_ImageCanvas";
        var existing = frame.transform.parent != null ? frame.transform.parent.Find(canvasName) : null;
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // A freshly-created GameObject starts at world identity, so the mesh's absolute
        // world-space vertices render correctly while unparented. Reparent AFTER, with
        // worldPositionStays=true, so the parent's own transform isn't double-applied on top of
        // coordinates that are already absolute.
        GameObject quad = new GameObject(canvasName);
        var meshFilter = quad.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = canvasMesh;
        var meshRenderer = quad.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = mat;
        quad.transform.SetParent(frame.transform.parent, true);

        if (!string.IsNullOrEmpty(narration))
        {
            var pn = frame.GetComponent<PaintingNarration>();
            if (pn == null) pn = frame.AddComponent<PaintingNarration>();
            pn.narration = narration;
            EditorUtility.SetDirty(frame);
        }

        EditorUtility.SetDirty(quad);
        Debug.Log("Placed " + texAssetPath + " on " + frame.name + " (" + w.ToString("F2") + "m x " + h.ToString("F2") +
            "m, face normal=" + worldNormal.ToString("F2") + ", crop U[" + uMin.ToString("F2") + "," + uMax.ToString("F2") +
            "] V[" + vMin.ToString("F2") + "," + vMax.ToString("F2") + "])");
        return quad;
    }

    public static Vector3 GuessRoomInterior(GameObject frame)
    {
        var teaTable = GameObject.Find("tea_table");
        return teaTable != null ? teaTable.transform.position : frame.transform.position;
    }
#endif
}
