using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
public static class JemparinganURPMigration
{
    const string Folder="Assets/Settings/JemparinganURP";
    const string PipelinePath=Folder+"/JemparinganURP.asset";
    const string RendererPath=Folder+"/JemparinganRenderer.asset";
    static Color ColorFrom(JToken v)=>new Color((float)v[0],(float)v[1],(float)v[2],(float)v[3]);
    static Vector2 VectorFrom(JToken v)=>new Vector2((float)v[0],(float)v[1]);
    static void FolderAt(string path){if(AssetDatabase.IsValidFolder(path))return;FolderAt(Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));}
    public static string Apply()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        var before=JObject.Parse(File.ReadAllText("docs/validation/urp-v1/before.json"));
        var rows=(JArray)before["materials"];
        if(rows.Count!=42)throw new Exception("Incomplete pre-conversion material snapshot");
        FolderAt(Folder);
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if(renderer==null){renderer=ScriptableObject.CreateInstance<UniversalRendererData>();renderer.name="JemparinganRenderer";AssetDatabase.CreateAsset(renderer,RendererPath);}
        var rp=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if(rp==null){rp=UniversalRenderPipelineAsset.Create(renderer);rp.name="JemparinganURP";AssetDatabase.CreateAsset(rp,PipelinePath);}
        // Forward renderer, native resolution, same 2x MSAA as the previous Ultra desktop preset.
        rp.renderScale=1;rp.msaaSampleCount=2;rp.supportsHDR=true;
        rp.supportsCameraDepthTexture=false;rp.supportsCameraOpaqueTexture=false;
        int converted=0;
        foreach(var row in rows){
            string path=(string)row["path"],source=(string)row["shader"];
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(AssetImporter.GetAtPath(path) is ModelImporter)continue;
            if(source=="Skybox/Procedural")continue; // Supported unchanged by URP.
            if(source!="Standard"||(float)row["mode"]!=0)throw new Exception("Unexpected material: "+path);
            if(m.shader.name=="Standard")MaterialUpgrader.Upgrade(m,new StandardUpgrader("Standard"),MaterialUpgrader.UpgradeFlags.None);
            if(m.shader.name!="Universal Render Pipeline/Lit")throw new Exception("Wrong destination shader "+path);
            m.SetColor("_BaseColor",ColorFrom(row["color"]));
            m.SetTexture("_BaseMap",string.IsNullOrEmpty((string)row["mainTex"])?null:AssetDatabase.LoadAssetAtPath<Texture>((string)row["mainTex"]));
            m.SetTextureScale("_BaseMap",VectorFrom(row["scale"]));m.SetTextureOffset("_BaseMap",VectorFrom(row["offset"]));
            m.SetFloat("_Metallic",(float)row["metallic"]);m.SetFloat("_Smoothness",(float)row["smoothness"]);
            m.SetColor("_EmissionColor",ColorFrom(row["emission"]));
            EditorUtility.SetDirty(m);converted++;
        }
        FolderAt("Assets/Resources/Jemparingan");
        var lens=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Jemparingan/OpticalLens.mat");
        if(lens==null){lens=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(lens,"Assets/Resources/Jemparingan/OpticalLens.mat");}
        lens.SetColor("_BaseColor",new Color(.37f,.76f,.90f,.12f));lens.SetFloat("_Surface",1);lens.SetFloat("_Blend",0);
        lens.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);lens.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        lens.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);lens.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
        lens.SetFloat("_ZWrite",0);lens.SetFloat("_Cull",0);lens.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        lens.SetOverrideTag("RenderType","Transparent");lens.renderQueue=(int)RenderQueue.Transparent;
        lens.SetShaderPassEnabled("ShadowCaster",false);lens.SetShaderPassEnabled("DepthOnly",false);EditorUtility.SetDirty(lens);
        // Referenced opaque template also anchors runtime-generated Lit variants in player builds.
        var lit=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Jemparingan/RuntimeLit.mat");
        if(lit==null){lit=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(lit,"Assets/Resources/Jemparingan/RuntimeLit.mat");}
        EditorUtility.SetDirty(lit);EditorUtility.SetDirty(rp);EditorUtility.SetDirty(renderer);
        GraphicsSettings.defaultRenderPipeline=rp;
        int quality=QualitySettings.GetQualityLevel();
        for(int i=0;i<QualitySettings.names.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=rp;}
        QualitySettings.SetQualityLevel(quality,false);
        AssetDatabase.SaveAssets();
        return $"Converted {converted} Standard assets; assigned Graphics + {QualitySettings.names.Length} Quality levels; renderer={rp.scriptableRenderer.GetType().Name}";
    }
    public static string ConfigureCameras()
    {
        var paths=AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Jemparingan/Scenes"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        foreach(var path in paths){
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
            foreach(var camera in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true))){
                var data=camera.GetComponent<UniversalAdditionalCameraData>()??camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                data.renderType=CameraRenderType.Base;data.renderPostProcessing=false;data.SetRenderer(-1);
                EditorUtility.SetDirty(data);
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(original);
        // URP initializes this persistent asset when the pipeline first renders.
        var settings=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/UniversalRenderPipelineGlobalSettings.asset");
        if(settings==null)throw new Exception("URP global settings initialization failed");
        AssetDatabase.SaveAssets();return "Configured cameras in "+paths.Length+" saved scenes; global settings="+AssetDatabase.GetAssetPath(settings);
    }
    public static string ConvertModelMaterials()
    {
        FolderAt(Folder+"/ModelMaterials");int count=0;
        foreach(var guid in AssetDatabase.FindAssets("t:Model",new[]{"Assets/Models"})){
            string path=AssetDatabase.GUIDToAssetPath(guid);
            var importer=AssetImporter.GetAtPath(path) as ModelImporter;if(importer==null)continue;
            var materials=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().ToArray();
            foreach(var source in materials){
                if(source.shader.name!="Standard"&&source.shader.name!="Universal Render Pipeline/Lit")throw new Exception("Unsupported model material "+source.name);
                string name=Path.GetFileNameWithoutExtension(path)+"_"+source.name.Replace('/','_')+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/ModelMaterials/"+name);
                if(material==null){
                    material=new Material(source);if(material.shader.name=="Standard")MaterialUpgrader.Upgrade(material,new StandardUpgrader("Standard"),MaterialUpgrader.UpgradeFlags.None);
                    AssetDatabase.CreateAsset(material,Folder+"/ModelMaterials/"+name);
                }
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),source.name),material);count++;
            }
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();return "Extracted and persistently remapped "+count+" model materials";
    }
    public static string RestoreQualityDefaults()
    {
        var data=JObject.Parse(File.ReadAllText("docs/validation/urp-v1/quality-defaults-before.json"));
        var so=new SerializedObject((UnityEngine.Object)typeof(QualitySettings).GetMethod("GetQualitySettings",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).Invoke(null,null));
        var map=so.FindProperty("m_PerPlatformDefaultQuality");map.arraySize=data.Count;int i=0;
        foreach(var row in data.Properties()){
            var item=map.GetArrayElementAtIndex(i++);
            var key=item.FindPropertyRelative("first");var value=item.FindPropertyRelative("second");
            if(key==null||value==null)throw new Exception("Unknown Quality default map layout");
            key.stringValue=row.Name;value.intValue=(int)row.Value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);
        UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new[]{so.targetObject},"ProjectSettings/QualitySettings.asset",true);
        string file="ProjectSettings/QualitySettings.asset";
        string text=File.ReadAllText(file);string marker="  m_PerPlatformDefaultQuality:";
        int start=text.IndexOf(marker,StringComparison.Ordinal);
        if(start<0)throw new Exception("Quality default map missing");
        // Preserve only the recorded default map if the 6000.2 serializer omits it.
        string expected=marker+"\n"+string.Join("\n",data.Properties().Select(p=>"    "+p.Name+": "+(int)p.Value))+"\n";
        if(!text.Substring(start).Equals(expected,StringComparison.Ordinal))File.WriteAllText(file,text.Substring(0,start)+expected);
        return "Restored and serialized "+i+" platform quality defaults";
    }
    public static string Verify()
    {
        var rp=GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        var rows=(JArray)JObject.Parse(File.ReadAllText("docs/validation/urp-v1/before.json"))["materials"];
        var materials=rows.Select(row=>{
            string path=(string)row["path"];
            var importer=AssetImporter.GetAtPath(path) as ModelImporter;
            var m=importer!=null?importer.GetExternalObjectMap().Values.OfType<Material>().FirstOrDefault():AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null)throw new Exception("No persisted material at "+path);
            bool sky=(string)row["shader"]=="Skybox/Procedural";
            return new {path=(string)row["path"],shader=m.shader.name,supported=m.shader.isSupported,guidPreserved=AssetDatabase.AssetPathToGUID((string)row["path"])==(string)row["guid"],colorPreserved=sky||m.GetColor("_BaseColor")==ColorFrom(row["color"]),texturePreserved=sky||AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap"))==(string)row["mainTex"],smoothnessPreserved=sky||Mathf.Approximately(m.GetFloat("_Smoothness"),(float)row["smoothness"])};
        }).ToArray();
        var result=new {pipeline=rp?.name,renderer=rp?.scriptableRenderer?.GetType().Name,renderScale=rp?.renderScale,msaa=rp?.msaaSampleCount,quality=QualitySettings.names.Select((n,i)=>new {name=n,asset=AssetDatabase.GetAssetPath(QualitySettings.GetRenderPipelineAssetAt(i))}).ToArray(),materials};
        string json=Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented);File.WriteAllText("docs/validation/urp-v1/conversion.json",json);return json;
    }
}
