using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Menu do hub dos livros (Canvas em World Space, montado pelo configurador na parede do hub).
///
/// - À esquerda, a lista dos livros: os que ainda não foram encontrados aparecem como "???".
/// - À direita, o livro escolhido: título, autor, introdução da história, capa e vídeo.
/// - Botão "Desafios": por enquanto só está preparado. Ele dispara o evento "Ao Abrir Desafios" (com os dados do
///   livro) e mostra a mensagem de "em breve". Para ligar um minijogo, use esse evento no Inspector.
///
/// Em VR os botões funcionam com o raio do controle (TrackedDeviceGraphicRaycaster + XRUIInputModule).
/// No PC, o DesktopInteracao usa <see cref="ClicarNaMira"/>: mire no botão e aperte E (ou clique).
/// </summary>
[AddComponentMenu("Sebraelab/Hub Livros Menu")]
public class HubLivrosMenu : MonoBehaviour
{
    [Serializable]
    public class Entrada
    {
        public Button botao;
        public TMP_Text titulo;
        public TMP_Text estado;
        public Graphic lombada;
    }

    [Serializable] public class EventoLivro : UnityEvent<LivroData> { }

    [SerializeField] private GerenciadorColeta gerenciador;

    [Header("Topo")]
    [SerializeField] private TMP_Text textoContador;

    [Header("Lista de livros")]
    [SerializeField] private Entrada[] entradas = new Entrada[0];

    [Header("Livro escolhido")]
    [SerializeField] private GameObject painelLivro;
    [SerializeField] private GameObject painelVazio;
    [SerializeField] private TMP_Text textoTitulo;
    [SerializeField] private TMP_Text textoAutor;
    [SerializeField] private TMP_Text textoIntroducao;
    [SerializeField] private Image imagemCapa;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage telaVideo;

    [Header("Desafios (fase futura)")]
    [SerializeField] private Button botaoDesafios;
    [SerializeField] private TMP_Text avisoDesafios;
    [SerializeField] private string mensagemDesafios = "Os desafios deste livro chegam em breve!";
    [Tooltip("Ligue aqui o minijogo do livro quando ele existir. Recebe os dados do livro escolhido.")]
    [SerializeField] private EventoLivro aoAbrirDesafios = new EventoLivro();

    [Header("Cores")]
    [SerializeField] private Color corBotao = new Color(0.13f, 0.17f, 0.26f);
    [SerializeField] private Color corEscolhido = new Color(0.16f, 0.36f, 0.62f);
    [SerializeField] private Color corNaMira = new Color(0.24f, 0.32f, 0.48f);
    [SerializeField] private Color corBloqueado = new Color(0.32f, 0.34f, 0.4f);

    private readonly List<Button> botoes = new List<Button>();
    private int escolhido = -1;
    private bool mostrandoDicas;
    private float proximaChecagem;
    private Button botaoNaMira;
    private RenderTexture texturaVideo;

    /// <summary>Índice do livro aberto no menu (-1 = nenhum).</summary>
    public int Escolhido => escolhido;

    private void Awake()
    {
        if (gerenciador == null) gerenciador = GetComponentInParent<GerenciadorColeta>();
        if (gerenciador == null) gerenciador = FindFirstObjectByType<GerenciadorColeta>();

        for (int i = 0; i < entradas.Length; i++)
        {
            if (entradas[i] == null || entradas[i].botao == null) continue;
            int indice = i;
            entradas[i].botao.onClick.AddListener(() => Selecionar(indice));
            botoes.Add(entradas[i].botao);
        }

        if (botaoDesafios != null)
        {
            botaoDesafios.onClick.AddListener(AbrirDesafios);
            botoes.Add(botaoDesafios);
        }

        if (avisoDesafios != null) avisoDesafios.gameObject.SetActive(false);
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = true;
        }
    }

    private void Start() => Atualizar();

    private void Update()
    {
        // As dicas aparecem sozinhas depois de um tempo sem achar livro (ver GerenciadorColeta).
        if (gerenciador == null || Time.time < proximaChecagem) return;
        proximaChecagem = Time.time + 1f;
        if (gerenciador.DicasLiberadas != mostrandoDicas) Atualizar();
    }

    private void OnDisable()
    {
        if (videoPlayer != null) videoPlayer.Stop();
    }

    private void OnDestroy()
    {
        if (texturaVideo == null) return;
        texturaVideo.Release();
        Destroy(texturaVideo);
    }

    /// <summary>Chamado pelo hub no começo da partida.</summary>
    public void Configurar(GerenciadorColeta hub)
    {
        gerenciador = hub;
        Atualizar();
    }

    /// <summary>Atualiza a lista e o contador (chamado a cada livro encontrado).</summary>
    public void Atualizar()
    {
        int total = gerenciador != null ? gerenciador.Total : entradas.Length;
        int achados = gerenciador != null ? gerenciador.Encontrados : 0;
        mostrandoDicas = gerenciador != null && gerenciador.DicasLiberadas;
        if (textoContador != null)
        {
            if (achados >= total && total > 0)
            {
                int segundos = Mathf.RoundToInt(gerenciador != null ? gerenciador.TempoDePartida : 0f);
                textoContador.text = $"Você encontrou os {total} livros em {segundos / 60}:{segundos % 60:00}!";
            }
            else
            {
                textoContador.text = $"Livros encontrados: {achados} de {total}";
            }
        }

        for (int i = 0; i < entradas.Length; i++)
        {
            var entrada = entradas[i];
            if (entrada == null || entrada.botao == null) continue;

            var livro = Livro(i);
            entrada.botao.gameObject.SetActive(i < total);
            bool encontrado = livro != null && livro.Coletado;
            entrada.botao.interactable = encontrado;
            if (entrada.titulo != null) entrada.titulo.text = encontrado ? livro.Titulo : "???";
            if (entrada.estado != null)
            {
                string dica = !encontrado && mostrandoDicas && livro != null ? gerenciador.DicaDe(livro) : string.Empty;
                entrada.estado.text = encontrado ? livro.Autor : string.IsNullOrEmpty(dica) ? "Ainda não encontrado" : "Dica: " + dica;
            }
            if (entrada.lombada != null) entrada.lombada.color = encontrado ? livro.Cor : corBloqueado;
        }

        if (escolhido >= 0) MostrarLivro(escolhido);
        else MostrarVazio();
        PintarBotoes();
    }

    /// <summary>Abre a introdução do livro <paramref name="indice"/> (só se ele já foi encontrado).</summary>
    public void Selecionar(int indice)
    {
        var livro = Livro(indice);
        if (livro == null || !livro.Coletado) return;
        escolhido = indice;
        MostrarLivro(indice);
        PintarBotoes();
    }

    /// <summary>Botão "Desafios": por enquanto só avisa que vem em breve e dispara o evento.</summary>
    public void AbrirDesafios()
    {
        var livro = Livro(escolhido);
        if (livro == null) return;

        if (avisoDesafios != null)
        {
            avisoDesafios.text = mensagemDesafios;
            avisoDesafios.gameObject.SetActive(true);
        }

        Debug.Log($"[Sebraelab] Desafios de '{livro.Titulo}': ainda não implementado (ligue o minijogo no evento Ao Abrir Desafios do menu do hub).", this);
        aoAbrirDesafios.Invoke(livro.Dados);
    }

    private LivroColetavel Livro(int indice) =>
        gerenciador != null && indice >= 0 && indice < gerenciador.Livros.Count ? gerenciador.Livros[indice] : null;

    private void MostrarVazio()
    {
        if (painelLivro != null) painelLivro.SetActive(false);
        if (painelVazio != null) painelVazio.SetActive(true);
        if (videoPlayer != null) videoPlayer.Stop();
    }

    private void MostrarLivro(int indice)
    {
        var livro = Livro(indice);
        if (livro == null || !livro.Coletado)
        {
            MostrarVazio();
            return;
        }

        if (painelVazio != null) painelVazio.SetActive(false);
        if (painelLivro != null) painelLivro.SetActive(true);
        if (avisoDesafios != null) avisoDesafios.gameObject.SetActive(false);

        var dados = livro.Dados;
        if (textoTitulo != null) textoTitulo.text = livro.Titulo;
        if (textoAutor != null)
        {
            textoAutor.text = livro.Autor;
            textoAutor.gameObject.SetActive(!string.IsNullOrWhiteSpace(livro.Autor));
        }
        if (textoIntroducao != null)
            textoIntroducao.text = dados != null && !string.IsNullOrWhiteSpace(dados.Resumo) ? dados.Resumo : "Introdução ainda não cadastrada.";
        if (imagemCapa != null)
        {
            imagemCapa.sprite = dados != null ? dados.Capa : null;
            imagemCapa.gameObject.SetActive(imagemCapa.sprite != null);
        }
        TocarVideo(dados != null ? dados.Video : null);
    }

    private void TocarVideo(VideoClip clipe)
    {
        if (videoPlayer == null || telaVideo == null) return;

        telaVideo.gameObject.SetActive(clipe != null);
        if (clipe == null)
        {
            videoPlayer.Stop();
            return;
        }
        if (videoPlayer.clip == clipe && videoPlayer.isPlaying) return;

        if (texturaVideo == null)
        {
            texturaVideo = new RenderTexture(640, 360, 0) { name = "Video do menu do hub" };
            texturaVideo.Create();
        }
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = clipe;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = texturaVideo;
        telaVideo.texture = texturaVideo;
        videoPlayer.Play();
    }

    private void PintarBotoes()
    {
        for (int i = 0; i < entradas.Length; i++)
        {
            var botao = entradas[i]?.botao;
            if (botao == null || botao.targetGraphic == null) continue;
            botao.targetGraphic.color = i == escolhido ? corEscolhido : botao == botaoNaMira && botao.interactable ? corNaMira : corBotao;
        }
    }

    // ------------------------------------------------------------------
    // PC: mirar com o centro da tela
    // ------------------------------------------------------------------

    /// <summary>Clica no botão que está no centro da tela (mira do PC). Retorna falso se não havia botão na mira.</summary>
    public bool ClicarNaMira(Camera cam, float alcance)
    {
        var botao = BotaoNaMira(cam, alcance);
        if (botao == null) return false;
        botao.onClick.Invoke();
        return true;
    }

    /// <summary>Destaca o botão que está na mira (chamado a cada quadro pelo DesktopInteracao).</summary>
    public void AtualizarMira(Camera cam, float alcance)
    {
        var botao = cam != null ? BotaoNaMira(cam, alcance) : null;
        if (botao == botaoNaMira) return;
        botaoNaMira = botao;
        PintarBotoes();
    }

    /// <summary>Verdadeiro se há um botão do menu na mira (para a dica na tela).</summary>
    public bool TemBotaoNaMira => botaoNaMira != null;

    private Button BotaoNaMira(Camera cam, float alcance)
    {
        if (cam == null || !isActiveAndEnabled) return null;

        var centro = new Vector2(cam.pixelWidth * 0.5f, cam.pixelHeight * 0.5f);
        var raio = cam.ScreenPointToRay(centro);
        var plano = new Plane(transform.forward, transform.position);
        if (!plano.Raycast(raio, out float distancia) || distancia > alcance) return null;

        foreach (var botao in botoes)
        {
            if (botao == null || !botao.isActiveAndEnabled || !botao.interactable) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)botao.transform, centro, cam)) return botao;
        }
        return null;
    }
}
