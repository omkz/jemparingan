#if UNITY_EDITOR && META_XR_SDK && ENABLE_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Drives the actual desktop adapter/game with virtual Input System devices.
// No native OS input, headset state, or scene asset is modified.
public static class JemparinganDesktopInputQA
{
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static object Get(object obj, string field) => obj.GetType().GetField(field, Flags).GetValue(obj);
    private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Flags).SetValue(obj, value);

    [MenuItem("Tools/Jemparingan/Quest VR v1/Run Desktop Input System Regression")]
    public static async void Run()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().path != JemparinganQuestSetup.DesktopScene)
            throw new InvalidOperationException("Enter Play Mode in the original desktop scene first.");
        var originalMouse = Mouse.current;
        var originalKeyboard = Keyboard.current;
        var mouse = InputSystem.AddDevice<Mouse>("JemparinganQA_Mouse");
        var keyboard = InputSystem.AddDevice<Keyboard>("JemparinganQA_Keyboard");
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        var rows = new List<object>();
        bool pass = true;
        var game = Object.FindFirstObjectByType<JemparinganGame>();
        bool arrowCam = (bool)Get(game, "modernEveryShotArrowCam");
        try
        {
            Set(game, "modernEveryShotArrowCam", false);
            InputSystem.QueueStateEvent(mouse, new MouseState());
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await Wait(.15f);
            // Exercises menu selection through the keyboard path.
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit1));
            await Wait(.15f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            bool selected = (bool)Get(game, "_modeChosen");
            rows.Add(new { check = "Keyboard 1 selects Traditional", pass = selected });
            pass &= selected;
            foreach (var mode in new[] { JemparinganGame.GameMode.Traditional, JemparinganGame.GameMode.Modern })
            {
                game.ChooseMode(mode);
                for (int i = 0; i < 10; ++i)
                {
                    int before = (int)Get(game, "_shots");
                    InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                    await Wait(.65f);
                    bool heldDoesNotFire = (int)Get(game, "_shots") == before;
                    InputSystem.QueueStateEvent(mouse, new MouseState());
                    await Wait(3.4f);
                    int after = (int)Get(game, "_shots");
                    bool shotPass = heldDoesNotFire && after == before + 1;
                    pass &= shotPass;
                    rows.Add(new { mode = mode.ToString(), shot = i + 1, before, after, heldDoesNotFire, pass = shotPass });
                }
                // R while drawing must reset and require release before a new shot.
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                await Wait(.3f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
                await Wait(.15f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                bool reset = (int)Get(game, "_shots") == 0 && (bool)Get(game, "_awaitStringRelease");
                InputSystem.QueueStateEvent(mouse, new MouseState());
                await Wait(.2f);
                reset &= (int)Get(game, "_shots") == 0;
                pass &= reset;
                rows.Add(new { mode = mode.ToString(), check = "R during held draw resets without release shot", pass = reset });
            }
            Save(new { pass, method = "Actual DesktopBowInput + JemparinganGame in original scene with virtual Input System mouse/keyboard; 10 releases per mode. No physical mouse or Quest input claim.", rows });
            Debug.Log("Desktop Input System regression " + (pass ? "PASS" : "FAIL"));
        }
        catch (Exception e) { Save(new { pass = false, error = e.ToString(), rows }); Debug.LogException(e); }
        finally
        {
            Set(game, "modernEveryShotArrowCam", arrowCam);
            InputSystem.RemoveDevice(mouse);
            InputSystem.RemoveDevice(keyboard);
            originalMouse?.MakeCurrent();
            originalKeyboard?.MakeCurrent();
            Application.runInBackground = background;
        }
    }

    private static void Save(object result) => File.WriteAllText(JemparinganQuestSetup.Evidence + "desktop-input-regression.json", JsonConvert.SerializeObject(result, Formatting.Indented));
    private static async Task Wait(float seconds)
    {
        float until = Time.realtimeSinceStartup + seconds;
        int start = Time.frameCount;
        while (EditorApplication.isPlaying && Time.realtimeSinceStartup < until)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            await Task.Delay(15);
        }
        if (!EditorApplication.isPlaying || Time.frameCount <= start) throw new Exception("Play Mode stopped or did not advance.");
    }
}
#endif
