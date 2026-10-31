using UnityEngine;
namespace HoloDebut.Runtime
{
    public static class VisualFactory
    {
        public static Material Material(Color color)
        {
            var template = Resources.Load<Material>("HoloDebutPrototypeMaterial");
            if (template) { var copy = new Material(template); copy.color = color; return copy; }
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) shader = Shader.Find("Standard");
            var mat = new Material(shader); mat.color = color; return mat;
        }
        public static GameObject Primitive(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, Color color, bool collider = false)
        {
            var o = GameObject.CreatePrimitive(type); o.name = name; o.transform.SetParent(parent, false);
            o.transform.localPosition = pos; o.transform.localScale = scale;
            var c = o.GetComponent<Collider>(); if (!collider && c) { c.enabled = false; Object.Destroy(c); }
            var mat=Material(color);o.GetComponent<Renderer>().sharedMaterial=mat;o.AddComponent<OwnedVisualResources>().material=mat;return o;
        }
        public static TextMesh Label(Transform parent, string text, Vector3 pos, float size, Color color)
        {
            var o = new GameObject("Label"); o.transform.SetParent(parent, false); o.transform.localPosition = pos;
            var t = o.AddComponent<TextMesh>(); t.text = text; t.anchor = TextAnchor.MiddleCenter;
            t.font = JapaneseFont.Load();
            o.GetComponent<MeshRenderer>().sharedMaterial = t.font.material;
            t.fontSize = 64; t.characterSize = size; t.color = color; return t;
        }
    }
}
