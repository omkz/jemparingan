// Run in memory through Unity MCP script_run; outside Assets, never in a player.
// DrawProgress requires a real held mouse button. It adjusts only the existing
// desktop timer to capture reproducible poses; it does not inject gameplay poses.
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
public static class TraditionalArmsValidation
{
    const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    static JemparinganGame Game => UnityEngine.Object.FindFirstObjectByType<JemparinganGame>();
    static object Get(string n) => typeof(JemparinganGame).GetField(n, Flags).GetValue(Game);
    static object Call(string n, params object[] a) => typeof(JemparinganGame).GetMethod(n, Flags).Invoke(Game, a);
    static T ArcherField<T>(string n)
    {
        var archer = (JemparinganTraditionalArcher)Get("_traditionalArcher");
        return (T)typeof(JemparinganTraditionalArcher).GetField(n, Flags).GetValue(archer);
    }
    static Vector3 Ring(Vector3[] vertices, int ring)
    {
        Vector3 center=Vector3.zero;
        for(int i=0;i<8;i++) center+=vertices[ring*8+i];
        return center/8f;
    }
    public static object Snapshot()
    {
        var root=(Transform)Get("_traditionalPosture");
        var left=ArcherField<Transform>("_leftHand");
        var handle=ArcherField<Transform>("_handle");
        var nock=(Transform)Get("_stringMarker");
        var l=ArcherField<Mesh>("_leftSleeve").vertices;
        var visuals=ArcherField<Transform>("_visuals");
        var arrow=(Transform)Get("_nockedArrow");
        bool visible=root.gameObject.activeSelf;
        float leftError=Vector3.Distance(left.position,handle.TransformPoint(Vector3.down*.55f));
        float forearmLength=l.Length>0?Vector3.Distance(Ring(l,0),Ring(l,4)):0f;
        int rightRenderers=visuals.GetComponentsInChildren<Renderer>(true)
            .Count(x=>x.name.IndexOf("right",StringComparison.OrdinalIgnoreCase)>=0);
        float arrowNockError=Vector3.Distance(nock.position,arrow.position);
        var result=new { mode=Get("_mode").ToString(), arrowsPerRound=Get("arrowsPerRound"),
            frame=Time.frameCount, time=Time.time, shots=Get("_shots"), hits=Get("_hits"), score=Get("_score"),
            molo=Get("_moloHits"), jangga=Get("_janggaHits"), awak=Get("_awakHits"),
            finished=Get("_finished"), drawing=Get("_wasDrawing"), bodyVisible=visible,
            cameraAngles=Camera.main.transform.eulerAngles.ToString("F3"), seatAngles=root.eulerAngles.ToString("F3"),
            leftGripError=leftError, arrowNockError=arrowNockError,
            visibleForearmLength=forearmLength,rightPresentationRenderers=rightRenderers,
            targetSway=((JemparinganTarget)Get("_target")).swayAmount,
            targetSpeed=((JemparinganTarget)Get("_target")).swaySpeed,
            classicTarget=((GameObject)Get("_classicBandulanVisual")).activeSelf,
            traditionalTarget=((GameObject)Get("_traditionalBandulanVisual")).activeSelf };
        if(visible && (leftError>.0001f || arrowNockError>.0001f ||
            forearmLength<.20f || forearmLength>.45f || rightRenderers!=0))
            throw new Exception("First-person presentation regression: "+result);
        Debug.Log("Traditional arm visual QA: mode="+Get("_mode")+" shots="+Get("_shots")+
            " gripError="+leftError+" arrowNockError="+arrowNockError+
            " forearm="+forearmLength+" rightRenderers="+rightRenderers);
        return result;
    }
    public static async Task<object> Mode(string mode)
    {
        Time.timeScale=1f;
        Call("ChooseMode", Enum.Parse(typeof(JemparinganGame.GameMode),mode));
        await Task.Delay(120); return Snapshot();
    }
    public static object Focus()
    {
        var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var window=EditorWindow.GetWindow(type);window.Focus();
        return new { rect=window.position.ToString(), focused=Application.isFocused };
    }
    public static async Task<object> DrawProgress(float fraction)
    {
        if(!Input.GetMouseButton(0)) throw new Exception("A real held mouse button is required");
        Time.timeScale=1f;
        var input=UnityEngine.Object.FindFirstObjectByType<DesktopBowInput>();
        var start=typeof(DesktopBowInput).GetField("_drawStart",Flags);
        start.SetValue(input,Time.time-fraction);
        await Task.Delay(100);
        Time.timeScale=0f;
        start.SetValue(input,Time.time-fraction);
        await Task.Delay(100); return Snapshot();
    }

    // The Linux desktop used for automated validation can focus the Game View,
    // but XTest button events are not reported by Unity's legacy Input API.
    // This validation-only component feeds a deterministic hand pose through
    // the production IJemparinganInput path; the game still updates the real
    // bow, string, arrow preview and left-hand presentation in its normal Update loop.
    sealed class ValidationBowInput : DesktopBowInput, IJemparinganInput
    {
        public float Progress;
        public new bool TryReadPose(out JemparinganHandPose pose)
        {
            Transform cam=Camera.main.transform;
            Vector3 forward=cam.forward.normalized;
            Vector3 bow=cam.position+forward*.92f-cam.right*.33f-cam.up*.18f;
            pose=new JemparinganHandPose {
                BowPinched=true,
                StringPinched=Progress>0f,
                BowPosition=bow,
                StringPosition=bow-forward*(.04f+Mathf.Clamp01(Progress)*.38f),
                AimDirection=forward
            };
            return true;
        }
    }
    public static async Task<object> Pose(float fraction)
    {
        Time.timeScale=1f;
        var original=UnityEngine.Object.FindObjectsByType<DesktopBowInput>(
            FindObjectsInactive.Include,FindObjectsSortMode.None)
            .First(x=>x.GetType()==typeof(DesktopBowInput));
        foreach(var old in UnityEngine.Object.FindObjectsByType<ValidationBowInput>(
            FindObjectsInactive.Include,FindObjectsSortMode.None))
            UnityEngine.Object.Destroy(old);
        var fake=original.gameObject.AddComponent<ValidationBowInput>();
        fake.Progress=Mathf.Clamp01(fraction);
        typeof(JemparinganGame).GetField("desktopInput",Flags).SetValue(Game,fake);
        await Task.Delay(180);
        Time.timeScale=0f;
        return Snapshot();
    }
    public static async Task<object> RestoreInput()
    {
        Time.timeScale=1f;
        var original=UnityEngine.Object.FindObjectsByType<DesktopBowInput>(
            FindObjectsInactive.Include,FindObjectsSortMode.None)
            .First(x=>x.GetType()==typeof(DesktopBowInput));
        typeof(JemparinganGame).GetField("desktopInput",Flags).SetValue(Game,original);
        foreach(var old in UnityEngine.Object.FindObjectsByType<ValidationBowInput>(
            FindObjectsInactive.Include,FindObjectsSortMode.None))
            UnityEngine.Object.Destroy(old);
        await Task.Delay(120);
        return Snapshot();
    }
    public static async Task<object> Rotate(float yaw,float pitch)
    {
        Camera.main.transform.rotation=Quaternion.Euler(pitch,yaw,0f);
        Time.timeScale=1f;
        await Task.Delay(650);
        Time.timeScale=0f;
        return Snapshot();
    }
    // Runtime regression fallback when OS focus prevents sustained native
    // input automation. Invokes the existing launch path, then waits for the
    // real projectile/target simulation; it does not set scores or shot counts.
    public static async Task<object> FireThroughExistingShoot()
    {
        Time.timeScale=1f;
        var input=UnityEngine.Object.FindFirstObjectByType<DesktopBowInput>();
        if(!input.TryReadPose(out var pose)) throw new Exception("No desktop pose");
        Call("Shoot",pose.BowPosition+pose.AimDirection*.25f,pose.AimDirection,
            (float)Get("maximumDraw"),false);
        await Task.Delay(100);
        float deadline=Time.realtimeSinceStartup+5f;
        while(((JemparinganArrow[])Get("_arrows")).Any(a=>a.IsFlying))
        {
            if(Time.realtimeSinceStartup>deadline) throw new Exception("Projectile did not settle");
            await Task.Delay(50);
        }
        await Task.Delay(100);
        return Snapshot();
    }

    // Fires the production projectile at a real Traditional scoring zone.
    // The helper computes only the release direction; Shoot, arrow pooling,
    // gravity, swept collision, scoring and callbacks remain unmodified.
    public static async Task<object> FireAtZone(float zoneY)
    {
        Time.timeScale=1f;
        var bow=(Transform)Get("_bow");
        Vector3 origin=bow.position+Camera.main.transform.forward*.25f;
        Vector3 target=((JemparinganTarget)Get("_target")).transform.TransformPoint(
            new Vector3(0f,zoneY,0f));
        float speed=(float)Get("maximumArrowSpeed"),gravity=(float)Get("simulatedGravity");
        Vector3 delta=target-origin;
        Vector3 flat=Vector3.ProjectOnPlane(delta,Vector3.up);
        float distance=flat.magnitude,speed2=speed*speed;
        float discriminant=speed2*speed2-gravity*(gravity*distance*distance+2f*delta.y*speed2);
        if(discriminant<=0f) throw new Exception("Zone is not ballistically reachable");
        float tangent=(speed2-Mathf.Sqrt(discriminant))/(gravity*distance);
        float angle=Mathf.Atan(tangent);
        Vector3 direction=(flat.normalized*Mathf.Cos(angle)+Vector3.up*Mathf.Sin(angle)).normalized;
        Call("Shoot",origin,direction,(float)Get("maximumDraw"),false);
        await Task.Delay(100);
        float deadline=Time.realtimeSinceStartup+5f;
        while(((JemparinganArrow[])Get("_arrows")).Any(a=>a.IsFlying))
        {
            if(Time.realtimeSinceStartup>deadline) throw new Exception("Projectile did not settle");
            await Task.Delay(50);
        }
        await Task.Delay(100);
        return Snapshot();
    }

    // Aim the *camera/input* at a zone for a real mouse-release shot. This
    // samples the game's unmodified assistance and exact ballistic curve.
    // Arrow tip offset and target sway are included; no physics fields change.
    public static object Aim(float zoneY)
    {
        var target=(JemparinganTarget)Get("_target");
        float speed=(float)Get("maximumArrowSpeed"), gravity=(float)Get("simulatedGravity");
        var cam=Camera.main.transform;
        float yaw=0f,pitch=-2f;
        Func<float,float,Vector2> error=(y,p)=> {
            cam.rotation=Quaternion.Euler(p,y,0f);
            Vector3 origin=cam.position+cam.forward*1.17f-cam.right*.33f-cam.up*.18f;
            Vector3 direction=(Vector3)Call("GetAssistedAim",origin,cam.forward,speed);
            float lo=0f,hi=4f;
            Vector3 tip=origin;
            for(int i=0;i<30;i++) {
                float t=(lo+hi)*.5f;
                Vector3 velocity=direction*speed-Vector3.up*(gravity*t);
                tip=JemparinganTrajectory.Position(origin,direction*speed,gravity,t)+velocity.normalized*.36f;
                if(tip.z<target.transform.position.z)lo=t;else hi=t;
            }
            Vector3 center=target.PredictCenter((lo+hi)*.5f)+target.transform.up*zoneY;
            return new Vector2(tip.x-center.x,tip.y-center.y);
        };
        for(int i=0;i<12;i++) {
            Vector2 e=error(yaw,pitch),dy=(error(yaw+.01f,pitch)-e)/.01f,dp=(error(yaw,pitch+.01f)-e)/.01f;
            float det=dy.x*dp.y-dp.x*dy.y;
            if(Mathf.Abs(det)<.000001f)break;
            yaw-=Mathf.Clamp((e.x*dp.y-dp.x*e.y)/det,-10f,10f);
            pitch-=Mathf.Clamp((dy.x*e.y-e.x*dy.y)/det,-10f,10f);
        }
        Vector2 residual=error(yaw,pitch);
        return new {yaw,pitch,residual=residual.ToString("F4"),zoneY};
    }
}
