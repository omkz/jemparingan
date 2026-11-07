using System.Collections.Generic;
using UnityEngine;

// Original low-poly procedural art. No external mesh importer or texture dependencies.
// Reuse the same material instances to keep SRP batching effective.
public static class JemparinganArt
{
    public enum Tone { DarkWood, Wood, LightWood, Gold, Brass, Red, Scarlet, Jade, Paper, Indigo, Shadow, BowTeak, BowEdge, BowLeather, BowStitch, BowBrass, BowIvory, BandulanWhite, BandulanYellow, TraditionalCloth, TraditionalTrim, TraditionalSkin, ArrowSteel, HandSkin }
    private static readonly Dictionary<Tone, Material> Materials = new Dictionary<Tone, Material>();

    public static Material Material(Tone tone)
    {
        if (Materials.TryGetValue(tone, out Material existing) && existing != null) return existing;
        // A referenced template keeps the runtime shader/variants in player builds.
        var template = Resources.Load<Material>("Jemparingan/RuntimeLit");
        if (template == null)
            throw new System.InvalidOperationException("Jemparingan URP RuntimeLit material is missing.");
        var mat = new Material(template) { name = "Jemparingan_" + tone };
        Color col;
        switch (tone)
        {
            case Tone.DarkWood: col = RGB(52, 29, 27); break;
            case Tone.Wood: col = RGB(117, 65, 40); break;
            case Tone.LightWood: col = RGB(192, 135, 77); break;
            case Tone.Gold: col = RGB(238, 189, 100); break;
            case Tone.Brass: col = RGB(149, 108, 56); break;
            case Tone.Red: col = RGB(147, 42, 46); break;
            case Tone.Scarlet: col = RGB(226, 78, 65); break;
            case Tone.Jade: col = RGB(66, 174, 145); break;
            case Tone.Paper: col = RGB(247, 219, 170); break;
            case Tone.Indigo: col = RGB(35, 49, 76); break;
            // Bow Polish v1 palette; extra enum values are appended to preserve
            // serialized numeric values of existing tones in the legacy game.
            case Tone.BowTeak: col = RGB(84, 53, 41); break;
            case Tone.BowEdge: col = RGB(164, 110, 66); break;
            case Tone.BowLeather: col = RGB(73, 49, 39); break;
            case Tone.BowStitch: col = RGB(125, 91, 68); break;
            case Tone.BowBrass: col = RGB(185, 149, 89); break;
            case Tone.BowIvory: col = RGB(232, 216, 181); break;
            case Tone.BandulanWhite: col = RGB(235, 229, 209); break;
            case Tone.BandulanYellow: col = RGB(224, 172, 60); break;
            case Tone.TraditionalCloth: col = RGB(64, 57, 80); break;
            case Tone.TraditionalTrim: col = RGB(168, 124, 73); break;
            case Tone.TraditionalSkin: col = RGB(164, 119, 91); break;
            case Tone.ArrowSteel: col = RGB(91, 101, 108); break;
            // Slightly warmer/readable first-person skin, separate from the
            // legacy Traditional body materials and existing enum values.
            case Tone.HandSkin: col = RGB(176, 128, 99); break;
            default: col = RGB(23, 30, 43); break;
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
        // Small, shared URP material differences: matte teak/leather and
        // skin, slightly smoother brass. No textures or expensive shaders.
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", tone == Tone.BowLeather ||
                tone == Tone.BowStitch ? .09f :
                tone == Tone.TraditionalSkin || tone == Tone.TraditionalCloth ? .035f :
                tone == Tone.HandSkin ? .065f :
                tone == Tone.BowTeak || tone == Tone.BowEdge ? .13f :
                tone == Tone.BowBrass ? .32f :
                tone == Tone.ArrowSteel ? .46f : .22f);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", tone == Tone.BowBrass ? .18f :
                tone == Tone.ArrowSteel ? .65f : 0f);
        Materials[tone] = mat;
        return mat;
    }

    private static Color RGB(byte r, byte g, byte b) => new Color32(r, g, b, 255);

    public static GameObject Part(string name, PrimitiveType primitive, Transform parent,
        Vector3 localPosition, Vector3 localScale, Tone tone, Quaternion? rotation = null)
    {
        GameObject go = GameObject.CreatePrimitive(primitive);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = rotation ?? Quaternion.identity;
        go.transform.localScale = localScale;
        Collider oldCollider = go.GetComponent<Collider>();
        if (oldCollider != null) Object.Destroy(oldCollider);
        go.GetComponent<Renderer>().sharedMaterial = Material(tone);
        return go;
    }

    public static void Beam(string name, Transform parent, Vector3 from, Vector3 to,
        float thickness, Tone tone, PrimitiveType primitive = PrimitiveType.Cylinder)
    {
        Vector3 diff = to - from;
        Part(name, primitive, parent, (to + from) * 0.5f,
            primitive == PrimitiveType.Cylinder
                ? new Vector3(thickness, diff.magnitude * 0.5f, thickness)
                : new Vector3(thickness, diff.magnitude, thickness),
            tone, Quaternion.FromToRotation(Vector3.up, diff.normalized));
    }

    public static TextMesh Label(string name, Transform parent, Vector3 localPosition,
        string content, float characterSize, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        var mesh = obj.AddComponent<TextMesh>();
        mesh.fontSize = 64;
        mesh.characterSize = characterSize;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
        mesh.text = content;
        return mesh;
    }
}
