// Temporary in-memory QA entry points: run with Unity MCP script_run in Play Mode.
// Reflection observes/invokes existing runtime code; this file is outside Assets.
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Unity.Profiling;
public static class URPQAProbe
{
    static string Json(object o)
    {
        if(o==null)return "null";
        if(o is string || o.GetType().IsEnum)return "\""+o.ToString().Replace("\\","\\\\").Replace("\"","\\\"").Replace("\n","\\n")+"\"";
        if(o is bool)return o.ToString().ToLowerInvariant();
        if(o is Vector3){var v=(Vector3)o;return Json(new[]{v.x,v.y,v.z});}
        if(o is System.Collections.IDictionary){var d=(System.Collections.IDictionary)o;return "{"+string.Join(",",d.Keys.Cast<object>().Select(k=>Json(k.ToString())+":"+Json(d[k])))+"}";}
        if(o is System.Collections.IEnumerable)return "["+string.Join(",",((System.Collections.IEnumerable)o).Cast<object>().Select(Json))+"]";
        if(o.GetType().IsPrimitive || o is decimal)return Convert.ToString(o,System.Globalization.CultureInfo.InvariantCulture);
        return "{"+string.Join(",",o.GetType().GetProperties().Select(p=>Json(p.Name)+":"+Json(p.GetValue(o))))+"}";
    }
    static string Save(string name, object value){var json=Json(value);System.IO.File.WriteAllText("docs/validation/urp-v1/"+name,json);return json;}
    public static string State(string name){return Save(name+".json",Snapshot());}
    static BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static JemparinganGame Game => UnityEngine.Object.FindFirstObjectByType<JemparinganGame>();
    static object Get(object obj, string field) => obj.GetType().GetField(field, Flags).GetValue(obj);
    static object Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Flags).Invoke(obj,args);
    public static object Snapshot()
    {
        var g=Game; var cam=Camera.main;
        var sight=(JemparinganModernBowSight)Get(g,"_modernPhysicalSight");
        var aperture=sight==null?null:(Transform)Get(sight,"_aperture");
        var target=(JemparinganTarget)Get(g,"_target");
        return new { utc=DateTime.UtcNow.ToString("O"), frame=Time.frameCount,
            pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.GetType().Name,
            cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(c=>new {c.name,c.enabled,c.depth,renderType=c.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()?.renderType.ToString()}).ToArray(),
            shaders=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).GroupBy(m=>m.shader?.name??"MISSING").Select(q=>new {shader=q.Key,count=q.Count(),supported=q.All(m=>m.shader!=null&&m.shader.isSupported)}).ToArray(),
            fields=new[]{"_mode","_modeChosen","_shots","_hits","_score","_stage","_finished","_wasDrawing","_awaitStringRelease","_moloHits","_janggaHits","_awakHits","_modernDrawPower","_modernSightZeroed"}.ToDictionary(n=>n,n=>Get(g,n)),
            sightCalibrated=sight!=null&&sight.HasCalibration,
            reticleViewport=aperture==null?Vector3.zero:cam.WorldToViewportPoint(aperture.position),
            camera=cam.name, fov=cam.fieldOfView, cameraPosition=cam.transform.position,
            targetPosition=target.transform.position, sway=target.swayAmount,
            inputHeld=Input.GetMouseButton(0), arrowCam=g.GetComponent<JemparinganArrowFollowCamera>()?.IsActive,
            flying=UnityEngine.Object.FindObjectsByType<JemparinganArrow>(FindObjectsSortMode.None).Count(a=>a.IsFlying)
        };
    }
    public static object Aim(int mode, float x, float y)
    {
        var g=Game; Transform t=mode==1?((JemparinganTarget)Get(g,"_target")).transform:((JemparinganModernTarget[])Get(g,"_modernTargets"))[0].transform;
        Camera.main.transform.LookAt(t.TransformPoint(new Vector3(x,y,0)));
        return Snapshot();
    }
    public static async Task<object> Round(int mode)
    {
        var g=Game;g.ChooseMode((JemparinganGame.GameMode)mode);
        var rows=new System.Collections.Generic.List<object>();
        var target=(JemparinganTarget)Get(g,"_target");
        var modern=((JemparinganModernTarget[])Get(g,"_modernTargets"))[0];
        for(int i=0;i<12;i++)
        {
            int before=(int)Get(g,"_score"); int expected;
            Transform t=mode==1?target.transform:modern.transform;
            float y=mode==1?new[]{.145f,.075f,-.1f,0f}[i%4]:0;
            float x=mode==1?(i%4==3?.8f:0):new[]{0f,.12f,.23f,.35f,.47f,.7f}[i%6];
            expected=mode==1?new[]{3,2,1,0}[i%4]:new[]{10,8,6,4,2,0}[i%6];
            // Short, real projectile flight through the active runtime target.
            // Intentionally bypasses mouse input/optical aim for deterministic zone coverage.
            Vector3 contact=t.TransformPoint(new Vector3(x,y,-.8f));
            Call(g,"Shoot",contact,t.forward,.4f,false);
            float until=Time.realtimeSinceStartup+5;
            while(((bool)Call(g,"AnyArrowFlying") || (g.GetComponent<JemparinganArrowFollowCamera>()?.IsActive??false)) && Time.realtimeSinceStartup<until)await Task.Delay(15);
            await Task.Delay(90);
            int delta=(int)Get(g,"_score")-before;
            rows.Add(new {shot=i+1,expected,actual=delta,stage=(int)Get(g,"_stage")+1,shots=Get(g,"_shots"),pass=delta==expected});
        }
        return Save("round-"+mode+".json", new {method="Existing Shoot + real projectile Update/collision/callbacks; near-target injected origin, no physical input", rows,final=Snapshot()});
    }
    [System.Runtime.InteropServices.DllImport("libX11.so.6")] static extern IntPtr XOpenDisplay(IntPtr name);
    [System.Runtime.InteropServices.DllImport("libX11.so.6")] static extern int XFlush(IntPtr d);
    [System.Runtime.InteropServices.DllImport("libX11.so.6")] static extern int XCloseDisplay(IntPtr d);
    [System.Runtime.InteropServices.DllImport("libXtst.so.6")] static extern int XTestFakeButtonEvent(IntPtr d,uint b,bool pressed,ulong delay);
    [System.Runtime.InteropServices.DllImport("libXtst.so.6")] static extern int XTestFakeKeyEvent(IntPtr d,uint k,bool pressed,ulong delay);
    [System.Runtime.InteropServices.DllImport("libX11.so.6")] static extern uint XKeysymToKeycode(IntPtr d,ulong symbol);
    [System.Runtime.InteropServices.DllImport("libXtst.so.6")] static extern int XTestFakeMotionEvent(IntPtr d,int screen,int x,int y,ulong delay);
    static void Button(IntPtr d,bool pressed){if(pressed){var w=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));w.Focus();var r=w.position;XTestFakeMotionEvent(d,-1,(int)r.center.x,(int)r.center.y,0);}XTestFakeButtonEvent(d,1,pressed,0);XFlush(d);}
    public static async Task<string> Physical(int mode)
    {
        var g=Game;g.ChooseMode((JemparinganGame.GameMode)mode);Aim(mode,0,mode==1?.145f:0);
        EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
        var d=XOpenDisplay(IntPtr.Zero); if(d==IntPtr.Zero)return "NO DISPLAY";
        var rows=new System.Collections.Generic.Dictionary<string,object>();
        try{
            var rect=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).position;XTestFakeMotionEvent(d,-1,(int)rect.center.x,(int)rect.center.y,0);XFlush(d);Button(d,false);await Task.Delay(300);
            rows["idle"]=Snapshot();ScreenCapture.CaptureScreenshot("docs/validation/urp-v1/input-"+mode+"-idle.png");
            Button(d,true);await Task.Delay(140);
            rows["earlyFrame"]=Snapshot();
            await Task.Delay(1000);
            rows["full"]=Snapshot();ScreenCapture.CaptureScreenshot("docs/validation/urp-v1/input-"+mode+"-full.png");
            Button(d,false);await Task.Delay(110);
            rows["release"]=Snapshot();ScreenCapture.CaptureScreenshot("docs/validation/urp-v1/input-"+mode+"-arrowcam.png");
            await Task.Delay(2400); rows["resolved"]=Snapshot();
            Button(d,true);await Task.Delay(600);rows["beforeRestart"]=Snapshot();
            uint key=XKeysymToKeycode(d,114);XTestFakeKeyEvent(d,key,true,0);XFlush(d);await Task.Delay(100);XTestFakeKeyEvent(d,key,false,0);XFlush(d);
            await Task.Delay(300);rows["restartHeld"]=Snapshot();
            Button(d,false);await Task.Delay(700);rows["restartReleased"]=Snapshot();
            Button(d,true);await Task.Delay(650);Button(d,false);await Task.Delay(2500);rows["nextShot"]=Snapshot();
            return Save("physical-"+mode+".json",rows);
        }finally{Button(d,false);XCloseDisplay(d);}
    }
    public static async Task<string> HalfDraw(int mode)
    {
        var g=Game;g.ChooseMode((JemparinganGame.GameMode)mode);Aim(mode,0,0);
        var input=UnityEngine.Object.FindFirstObjectByType<DesktopBowInput>();
        var duration=input.GetType().GetField("modernFullDrawSeconds",Flags);float saved=(float)duration.GetValue(input);
        var d=XOpenDisplay(IntPtr.Zero);if(d==IntPtr.Zero)return "NO DISPLAY";
        try{
            duration.SetValue(input,2f);Button(d,true);int frame=Time.frameCount;float until=Time.realtimeSinceStartup+3;
            while(Time.frameCount<frame+3&&Time.realtimeSinceStartup<until)await Task.Delay(5);
            while((float)Get(g,"_modernDrawPower")<.45f&&Time.realtimeSinceStartup<until)await Task.Delay(5);
            var state=Snapshot();ScreenCapture.CaptureScreenshot("docs/validation/urp-v1/verified-half-"+mode+".png");
            await Task.Delay(100);g.ChooseMode((JemparinganGame.GameMode)mode);
            return Save("verified-half-"+mode+".json",state);
        }finally{duration.SetValue(input,saved);Button(d,false);XCloseDisplay(d);}
    }
    public static async Task<string> MotionAndFx()
    {
        var g=Game;g.ChooseMode(JemparinganGame.GameMode.Traditional);
        var t=(JemparinganTarget)Get(g,"_target");var cord=(Transform)Get(t,"_suspension");
        var rows=new System.Collections.Generic.List<object>();
        for(int stage=0;stage<3;stage++){
            if(stage>0)Call(g,"AdvanceStage");
            Vector3 first=t.transform.position;float maxTravel=0,maxError=0;
            for(int i=0;i<35;i++){
                await Task.Delay(60);maxTravel=Mathf.Max(maxTravel,Vector3.Distance(first,t.transform.position));
                Vector3 end=t.transform.TransformPoint(new Vector3(0,JemparinganTarget.TraditionalTop,0));
                maxError=Mathf.Max(maxError,Vector3.Distance(cord.TransformPoint(Vector3.up),end));
            }
            rows.Add(new {stage=stage+1,sway=t.swayAmount,maxTravel,ropeEndpointMaxError=maxError});
        }
        var fx=new System.Collections.Generic.List<object>();
        foreach(int mode in new[]{1,3}){
            g.ChooseMode((JemparinganGame.GameMode)mode);
            object target=mode==1?(object)t:((JemparinganModernTarget[])Get(g,"_modernTargets"))[0];
            var tr=((Component)target).transform;var sparks=(Transform[])Get(target,"_sparks");var ids=sparks.Select(v=>v.GetInstanceID()).ToArray();
            Call(g,"Shoot",tr.TransformPoint(new Vector3(0,mode==1?.145f:0,-.8f)),tr.forward,.4f,false);
            await Task.Delay(100);var early=sparks.Select(v=>new {scale=v.localScale,position=v.localPosition,active=v.gameObject.activeInHierarchy}).ToArray();
            await Task.Delay(110);var late=sparks.Select(v=>new {scale=v.localScale,position=v.localPosition,active=v.gameObject.activeInHierarchy}).ToArray();
            await Task.Delay(500);
            fx.Add(new {mode,count=sparks.Length,reused=ids.SequenceEqual(sparks.Select(v=>v.GetInstanceID())),early,late,expired=sparks.All(v=>!v.gameObject.activeInHierarchy),score=Get(g,"_score")});
        }
        return Save("motion-fx.json",new {motion=rows,fx});
    }
    public static async Task<object> Profile(int mode)
    {
        Game.ChooseMode((JemparinganGame.GameMode)mode); await Task.Delay(600);
        string[] names={"Main Thread","Render Thread","GC Allocated In Frame","Draw Calls Count","SetPass Calls Count","Triangles Count","Batches Count"};
        ProfilerCategory[] cats={ProfilerCategory.Internal,ProfilerCategory.Internal,ProfilerCategory.Memory,ProfilerCategory.Render,ProfilerCategory.Render,ProfilerCategory.Render,ProfilerCategory.Render};
        var rec=names.Select((n,i)=>ProfilerRecorder.StartNew(cats[i],n,2048)).ToArray();
        var start=Time.realtimeSinceStartup;int frame=Time.frameCount;
        await Task.Delay(500);
        var t=mode==1?((JemparinganTarget)Get(Game,"_target")).transform:((JemparinganModernTarget[])Get(Game,"_modernTargets"))[0].transform;
        Call(Game,"Shoot",t.TransformPoint(new Vector3(0,mode==1?.145f:0,-.8f)),t.forward,.4f,false);
        await Task.Delay(2300);
        Call(Game,"Shoot",t.TransformPoint(new Vector3(.8f,0,-.8f)),t.forward,.4f,false);
        await Task.Delay(3200);
        var result=names.Select((n,i)=>{
            var samples=rec[i].ToArray().Select(v=>(double)v.Value).OrderBy(v=>v).ToArray();
            return new {name=n,valid=rec[i].Valid,count=samples.Length,mean=samples.Length==0?0:samples.Average(),p95=samples.Length==0?0:samples[(int)((samples.Length-1)*.95)],max=samples.Length==0?0:samples.Last(),unit=rec[i].UnitType.ToString()};
        }).ToArray();
        foreach(var r in rec)r.Dispose();
        return Save("profile-"+mode+".json", new {mode,workload="Idle + one injected hit + one miss + Arrow Cam, Editor",seconds=Time.realtimeSinceStartup-start,frames=Time.frameCount-frame,width=Screen.width,height=Screen.height,quality=QualitySettings.names[QualitySettings.GetQualityLevel()],vsync=QualitySettings.vSyncCount,frameCap=Application.targetFrameRate,metrics=result});
    }
}
