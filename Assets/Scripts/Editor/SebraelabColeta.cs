using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XRI 3.x. No XRI 2.x quem faz isto compilar é o XRICompatEditor.cs
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Monta a coleta de livros na cena (estilo Slender):
/// - <b>Hub dos livros</b> em cima de uma mesa (a "Mesa Grande" do Sebraelab): lugares para os livros encontrados,
///   luz de destaque, painel de azulejos de São Luís e o menu com a introdução de cada história e o botão Desafios;
/// - <b>Esconderijos</b> em cima de móveis espalhados pelo Sebraelab (arquibancada, recepção, cozinha, estações...);
/// - <b>3 livros escondidos</b>, que o jogador precisa encontrar. Pegar o livro de qualquer jeito conta.
/// O configurador automático chama <see cref="Montar"/>; numa cena antiga use o menu Sebraelab > Coleta de livros.
/// </summary>
internal static class SebraelabColeta
{
    internal const string NomeHub = "Hub dos livros";
    private const string NomeEsconderijos = "Esconderijos dos livros";
    private const string NomeLivros = "Livros escondidos";
    private const string PastaMateriais = "Assets/Materials";
    private const int QuantidadeDeLivros = 3;

    /// <summary>
    /// Móveis do modelo do Sebraelab que viram esconderijos: nome no modelo e nome do esconderijo na cena.
    /// O que não existir no modelo é ignorado.
    /// </summary>
    private static readonly (string modelo, string esconderijo)[] MoveisParaEsconder =
    {
        ("Arquibancada", "arquibancada"),
        ("Mesa Recepção", "mesa da recepção"),
        ("Bancada Cozinha", "bancada da cozinha"),
        ("Cube003", "mesa perto da TV"),
        ("Area de Trabalho 2", "estação de trabalho 2"),
        ("Area de Trabalho 1", "estação de trabalho 1"),
        ("Cube025", "banco da parede leste"),
        ("Cube026", "banco perto da cozinha"),
    };

    // =====================================================================
    // Menus
    // =====================================================================

    [MenuItem("Sebraelab/Coleta de livros/Montar hub e livros escondidos na cena aberta", false, 30)]
    private static void MontarNaCenaAberta()
    {
        if (GameObject.Find(NomeHub) != null)
        {
            EditorUtility.DisplayDialog("Sebraelab", $"A cena já tem o \"{NomeHub}\".", "OK");
            return;
        }

        var gerenciador = MontarNaCena();
        if (gerenciador != null) Selection.activeGameObject = gerenciador.gameObject;
    }

    /// <summary>Monta hub, esconderijos e livros na cena aberta (sem perguntas; usado também pelo modo automático).</summary>
    internal static GerenciadorColeta MontarNaCena()
    {
        Undo.SetCurrentGroupName("Coleta de livros (Sebraelab)");
        int grupo = Undo.GetCurrentGroup();

        var cenario = GameObject.Find("Cenario Sebraelab");
        var mesa = cenario != null ? SebraelabMenu.EncontrarMesa(cenario) : null;
        float piso = cenario != null ? SebraelabMenu.LimitesDoPiso(cenario).max.y : 0f;

        Bounds limitesMesa;
        Vector3 direcao;
        if (mesa != null)
        {
            limitesMesa = mesa.bounds;
            var centro = new Vector3(limitesMesa.center.x, piso, limitesMesa.center.z);
            var limitesPiso = SebraelabMenu.LimitesDoPiso(cenario);
            direcao = limitesPiso.center - centro;
        }
        else
        {
            // Sem o modelo: uma mesa de exemplo onde a Scene View está olhando.
            var vista = SceneView.lastActiveSceneView;
            var lugar = vista != null ? new Vector3(vista.pivot.x, piso, vista.pivot.z) : Vector3.zero;
            limitesMesa = CriarMesaDeExemplo(lugar, Quaternion.identity).bounds;
            direcao = Vector3.back;
        }

        var gerenciador = Montar(limitesMesa, direcao, piso, cenario);

        // A estante antiga (livros com popup em cima da mesa) sai de cena: agora os livros ficam escondidos.
        var antiga = GameObject.Find("Estante maranhense (livros na mesa)");
        if (antiga != null)
        {
            Undo.RecordObject(antiga, "Desligar estante antiga");
            antiga.SetActive(false);
            Debug.Log("[Sebraelab] A estante antiga (livros com popup na mesa) foi desligada. Pode apagá-la.", antiga);
        }

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(gerenciador.gameObject.scene);
        return gerenciador;
    }

    [MenuItem("Sebraelab/Coleta de livros/Criar esconderijo onde a Scene View está olhando", false, 31)]
    private static void CriarEsconderijoNaVista()
    {
        var vista = SceneView.lastActiveSceneView;
        if (vista == null || vista.camera == null) return;

        Physics.SyncTransforms();
        var olhos = vista.camera.transform;
        if (!Physics.Raycast(olhos.position, olhos.forward, out RaycastHit acerto, 100f, ~0, QueryTriggerInteraction.Ignore))
        {
            EditorUtility.DisplayDialog("Sebraelab", "Olhe para um móvel com collider na Scene View e tente de novo.", "OK");
            return;
        }

        var grupo = GameObject.Find(NomeEsconderijos);
        if (grupo == null)
        {
            grupo = new GameObject(NomeEsconderijos);
            Undo.RegisterCreatedObjectUndo(grupo, "Criar esconderijos");
        }
        var ponto = NovoEsconderijo(grupo.transform, $"Esconderijo {grupo.transform.childCount + 1}", acerto.point, -olhos.forward);
        Selection.activeGameObject = ponto.gameObject;
    }

    [MenuItem("Sebraelab/Livros/Transformar em livro escondido (coleta)", false, 1)]
    private static void TransformarSelecionados()
    {
        Undo.SetCurrentGroupName("Livro escondido (Sebraelab)");
        int grupo = Undo.GetCurrentGroup();
        var gerenciador = Object.FindFirstObjectByType<GerenciadorColeta>(FindObjectsInactive.Include);
        foreach (var objeto in Selection.gameObjects)
        {
            if (EditorUtility.IsPersistent(objeto)) continue;
            var livro = TransformarEmLivroEscondido(objeto, null, Color.gray, null, null);
            if (gerenciador != null) AdicionarNaLista(gerenciador, "livros", livro);
        }
        Undo.CollapseUndoOperations(grupo);
        if (gerenciador == null)
            Debug.LogWarning("[Sebraelab] A cena não tem o hub (GerenciadorColeta). Use Sebraelab > Coleta de livros > Montar hub e livros escondidos na cena aberta.");
    }

    [MenuItem("Sebraelab/Livros/Transformar em livro escondido (coleta)", true)]
    private static bool ValidarSelecionados() => Selection.gameObjects.Length > 0;

    // =====================================================================
    // Montagem
    // =====================================================================

    /// <summary>
    /// Cria o hub em cima da mesa, os esconderijos pelo cenário e os 3 livros escondidos, e liga tudo.
    /// </summary>
    /// <param name="mesa">Limites da mesa do hub.</param>
    /// <param name="direcaoJogador">Direção, a partir da mesa, de onde o jogador chega.</param>
    /// <param name="alturaPiso">Altura do chão.</param>
    /// <param name="cenario">Modelo do Sebraelab na cena (pode ser nulo).</param>
    internal static GerenciadorColeta Montar(Bounds mesa, Vector3 direcaoJogador, float alturaPiso, GameObject cenario)
    {
        direcaoJogador.y = 0f;
        direcaoJogador = direcaoJogador.sqrMagnitude > 0.0001f ? direcaoJogador.normalized : Vector3.back;
        Physics.SyncTransforms();

        // ---- Hub: em cima da mesa, de frente para o jogador ----
        var hub = new GameObject(NomeHub);
        hub.transform.SetPositionAndRotation(new Vector3(mesa.center.x, mesa.max.y, mesa.center.z), Quaternion.LookRotation(direcaoJogador, Vector3.up));
        Undo.RegisterCreatedObjectUndo(hub, "Criar hub dos livros");
        var gerenciador = hub.AddComponent<GerenciadorColeta>();

        float largura = Mathf.Min(mesa.size.x, mesa.size.z);
        float espaco = Mathf.Clamp((largura - 0.2f) / 2f, 0.18f, 0.32f);
        var lugares = new List<Transform>();
        var materialMarca = MaterialSalvo("Hub_Marca", new Color(0.1f, 0.11f, 0.13f), 0f);
        for (int i = 0; i < QuantidadeDeLivros; i++)
        {
            var lugar = new GameObject($"Lugar do livro {i + 1}").transform;
            lugar.SetParent(hub.transform, false);
            lugar.localPosition = new Vector3((i - 1) * espaco, 0.14f, 0f);

            // Marca escura na mesa: mostra que falta um livro ali (some quando o livro chega).
            var marca = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marca.name = "Marca";
            Object.DestroyImmediate(marca.GetComponent<Collider>());
            marca.transform.SetParent(lugar, false);
            marca.transform.localPosition = new Vector3(0f, -0.137f, 0f);
            marca.transform.localScale = new Vector3(0.18f, 0.004f, 0.06f);
            marca.GetComponent<Renderer>().sharedMaterial = materialMarca;
            lugares.Add(lugar);
        }

        CriarLuzEZona(hub.transform, mesa.size.y);

        // ---- Painel de azulejos de São Luís + menu do hub em cima dele ----
        var painel = SebraelabCulturaSaoLuis.CriarPainel(hub.transform.position + Vector3.up * 0.02f, direcaoJogador, alturaPiso, mesa.max.y);
        var menu = CriarMenu(painel, hub.transform, gerenciador);

        // ---- Esconderijos e livros ----
        var esconderijos = CriarEsconderijos(cenario, alturaPiso, hub.transform.position, direcaoJogador);
        var livros = CriarLivros(esconderijos);

        var serializado = new SerializedObject(gerenciador);
        Preencher(serializado.FindProperty("livros"), livros.Cast<Object>().ToList());
        Preencher(serializado.FindProperty("lugaresNoHub"), lugares.Cast<Object>().ToList());
        Preencher(serializado.FindProperty("esconderijos"), esconderijos.Cast<Object>().ToList());
        serializado.FindProperty("menu").objectReferenceValue = menu;
        serializado.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"[Sebraelab] Coleta de livros montada: hub em cima de '{mesa.center}', {esconderijos.Count} esconderijos e {livros.Count} livros escondidos.");
        return gerenciador;
    }

    /// <summary>Mesa simples para quando o cenário não tem uma (o hub precisa de uma mesa).</summary>
    internal static Renderer CriarMesaDeExemplo(Vector3 posicao, Quaternion rotacao)
    {
        var mesa = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mesa.name = "Mesa do hub (exemplo)";
        mesa.transform.SetPositionAndRotation(posicao + Vector3.up * 0.375f, rotacao);
        mesa.transform.localScale = new Vector3(1.2f, 0.75f, 0.6f);
        mesa.GetComponent<Renderer>().sharedMaterial = MaterialSalvo("Mesa_Exemplo", new Color(0.45f, 0.3f, 0.2f), 0f);
        mesa.isStatic = true;
        Undo.RegisterCreatedObjectUndo(mesa, "Criar mesa do hub");
        Physics.SyncTransforms();
        return mesa.GetComponent<Renderer>();
    }

    private static void CriarLuzEZona(Transform hub, float alturaTampo)
    {
        var luz = new GameObject("Luz do hub", typeof(Light));
        luz.transform.SetParent(hub, false);
        luz.transform.localPosition = Vector3.up * 1.7f + Vector3.forward * 0.3f;
        luz.transform.rotation = Quaternion.LookRotation(Vector3.down, hub.forward);
        var spot = luz.GetComponent<Light>();
        spot.type = LightType.Spot;
        spot.range = 4f;
        spot.spotAngle = 70f;
        spot.intensity = 3f;
        spot.color = new Color(1f, 0.93f, 0.8f);

        // A luz acende quando o jogador chega perto do hub.
        var zona = new GameObject("Zona do hub", typeof(BoxCollider));
        zona.transform.SetParent(hub, false);
        zona.transform.localPosition = Vector3.forward * 1.3f + Vector3.up * (1.1f - alturaTampo);
        var caixa = zona.GetComponent<BoxCollider>();
        caixa.isTrigger = true;
        caixa.size = new Vector3(3f, 2.2f, 2.4f);
        var destaque = zona.AddComponent<EstandeZonaDestaque>();
        var serializado = new SerializedObject(destaque);
        Preencher(serializado.FindProperty("objetosDestaque"), new List<Object> { luz });
        serializado.ApplyModifiedPropertiesWithoutUndo();
    }

    // =====================================================================
    // Esconderijos
    // =====================================================================

    private static List<PontoDeLivro> CriarEsconderijos(GameObject cenario, float alturaPiso, Vector3 centroHub, Vector3 direcaoJogador)
    {
        var grupo = new GameObject(NomeEsconderijos);
        Undo.RegisterCreatedObjectUndo(grupo, "Criar esconderijos");
        var pontos = new List<PontoDeLivro>();

        if (cenario != null)
        {
            foreach (var (nome, rotulo) in MoveisParaEsconder)
            {
                var movel = EncontrarPorNome(cenario.transform, nome);
                if (movel == null || !movel.gameObject.activeInHierarchy) continue;
                if (!AcharSuperficie(movel.gameObject, alturaPiso, out var ponto)) continue;
                if (Vector3.Distance(Plano(ponto), Plano(centroHub)) < 2.5f) continue; // perto demais do hub
                pontos.Add(NovoEsconderijo(grupo.transform, $"Esconderijo - {rotulo}", ponto, centroHub - ponto));
            }
        }

        // Cenário sem móveis conhecidos: pedestais em volta do hub.
        for (int k = 0; pontos.Count < QuantidadeDeLivros + 2 && k < 12; k++)
        {
            float angulo = 180f + (k % 2 == 0 ? 1f : -1f) * (25f + 30f * (k / 2));
            float raio = 5f + (k % 3);
            var direcao = Quaternion.Euler(0f, angulo, 0f) * -direcaoJogador;
            var chao = new Vector3(centroHub.x, alturaPiso, centroHub.z) + direcao * raio;
            if (!LugarLivre(chao)) continue;
            var pedestal = CriarPedestal(grupo.transform, chao, pontos.Count + 1);
            pontos.Add(NovoEsconderijo(grupo.transform, $"Esconderijo - pedestal {pontos.Count + 1}", pedestal, centroHub - chao));
        }

        if (pontos.Count < QuantidadeDeLivros)
            Debug.LogWarning($"[Sebraelab] Só foram criados {pontos.Count} esconderijos. Crie mais com Sebraelab > Coleta de livros > Criar esconderijo onde a Scene View está olhando.");
        return pontos;
    }

    private static PontoDeLivro NovoEsconderijo(Transform grupo, string nome, Vector3 posicao, Vector3 olharPara)
    {
        olharPara.y = 0f;
        var objeto = new GameObject(nome);
        objeto.transform.SetParent(grupo, false);
        objeto.transform.SetPositionAndRotation(posicao, olharPara.sqrMagnitude > 0.001f ? Quaternion.LookRotation(olharPara, Vector3.up) : Quaternion.identity);
        Undo.RegisterCreatedObjectUndo(objeto, "Criar esconderijo");
        return objeto.AddComponent<PontoDeLivro>();
    }

    /// <summary>
    /// Procura, de cima para baixo, uma parte plana do móvel entre 30 cm e 1,25 m do chão, larga o bastante para o livro
    /// (perto do centro do móvel). Usa os colliders do cenário.
    /// </summary>
    private static bool AcharSuperficie(GameObject movel, float alturaPiso, out Vector3 ponto)
    {
        ponto = Vector3.zero;
        var limites = SebraelabMenu.CalcularLimites(movel);
        var centro = Plano(limites.center);
        float melhor = float.MaxValue;
        const int passos = 7;
        for (int ix = 0; ix < passos; ix++)
        {
            for (int iz = 0; iz < passos; iz++)
            {
                float x = Mathf.Lerp(limites.min.x + 0.1f * limites.size.x, limites.max.x - 0.1f * limites.size.x, ix / (passos - 1f));
                float z = Mathf.Lerp(limites.min.z + 0.1f * limites.size.z, limites.max.z - 0.1f * limites.size.z, iz / (passos - 1f));
                if (!Superficie(new Vector3(x, alturaPiso + 1.4f, z), 1.4f, out RaycastHit acerto)) continue;
                float altura = acerto.point.y - alturaPiso;
                if (altura < 0.3f || altura > 1.25f || !Estavel(acerto.point)) continue;

                float distancia = Vector3.Distance(Plano(acerto.point), centro);
                if (distancia >= melhor) continue;
                melhor = distancia;
                ponto = acerto.point;
            }
        }
        return melhor < float.MaxValue;
    }

    private static bool Superficie(Vector3 origem, float distancia, out RaycastHit acerto) =>
        Physics.Raycast(origem, Vector3.down, out acerto, distancia, ~0, QueryTriggerInteraction.Ignore) && acerto.normal.y > 0.95f;

    /// <summary>A superfície continua plana 9 cm para cada lado (cabe o livro).</summary>
    private static bool Estavel(Vector3 ponto)
    {
        foreach (var lado in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
        {
            if (!Superficie(ponto + lado * 0.09f + Vector3.up * 0.3f, 0.6f, out RaycastHit acerto)) return false;
            if (Mathf.Abs(acerto.point.y - ponto.y) > 0.02f) return false;
        }
        return true;
    }

    private static bool LugarLivre(Vector3 chao) =>
        Physics.Raycast(chao + Vector3.up * 0.5f, Vector3.down, 1f, ~0, QueryTriggerInteraction.Ignore) &&
        !Physics.CheckBox(chao + Vector3.up * 0.9f, new Vector3(0.6f, 0.7f, 0.6f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);

    private static Vector3 CriarPedestal(Transform grupo, Vector3 chao, int numero)
    {
        var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pedestal.name = $"Pedestal {numero}";
        pedestal.transform.SetParent(grupo, false);
        pedestal.transform.position = chao + Vector3.up * 0.45f;
        pedestal.transform.localScale = new Vector3(0.4f, 0.9f, 0.4f);
        pedestal.GetComponent<Renderer>().sharedMaterial = MaterialSalvo("Pedestal", new Color(0.85f, 0.83f, 0.78f), 0f);
        pedestal.isStatic = true;
        return chao + Vector3.up * 0.9f;
    }

    private static Transform EncontrarPorNome(Transform raiz, string nome)
    {
        string procurado = Normalizar(nome);
        foreach (var parte in raiz.GetComponentsInChildren<Transform>(true))
            if (Normalizar(parte.name) == procurado) return parte;
        return null;
    }

    /// <summary>Compara nomes ignorando maiúsculas, acentos, espaços e "_" (o FBX pode trazer qualquer um).</summary>
    private static string Normalizar(string texto)
    {
        var sb = new StringBuilder();
        foreach (char c in texto.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark || c == ' ' || c == '_') continue;
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    private static Vector3 Plano(Vector3 v) => new Vector3(v.x, 0f, v.z);

    // =====================================================================
    // Livros
    // =====================================================================

    private static List<LivroColetavel> CriarLivros(List<PontoDeLivro> esconderijos)
    {
        var grupo = new GameObject(NomeLivros);
        Undo.RegisterCreatedObjectUndo(grupo, "Criar livros escondidos");
        var livros = new List<LivroColetavel>();

        for (int i = 0; i < QuantidadeDeLivros; i++)
        {
            var dados = SebraelabCulturaSaoLuis.DadosDoLivro(i);
            var cor = SebraelabCulturaSaoLuis.CorDoLivro(i);
            var livro = new GameObject($"Livro {i + 1} - {dados.Titulo}");
            livro.transform.SetParent(grupo.transform, false);
            if (i < esconderijos.Count) livro.transform.SetPositionAndRotation(esconderijos[i].PosicaoDoLivro, esconderijos[i].RotacaoDoLivro);
            else livro.transform.position = Vector3.up * (1f + i * 0.3f);

            CriarModeloDoLivro(livro.transform, i, cor);
            livros.Add(TransformarEmLivroEscondido(livro, dados, cor, CriarBrilho(livro.transform, cor), CriarSom(livro.transform)));
        }
        return livros;
    }

    /// <summary>Livro em pé (16 x 23 cm): capa, contracapa e lombada coloridas e as folhas aparecendo nas bordas.</summary>
    private static void CriarModeloDoLivro(Transform livro, int indice, Color cor)
    {
        var capa = MaterialSalvo($"Livro_Coleta_{indice + 1}", cor, 0.35f);
        var folhas = MaterialSalvo("Livro_Folhas", new Color(0.95f, 0.92f, 0.84f), 0f);
        Parte(livro, "Capa", new Vector3(0f, 0f, 0.0135f), new Vector3(0.16f, 0.23f, 0.003f), capa);
        Parte(livro, "Contracapa", new Vector3(0f, 0f, -0.0135f), new Vector3(0.16f, 0.23f, 0.003f), capa);
        Parte(livro, "Lombada", new Vector3(-0.0785f, 0f, 0f), new Vector3(0.003f, 0.23f, 0.03f), capa);
        Parte(livro, "Folhas", new Vector3(0.002f, 0f, 0f), new Vector3(0.152f, 0.222f, 0.024f), folhas);

        // Um collider só, do tamanho do livro (as peças não têm collider).
        var caixa = livro.gameObject.AddComponent<BoxCollider>();
        caixa.size = new Vector3(0.16f, 0.23f, 0.03f);
    }

    private static void Parte(Transform pai, string nome, Vector3 posicao, Vector3 escala, Material material)
    {
        var parte = GameObject.CreatePrimitive(PrimitiveType.Cube);
        parte.name = nome;
        Object.DestroyImmediate(parte.GetComponent<Collider>());
        parte.transform.SetParent(pai, false);
        parte.transform.localPosition = posicao;
        parte.transform.localScale = escala;
        parte.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Light CriarBrilho(Transform livro, Color cor)
    {
        var objeto = new GameObject("Brilho", typeof(Light));
        objeto.transform.SetParent(livro, false);
        objeto.transform.localPosition = new Vector3(0f, 0f, 0.12f);
        var luz = objeto.GetComponent<Light>();
        luz.type = LightType.Point;
        luz.range = 1.6f;
        luz.intensity = 1.5f;
        luz.color = Color.Lerp(cor, new Color(1f, 0.86f, 0.55f), 0.6f);
        luz.shadows = LightShadows.None;
        return luz;
    }

    private static AudioSource CriarSom(Transform livro)
    {
        var objeto = new GameObject("Som", typeof(AudioSource));
        objeto.transform.SetParent(livro, false);
        var som = objeto.GetComponent<AudioSource>();
        som.playOnAwake = false;
        som.loop = true;
        som.volume = 0.25f;
        som.spatialBlend = 1f;
        som.dopplerLevel = 0f;
        som.rolloffMode = AudioRolloffMode.Linear;
        som.minDistance = 0.6f;
        som.maxDistance = 7f;
        return som;
    }

    /// <summary>
    /// Deixa um objeto pronto para ser um livro escondido: física parada (não cai), XR Grab Interactable (pegar com a
    /// mão ou com o raio) e o <see cref="LivroColetavel"/>.
    /// </summary>
    internal static LivroColetavel TransformarEmLivroEscondido(GameObject livro, LivroData dados, Color cor, Light brilho, AudioSource som)
    {
        foreach (var parte in livro.GetComponentsInChildren<Transform>(true)) parte.gameObject.isStatic = false;

        if (livro.GetComponentInChildren<Collider>(true) == null)
        {
            var caixa = Undo.AddComponent<BoxCollider>(livro);
            var limites = SebraelabMenu.CalcularLimites(livro);
            caixa.center = livro.transform.InverseTransformPoint(limites.center);
            caixa.size = livro.transform.InverseTransformVector(limites.size);
        }

        var corpo = livro.GetComponent<Rigidbody>();
        if (corpo == null) corpo = Undo.AddComponent<Rigidbody>(livro);
        corpo.isKinematic = true; // fica flutuando no esconderijo, sem cair
        corpo.useGravity = false;
        corpo.interpolation = RigidbodyInterpolation.Interpolate;

        var interactable = livro.GetComponent<XRBaseInteractable>();
        if (interactable == null)
        {
            var pegar = Undo.AddComponent<XRGrabInteractable>(livro);
            pegar.throwOnDetach = false;
        }

        var coletavel = livro.GetComponent<LivroColetavel>();
        if (coletavel == null) coletavel = Undo.AddComponent<LivroColetavel>(livro);
        var serializado = new SerializedObject(coletavel);
        if (dados != null) serializado.FindProperty("dados").objectReferenceValue = dados;
        serializado.FindProperty("cor").colorValue = cor;
        if (brilho != null) serializado.FindProperty("brilho").objectReferenceValue = brilho;
        if (som != null) serializado.FindProperty("somProximidade").objectReferenceValue = som;
        serializado.ApplyModifiedPropertiesWithoutUndo();
        return coletavel;
    }

    // =====================================================================
    // Menu do hub (Canvas em World Space)
    // =====================================================================

    private const float LarguraMenu = 1100f; // unidades da UI (1 unidade = 1 mm com escala 0.001)
    private const float AlturaMenu = 680f;
    private static readonly Color CorTexto = new Color(0.93f, 0.95f, 1f);
    private static readonly Color CorSuave = new Color(0.72f, 0.78f, 0.9f);
    private static readonly Color CorDestaque = new Color(0.3f, 0.79f, 0.94f);

    private static HubLivrosMenu CriarMenu(GameObject painel, Transform hub, GerenciadorColeta gerenciador)
    {
        // Em cima dos azulejos do painel, virado para o jogador (a frente do painel é o -Z dele).
        var azulejos = painel != null ? painel.transform.Find("Azulejos") : null;
        Vector3 posicao;
        Quaternion rotacao;
        float larguraDisponivel = 1.1f;
        if (azulejos != null)
        {
            posicao = azulejos.position - painel.transform.forward * 0.012f;
            rotacao = painel.transform.rotation;
            larguraDisponivel = azulejos.lossyScale.x - 0.06f;
        }
        else
        {
            posicao = hub.position + Vector3.up * 0.75f - hub.forward * 0.1f;
            rotacao = Quaternion.LookRotation(-hub.forward, Vector3.up);
        }
        float escala = Mathf.Min(0.001f, larguraDisponivel / LarguraMenu);

        var canvasGO = new GameObject("Menu do hub", typeof(RectTransform));
        canvasGO.layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 0;
        canvasGO.transform.SetParent(hub, true);
        var raiz = (RectTransform)canvasGO.transform;
        raiz.sizeDelta = new Vector2(LarguraMenu, AlturaMenu);
        raiz.SetPositionAndRotation(posicao, rotacao);
        raiz.localScale = Vector3.one * escala / Mathf.Max(0.0001f, hub.lossyScale.x);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
        canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>(); // botões clicáveis com o raio do controle
        var menu = canvasGO.AddComponent<HubLivrosMenu>();
        Undo.RegisterCreatedObjectUndo(canvasGO, "Criar menu do hub");

        var spriteUI = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Imagem(raiz, "Fundo", 0f, 0f, LarguraMenu, AlturaMenu, new Color(0.07f, 0.09f, 0.13f, 0.97f), spriteUI);

        // Faixa de azulejos no topo.
        var textura = SebraelabCulturaSaoLuis.TexturaAzulejo();
        if (textura != null)
        {
            var faixa = Caixa(raiz, "Faixa de azulejos", 0f, 0f, LarguraMenu, 38f).gameObject.AddComponent<RawImage>();
            faixa.texture = textura;
            faixa.uvRect = new Rect(0f, 0f, LarguraMenu / 38f, 1f);
            faixa.raycastTarget = false;
        }

        // ---- Topo ----
        Texto(raiz, "Titulo", "Estante maranhense", 46f, FontStyles.Bold, CorTexto, 36f, 58f, 640f, 60f, TextAlignmentOptions.Left);
        var contador = Texto(raiz, "Contador", "Livros encontrados: 0 de 3", 26f, FontStyles.Normal, CorDestaque, 36f, 118f, 640f, 34f, TextAlignmentOptions.Left);
        Texto(raiz, "Dica", "VR: aponte o raio e aperte o gatilho\nPC: mire e aperte E", 19f, FontStyles.Italic, CorSuave, 700f, 64f, 364f, 60f, TextAlignmentOptions.Right);

        // ---- Lista dos livros (esquerda) ----
        var entradas = new List<(Button botao, TMP_Text titulo, TMP_Text estado, Graphic lombada)>();
        for (int i = 0; i < QuantidadeDeLivros; i++)
        {
            float y = 172f + i * 128f;
            var fundoBotao = Imagem(raiz, $"Livro {i + 1}", 36f, y, 360f, 112f, new Color(0.13f, 0.17f, 0.26f), spriteUI);
            fundoBotao.raycastTarget = true;
            var botao = fundoBotao.gameObject.AddComponent<Button>();
            botao.targetGraphic = fundoBotao;
            var lombada = Imagem((RectTransform)fundoBotao.transform, "Lombada", 16f, 14f, 16f, 84f, new Color(0.32f, 0.34f, 0.4f), null);
            var titulo = Texto((RectTransform)fundoBotao.transform, "Titulo", "???", 25f, FontStyles.Bold, CorTexto, 48f, 12f, 300f, 58f, TextAlignmentOptions.Left);
            AjustarTamanho(titulo, 17f); // títulos longos diminuem para caber
            var estado = Texto((RectTransform)fundoBotao.transform, "Estado", "Ainda não encontrado", 19f, FontStyles.Italic, CorSuave, 48f, 72f, 300f, 28f, TextAlignmentOptions.Left);
            entradas.Add((botao, titulo, estado, lombada));
        }

        // ---- Livro escolhido (direita) ----
        const float x0 = 430f, y0 = 172f, larguraDireita = LarguraMenu - x0 - 36f;
        var painelLivro = Caixa(raiz, "Livro escolhido", x0, y0, larguraDireita, AlturaMenu - y0 - 32f);
        var titulo2 = Texto(painelLivro, "Titulo", "Título do livro", 36f, FontStyles.Bold, CorTexto, 0f, 0f, larguraDireita, 48f, TextAlignmentOptions.Left);
        AjustarTamanho(titulo2, 24f);
        var autor = Texto(painelLivro, "Autor", "Autor", 22f, FontStyles.Italic, CorSuave, 0f, 50f, larguraDireita, 30f, TextAlignmentOptions.Left);
        Texto(painelLivro, "Rotulo", "INTRODUÇÃO DA HISTÓRIA", 17f, FontStyles.Bold, CorDestaque, 0f, 96f, 360f, 24f, TextAlignmentOptions.Left);
        var introducao = Texto(painelLivro, "Introducao", "", 20f, FontStyles.Normal, new Color(0.9f, 0.92f, 0.96f), 0f, 126f, 370f, 270f, TextAlignmentOptions.TopLeft);
        AjustarTamanho(introducao, 15f); // introduções longas também cabem

        var tela = Caixa(painelLivro, "Video", 390f, 96f, larguraDireita - 390f, (larguraDireita - 390f) * 9f / 16f).gameObject.AddComponent<RawImage>();
        tela.color = Color.white;
        tela.raycastTarget = false;
        var video = tela.gameObject.AddComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.isLooping = true;
        video.source = VideoSource.VideoClip;
        video.renderMode = VideoRenderMode.RenderTexture;
        video.audioOutputMode = VideoAudioOutputMode.None;
        video.skipOnDrop = true;

        var capa = Caixa(painelLivro, "Capa", 390f, 96f + (larguraDireita - 390f) * 9f / 16f + 10f, larguraDireita - 390f, 110f).gameObject.AddComponent<Image>();
        capa.preserveAspect = true;
        capa.raycastTarget = false;
        capa.gameObject.SetActive(false);

        float yBotao = AlturaMenu - y0 - 32f - 66f;
        var fundoDesafios = Imagem(painelLivro, "Botao Desafios", 0f, yBotao, 260f, 66f, new Color(0.87f, 0.48f, 0.12f), spriteUI);
        fundoDesafios.raycastTarget = true;
        var botaoDesafios = fundoDesafios.gameObject.AddComponent<Button>();
        botaoDesafios.targetGraphic = fundoDesafios;
        var textoBotao = Texto((RectTransform)fundoDesafios.transform, "Texto", "Desafios", 28f, FontStyles.Bold, Color.white, 0f, 0f, 260f, 66f, TextAlignmentOptions.Center);
        textoBotao.raycastTarget = false;
        var avisoDesafios = Texto(painelLivro, "Aviso desafios", "Os desafios deste livro chegam em breve!", 19f, FontStyles.Italic,
                                  new Color(1f, 0.82f, 0.5f), 276f, yBotao, larguraDireita - 276f, 66f, TextAlignmentOptions.Left);

        var painelVazio = Caixa(raiz, "Nenhum livro escolhido", x0, y0, larguraDireita, AlturaMenu - y0 - 32f);
        Texto(painelVazio, "Texto", "Os livros estão escondidos pelo Sebraelab.\n\nQuando encontrar um, escolha-o na lista para ler a introdução da história.",
              24f, FontStyles.Normal, CorSuave, 0f, 40f, larguraDireita, 260f, TextAlignmentOptions.TopLeft);

        // ---- Liga tudo no HubLivrosMenu ----
        var serializado = new SerializedObject(menu);
        serializado.FindProperty("gerenciador").objectReferenceValue = gerenciador;
        serializado.FindProperty("textoContador").objectReferenceValue = contador;
        var lista = serializado.FindProperty("entradas");
        lista.arraySize = entradas.Count;
        for (int i = 0; i < entradas.Count; i++)
        {
            var item = lista.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("botao").objectReferenceValue = entradas[i].botao;
            item.FindPropertyRelative("titulo").objectReferenceValue = entradas[i].titulo;
            item.FindPropertyRelative("estado").objectReferenceValue = entradas[i].estado;
            item.FindPropertyRelative("lombada").objectReferenceValue = entradas[i].lombada;
        }
        serializado.FindProperty("painelLivro").objectReferenceValue = painelLivro.gameObject;
        serializado.FindProperty("painelVazio").objectReferenceValue = painelVazio.gameObject;
        serializado.FindProperty("textoTitulo").objectReferenceValue = titulo2;
        serializado.FindProperty("textoAutor").objectReferenceValue = autor;
        serializado.FindProperty("textoIntroducao").objectReferenceValue = introducao;
        serializado.FindProperty("imagemCapa").objectReferenceValue = capa;
        serializado.FindProperty("videoPlayer").objectReferenceValue = video;
        serializado.FindProperty("telaVideo").objectReferenceValue = tela;
        serializado.FindProperty("botaoDesafios").objectReferenceValue = botaoDesafios;
        serializado.FindProperty("avisoDesafios").objectReferenceValue = avisoDesafios;
        serializado.ApplyModifiedPropertiesWithoutUndo();

        painelLivro.gameObject.SetActive(false);
        return menu;
    }

    /// <summary>RectTransform ancorado no canto superior esquerdo do pai (x para a direita, y para baixo, em mm).</summary>
    private static RectTransform Caixa(RectTransform pai, string nome, float x, float y, float largura, float altura)
    {
        var objeto = new GameObject(nome, typeof(RectTransform));
        objeto.layer = pai.gameObject.layer;
        objeto.transform.SetParent(pai, false);
        var rt = (RectTransform)objeto.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(largura, altura);
        return rt;
    }

    private static Image Imagem(RectTransform pai, string nome, float x, float y, float largura, float altura, Color cor, Sprite sprite)
    {
        var imagem = Caixa(pai, nome, x, y, largura, altura).gameObject.AddComponent<Image>();
        imagem.color = cor;
        if (sprite != null)
        {
            imagem.sprite = sprite;
            imagem.type = Image.Type.Sliced;
        }
        imagem.raycastTarget = false;
        return imagem;
    }

    private static TextMeshProUGUI Texto(RectTransform pai, string nome, string conteudo, float tamanho, FontStyles estilo, Color cor,
                                         float x, float y, float largura, float altura, TextAlignmentOptions alinhamento)
    {
        var texto = Caixa(pai, nome, x, y, largura, altura).gameObject.AddComponent<TextMeshProUGUI>();
        texto.text = conteudo;
        texto.fontSize = tamanho;
        texto.fontStyle = estilo;
        texto.color = cor;
        texto.alignment = alinhamento;
        texto.raycastTarget = false;
        return texto;
    }

    /// <summary>O texto diminui sozinho (até <paramref name="minimo"/>) se não couber na caixa.</summary>
    private static void AjustarTamanho(TMP_Text texto, float minimo)
    {
        texto.fontSizeMax = texto.fontSize;
        texto.fontSizeMin = minimo;
        texto.enableAutoSizing = true;
    }

    // =====================================================================
    // Utilitários
    // =====================================================================

    private static void Preencher(SerializedProperty lista, List<Object> valores)
    {
        lista.arraySize = valores.Count;
        for (int i = 0; i < valores.Count; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
    }

    private static void AdicionarNaLista(Object dono, string campo, Object valor)
    {
        var serializado = new SerializedObject(dono);
        var lista = serializado.FindProperty(campo);
        for (int i = 0; i < lista.arraySize; i++)
            if (lista.GetArrayElementAtIndex(i).objectReferenceValue == valor) return;
        lista.arraySize++;
        lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = valor;
        serializado.ApplyModifiedProperties();
    }

    /// <summary>Material do pipeline ativo (URP/Lit), salvo em Assets/Materials. Brilho &gt; 0 liga a emissão.</summary>
    private static Material MaterialSalvo(string nome, Color cor, float brilho)
    {
        if (!AssetDatabase.IsValidFolder(PastaMateriais)) AssetDatabase.CreateFolder("Assets", "Materials");
        string caminho = $"{PastaMateriais}/{nome}.mat";
        var existente = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (existente != null) return existente;

        var padrao = GraphicsSettings.defaultRenderPipeline != null ? GraphicsSettings.defaultRenderPipeline.defaultMaterial : null;
        var material = padrao != null ? new Material(padrao) : new Material(Shader.Find("Standard"));
        material.color = cor;
        if (brilho > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", cor * brilho);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        AssetDatabase.CreateAsset(material, caminho);
        return material;
    }
}
