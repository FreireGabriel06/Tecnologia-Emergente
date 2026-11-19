using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Cultura de São Luís do Maranhão no Sebraelab VR:
/// - a "Estante maranhense": os livros de exemplo viram obras de autores nascidos em São Luís;
/// - um painel de azulejos (padrão português, como nos casarões do Centro Histórico) atrás dos livros;
/// - a textura de azulejo, gerada aqui mesmo (não depende de imagem externa), também enfeita o topo dos popups.
///
/// O configurador automático já monta tudo isso na cena nova. Para uma cena criada antes, use
/// Sebraelab > Cultura de São Luís > Montar estante maranhense na cena aberta.
/// </summary>
internal static class SebraelabCulturaSaoLuis
{
    internal const string Pasta = "Assets/CulturaSaoLuis";
    private const string PastaLivros = "Assets/Livros";
    private const string CaminhoTextura = Pasta + "/Azulejo_SaoLuis.png";
    private const string CaminhoMaterialAzulejo = Pasta + "/Azulejo_SaoLuis.mat";
    private const string CaminhoMaterialMoldura = Pasta + "/Moldura_Madeira.mat";
    private const string CaminhoVideo = Pasta + "/AzulejosSaoLuis.mp4";
    internal const string NomePainel = "Painel de azulejos (São Luís)";

    private const float TamanhoAzulejo = 0.15f; // 15 cm, como os azulejos dos casarões
    private const int LinhasDeAzulejos = 5;
    private const int ResolucaoTextura = 512;

    // Cores do azulejo português (as mesmas do vídeo de apresentação).
    private static readonly Color32 Esmalte = new Color32(243, 239, 228, 255);
    private static readonly Color32 Cobalto = new Color32(31, 60, 136, 255);
    private static readonly Color32 AzulClaro = new Color32(111, 143, 208, 255);
    private static readonly Color32 Rejunte = new Color32(214, 209, 197, 255);

    private readonly struct Livro
    {
        public readonly string Titulo, Autor, Resumo;
        public readonly Color Cor;

        public Livro(string titulo, string autor, string resumo, Color cor)
        {
            Titulo = titulo;
            Autor = autor;
            Resumo = resumo;
            Cor = cor;
        }
    }

    /// <summary>Obras de três escritores nascidos em São Luís. A capa fica vazia: coloque a imagem no asset, se tiver.</summary>
    private static readonly Livro[] Estante =
    {
        new Livro("O Mulato", "Aluísio Azevedo (1881)",
            "Um dos marcos do Naturalismo no Brasil. Raimundo volta a São Luís depois de estudar na Europa e descobre " +
            "que é filho de uma mulher escravizada. O romance expõe o preconceito racial e a hipocrisia da sociedade " +
            "maranhense do século XIX.",
            new Color(0.12f, 0.24f, 0.53f)), // azul cobalto dos azulejos
        new Livro("Os Tambores de São Luís", "Josué Montello (1975)",
            "Ao atravessar as ruas de São Luís numa noite, o velho Damião relembra a própria vida, da escravidão à " +
            "liberdade. O romance retrata a luta do povo negro no Maranhão, ao som dos tambores.",
            new Color(0.7f, 0.13f, 0.16f)), // vermelho do Bumba-meu-boi
        new Livro("Poema Sujo", "Ferreira Gullar (1976)",
            "Poema longo escrito em 1975, no exílio em Buenos Aires. Ferreira Gullar revisita a infância em São Luís " +
            "(as ruas, a família, os cheiros e os sons da cidade) num dos grandes poemas da língua portuguesa.",
            new Color(0.86f, 0.62f, 0.12f)), // amarelo
    };

    internal static int QuantidadeDeLivros => Estante.Length;

    internal static Color CorDoLivro(int indice) => Estante[indice % Estante.Length].Cor;

    // =====================================================================
    // Menu
    // =====================================================================

    [MenuItem("Sebraelab/Cultura de São Luís/Montar estante maranhense na cena aberta", false, 40)]
    private static void MontarNaCenaAberta()
    {
        var livros = new List<LivroPopup>(Object.FindObjectsByType<LivroPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        if (livros.Count == 0)
        {
            EditorUtility.DisplayDialog("Sebraelab",
                "A cena aberta não tem livros. Rode Sebraelab > Configurar projeto VR (automático) ou configure um livro " +
                "com Sebraelab > Livros > Configurar livro selecionado e tente de novo.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Estante maranhense (Sebraelab)");
        int grupo = Undo.GetCurrentGroup();

        // Ordena os livros da esquerda para a direita, do ponto de vista do jogador.
        var centro = Vector3.zero;
        foreach (var livro in livros) centro += livro.transform.position;
        centro /= livros.Count;
        var direcao = DirecaoDoJogador(centro);
        var lateral = Vector3.Cross(Vector3.up, direcao);
        livros.Sort((a, b) => Vector3.Dot(b.transform.position - centro, lateral).CompareTo(Vector3.Dot(a.transform.position - centro, lateral)));

        int trocados = 0;
        for (int i = 0; i < livros.Count; i++)
        {
            var atual = livros[i].Dados;
            if (atual != null && !atual.name.StartsWith("Livro de exemplo")) continue; // não mexe em livro já cadastrado

            var serializado = new SerializedObject(livros[i]);
            serializado.FindProperty("dados").objectReferenceValue = DadosDoLivro(i);
            serializado.ApplyModifiedProperties();
            livros[i].AplicarDados();
            trocados++;
        }

        bool painelNovo = GameObject.Find(NomePainel) == null;
        if (painelNovo)
        {
            float tampo = centro.y - 0.02f;
            float piso = AlturaDoPiso(centro, tampo - 0.75f);
            CriarPainel(centro, direcao, piso, tampo);
        }

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(livros[0].gameObject.scene);
        Debug.Log($"[Sebraelab] Estante maranhense: {trocados} livro(s) com obras de autores de São Luís" +
                  (painelNovo ? " e painel de azulejos criado." : "; o painel de azulejos já existia."));
    }

    // =====================================================================
    // Livros
    // =====================================================================

    /// <summary>Asset "Dados do Livro" da obra número <paramref name="indice"/> da estante (cria na primeira vez).</summary>
    internal static LivroData DadosDoLivro(int indice)
    {
        var livro = Estante[indice % Estante.Length];
        CriarPasta(PastaLivros);
        string caminho = $"{PastaLivros}/{livro.Titulo}.asset";

        var existente = AssetDatabase.LoadAssetAtPath<LivroData>(caminho);
        if (existente != null) return existente; // preserva o que o grupo já editou

        var dados = ScriptableObject.CreateInstance<LivroData>();
        AssetDatabase.CreateAsset(dados, caminho);

        var serializado = new SerializedObject(dados);
        serializado.FindProperty("titulo").stringValue = livro.Titulo;
        serializado.FindProperty("autor").stringValue = livro.Autor;
        serializado.FindProperty("resumo").stringValue = livro.Resumo;
        serializado.FindProperty("video").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VideoClip>(CaminhoVideo);
        serializado.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return dados;
    }

    // =====================================================================
    // Azulejo
    // =====================================================================

    /// <summary>
    /// Desenho de um azulejo (x, y entre 0 e 1): roseta de 8 pétalas no centro, um quarto de flor em cada canto
    /// (quatro azulejos juntos formam a flor inteira), folhas no meio das bordas e o rejunte.
    /// Retorna 0 = esmalte, 1 = cobalto, 2 = azul claro, 3 = rejunte.
    /// </summary>
    internal static int AmostraAzulejo(float x, float y)
    {
        float cx = x - 0.5f, cy = y - 0.5f;
        float r = Mathf.Sqrt(cx * cx + cy * cy), angulo = Mathf.Atan2(cy, cx);
        float dx = Mathf.Min(x, 1f - x), dy = Mathf.Min(y, 1f - y), dc = Mathf.Sqrt(dx * dx + dy * dy);
        if (dx < 0.006f || dy < 0.006f) return 3;

        int c = 0;
        float anguloCanto = Mathf.Atan2(dy, dx);
        if (dc < 0.2f * (0.55f + 0.45f * Mathf.Pow(Mathf.Abs(Mathf.Cos(2f * anguloCanto)), 0.8f))) c = 1;
        if (dc < 0.085f) c = 0;
        if (dc < 0.045f) c = 2;
        if (Mathf.Abs(dc - 0.245f) < 0.011f) c = 2;

        Folha(cx, 0.5f - Mathf.Abs(cy), ref c);
        Folha(cy, 0.5f - Mathf.Abs(cx), ref c);

        if (Mathf.Abs(Mathf.Abs(cx) - Mathf.Abs(cy)) < 0.009f * 1.41421356f && r > 0.33f && dc > 0.262f) c = 2;

        if (Mathf.Abs(r - 0.315f) < 0.012f) c = 2;
        if (r < 0.27f * (0.42f + 0.58f * Mathf.Pow(Mathf.Abs(Mathf.Cos(4f * angulo)), 0.75f))) c = 1;
        float petalaInterna = 0.17f * (0.45f + 0.55f * Mathf.Pow(Mathf.Abs(Mathf.Cos(4f * angulo)), 1.2f));
        if (r < petalaInterna && r > 0.02f) c = r < petalaInterna - 0.022f ? 2 : 0;
        if (r < 0.075f) c = 0;
        if (r < 0.042f) c = 1;
        return c;
    }

    private static void Folha(float u, float v, ref int c)
    {
        if ((u / 0.075f) * (u / 0.075f) + (v / 0.06f) * (v / 0.06f) < 1f) c = 1;
        if ((u / 0.035f) * (u / 0.035f) + (v / 0.028f) * (v / 0.028f) < 1f) c = 0;
    }

    /// <summary>Textura do azulejo (gera o PNG na primeira vez). Repete sem emenda: use tiling para cobrir uma parede.</summary>
    internal static Texture2D TexturaAzulejo()
    {
        var existente = AssetDatabase.LoadAssetAtPath<Texture2D>(CaminhoTextura);
        if (existente != null) return existente;

        CriarPasta(Pasta);
        var cores = new[] { Esmalte, Cobalto, AzulClaro, Rejunte };
        const int amostras = 3; // 3 x 3 amostras por pixel deixam as curvas suaves
        var pixels = new Color32[ResolucaoTextura * ResolucaoTextura];
        for (int j = 0; j < ResolucaoTextura; j++)
        {
            for (int i = 0; i < ResolucaoTextura; i++)
            {
                int r = 0, g = 0, b = 0;
                for (int a = 0; a < amostras; a++)
                {
                    for (int k = 0; k < amostras; k++)
                    {
                        var cor = cores[AmostraAzulejo((i + (a + 0.5f) / amostras) / ResolucaoTextura,
                                                       (j + (k + 0.5f) / amostras) / ResolucaoTextura)];
                        r += cor.r;
                        g += cor.g;
                        b += cor.b;
                    }
                }
                const int total = amostras * amostras;
                pixels[j * ResolucaoTextura + i] = new Color32((byte)(r / total), (byte)(g / total), (byte)(b / total), 255);
            }
        }

        var textura = new Texture2D(ResolucaoTextura, ResolucaoTextura, TextureFormat.RGBA32, false);
        textura.SetPixels32(pixels);
        textura.Apply();
        File.WriteAllBytes(CaminhoTextura, textura.EncodeToPNG());
        Object.DestroyImmediate(textura);

        AssetDatabase.ImportAsset(CaminhoTextura, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(CaminhoTextura) is TextureImporter importador)
        {
            importador.textureType = TextureImporterType.Default;
            importador.sRGBTexture = true;
            importador.wrapMode = TextureWrapMode.Repeat; // necessário para repetir na parede e na faixa do popup
            importador.mipmapEnabled = true;
            importador.anisoLevel = 4;
            importador.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(CaminhoTextura);
    }

    private static Material MaterialAzulejo()
    {
        var existente = AssetDatabase.LoadAssetAtPath<Material>(CaminhoMaterialAzulejo);
        if (existente != null) return existente;

        var material = NovoMaterial(Color.white);
        material.mainTexture = TexturaAzulejo();
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.75f); // azulejo é vidrado
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.75f);
        AssetDatabase.CreateAsset(material, CaminhoMaterialAzulejo);
        return material;
    }

    private static Material MaterialMoldura()
    {
        var existente = AssetDatabase.LoadAssetAtPath<Material>(CaminhoMaterialMoldura);
        if (existente != null) return existente;

        var material = NovoMaterial(new Color(0.24f, 0.15f, 0.09f));
        AssetDatabase.CreateAsset(material, CaminhoMaterialMoldura);
        return material;
    }

    private static Material NovoMaterial(Color cor)
    {
        CriarPasta(Pasta);
        var padrao = GraphicsSettings.defaultRenderPipeline != null ? GraphicsSettings.defaultRenderPipeline.defaultMaterial : null;
        var material = padrao != null ? new Material(padrao) : new Material(Shader.Find("Standard"));
        material.color = cor;
        return material;
    }

    // =====================================================================
    // Painel de azulejos
    // =====================================================================

    /// <summary>
    /// Painel de azulejos com a placa "Estante maranhense" atrás dos livros. Se houver uma parede logo atrás, ele é
    /// fixado nela; senão fica em pé sozinho, num totem de madeira.
    /// </summary>
    /// <param name="centroLivros">Centro dos livros (em cima da mesa).</param>
    /// <param name="direcaoJogador">Direção, a partir dos livros, de onde o jogador olha.</param>
    /// <param name="alturaPiso">Altura do chão.</param>
    /// <param name="alturaTampo">Altura do tampo da mesa.</param>
    internal static GameObject CriarPainel(Vector3 centroLivros, Vector3 direcaoJogador, float alturaPiso, float alturaTampo)
    {
        var existente = GameObject.Find(NomePainel);
        if (existente != null) return existente;

        direcaoJogador.y = 0f;
        direcaoJogador = direcaoJogador.sqrMagnitude > 0.0001f ? direcaoJogador.normalized : Vector3.back;
        float alturaAzulejos = LinhasDeAzulejos * TamanhoAzulejo;
        float baseAzulejos = Mathf.Max(alturaPiso + 0.8f, alturaTampo + 0.25f);
        var origem = new Vector3(centroLivros.x, baseAzulejos + alturaAzulejos / 2f, centroLivros.z);

        bool naParede = ProcurarParede(origem, direcaoJogador, alturaAzulejos, out var ponto, out var normal, out float largura);
        if (!naParede)
        {
            largura = 10 * TamanhoAzulejo;
            normal = direcaoJogador;
            // Totem 75 cm atrás dos livros, ou um pouco antes do que estiver no caminho.
            float distancia = Raio(origem, -direcaoJogador, out var obstaculo) ? Mathf.Clamp(obstaculo.distance - 0.1f, 0.45f, 0.75f) : 0.75f;
            ponto = origem - direcaoJogador * distancia;
        }

        // Frente do painel (-Z local) virada para o jogador, como um Quad ou um Canvas visto de frente.
        var painel = new GameObject(NomePainel);
        painel.transform.SetPositionAndRotation(ponto, Quaternion.LookRotation(-normal, Vector3.up));
        Undo.RegisterCreatedObjectUndo(painel, "Criar painel de azulejos");

        const float borda = 0.04f, alturaPlaca = 0.18f, folgaPlaca = 0.03f, espessura = 0.025f;
        float topo = alturaAzulejos / 2f + folgaPlaca + alturaPlaca + borda;
        float fundoMoldura = -alturaAzulejos / 2f - borda;
        if (!naParede) fundoMoldura = alturaPiso - origem.y; // o totem vai até o chão
        float profundidade = naParede ? espessura : 0.06f;

        var moldura = GameObject.CreatePrimitive(PrimitiveType.Cube);
        moldura.name = "Moldura";
        moldura.transform.SetParent(painel.transform, false);
        moldura.transform.localPosition = new Vector3(0f, (topo + fundoMoldura) / 2f, naParede ? -espessura / 2f : 0f);
        moldura.transform.localScale = new Vector3(largura + 2f * borda, topo - fundoMoldura, profundidade);
        moldura.GetComponent<Renderer>().sharedMaterial = MaterialMoldura();
        moldura.isStatic = true;

        float frente = naParede ? -espessura - 0.002f : -profundidade / 2f - 0.002f;

        var azulejos = GameObject.CreatePrimitive(PrimitiveType.Quad);
        azulejos.name = "Azulejos";
        Object.DestroyImmediate(azulejos.GetComponent<Collider>()); // a moldura já tem colisão
        azulejos.transform.SetParent(painel.transform, false);
        azulejos.transform.localPosition = new Vector3(0f, 0f, frente);
        azulejos.transform.localScale = new Vector3(largura, alturaAzulejos, 1f);
        // A textura tem 1 azulejo: o tiling do material faz cada um medir 15 cm (um material para cada tamanho de painel).
        int colunas = Mathf.RoundToInt(largura / TamanhoAzulejo);
        string caminhoMaterialPainel = $"{Pasta}/Azulejo_Painel_{colunas}x{LinhasDeAzulejos}.mat";
        var materialPainel = AssetDatabase.LoadAssetAtPath<Material>(caminhoMaterialPainel);
        if (materialPainel == null)
        {
            materialPainel = new Material(MaterialAzulejo());
            materialPainel.mainTextureScale = new Vector2(colunas, LinhasDeAzulejos);
            AssetDatabase.CreateAsset(materialPainel, caminhoMaterialPainel);
        }
        azulejos.GetComponent<Renderer>().sharedMaterial = materialPainel;
        azulejos.isStatic = true;

        CriarPlaca(painel.transform, largura, alturaAzulejos / 2f + folgaPlaca + alturaPlaca / 2f, alturaPlaca, frente);

        Debug.Log(naParede
            ? $"[Sebraelab] Painel de azulejos fixado na parede atrás dos livros ({largura:0.##} m de largura)."
            : "[Sebraelab] Não há parede logo atrás dos livros: o painel de azulejos ficou num totem.");
        return painel;
    }

    private static void CriarPlaca(Transform painel, float largura, float centroY, float altura, float frente)
    {
        var placa = new GameObject("Placa", typeof(RectTransform));
        placa.layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 0;
        placa.transform.SetParent(painel, false);
        var rt = (RectTransform)placa.transform;
        rt.sizeDelta = new Vector2(largura * 1000f, altura * 1000f); // 1 unidade = 1 mm
        rt.localScale = Vector3.one * 0.001f;
        rt.localPosition = new Vector3(0f, centroY, frente);

        placa.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        placa.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
        var fundo = placa.AddComponent<Image>();
        fundo.color = new Color32(Esmalte.r, Esmalte.g, Esmalte.b, 255);
        fundo.raycastTarget = false;

        Texto(placa.transform, "Titulo", "Estante maranhense", 74f, FontStyles.Bold, Cobalto, new Vector2(0f, 0.42f), new Vector2(1f, 1f));
        Texto(placa.transform, "Subtitulo", "Autores nascidos em São Luís do Maranhão", 38f, FontStyles.Normal,
              new Color(0.25f, 0.3f, 0.42f), new Vector2(0f, 0.05f), new Vector2(1f, 0.45f));
    }

    private static void Texto(Transform pai, string nome, string conteudo, float tamanho, FontStyles estilo, Color cor,
                              Vector2 ancoraMin, Vector2 ancoraMax)
    {
        var objeto = new GameObject(nome, typeof(RectTransform));
        objeto.layer = pai.gameObject.layer;
        objeto.transform.SetParent(pai, false);
        var rt = (RectTransform)objeto.transform;
        rt.anchorMin = ancoraMin;
        rt.anchorMax = ancoraMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var texto = objeto.AddComponent<TextMeshProUGUI>();
        texto.text = conteudo;
        texto.fontSize = tamanho;
        texto.fontStyle = estilo;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = cor;
        texto.raycastTarget = false;
    }

    /// <summary>
    /// Procura uma parede atrás dos livros, virada para o jogador, larga o bastante para o painel (tenta 10, 8 e depois
    /// 6 azulejos de largura). Aceita paredes com ripas ou frestas (até 12 cm de profundidade), mas não algo saindo
    /// mais de 4 cm para fora dela; o painel encosta na parte mais saliente.
    /// </summary>
    private static bool ProcurarParede(Vector3 origem, Vector3 direcaoJogador, float alturaAzulejos,
                                       out Vector3 ponto, out Vector3 normal, out float largura)
    {
        Physics.SyncTransforms();
        ponto = normal = Vector3.zero;
        largura = 0f;
        if (!Raio(origem, -direcaoJogador, out var centro)) return false;

        var n = centro.normal;
        n.y = 0f;
        if (n.sqrMagnitude < 0.5f) return false; // não é uma parede vertical
        n.Normalize();
        if (Vector3.Dot(n, direcaoJogador) < 0.85f) return false;

        var lateral = Vector3.Cross(Vector3.up, n);
        float planoCentro = Vector3.Dot(centro.point, n);
        var alturas = new[] { -alturaAzulejos / 2f, alturaAzulejos / 2f + 0.21f };
        foreach (int colunas in new[] { 10, 8, 6 })
        {
            float meia = colunas * TamanhoAzulejo / 2f + 0.04f;
            float frente = planoCentro;
            bool cabe = true;
            foreach (float lado in new[] { -meia, meia })
            {
                foreach (float altura in alturas)
                {
                    var inicio = centro.point + n * 0.5f + lateral * lado + Vector3.up * (altura + origem.y - centro.point.y);
                    if (!Raio(inicio, -n, out var teste) || Vector3.Dot(teste.normal, n) < 0.95f)
                    {
                        cabe = false;
                        break;
                    }
                    float desvio = Vector3.Dot(teste.point, n) - planoCentro;
                    if (desvio > 0.04f || desvio < -0.12f)
                    {
                        cabe = false;
                        break;
                    }
                    frente = Mathf.Max(frente, planoCentro + desvio);
                }
                if (!cabe) break;
            }
            if (!cabe) continue;

            normal = n;
            ponto = centro.point + n * (frente - planoCentro + 0.002f);
            largura = colunas * TamanhoAzulejo;
            return true;
        }
        return false;
    }

    /// <summary>Raio que ignora os livros, triggers e o chão de teleporte.</summary>
    private static bool Raio(Vector3 origem, Vector3 direcao, out RaycastHit acerto)
    {
        var acertos = Physics.RaycastAll(origem, direcao, 1.6f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(acertos, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var a in acertos)
        {
            if (a.collider.GetComponentInParent<LivroPopup>() != null) continue;
            if (a.collider.GetComponentInParent<Rigidbody>() != null) continue;
            acerto = a;
            return true;
        }
        acerto = default;
        return false;
    }

    // =====================================================================
    // Utilitários
    // =====================================================================

    /// <summary>De que lado dos livros o jogador fica: o jogador de PC ou a câmera principal; senão, a frente dos livros.</summary>
    private static Vector3 DirecaoDoJogador(Vector3 centro)
    {
        Vector3? posicao = null;
        var jogador = Object.FindFirstObjectByType<DesktopPlayerController>(FindObjectsInactive.Include);
        if (jogador != null) posicao = jogador.transform.position;
        else if (Camera.main != null) posicao = Camera.main.transform.position;

        var direcao = posicao.HasValue ? posicao.Value - centro : Vector3.back;
        direcao.y = 0f;
        return direcao.sqrMagnitude > 0.01f ? direcao.normalized : Vector3.back;
    }

    private static float AlturaDoPiso(Vector3 centroLivros, float padrao)
    {
        Physics.SyncTransforms();
        foreach (var a in Physics.RaycastAll(centroLivros + Vector3.up * 0.1f, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (a.collider.GetComponentInParent<LivroPopup>() != null) continue;
            if (a.collider.gameObject.name == SebraelabMenu.NomeChaoTeleporte) return a.point.y;
        }
        return padrao;
    }

    private static void CriarPasta(string caminho)
    {
        if (AssetDatabase.IsValidFolder(caminho)) return;
        var pai = Path.GetDirectoryName(caminho)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(pai) && !AssetDatabase.IsValidFolder(pai)) CriarPasta(pai);
        AssetDatabase.CreateFolder(pai, Path.GetFileName(caminho));
    }
}
