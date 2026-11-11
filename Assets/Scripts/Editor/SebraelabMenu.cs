using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XRI 3.x. No XRI 2.x quem faz isto compilar é o XRICompatEditor.cs
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation; // idem
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Menu "Sebraelab" na barra do Unity, com atalhos para os passos repetitivos do guia:
///   Sebraelab > Livros  > Configurar livro selecionado
///   Sebraelab > Cenário > Colocar modelo do Sebraelab na cena
///   Sebraelab > Cenário > Adicionar colliders no cenário selecionado
///   Sebraelab > Jogador > Criar jogador de teste (teclado e mouse)
/// Tudo pode ser desfeito com Ctrl+Z.
/// </summary>
public static class SebraelabMenu
{
    private const float EscalaPopup = 0.001f;  // 1 unidade da UI = 1 mm no mundo
    private const float LarguraPopup = 450f;   // = 45 cm
    private const float Margem = 24f;
    private const int CamadaUI = 5;            // layer "UI" padrão do Unity

    /// <summary>Camada de interação 31: a que o raio de teleporte dos Starter Assets do XRI usa.</summary>
    internal const int CamadaTeleporte = 31;

    internal const string PastaModelos = "Assets/Models";
    internal const string NomeChaoTeleporte = "Chao (teleporte)";

    private const string SpriteUI = "UI/Skin/UISprite.psd";

    // =====================================================================
    // Livros
    // =====================================================================

    [MenuItem("Sebraelab/Livros/Configurar livro selecionado", false, 0)]
    private static void ConfigurarLivrosSelecionados()
    {
        Undo.SetCurrentGroupName("Configurar livro (Sebraelab)");
        int grupo = Undo.GetCurrentGroup();

        foreach (var livro in Selection.gameObjects) ConfigurarLivro(livro);

        Undo.CollapseUndoOperations(grupo);

        if (Object.FindFirstObjectByType<XRUIInputModule>() == null)
        {
            Debug.LogWarning("[Sebraelab] A cena não tem um EventSystem de VR. Para os botões do popup funcionarem com o " +
                             "controle, use GameObject > XR > UI Event System.");
        }
    }

    [MenuItem("Sebraelab/Livros/Configurar livro selecionado", true)]
    private static bool ValidarLivrosSelecionados() => Selection.gameObjects.Length > 0;

    internal static LivroPopup ConfigurarLivro(GameObject livro)
    {
        if (EditorUtility.IsPersistent(livro))
        {
            Debug.LogWarning($"[Sebraelab] '{livro.name}' está na aba Project. Arraste o livro para a cena e selecione-o na Hierarchy.", livro);
            return null;
        }

        // Livros se mexem: não podem ser "Static" (senão ficam parados no lugar ao serem pegos).
        foreach (var parte in livro.GetComponentsInChildren<Transform>(true))
        {
            if (!parte.gameObject.isStatic) continue;
            Undo.RecordObject(parte.gameObject, "Livro não estático");
            parte.gameObject.isStatic = false;
        }

        // 1. Collider: área de toque da mão.
        if (livro.GetComponentInChildren<Collider>(true) == null)
        {
            var caixa = Undo.AddComponent<BoxCollider>(livro);
            AjustarCaixaAoModelo(caixa);
        }

        // Mesh Collider côncavo não funciona em objeto com física.
        foreach (var malha in livro.GetComponentsInChildren<MeshCollider>(true))
        {
            if (malha.convex) continue;
            Undo.RecordObject(malha, "Mesh Collider convexo");
            malha.convex = true;
        }

        // 2. Rigidbody: física do livro (cai na mesa, pode ser jogado).
        if (livro.GetComponent<Rigidbody>() == null)
        {
            var corpo = Undo.AddComponent<Rigidbody>(livro);
            Undo.RecordObject(corpo, "Configurar Rigidbody");
            corpo.mass = 0.5f;
            corpo.useGravity = true;
            corpo.isKinematic = false;
            corpo.interpolation = RigidbodyInterpolation.Interpolate;
            corpo.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        // 3. XR Grab Interactable: permite pegar o livro com a mão (botão Grip).
        if (livro.GetComponent<XRBaseInteractable>() == null)
            Undo.AddComponent<XRGrabInteractable>(livro);

        // 4. LivroPopup + Canvas do popup.
        var livroPopup = livro.GetComponent<LivroPopup>();
        if (livroPopup == null) livroPopup = Undo.AddComponent<LivroPopup>(livro);

        var popupSerializado = new SerializedObject(livroPopup);
        if (popupSerializado.FindProperty("canvasPopup").objectReferenceValue == null)
        {
            CriarPopup(livro, livroPopup, popupSerializado);
            Debug.Log($"[Sebraelab] Livro '{livro.name}' configurado. Edite os textos em '{livro.name}/Popup' ou ligue um asset " +
                      "'Dados do Livro' no campo Dados do LivroPopup.", livro);
        }
        else
        {
            Debug.Log($"[Sebraelab] Livro '{livro.name}' já tinha popup; só os componentes que faltavam foram adicionados.", livro);
        }

        return livroPopup;
    }

    private static void CriarPopup(GameObject livro, LivroPopup livroPopup, SerializedObject popupSerializado)
    {
        float alturaPopup = CalcularTopo(livro) + 0.05f;

        // ---- Canvas em World Space, logo acima do livro ----
        var canvasGO = CriarObjetoUI("Popup", livro.transform);
        var canvasRT = (RectTransform)canvasGO.transform;
        canvasRT.sizeDelta = new Vector2(LarguraPopup, 600f);
        canvasRT.pivot = new Vector2(0.5f, 0f); // o painel cresce para cima a partir deste ponto

        // Compensa a escala do livro para o popup ter sempre o mesmo tamanho no mundo.
        var escalaLivro = livro.transform.lossyScale;
        canvasRT.localScale = new Vector3(EscalaPopup / NaoZero(escalaLivro.x),
                                          EscalaPopup / NaoZero(escalaLivro.y),
                                          EscalaPopup / NaoZero(escalaLivro.z));
        canvasRT.SetPositionAndRotation(livro.transform.position + Vector3.up * alturaPopup, Quaternion.identity);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
        canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>(); // permite clicar nos botões com o raio do controle

        var olhar = canvasGO.AddComponent<PopupOlharParaJogador>();
        var olharSerializado = new SerializedObject(olhar);
        DefinirReferencia(olharSerializado, "alvo", livro.transform);
        PropriedadeObrigatoria(olharSerializado, "deslocamento").vector3Value = new Vector3(0f, alturaPopup, 0f);
        olharSerializado.ApplyModifiedPropertiesWithoutUndo();

        // ---- Painel de fundo com layout vertical que cresce conforme o conteúdo ----
        var painel = CriarObjetoUI("Painel", canvasGO.transform);
        var painelRT = (RectTransform)painel.transform;
        painelRT.anchorMin = new Vector2(0f, 0f);
        painelRT.anchorMax = new Vector2(1f, 0f);
        painelRT.pivot = new Vector2(0.5f, 0f);
        painelRT.anchoredPosition = Vector2.zero;
        painelRT.sizeDelta = new Vector2(0f, 400f);

        var fundo = painel.AddComponent<Image>();
        fundo.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(SpriteUI);
        fundo.type = Image.Type.Sliced;
        fundo.color = new Color(0.07f, 0.09f, 0.13f, 0.92f);
        fundo.raycastTarget = false;

        var layout = painel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset((int)Margem, (int)Margem, (int)Margem, (int)Margem);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        painel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ---- Conteúdo ----
        var titulo = CriarTexto("Titulo", painel.transform, livro.name, 34f, FontStyles.Bold,
                                TextAlignmentOptions.Center, Color.white);
        var autor = CriarTexto("Autor", painel.transform, "Autor(a)", 22f, FontStyles.Italic,
                               TextAlignmentOptions.Center, new Color(0.75f, 0.8f, 0.9f));

        var capaGO = CriarObjetoUI("Capa", painel.transform);
        var capa = capaGO.AddComponent<Image>();
        capa.preserveAspect = true;
        capa.raycastTarget = false;
        capaGO.AddComponent<LayoutElement>().preferredHeight = 220f;

        var resumo = CriarTexto("Resumo", painel.transform,
                                "Escreva aqui o resumo do livro, ou ligue um asset \"Dados do Livro\" no LivroPopup.",
                                22f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.92f, 0.92f, 0.92f));

        // Vídeo: a área tem altura fixa e a tela dentro dela mantém a proporção do vídeo.
        var areaVideo = CriarObjetoUI("Video", painel.transform);
        areaVideo.AddComponent<LayoutElement>().preferredHeight = (LarguraPopup - 2f * Margem) * 9f / 16f;
        var video = areaVideo.AddComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.isLooping = true;
        video.source = VideoSource.VideoClip;
        video.renderMode = VideoRenderMode.RenderTexture;
        video.audioOutputMode = VideoAudioOutputMode.None;
        video.skipOnDrop = true;

        var telaGO = CriarObjetoUI("Tela", areaVideo.transform);
        Esticar((RectTransform)telaGO.transform);
        var tela = telaGO.AddComponent<RawImage>();
        tela.raycastTarget = false;
        var proporcao = telaGO.AddComponent<AspectRatioFitter>();
        proporcao.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        proporcao.aspectRatio = 16f / 9f;

        // Botão do minigame (fase futura do GDD): já vem ligado ao LivroPopup.IniciarMinigame, mas desativado.
        var botaoGO = CriarObjetoUI("BotaoMinigame", painel.transform);
        var fundoBotao = botaoGO.AddComponent<Image>();
        fundoBotao.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(SpriteUI);
        fundoBotao.type = Image.Type.Sliced;
        fundoBotao.color = new Color(0f, 0.37f, 0.72f);
        var botao = botaoGO.AddComponent<Button>();
        botao.targetGraphic = fundoBotao;
        botaoGO.AddComponent<LayoutElement>().preferredHeight = 56f;
        var textoBotao = CriarTexto("Texto", botaoGO.transform, "Iniciar Desafio", 26f, FontStyles.Bold,
                                    TextAlignmentOptions.Center, Color.white);
        Esticar(textoBotao.rectTransform);
        UnityEventTools.AddPersistentListener(botao.onClick, livroPopup.IniciarMinigame);
        botaoGO.SetActive(false);

        canvasGO.SetActive(false); // o popup começa invisível
        Undo.RegisterCreatedObjectUndo(canvasGO, "Criar popup do livro");

        // ---- Liga tudo no LivroPopup ----
        DefinirReferencia(popupSerializado, "canvasPopup", canvasGO);
        DefinirReferencia(popupSerializado, "textoTitulo", titulo);
        DefinirReferencia(popupSerializado, "textoAutor", autor);
        DefinirReferencia(popupSerializado, "textoResumo", resumo);
        DefinirReferencia(popupSerializado, "imagemCapa", capa);
        DefinirReferencia(popupSerializado, "videoPlayer", video);
        DefinirReferencia(popupSerializado, "telaVideo", tela);
        DefinirReferencia(popupSerializado, "areaVideo", areaVideo);
        popupSerializado.ApplyModifiedProperties();
    }

    // =====================================================================
    // Cenário
    // =====================================================================

    [MenuItem("Sebraelab/Cenário/Colocar modelo do Sebraelab na cena", false, 19)]
    private static void ColocarModeloSelecionadoOuDaPasta()
    {
        // Usa o modelo selecionado na aba Project; se não tiver, o primeiro da pasta Assets/Models.
        var modelo = Selection.activeObject as GameObject;
        if (modelo == null || !EditorUtility.IsPersistent(modelo)) modelo = EncontrarModeloDoCenario();

        if (modelo == null)
        {
            EditorUtility.DisplayDialog("Sebraelab",
                "Nenhum modelo 3D encontrado. Coloque o arquivo do Sebraelab (.fbx, .obj, .glb...) na pasta Assets/Models " +
                "e tente de novo.", "OK");
            return;
        }

        var cenario = ColocarModeloNaCena(modelo);
        Selection.activeGameObject = cenario;
        EditorGUIUtility.PingObject(cenario);
    }

    /// <summary>Primeiro modelo 3D da pasta Assets/Models (ou null).</summary>
    internal static GameObject EncontrarModeloDoCenario()
    {
        if (!AssetDatabase.IsValidFolder(PastaModelos)) return null;

        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { PastaModelos }))
        {
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (modelo != null) return modelo;
        }
        return null;
    }

    /// <summary>
    /// Coloca o modelo do cenário na cena com colliders em todas as partes e um chão de teleporte invisível
    /// na altura do ponto mais baixo do modelo. Se a cena já tinha o chão de exemplo, ele vira invisível.
    /// </summary>
    internal static GameObject ColocarModeloNaCena(GameObject modelo)
    {
        Undo.SetCurrentGroupName("Colocar modelo do Sebraelab");
        int grupo = Undo.GetCurrentGroup();

        var cenario = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
        cenario.name = "Cenario Sebraelab";
        Undo.RegisterCreatedObjectUndo(cenario, "Colocar modelo do Sebraelab");

        AdicionarColliders(cenario, out int adicionados, out _);

        var limites = CalcularLimites(cenario);
        var chao = GameObject.Find(NomeChaoTeleporte);
        if (chao == null)
        {
            chao = CriarChaoTeleporte(new Vector3(limites.center.x, limites.min.y + 0.01f, limites.center.z),
                                      new Vector2(limites.size.x, limites.size.z), visivel: false, material: null);
        }
        else
        {
            Undo.RecordObject(chao.transform, "Ajustar chão de teleporte");
            chao.transform.position = new Vector3(limites.center.x, limites.min.y + 0.01f, limites.center.z);
            chao.transform.localScale = new Vector3(Mathf.Max(1f, limites.size.x / 10f), 1f, Mathf.Max(1f, limites.size.z / 10f));
            var visual = chao.GetComponent<Renderer>();
            if (visual != null)
            {
                Undo.RecordObject(visual, "Esconder chão de exemplo");
                visual.enabled = false;
            }
        }

        Undo.CollapseUndoOperations(grupo);
        Debug.Log($"[Sebraelab] Modelo '{modelo.name}' colocado na cena com {adicionados} Mesh Collider(s). " +
                  $"O chão de teleporte foi colocado na altura {limites.min.y + 0.01f:0.00} m; se o teleporte não " +
                  $"funcionar, ajuste a altura do objeto '{NomeChaoTeleporte}' para ficar no nível do piso.", cenario);
        return cenario;
    }

    /// <summary>
    /// Plano com collider e Teleportation Area (camada de interação 31, a do raio de teleporte dos Starter Assets).
    /// </summary>
    internal static GameObject CriarChaoTeleporte(Vector3 centro, Vector2 tamanhoEmMetros, bool visivel, Material material)
    {
        var chao = GameObject.CreatePrimitive(PrimitiveType.Plane); // o plano padrão tem 10 x 10 m
        chao.name = NomeChaoTeleporte;
        chao.transform.position = centro;
        chao.transform.localScale = new Vector3(Mathf.Max(1f, tamanhoEmMetros.x / 10f), 1f, Mathf.Max(1f, tamanhoEmMetros.y / 10f));

        var visual = chao.GetComponent<Renderer>();
        if (material != null) visual.sharedMaterial = material;
        visual.enabled = visivel;

        var area = chao.AddComponent<TeleportationArea>();
        area.interactionLayers = 1 << CamadaTeleporte;

        Undo.RegisterCreatedObjectUndo(chao, "Criar chão de teleporte");
        return chao;
    }

    [MenuItem("Sebraelab/Cenário/Adicionar colliders no cenário selecionado", false, 20)]
    private static void AdicionarCollidersNoCenario()
    {
        Undo.SetCurrentGroupName("Adicionar colliders no cenário (Sebraelab)");
        int grupo = Undo.GetCurrentGroup();
        int adicionados = 0;
        int ignorados = 0;

        foreach (var raiz in Selection.gameObjects)
        {
            if (EditorUtility.IsPersistent(raiz)) continue;
            AdicionarColliders(raiz, out int a, out int i);
            adicionados += a;
            ignorados += i;
        }

        Undo.CollapseUndoOperations(grupo);
        Debug.Log($"[Sebraelab] {adicionados} Mesh Collider(s) adicionados ao cenário" +
                  (ignorados > 0 ? $" ({ignorados} objeto(s) com física/livro foram ignorados)." : "."));
    }

    [MenuItem("Sebraelab/Cenário/Adicionar colliders no cenário selecionado", true)]
    private static bool ValidarCenarioSelecionado() => Selection.gameObjects.Length > 0;

    /// <summary>
    /// Mesh Collider em cada parte com malha (no objeto raiz de um FBX ele não faria nada) e marca como Static.
    /// Livros e objetos com física são ignorados.
    /// </summary>
    internal static void AdicionarColliders(GameObject raiz, out int adicionados, out int ignorados)
    {
        adicionados = 0;
        ignorados = 0;

        foreach (var filtro in raiz.GetComponentsInChildren<MeshFilter>(true))
        {
            var parte = filtro.gameObject;
            if (filtro.sharedMesh == null || parte.GetComponent<Collider>() != null) continue;
            if (EhObjetoInterativo(parte.transform))
            {
                ignorados++;
                continue;
            }

            var colisor = Undo.AddComponent<MeshCollider>(parte);
            Undo.RecordObject(colisor, "Configurar Mesh Collider");
            colisor.sharedMesh = filtro.sharedMesh;

            Undo.RecordObject(parte, "Marcar cenário como Static");
            parte.isStatic = true;
            adicionados++;
        }
    }

    private static bool EhObjetoInterativo(Transform parte)
    {
        for (var t = parte; t != null; t = t.parent)
        {
            if (t.GetComponent<Rigidbody>() != null || t.GetComponent<XRBaseInteractable>() != null ||
                t.GetComponent<LivroPopup>() != null)
                return true;
        }
        return false;
    }

    // =====================================================================
    // Jogador
    // =====================================================================

    [MenuItem("Sebraelab/Jogador/Criar jogador de teste (teclado e mouse)", false, 40)]
    private static void CriarJogadorDesktopMenu()
    {
        var jogador = CriarJogadorDesktop();
        Selection.activeGameObject = jogador;

        Debug.Log("[Sebraelab] Jogador de teste criado. Sem headset ele substitui o XR Origin ao dar Play " +
                  "(WASD + mouse, Shift corre, E/clique abre o popup do livro, Esc solta o mouse).", jogador);
    }

    internal static GameObject CriarJogadorDesktop()
    {
        var jogador = new GameObject("Jogador PC (teste sem oculos)");
        var corpo = jogador.AddComponent<CharacterController>();
        corpo.height = 1.7f;
        corpo.radius = 0.3f;
        corpo.center = new Vector3(0f, 0.85f, 0f);
        jogador.AddComponent<DesktopPlayerController>();
        jogador.AddComponent<DesktopInteracao>();

        var cameraGO = new GameObject("Camera PC");
        cameraGO.tag = "MainCamera";
        cameraGO.transform.SetParent(jogador.transform, false);
        cameraGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        cameraGO.AddComponent<Camera>().nearClipPlane = 0.05f;
        cameraGO.AddComponent<AudioListener>();

        var xrOrigin = DesktopPlayerController.EncontrarXROrigin();
        if (xrOrigin != null)
        {
            jogador.transform.SetPositionAndRotation(xrOrigin.transform.position,
                                                     Quaternion.Euler(0f, xrOrigin.transform.eulerAngles.y, 0f));
        }

        Undo.RegisterCreatedObjectUndo(jogador, "Criar jogador de teste");
        return jogador;
    }

    // =====================================================================
    // Utilitários
    // =====================================================================

    private static GameObject CriarObjetoUI(string nome, Transform pai)
    {
        var objeto = new GameObject(nome, typeof(RectTransform));
        objeto.layer = CamadaUI;
        objeto.transform.SetParent(pai, false);
        return objeto;
    }

    private static TextMeshProUGUI CriarTexto(string nome, Transform pai, string conteudo, float tamanho,
                                              FontStyles estilo, TextAlignmentOptions alinhamento, Color cor)
    {
        var texto = CriarObjetoUI(nome, pai).AddComponent<TextMeshProUGUI>();
        texto.text = conteudo;
        texto.fontSize = tamanho;
        texto.fontStyle = estilo;
        texto.alignment = alinhamento;
        texto.color = cor;
        texto.raycastTarget = false;
        return texto;
    }

    private static void Esticar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static SerializedProperty PropriedadeObrigatoria(SerializedObject objeto, string nome)
    {
        var propriedade = objeto.FindProperty(nome);
        if (propriedade == null)
            throw new System.InvalidOperationException($"Campo '{nome}' não encontrado em {objeto.targetObject.GetType().Name}.");
        return propriedade;
    }

    private static void DefinirReferencia(SerializedObject objeto, string nome, Object valor) =>
        PropriedadeObrigatoria(objeto, nome).objectReferenceValue = valor;

    /// <summary>Caixa que envolve todos os Renderers do objeto, no espaço do mundo.</summary>
    internal static Bounds CalcularLimites(GameObject objeto)
    {
        var renderers = objeto.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(objeto.transform.position, Vector3.one * 10f);

        var limites = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) limites.Encapsulate(renderers[i].bounds);
        return limites;
    }

    /// <summary>Altura do topo do modelo do livro em relação à posição dele, em metros.</summary>
    private static float CalcularTopo(GameObject livro)
    {
        var renderers = livro.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return 0.1f;

        var limites = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) limites.Encapsulate(renderers[i].bounds);
        return Mathf.Max(0f, limites.max.y - livro.transform.position.y);
    }

    /// <summary>Ajusta o Box Collider para envolver todas as malhas do livro (inclusive em objetos filhos).</summary>
    private static void AjustarCaixaAoModelo(BoxCollider caixa)
    {
        var raiz = caixa.transform;
        bool encontrou = false;
        var limites = new Bounds();

        foreach (var filtro in raiz.GetComponentsInChildren<MeshFilter>())
        {
            if (filtro.sharedMesh == null) continue;

            var b = filtro.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var canto = new Vector3((i & 1) == 0 ? b.min.x : b.max.x,
                                        (i & 2) == 0 ? b.min.y : b.max.y,
                                        (i & 4) == 0 ? b.min.z : b.max.z);
                var local = raiz.InverseTransformPoint(filtro.transform.TransformPoint(canto));

                if (encontrou)
                {
                    limites.Encapsulate(local);
                }
                else
                {
                    limites = new Bounds(local, Vector3.zero);
                    encontrou = true;
                }
            }
        }

        if (!encontrou)
        {
            Debug.LogWarning($"[Sebraelab] '{raiz.name}' não tem malha; ajuste o tamanho do Box Collider manualmente.", raiz);
            return;
        }

        Undo.RecordObject(caixa, "Ajustar Box Collider");
        caixa.center = limites.center;
        caixa.size = limites.size;
    }

    private static float NaoZero(float valor) => Mathf.Abs(valor) < 0.0001f ? 1f : Mathf.Abs(valor);
}
