using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class URPPreflight
{
    sealed class VectorConverter : Newtonsoft.Json.JsonConverter {
        public override bool CanConvert(Type t)=>t==typeof(Color)||t==typeof(Vector2);
        public override void WriteJson(Newtonsoft.Json.JsonWriter w, object value, Newtonsoft.Json.JsonSerializer s){
            if(value is Color){var c=(Color)value;s.Serialize(w,new[]{c.r,c.g,c.b,c.a});}
            else {var v=(Vector2)value;s.Serialize(w,new[]{v.x,v.y});}
        }
        public override object ReadJson(Newtonsoft.Json.JsonReader r,Type t,object v,Newtonsoft.Json.JsonSerializer s)=>throw new NotSupportedException();
    }
    [Serializable] public class MaterialRow {
        public string path,guid,shader,mainTex,normal,metallicMap,specMap,emissionMap;
        public Color color,emission,specular;public Vector2 scale,offset;
        public float metallic,smoothness,cutoff,mode;
    }
    [Serializable] public class Snapshot {public MaterialRow[] materials;public string[] sceneObjects;public string[] components;}
    static string TexturePath(Material m,string prop)=>m.HasProperty(prop)?AssetDatabase.GetAssetPath(m.GetTexture(prop)):"";
    static float Float(Material m,string prop,float fallback)=>m.HasProperty(prop)?m.GetFloat(prop):fallback;
    public static string Capture(){
        var rows=AssetDatabase.FindAssets("t:Material",new[]{"Assets"}).Select(g=>{
            var path=AssetDatabase.GUIDToAssetPath(g);var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            return new MaterialRow {path=path,guid=g,shader=m.shader.name,mainTex=TexturePath(m,"_MainTex"),normal=TexturePath(m,"_BumpMap"),metallicMap=TexturePath(m,"_MetallicGlossMap"),specMap=TexturePath(m,"_SpecGlossMap"),emissionMap=TexturePath(m,"_EmissionMap"),color=m.HasProperty("_Color")?m.GetColor("_Color"):Color.white,emission=m.HasProperty("_EmissionColor")?m.GetColor("_EmissionColor"):Color.black,specular=m.HasProperty("_SpecColor")?m.GetColor("_SpecColor"):Color.black,scale=m.HasProperty("_MainTex")?m.GetTextureScale("_MainTex"):Vector2.one,offset=m.HasProperty("_MainTex")?m.GetTextureOffset("_MainTex"):Vector2.zero,metallic=Float(m,"_Metallic",0),smoothness=Float(m,"_Glossiness",.5f),cutoff=Float(m,"_Cutoff",.5f),mode=Float(m,"_Mode",0)};
        }).OrderBy(r=>r.path).ToArray();
        var transforms=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var snapshot=new Snapshot {materials=rows,sceneObjects=transforms.Select(t=>t.name+"|"+t.localPosition.ToString("R")+"|"+t.localRotation.ToString("R")+"|"+t.localScale.ToString("R")).ToArray(),components=transforms.SelectMany(t=>t.GetComponents<Component>()).Where(c=>c!=null).Select(c=>c.GetType().FullName).Distinct().OrderBy(n=>n).ToArray()};
        var json=Newtonsoft.Json.JsonConvert.SerializeObject(snapshot,Newtonsoft.Json.Formatting.Indented,new VectorConverter());System.IO.File.WriteAllText("docs/validation/urp-v1/before.json",json);return $"Saved {rows.Length} material snapshots, {transforms.Length} scene transforms";
    }
}
