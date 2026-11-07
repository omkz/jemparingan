// QA script evaluated inside the Linux development player; never imported into Assets.
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using Unity.Profiling;
public static class NativeURPQA
{
    const string Output="/home/omz/Downloads/Jemparingan/docs/validation/urp-v1";
    static BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static JemparinganGame Game=>UnityEngine.Object.FindFirstObjectByType<JemparinganGame>();
    static object Get(string n)=>typeof(JemparinganGame).GetField(n,Flags).GetValue(Game);
    static string Json(object value){
        if(value==null)return "null";
        if(value is string||value.GetType().IsEnum)return "\""+value.ToString().Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"";
        if(value is bool)return value.ToString().ToLowerInvariant();
        if(value is System.Collections.IDictionary){var d=(System.Collections.IDictionary)value;return "{"+string.Join(",",d.Keys.Cast<object>().Select(k=>Json(k.ToString())+":"+Json(d[k])))+"}";}
        if(value is System.Collections.IEnumerable)return "["+string.Join(",",((System.Collections.IEnumerable)value).Cast<object>().Select(Json))+"]";
        if(value.GetType().IsPrimitive||value is decimal)return Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture);
        return "{"+string.Join(",",value.GetType().GetProperties().Select(p=>Json(p.Name)+":"+Json(p.GetValue(value))))+"}";
    }
    static void Save(string n,object value)=>File.WriteAllText(Output+"/"+n,Json(value));
    static object State()=>new {frame=Time.frameCount,width=Screen.width,height=Screen.height,quality=QualitySettings.names[QualitySettings.GetQualityLevel()],platform=Application.platform.ToString(),pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.GetType().Name,
        fields=new[]{"_mode","_modeChosen","_shots","_hits","_score","_stage","_finished","_wasDrawing","_awaitStringRelease","_modernDrawPower"}.ToDictionary(n=>n,n=>Get(n)),
        held=Input.GetMouseButton(0),focused=Application.isFocused,
        arrowCam=Game.GetComponent<JemparinganArrowFollowCamera>()?.IsActive??false,
        flying=UnityEngine.Object.FindObjectsByType<JemparinganArrow>(FindObjectsSortMode.None).Count(a=>a.IsFlying),
        shaders=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).GroupBy(m=>m.shader?.name??"MISSING").Select(q=>new {shader=q.Key,count=q.Count(),supported=q.All(m=>m.shader!=null&&m.shader.isSupported)}).ToArray(),
        lens=UnityEngine.Object.FindFirstObjectByType<JemparinganModernBowSight>()!=null};
    [DllImport("libX11.so.6")] static extern IntPtr XOpenDisplay(IntPtr name);
    [DllImport("libX11.so.6")] static extern int XCloseDisplay(IntPtr d);
    [DllImport("libX11.so.6")] static extern int XFlush(IntPtr d);
    [DllImport("libX11.so.6")] static extern int XRaiseWindow(IntPtr d,ulong w);
    [DllImport("libX11.so.6")] static extern int XSetInputFocus(IntPtr d,ulong w,int revert,ulong time);
    [DllImport("libX11.so.6")] static extern ulong XDefaultRootWindow(IntPtr d);
    [DllImport("libX11.so.6")] static extern uint XKeysymToKeycode(IntPtr d,ulong symbol);
    [DllImport("libX11.so.6")] static extern int XGetGeometry(IntPtr d,ulong w,out ulong root,out int x,out int y,out uint width,out uint height,out uint border,out uint depth);
    [DllImport("libX11.so.6")] static extern int XTranslateCoordinates(IntPtr d,ulong source,ulong dest,int x,int y,out int rx,out int ry,out ulong child);
    [DllImport("libXtst.so.6")] static extern int XTestFakeButtonEvent(IntPtr d,uint button,bool pressed,ulong delay);
    [DllImport("libXtst.so.6")] static extern int XTestFakeMotionEvent(IntPtr d,int screen,int x,int y,ulong delay);
    [DllImport("libXtst.so.6")] static extern int XTestFakeKeyEvent(IntPtr d,uint key,bool pressed,ulong delay);
    static void Button(IntPtr d,bool held){XTestFakeButtonEvent(d,1,held,0);XFlush(d);}
    static async Task Focus(IntPtr d,ulong window){
        var processType=Type.GetType("System.Diagnostics.Process, System",true);
        var process=processType.GetMethod("Start",new[]{typeof(string),typeof(string)}).Invoke(null,new object[]{"/usr/bin/wmctrl","-ia 0x"+window.ToString("x")});
        processType.GetMethod("WaitForExit",Type.EmptyTypes).Invoke(process,null);((IDisposable)process).Dispose();
        XRaiseWindow(d,window);XSetInputFocus(d,window,2,0);
        ulong root,child;int x,y,rx,ry;uint width,height,border,depth;
        XGetGeometry(d,window,out root,out x,out y,out width,out height,out border,out depth);
        XTranslateCoordinates(d,window,XDefaultRootWindow(d),(int)width/2,(int)height/2,out rx,out ry,out child);
        XTestFakeMotionEvent(d,-1,rx,ry,0);XFlush(d);await Task.Delay(120);
    }
    static async Task Key(IntPtr d,ulong key){uint code=XKeysymToKeycode(d,key);XTestFakeKeyEvent(d,code,true,0);XFlush(d);await Task.Delay(80);XTestFakeKeyEvent(d,code,false,0);XFlush(d);await Task.Delay(120);}
    static async Task WaitResolved(){
        float deadline=Time.realtimeSinceStartup+7;
        while((UnityEngine.Object.FindObjectsByType<JemparinganArrow>(FindObjectsSortMode.None).Any(a=>a.IsFlying)||(Game.GetComponent<JemparinganArrowFollowCamera>()?.IsActive??false))&&Time.realtimeSinceStartup<deadline)await Task.Delay(30);
        await Task.Delay(160);
    }
    static async Task Capture(string name){ScreenCapture.CaptureScreenshot(Output+"/"+name+".png");int f=Time.frameCount;float deadline=Time.realtimeSinceStartup+2;while(Time.frameCount<f+2&&Time.realtimeSinceStartup<deadline)await Task.Delay(10);}
    public static async Task<object> Run(int mode,ulong window)
    {
        var d=XOpenDisplay(IntPtr.Zero);if(d==IntPtr.Zero)throw new Exception("No X11 display");
        bool background=Application.runInBackground;Application.runInBackground=true;
        var attempts=new System.Collections.Generic.List<object>();
        try{
            await Focus(d,window);Button(d,false);await Task.Delay(120);
            // Real keyboard mode selection from the startup menu.
            if(!File.Exists(Output+"/native-startup.png"))await Capture("native-startup");
            if(!(bool)Get("_modeChosen"))await Key(d,mode==1?49ul:50ul);
            if(!(bool)Get("_modeChosen")||(int)(JemparinganGame.GameMode)Get("_mode")!=mode)throw new Exception("Mode key was not received");
            object initial=State();
            while((int)Get("_shots")<12&&attempts.Count<16){
                int before=(int)Get("_shots");await Focus(d,window);
                // Aim setup only: projectile origin/speed and draw/release remain the game's own.
                Transform target=mode==1?((JemparinganTarget)Get("_target")).transform:((JemparinganModernTarget[])Get("_modernTargets"))[0].transform;
                Camera.main.transform.LookAt(target.TransformPoint(new Vector3(0,mode==1?.145f:0,0)));
                Button(d,true);await Task.Delay(650);object held=State();bool received=Input.GetMouseButton(0)&&(bool)Get("_wasDrawing");
                if(before==0)await Capture("native-full-draw");
                Button(d,false);await Task.Delay(100);object release=State();
                if(before==0)await Capture("native-arrow-cam");
                await WaitResolved();int after=(int)Get("_shots");
                attempts.Add(new {before,received,held,release,resolved=State(),exactlyOne=after==before+1});
                if(after==4||after==8)await Capture("native-stage-"+(after/4+1));
                if(after==before&&!received)continue;
                if(after!=before+1)throw new Exception("Input attempt did not fire exactly one arrow; see native-round.json");
            }
            await WaitResolved();object results=State();await Capture("native-results");
            bool finished=(bool)Get("_finished")&&(int)Get("_shots")==12;
            await Focus(d,window);await Key(d,114);object restart=State();await Capture("native-restart");
            var report=new {method="Real X11 mode key, LMB draw/release and R; camera orientation aimed by QA; no Shoot/collision injection",mode,initial,attempts,results,finished,restart,restarted=(int)Get("_shots")==0&&!(bool)Get("_finished")};
            Save("native-round.json",report);return report;
        }catch(Exception e){Save("native-round.json",new {error=e.ToString(),attempts,state=State()});throw;}
        finally{Button(d,false);XCloseDisplay(d);Application.runInBackground=background;}
    }
}
return await NativeURPQA.Run(3, ulong.Parse(File.ReadAllText("/tmp/jemparingan-urp-v1/window-id.txt").Trim(),System.Globalization.NumberStyles.HexNumber)); // Set the current QA player X11 window ID before evaluation.
