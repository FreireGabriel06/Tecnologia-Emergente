using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
using Amostra = UnityEditor.PackageManager.UI.Sample;
using InfoPacote = UnityEditor.PackageManager.PackageInfo;

/// <summary>
/// Configura o projeto inteiro sozinho: Sebraelab > Configurar projeto VR (automático).
/// Na primeira vez que o projeto é aberto, ele pergunta se pode configurar.
///
/// Etapas (o Unity recompila entre algumas delas; o configurador continua sozinho depois):
///   1. Instala o URP (Universal Render Pipeline).
///   2. Importa os "Starter Assets" do XR Interaction Toolkit (XR Origin com locomoção pronta).
///   3. Importa o "TMP Essential Resources" do TextMeshPro.
///   4. Cria e liga o asset do URP, ativa o OpenXR (PC e Android/Quest) com os controles do Quest,
///      nomeia a camada de teleporte e acerta as configurações de build do Quest.
///   5. Cria a cena Assets/Scenes/Sebraelab.unity com XR Origin, EventSystem de VR, jogador de teste do PC,
///      chão de teleporte, o modelo do Sebraelab (se estiver em Assets/Models) e um estande com livros de exemplo.
/// </summary>
[InitializeOnLoad]
public static class SebraelabConfigurador
{
    internal const string CaminhoCena = "Assets/Scenes/Sebraelab.unity";
    private const string PastaConfiguracoes = "Assets/Settings";
    private const string PastaMateriais = "Assets/Materials";

    private const string PacoteURP = "com.unity.render-pipelines.universal";
    private const string PacoteXRI = "com.unity.xr.interaction.toolkit";
    private const string NomePrefabXROrigin = "XR Origin (XR Rig)";

    private const string ChaveEmAndamento = "Sebraelab.Configurador.EmAndamento";
    private const string ChaveJaPerguntou = "Sebraelab.Configurador.JaPerguntou";
    private const string ChaveTentativa = "Sebraelab.Configurador.Tentativa.";
    private const int MaximoTentativas = 2;

    private static AddRequest pedidoURP;
    private static double horaProximaEtapa;
    private static int esperasPelaRecompilacao;

    static SebraelabConfigurador()
    {
        EditorApplication.update += Atualizar;
        EditorApplication.delayCall += PerguntarNaPrimeiraVez;
    }

    // =====================================================================
    // Início
    // =====================================================================

    [MenuItem("Sebraelab/Configurar projeto VR (automático)", false, -100)]
    public static void Iniciar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Sebraelab", "Saia do modo Play antes de configurar o projeto.", "OK");
            return;
        }

        foreach (var etapa in new[] { "URP", "StarterAssets", "TMP" }) SessionState.EraseInt(ChaveTentativa + etapa);
        SessionState.SetBool(ChaveEmAndamento, true);
        horaProximaEtapa = 0;
        Debug.Log("[Sebraelab] Configuração automática iniciada. O Unity vai recompilar algumas vezes; não feche o editor.");
    }

    private static void PerguntarNaPrimeiraVez()
    {
        if (Application.isBatchMode || SessionState.GetBool(ChaveEmAndamento, false)) return;
        if (SessionState.GetBool(ChaveJaPerguntou, false)) return;
        SessionState.SetBool(ChaveJaPerguntou, true);

        if (File.Exists(CaminhoCena)) return; // projeto já configurado

        bool configurar = EditorUtility.DisplayDialog(
            "Sebraelab VR",
            "Este projeto ainda não foi configurado.\n\n" +
            "O configurador vai instalar o URP, importar o XR Origin do XR Interaction Toolkit, ativar o OpenXR para PC e " +
            "Meta Quest e criar a cena Assets/Scenes/Sebraelab.unity com o jogador, o chão, um estande e livros de exemplo.\n\n" +
            "Leva alguns minutos e o Unity recompila algumas vezes. Configurar agora?",
            "Configurar agora", "Depois");

        if (configurar) Iniciar();
        else Debug.Log("[Sebraelab] Quando quiser configurar, use o menu Sebraelab > Configurar projeto VR (automático).");
    }

    // =====================================================================
    // Máquina de etapas (roda no EditorApplication.update)
    // =====================================================================

    private static void Atualizar()
    {
        if (!SessionState.GetBool(ChaveEmAndamento, false)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (pedidoURP != null && !pedidoURP.IsCompleted) return;
        if (EditorApplication.timeSinceStartup < horaProximaEtapa) return;

        horaProximaEtapa = EditorApplication.timeSinceStartup + 1.0;

        try
        {
            ExecutarProximaEtapa();
        }
        catch (Exception erro)
        {
            SessionState.SetBool(ChaveEmAndamento, false);
            EditorUtility.ClearProgressBar();
            Debug.LogException(erro);
            if (SebraelabAutomatico.Ativo)
            {
                SebraelabAutomatico.ConfiguracaoFalhou(erro);
                return;
            }
            EditorUtility.DisplayDialog("Sebraelab",
                "A configuração automática parou por causa de um erro (detalhes no Console).\n" +
                "Depois de resolver, rode de novo: Sebraelab > Configurar projeto VR (automático).", "OK");
        }
    }

    private static void ExecutarProximaEtapa()
    {
        // 1. URP
        if (pedidoURP != null)
        {
            if (pedidoURP.Status == StatusCode.Failure)
                Debug.LogWarning($"[Sebraelab] Não foi possível instalar o URP: {pedidoURP.Error?.message}. O projeto continua no pipeline padrão.");
            pedidoURP = null;
            horaProximaEtapa = EditorApplication.timeSinceStartup + 5.0; // dá tempo do Unity começar a recompilar
            return;
        }

        if (!PacoteInstalado(PacoteURP))
        {
            if (NovaTentativa("URP"))
            {
                Progresso("Instalando o URP (Universal Render Pipeline)...", 0.1f);
                pedidoURP = Client.Add(PacoteURP);
                return;
            }
        }
        else if (BuscarTipo("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset") == null && esperasPelaRecompilacao++ < 60)
        {
            return; // pacote instalado, esperando o Unity recompilar
        }

        // 2. Starter Assets do XR Interaction Toolkit
        if (CaminhoPrefabXROrigin() == null && NovaTentativa("StarterAssets"))
        {
            Progresso("Importando os Starter Assets do XR Interaction Toolkit...", 0.35f);
            ImportarStarterAssets();
            horaProximaEtapa = EditorApplication.timeSinceStartup + 3.0;
            return;
        }

        // 3. TextMeshPro
        if (!TMPEssenciaisInstalados() && NovaTentativa("TMP"))
        {
            Progresso("Importando o TMP Essential Resources...", 0.55f);
            ImportarTMPEssenciais();
            horaProximaEtapa = EditorApplication.timeSinceStartup + 3.0;
            return;
        }

        // 4 e 5. Configurações e cena
        var avisos = new List<string>();
        Progresso("Configurando URP, OpenXR e Quest...", 0.7f);
        Tentar("URP", ConfigurarURP, avisos);
        Tentar("OpenXR", ConfigurarXR, avisos);
        Tentar("camada de teleporte", NomearCamadaTeleporte, avisos);
        Tentar("configurações do Quest", ConfigurarPlayer, avisos);

        Progresso("Criando a cena do Sebraelab...", 0.85f);
        if (!CriarCena()) avisos.Add("cena (não foi criada porque a cena aberta não foi salva)");

        SessionState.SetBool(ChaveEmAndamento, false);
        EditorUtility.ClearProgressBar();
        Concluir(avisos);
    }

    private static void Concluir(List<string> avisos)
    {
        string mensagem =
            "Pronto! A cena Assets/Scenes/Sebraelab.unity foi criada e está aberta.\n\n" +
            "• Dê Play: sem óculos, use WASD + mouse e aperte E num livro.\n" +
            "• Com o Quest conectado (Link/Air Link), a mesma cena roda em VR.\n" +
            "• Encontre os 3 livros escondidos pelo Sebraelab: pegue cada um (mão, raio ou E no PC).\n" +
            "• No hub (a mesa na sua frente), o menu mostra a introdução de cada livro encontrado e o botão Desafios.\n" +
            "• Para gravar um vídeo do jogo: Sebraelab > Gravar vídeo de demonstração.\n\n" +
            "Lembre de fazer commit no Git (inclusive das pastas Packages e ProjectSettings).";

        if (avisos.Count > 0)
            mensagem += "\n\nAlgumas etapas precisam de atenção (veja o Console):\n• " + string.Join("\n• ", avisos);

#if !ENABLE_INPUT_SYSTEM
        mensagem += "\n\nAtenção: o Input System novo está desligado e os controles VR não vão funcionar. " +
                    "Vá em Edit > Project Settings > Player > Active Input Handling, escolha \"Both\" e reinicie o Unity.";
#endif

        if (SebraelabAutomatico.Ativo)
        {
            SebraelabAutomatico.ConfiguracaoTerminou(mensagem, avisos);
            return;
        }

        Debug.Log("[Sebraelab] " + mensagem.Replace("\n\n", "\n"));
        EditorUtility.DisplayDialog("Sebraelab VR", mensagem, "OK");
    }

    // =====================================================================
    // Etapas 1 a 3: pacotes e samples
    // =====================================================================

    private static bool PacoteInstalado(string nome) => InfoPacote.FindForAssetPath($"Packages/{nome}/package.json") != null;

    private static string CaminhoPrefabXROrigin()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Samples")) return null;

        return AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Samples" })
                            .Select(AssetDatabase.GUIDToAssetPath)
                            .Where(caminho => Path.GetFileNameWithoutExtension(caminho) == NomePrefabXROrigin)
                            .OrderBy(caminho => caminho)
                            .LastOrDefault();
    }

    private static void ImportarStarterAssets()
    {
        foreach (var amostra in Amostra.FindByPackage(PacoteXRI, string.Empty))
        {
            if (amostra.displayName != "Starter Assets") continue;
            if (!amostra.Import(Amostra.ImportOptions.OverridePreviousImports))
                Debug.LogWarning("[Sebraelab] Não foi possível importar os Starter Assets do XR Interaction Toolkit.");
            return;
        }

        Debug.LogWarning("[Sebraelab] Sample \"Starter Assets\" não encontrado no XR Interaction Toolkit. " +
                         "Importe pelo Package Manager > XR Interaction Toolkit > Samples.");
    }

    private static bool TMPEssenciaisInstalados() => AssetDatabase.FindAssets("t:TMP_Settings").Length > 0;

    private static void ImportarTMPEssenciais()
    {
        var importador = BuscarTipo("TMPro.TMP_PackageResourceImporter");
        var metodo = importador?.GetMethod("ImportResources", BindingFlags.Public | BindingFlags.Static, null,
                                           new[] { typeof(bool), typeof(bool), typeof(bool) }, null);
        if (metodo != null)
        {
            metodo.Invoke(null, new object[] { true, false, false });
            return;
        }

        foreach (var caminho in new[]
                 {
                     "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage",
                     "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage",
                 })
        {
            var completo = Path.GetFullPath(caminho);
            if (!File.Exists(completo)) continue;
            AssetDatabase.ImportPackage(completo, false);
            return;
        }

        Debug.LogWarning("[Sebraelab] Importe o TextMeshPro manualmente: Window > TextMeshPro > Import TMP Essential Resources.");
    }

    // =====================================================================
    // Etapa 4: configurações
    // =====================================================================

    private static void ConfigurarURP()
    {
        if (GraphicsSettings.defaultRenderPipeline != null) return;

        // Feito por reflexão para este script compilar mesmo antes de o URP estar instalado.
        var tipoAsset = BuscarTipo("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset");
        var tipoRenderer = BuscarTipo("UnityEngine.Rendering.Universal.UniversalRendererData");
        var tipoDadosRenderer = BuscarTipo("UnityEngine.Rendering.Universal.ScriptableRendererData");
        if (tipoAsset == null || tipoRenderer == null || tipoDadosRenderer == null)
            throw new InvalidOperationException("URP não está instalado.");

        CriarPasta(PastaConfiguracoes);

        var renderer = ScriptableObject.CreateInstance(tipoRenderer);
        var dadosPosProcessamento = BuscarTipo("UnityEngine.Rendering.Universal.PostProcessData")?
            .GetMethod("GetDefaultPostProcessData", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?
            .Invoke(null, null);
        tipoRenderer.GetField("postProcessData", BindingFlags.Instance | BindingFlags.Public)?.SetValue(renderer, dadosPosProcessamento);
        AssetDatabase.CreateAsset(renderer, $"{PastaConfiguracoes}/Sebraelab_URP_Renderer.asset");

        var criar = tipoAsset.GetMethod("Create", BindingFlags.Static | BindingFlags.Public, null, new[] { tipoDadosRenderer }, null);
        if (criar == null) throw new InvalidOperationException("Método UniversalRenderPipelineAsset.Create não encontrado.");

        var pipeline = (RenderPipelineAsset)criar.Invoke(null, new object[] { renderer });
        AssetDatabase.CreateAsset(pipeline, $"{PastaConfiguracoes}/Sebraelab_URP.asset");

        GraphicsSettings.defaultRenderPipeline = pipeline;
        AssetDatabase.SaveAssets();
        Debug.Log("[Sebraelab] URP configurado (Assets/Settings/Sebraelab_URP.asset).");
    }

    private static void ConfigurarXR()
    {
        var porPlataforma = ObterConfiguracoesXR();

        foreach (var plataforma in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android })
        {
            if (!porPlataforma.HasManagerSettingsForBuildTarget(plataforma))
                porPlataforma.CreateDefaultManagerSettingsForBuildTarget(plataforma);

            var gerais = porPlataforma.SettingsForBuildTarget(plataforma);
            if (gerais != null) gerais.InitManagerOnStart = true;

            var gerenciador = porPlataforma.ManagerSettingsForBuildTarget(plataforma);
            if (!XRPackageMetadataStore.AssignLoader(gerenciador, typeof(OpenXRLoader).FullName, plataforma))
                Debug.LogWarning($"[Sebraelab] Não foi possível ativar o OpenXR para {plataforma}.");

            FeatureHelpers.RefreshFeatures(plataforma);
            var openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(plataforma);
            if (openXR == null)
            {
                Debug.LogWarning($"[Sebraelab] Configurações do OpenXR para {plataforma} não encontradas.");
                continue;
            }

            AtivarRecurso<OculusTouchControllerProfile>(openXR);
            AtivarRecurso<MetaQuestTouchPlusControllerProfile>(openXR);
            if (plataforma == BuildTargetGroup.Android) AtivarRecurso<MetaQuestFeature>(openXR);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Sebraelab] OpenXR ativado para PC e Android (Meta Quest), com os controles do Quest.");
    }

    private static XRGeneralSettingsPerBuildTarget ObterConfiguracoesXR()
    {
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out XRGeneralSettingsPerBuildTarget configuracoes);

        if (configuracoes == null)
        {
            var encontrados = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            if (encontrados.Length > 0)
                configuracoes = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(AssetDatabase.GUIDToAssetPath(encontrados[0]));
        }

        if (configuracoes == null)
        {
            CriarPasta("Assets/XR");
            configuracoes = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(configuracoes, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            AssetDatabase.SaveAssets();
        }

        EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, configuracoes, true);
        return configuracoes;
    }

    private static void AtivarRecurso<T>(OpenXRSettings configuracoes) where T : OpenXRFeature
    {
        var recurso = configuracoes.GetFeature<T>();
        if (recurso == null) return;
        recurso.enabled = true;
        EditorUtility.SetDirty(recurso);
    }

    /// <summary>
    /// O raio de teleporte dos Starter Assets usa a camada de interação 31. Aqui ela recebe o nome "Teleport".
    /// (A classe de camadas do XRI é interna, por isso o acesso é por reflexão.)
    /// </summary>
    private static void NomearCamadaTeleporte()
    {
        var tipo = BuscarTipo("UnityEngine.XR.Interaction.Toolkit.InteractionLayerSettings");
        var instancia = tipo?.GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)?
                            .GetValue(null);
        var definirNome = tipo?.GetMethod("SetLayerNameAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (instancia == null || definirNome == null)
            throw new InvalidOperationException("Use Edit > Project Settings > XR Plug-in Management > Project Validation > Fix All.");

        definirNome.Invoke(instancia, new object[] { SebraelabMenu.CamadaTeleporte, "Teleport" });
        if (instancia is UnityEngine.Object asset) EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigurarPlayer()
    {
        PlayerSettings.productName = "Sebraelab VR";
        PlayerSettings.colorSpace = ColorSpace.Linear;

        // Meta Quest (Android): IL2CPP, ARM64, Vulkan e texturas ASTC, como pede a validação do OpenXR.
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.tecnologiaemergente.sebraelabvr");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        if ((int)PlayerSettings.Android.minSdkVersion < 29) PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)29;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

        AtivarInputSystemNovo();
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Active Input Handling = "Both": os controles VR usam o Input System novo e o teste no PC funciona com qualquer um.
    /// Vale a partir da próxima vez que a Unity abrir; com isso ela não pergunta mais sobre os "backends" do Input System.
    /// </summary>
    private static void AtivarInputSystemNovo()
    {
        var configuracoes = Resources.FindObjectsOfTypeAll<PlayerSettings>();
        if (configuracoes.Length == 0) return;

        var serializado = new SerializedObject(configuracoes[0]);
        var entrada = serializado.FindProperty("activeInputHandler");
        if (entrada == null || entrada.intValue != 0) return; // já usa o novo ou os dois

        entrada.intValue = 2; // 0 = antigo, 1 = novo, 2 = os dois
        serializado.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[Sebraelab] Active Input Handling = Both (vale depois de reabrir a Unity).");
    }

    // =====================================================================
    // Etapa 5: cena
    // =====================================================================

    private static bool CriarCena()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

        if (File.Exists(CaminhoCena))
        {
            Debug.Log($"[Sebraelab] A cena {CaminhoCena} já existe; ela não foi recriada.");
            EditorSceneManager.OpenScene(CaminhoCena);
            return true;
        }

        var cena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // A câmera padrão sai: quem tem câmera é o XR Origin (VR) e o jogador de teste (PC).
        var cameraPadrao = Camera.main;
        if (cameraPadrao != null) UnityEngine.Object.DestroyImmediate(cameraPadrao.gameObject);

        var pontoInicial = Vector3.zero;
        var rotacaoInicial = Quaternion.identity;

        // Cenário: o modelo do Sebraelab, se estiver no projeto; senão um chão de exemplo.
        var modelo = SebraelabMenu.EncontrarModeloDoCenario();
        if (modelo != null)
        {
            var cenario = SebraelabMenu.ColocarModeloNaCena(modelo);
            var piso = SebraelabMenu.LimitesDoPiso(cenario);
            IluminarInterior();

            var mesa = SebraelabMenu.EncontrarMesa(cenario);
            if (mesa != null)
            {
                // Hub dos livros em cima de uma mesa do Sebraelab e o jogador começa 1,8 m à frente dela.
                // Os livros ficam escondidos em móveis espalhados pela sala.
                var limitesMesa = mesa.bounds;
                var centroMesa = new Vector3(limitesMesa.center.x, piso.max.y, limitesMesa.center.z);
                var direcao = piso.center - centroMesa;
                direcao.y = 0f;
                direcao = direcao.sqrMagnitude > 0.01f ? direcao.normalized : Vector3.forward;

                pontoInicial = centroMesa + direcao * 1.8f;
                rotacaoInicial = Quaternion.LookRotation(-direcao, Vector3.up);
                SebraelabColeta.Montar(limitesMesa, direcao, piso.max.y, cenario);
            }
            else
            {
                pontoInicial = new Vector3(piso.center.x, piso.max.y, piso.center.z);
                CriarHubDeExemplo(pontoInicial + Vector3.forward * 2.5f, Quaternion.identity, cenario);
            }
        }
        else
        {
            SebraelabMenu.CriarChaoTeleporte(Vector3.zero, new Vector2(30f, 30f), visivel: true,
                                             material: CriarMaterial("Chao_Exemplo", new Color(0.55f, 0.57f, 0.6f)));
            CriarHubDeExemplo(new Vector3(0f, 0f, 2.5f), Quaternion.identity, null);
        }

        // Jogador VR
        var caminhoXROrigin = CaminhoPrefabXROrigin();
        if (caminhoXROrigin != null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminhoXROrigin);
            var xrOrigin = (GameObject)PrefabUtility.InstantiatePrefab(prefab, cena);
            xrOrigin.transform.SetPositionAndRotation(pontoInicial, rotacaoInicial);
        }
        else
        {
            Debug.LogWarning("[Sebraelab] XR Origin dos Starter Assets não encontrado: a cena ficou só com o jogador de teste do PC.");
        }

        // EventSystem que funciona com os controles VR (e com o mouse).
        new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));

        // Jogador de teste do PC (some sozinho quando há headset). Começa onde está o XR Origin.
        var jogadorPC = SebraelabMenu.CriarJogadorDesktop();
        if (caminhoXROrigin == null) jogadorPC.transform.SetPositionAndRotation(pontoInicial, rotacaoInicial);

        CriarPasta(Path.GetDirectoryName(CaminhoCena).Replace('\\', '/'));
        EditorSceneManager.SaveScene(cena, CaminhoCena);
        AdicionarCenaAoBuild(CaminhoCena);
        Debug.Log($"[Sebraelab] Cena criada: {CaminhoCena}");
        return true;
    }

    /// <summary>
    /// A sala tem teto, então a luz do "sol" padrão ficaria toda em sombra. Sem sombras ela atravessa o teto e ilumina
    /// o interior por igual (também é mais leve para o Quest), e a luz ambiente fica mais clara.
    /// </summary>
    private static void IluminarInterior()
    {
        foreach (var luz in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (luz.type == LightType.Directional) luz.shadows = LightShadows.None;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.75f, 0.78f, 0.82f);
        RenderSettings.ambientEquatorColor = new Color(0.55f, 0.56f, 0.58f);
        RenderSettings.ambientGroundColor = new Color(0.32f, 0.31f, 0.3f);
    }

    /// <summary>
    /// Hub com uma mesa de exemplo, usado quando não há modelo do Sebraelab ou ele não tem mesa. O jogador está no -Z
    /// local (de frente para a mesa).
    /// </summary>
    private static void CriarHubDeExemplo(Vector3 posicao, Quaternion rotacao, GameObject cenario)
    {
        var mesa = SebraelabColeta.CriarMesaDeExemplo(posicao, rotacao);
        SebraelabColeta.Montar(mesa.bounds, rotacao * Vector3.back, posicao.y, cenario);
    }

    private static Material CriarMaterial(string nome, Color cor)
    {
        CriarPasta(PastaMateriais);
        string caminho = $"{PastaMateriais}/{nome}.mat";

        var existente = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (existente != null) return existente;

        // Usa o material padrão do pipeline ativo (URP/Lit com o URP ligado).
        var basePipeline = GraphicsSettings.defaultRenderPipeline != null ? GraphicsSettings.defaultRenderPipeline.defaultMaterial : null;
        var material = basePipeline != null ? new Material(basePipeline) : new Material(Shader.Find("Standard"));
        material.color = cor;
        AssetDatabase.CreateAsset(material, caminho);
        return material;
    }

    private static void AdicionarCenaAoBuild(string caminho)
    {
        var cenas = EditorBuildSettings.scenes.Where(c => c.path != caminho).ToList();
        cenas.Insert(0, new EditorBuildSettingsScene(caminho, true));
        EditorBuildSettings.scenes = cenas.ToArray();
    }

    // =====================================================================
    // Utilitários
    // =====================================================================

    private static void Tentar(string nome, Action acao, List<string> avisos)
    {
        try
        {
            acao();
        }
        catch (Exception erro)
        {
            var causa = erro is TargetInvocationException { InnerException: not null } ? erro.InnerException : erro;
            Debug.LogWarning($"[Sebraelab] Falha ao configurar {nome}: {causa.Message}");
            avisos.Add(nome);
        }
    }

    private static bool NovaTentativa(string etapa)
    {
        int tentativas = SessionState.GetInt(ChaveTentativa + etapa, 0);
        if (tentativas >= MaximoTentativas) return false;
        SessionState.SetInt(ChaveTentativa + etapa, tentativas + 1);
        return true;
    }

    private static void Progresso(string mensagem, float fracao)
    {
        Debug.Log("[Sebraelab] " + mensagem);
        EditorUtility.DisplayProgressBar("Configurando o Sebraelab VR", mensagem, fracao);
        EditorApplication.delayCall += EditorUtility.ClearProgressBar;
    }

    private static Type BuscarTipo(string nomeCompleto)
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
