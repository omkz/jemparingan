return new System.Collections.Generic.Dictionary<string,object> {
 {"pipelineInstance",UnityEngine.Rendering.RenderPipelineManager.currentPipeline?.GetType().FullName},
 {"pipelineAsset",UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.name},
 {"cpu",UnityEngine.SystemInfo.processorType},{"gpu",UnityEngine.SystemInfo.graphicsDeviceName},
 {"graphicsAPI",UnityEngine.SystemInfo.graphicsDeviceType.ToString()},
 {"width",UnityEngine.Screen.width},{"height",UnityEngine.Screen.height},
 {"quality",UnityEngine.QualitySettings.names[UnityEngine.QualitySettings.GetQualityLevel()]},
 {"audioListeners",UnityEngine.Object.FindObjectsByType<UnityEngine.AudioListener>(UnityEngine.FindObjectsSortMode.None).Length},
 {"unsupportedRendererMaterials",UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None).SelectMany(r=>r.sharedMaterials).Count(m=>m==null||m.shader==null||!m.shader.isSupported||m.shader.name=="Hidden/InternalErrorShader")},
 {"lensShader",UnityEngine.Resources.Load<UnityEngine.Material>("Jemparingan/OpticalLens")?.shader?.name}
};
