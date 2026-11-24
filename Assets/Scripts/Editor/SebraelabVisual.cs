using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// Acabamento visual da cena (chamado pelo configurador):
/// - URP com HDR, MSAA 4x, sombras suaves do sol e das luzes do hub;
/// - sol com sombras suaves (o modelo do Sebraelab não tem teto fechado: as paredes e os móveis fazem sombra no chão),
///   céu procedural, luz ambiente em três tons e um reflection probe assado para os reflexos;
/// - pós-processamento (tonemapping, bloom para os livros brilharem, cor, vinheta), desligado no Quest;
/// - material e textura de partículas (brilhinhos dos livros e comemoração).
/// Tudo por reflexão/SerializedObject, porque este script compila antes de o URP estar instalado.
/// </summary>
internal static class SebraelabVisual
{
    private const string Pasta = "Assets/Settings";
    private const string CaminhoPerfil = Pasta + "/Sebraelab_PosProcessamento.asset";
    private const string CaminhoCeu = Pasta + "/Ceu_Sebraelab.mat";
    private const string CaminhoReflexos = Pasta + "/Reflexos_Sebraelab.exr";
    private const string CaminhoTexturaParticula = "Assets/Materials/Particula_Suave.png";
    private const string CaminhoMaterialParticula = "Assets/Materials/Particula_Brilho.mat";

    // =====================================================================
    // URP
    // =====================================================================

    /// <summary>Sombras suaves, HDR (para o bloom) e MSAA no asset do URP que estiver em uso.</summary>
    internal static void AjustarURP()
    {
        var pipeline = GraphicsSettings.defaultRenderPipeline;
        if (pipeline == null || pipeline.GetType().Name != "UniversalRenderPipelineAsset") return;

        var serializado = new SerializedObject(pipeline);
        Definir(serializado, "m_SupportsHDR", true);
        Definir(serializado, "m_MSAA", 4);
        Definir(serializado, "m_MainLightShadowsSupported", true);
        Definir(serializado, "m_MainLightShadowmapResolution", 2048);
        Definir(serializado, "m_ShadowDistance", 30f);
        Definir(serializado, "m_ShadowCascadeCount", 2);
        Definir(serializado, "m_SoftShadowsSupported", true);
        Definir(serializado, "m_AdditionalLightShadowsSupported", true);
        Definir(serializado, "m_AdditionalLightsShadowmapResolution", 1024);
        Definir(serializado, "m_AdditionalLightsPerObjectLimit", 8);
        serializado.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pipeline);
        AssetDatabase.SaveAssets();
    }

    private static void Definir(SerializedObject objeto, string campo, object valor)
    {
        var propriedade = objeto.FindProperty(campo);
        if (propriedade == null) return; // outra versão do URP: ignora o campo
        switch (valor)
        {
            case bool b: propriedade.boolValue = b; break;
            case int i: propriedade.intValue = i; break;
            case float f: propriedade.floatValue = f; break;
        }
    }

    // =====================================================================
    // Luz
    // =====================================================================

    /// <summary>Sol, céu, luz ambiente e reflexos. Retorna o sol (para ir para o grupo "Iluminação").</summary>
    internal static Light Iluminar(Bounds limitesDoCenario)
    {
        Light sol = null;
        foreach (var luz in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (luz.type == LightType.Directional) { sol = luz; break; }
        if (sol == null) sol = new GameObject("Sol", typeof(Light)).GetComponent<Light>();

        sol.name = "Sol";
        sol.type = LightType.Directional;
        sol.transform.rotation = Quaternion.Euler(50f, 140f, 0f);
        sol.color = new Color(1f, 0.95f, 0.87f);
        sol.intensity = 1.3f;
        sol.shadows = LightShadows.Soft;
        sol.shadowStrength = 0.8f;

        // Luz ambiente em três tons: céu frio em cima, chão quente embaixo (o interior não fica chapado).
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.6f, 0.66f, 0.76f);
        RenderSettings.ambientEquatorColor = new Color(0.47f, 0.46f, 0.45f);
        RenderSettings.ambientGroundColor = new Color(0.27f, 0.25f, 0.23f);
        RenderSettings.sun = sol;

        var ceu = Ceu();
        if (ceu != null) RenderSettings.skybox = ceu;

        CriarReflexos(limitesDoCenario);
        return sol;
    }

    private static Material Ceu()
    {
        var shader = Shader.Find("Skybox/Procedural");
        if (shader == null) return null;
        var material = AssetDatabase.LoadAssetAtPath<Material>(CaminhoCeu);
        if (material == null)
        {
            CriarPasta(Pasta);
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, CaminhoCeu);
        }
        material.SetFloat("_SunSize", 0.035f);
        material.SetFloat("_SunSizeConvergence", 6f);
        material.SetFloat("_AtmosphereThickness", 0.85f);
        material.SetColor("_SkyTint", new Color(0.55f, 0.64f, 0.82f));
        material.SetColor("_GroundColor", new Color(0.36f, 0.34f, 0.33f));
        material.SetFloat("_Exposure", 1.15f);
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>Reflection probe do tamanho do salão, assado uma vez (reflexos no chão e nos objetos brilhantes).</summary>
    private static void CriarReflexos(Bounds limites)
    {
        var objeto = new GameObject("Reflexos do salão", typeof(ReflectionProbe));
        objeto.transform.position = limites.center;
        var probe = objeto.GetComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Baked;
        probe.size = new Vector3(Mathf.Max(1f, limites.size.x), Mathf.Max(1f, limites.size.y), Mathf.Max(1f, limites.size.z));
        probe.boxProjection = true;
        probe.resolution = 128;
        probe.importance = 1;
        Undo.RegisterCreatedObjectUndo(objeto, "Criar reflexos");

        try
        {
            CriarPasta(Pasta);
            if (!Lightmapping.BakeReflectionProbe(probe, CaminhoReflexos))
                Debug.LogWarning("[Sebraelab] Não deu para assar os reflexos agora; a cena funciona sem eles.");
        }
        catch (Exception erro)
        {
            Debug.LogWarning("[Sebraelab] Reflexos não assados: " + erro.Message);
        }
    }

    // =====================================================================
    // Pós-processamento (URP Volume)
    // =====================================================================

    /// <summary>Volume global com tonemapping, bloom, cor e vinheta. Retorna o objeto (ou nulo sem URP).</summary>
    internal static GameObject CriarPosProcessamento()
    {
        var tipoVolume = Tipo("UnityEngine.Rendering.Volume");
        var tipoPerfil = Tipo("UnityEngine.Rendering.VolumeProfile");
        if (tipoVolume == null || tipoPerfil == null) return null;

        try
        {
            var perfil = AssetDatabase.LoadAssetAtPath(CaminhoPerfil, tipoPerfil);
            if (perfil == null)
            {
                CriarPasta(Pasta);
                perfil = ScriptableObject.CreateInstance(tipoPerfil);
                AssetDatabase.CreateAsset(perfil, CaminhoPerfil);
                var adicionar = tipoPerfil.GetMethod("Add", new[] { typeof(Type), typeof(bool) });

                Componente(perfil, adicionar, "UnityEngine.Rendering.Universal.Tonemapping", ("mode", "Neutral"));
                Componente(perfil, adicionar, "UnityEngine.Rendering.Universal.Bloom",
                           ("threshold", 1.3f), ("intensity", 0.5f), ("scatter", 0.65f)); // só o que brilha de verdade (livros)
                Componente(perfil, adicionar, "UnityEngine.Rendering.Universal.ColorAdjustments",
                           ("postExposure", 0.1f), ("contrast", 12f), ("saturation", 10f));
                Componente(perfil, adicionar, "UnityEngine.Rendering.Universal.WhiteBalance", ("temperature", 6f));
                Componente(perfil, adicionar, "UnityEngine.Rendering.Universal.Vignette", ("intensity", 0.2f), ("smoothness", 0.45f));
                EditorUtility.SetDirty(perfil);
                AssetDatabase.SaveAssets();
            }

            var objeto = new GameObject("Pós-processamento");
            var volume = objeto.AddComponent(tipoVolume);
            tipoVolume.GetField("isGlobal")?.SetValue(volume, true);
            tipoVolume.GetField("sharedProfile")?.SetValue(volume, perfil);
            Undo.RegisterCreatedObjectUndo(objeto, "Criar pós-processamento");
            return objeto;
        }
        catch (Exception erro)
        {
            Debug.LogWarning("[Sebraelab] Pós-processamento não configurado: " + erro.Message);
            return null;
        }
    }

    private static void Componente(Object perfil, MethodInfo adicionar, string nomeTipo, params (string campo, object valor)[] parametros)
    {
        var tipo = Tipo(nomeTipo);
        if (tipo == null || adicionar == null) return;

        var componente = (Object)adicionar.Invoke(perfil, new object[] { tipo, true });
        componente.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(componente, perfil); // os componentes ficam dentro do asset do perfil

        foreach (var (campo, valor) in parametros)
        {
            var parametro = tipo.GetField(campo)?.GetValue(componente);
            if (parametro == null) continue;
            var tipoValor = parametro.GetType().GetProperty("value")?.PropertyType;
            var sobrescrever = parametro.GetType().GetMethod("Override");
            if (tipoValor == null || sobrescrever == null) continue;

            object convertido = tipoValor.IsEnum ? Enum.Parse(tipoValor, (string)valor) : Convert.ChangeType(valor, tipoValor);
            sobrescrever.Invoke(parametro, new[] { convertido });
        }
        EditorUtility.SetDirty(componente);
    }

    /// <summary>Liga o pós-processamento nas câmeras (e SMAA na câmera do PC).</summary>
    internal static void PrepararCameras()
    {
        var tipoDados = Tipo("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
        if (tipoDados == null) return;

        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var dados = camera.GetComponent(tipoDados);
            if (dados == null) dados = camera.gameObject.AddComponent(tipoDados);
            tipoDados.GetProperty("renderPostProcessing")?.SetValue(dados, true);

            // A câmera do PC não usa MSAA do óculos: SMAA deixa as bordas lisas.
            if (camera.GetComponentInParent<DesktopPlayerController>() != null)
            {
                var propriedade = tipoDados.GetProperty("antialiasing");
                if (propriedade != null)
                    propriedade.SetValue(dados, Enum.Parse(propriedade.PropertyType, "SubpixelMorphologicalAntiAliasing"));
            }
            EditorUtility.SetDirty(camera.gameObject);
        }
    }

    // =====================================================================
    // Partículas
    // =====================================================================

    /// <summary>Material aditivo (brilho) com uma textura redonda e suave, para as partículas.</summary>
    internal static Material MaterialDeParticula()
    {
        var existente = AssetDatabase.LoadAssetAtPath<Material>(CaminhoMaterialParticula);
        if (existente != null) return existente;

        var textura = TexturaSuave();
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Material material;
        if (shader != null)
        {
            material = new Material(shader);
            material.SetTexture("_BaseMap", textura);
            material.SetColor("_BaseColor", Color.white);
            // Transparente e aditivo (o que o Inspector do URP faria ao escolher Surface = Transparent, Blend = Additive).
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        else
        {
            material = new Material(Shader.Find("Legacy Shaders/Particles/Additive")) { mainTexture = textura };
        }

        CriarPasta("Assets/Materials");
        AssetDatabase.CreateAsset(material, CaminhoMaterialParticula);
        return material;
    }

    private static Texture2D TexturaSuave()
    {
        var existente = AssetDatabase.LoadAssetAtPath<Texture2D>(CaminhoTexturaParticula);
        if (existente != null) return existente;

        const int lado = 64;
        var pixels = new Color32[lado * lado];
        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                float dx = (x + 0.5f) / lado * 2f - 1f, dy = (y + 0.5f) / lado * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - r);
                a = a * a * (3f - 2f * a); // centro forte, borda bem suave
                pixels[y * lado + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }
        var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        textura.SetPixels32(pixels);
        textura.Apply();

        CriarPasta("Assets/Materials");
        File.WriteAllBytes(CaminhoTexturaParticula, textura.EncodeToPNG());
        Object.DestroyImmediate(textura);
        AssetDatabase.ImportAsset(CaminhoTexturaParticula, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(CaminhoTexturaParticula) is TextureImporter importador)
        {
            importador.alphaIsTransparency = true;
            importador.wrapMode = TextureWrapMode.Clamp;
            importador.mipmapEnabled = true;
            importador.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(CaminhoTexturaParticula);
    }

    /// <summary>
    /// Sistema de partículas pronto: <paramref name="porSegundo"/> &gt; 0 = contínuo (brilhinhos);
    /// <paramref name="rajada"/> &gt; 0 = uma explosão só, quando tocado por código.
    /// </summary>
    internal static ParticleSystem Particulas(string nome, Transform pai, Vector3 posicaoLocal, float porSegundo, int rajada,
                                              float raio, Vector2 vida, Vector2 tamanho, Vector2 velocidade, float gravidade,
                                              Color corA, Color corB)
    {
        var objeto = new GameObject(nome);
        objeto.transform.SetParent(pai, false);
        objeto.transform.localPosition = posicaoLocal;
        var sistema = objeto.AddComponent<ParticleSystem>();

        var principal = sistema.main;
        principal.loop = porSegundo > 0f;
        principal.playOnAwake = porSegundo > 0f;
        principal.duration = 2f;
        principal.startLifetime = new ParticleSystem.MinMaxCurve(vida.x, vida.y);
        principal.startSize = new ParticleSystem.MinMaxCurve(tamanho.x, tamanho.y);
        principal.startSpeed = new ParticleSystem.MinMaxCurve(velocidade.x, velocidade.y);
        principal.startColor = new ParticleSystem.MinMaxGradient(corA, corB);
        principal.gravityModifier = gravidade;
        principal.simulationSpace = ParticleSystemSimulationSpace.World;
        principal.maxParticles = Mathf.Max(16, rajada + Mathf.CeilToInt(porSegundo * vida.y) + 8);

        var emissao = sistema.emission;
        emissao.rateOverTime = porSegundo;
        if (rajada > 0) emissao.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)rajada) });

        var forma = sistema.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = raio;

        // Aparece e some suavemente.
        var cor = sistema.colorOverLifetime;
        cor.enabled = true;
        var gradiente = new Gradient();
        gradiente.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                          new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        cor.color = new ParticleSystem.MinMaxGradient(gradiente);

        var renderer = objeto.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = MaterialDeParticula();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        if (porSegundo <= 0f) sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return sistema;
    }

    // =====================================================================
    // Utilitários
    // =====================================================================

    private static Type Tipo(string nomeCompleto)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var tipo = assembly.GetType(nomeCompleto, false);
            if (tipo != null) return tipo;
        }
        return null;
    }

    private static void CriarPasta(string caminho)
    {
        if (AssetDatabase.IsValidFolder(caminho)) return;
        var pai = Path.GetDirectoryName(caminho)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(pai) && !AssetDatabase.IsValidFolder(pai)) CriarPasta(pai);
        AssetDatabase.CreateFolder(pai, Path.GetFileName(caminho));
    }
}
