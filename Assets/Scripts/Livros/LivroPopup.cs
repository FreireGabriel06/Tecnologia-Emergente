using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XRI 3.x. No XRI 2.x quem faz isto compilar é o Compat/XRICompat.cs

/// <summary>
/// Controla o popup (Canvas em World Space) de um livro da exposição.
///
/// Como usar:
/// 1. Selecione o livro na Hierarchy e use o menu Sebraelab > Livros > Configurar livro selecionado.
///    Isso adiciona Collider, Rigidbody, XR Grab Interactable, este script e cria o popup já ligado.
/// 2. (Opcional) Crie um asset em Assets > Create > Sebraelab > Dados do Livro e arraste para "Dados".
///    Sem ele, o popup mostra o texto que você escrever direto nos textos do Canvas.
///
/// O popup abre quando o jogador aproxima a mão (Hover) e/ou pega o livro (Select)
/// e fecha quando ele afasta a mão ou solta o livro.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Sebraelab/Livro Popup")]
public class LivroPopup : MonoBehaviour
{
    public enum QuandoMostrar
    {
        [InspectorName("Ao segurar o livro (Select)")] AoSegurar,
        [InspectorName("Ao aproximar a mão (Hover)")] AoAproximarAMao,
        [InspectorName("Ao aproximar a mão ou segurar")] AoAproximarOuSegurar,
    }

    [Header("Dados do livro (opcional)")]
    [Tooltip("Asset com título, autor, resumo, capa e vídeo. Se ficar vazio, o popup mostra o que foi escrito direto nos textos do Canvas.")]
    [SerializeField] private LivroData dados;

    [Header("Quando mostrar o popup")]
    [SerializeField] private QuandoMostrar quandoMostrar = QuandoMostrar.AoAproximarOuSegurar;

    [Tooltip("Segundos de espera antes de fechar depois que a mão sai ou solta o livro. Evita que o popup fique piscando.")]
    [SerializeField, Min(0f)] private float atrasoParaFechar = 0.3f;

    [Header("Referências da UI")]
    [Tooltip("Objeto raiz do popup (Canvas em World Space). Ele é desativado ao iniciar o jogo.")]
    [SerializeField] private GameObject canvasPopup;
    [SerializeField] private TMP_Text textoTitulo;
    [SerializeField] private TMP_Text textoAutor;
    [SerializeField] private TMP_Text textoResumo;
    [SerializeField] private Image imagemCapa;

    [Header("Vídeo (GIF convertido para MP4)")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Tooltip("RawImage onde o vídeo aparece.")]
    [SerializeField] private RawImage telaVideo;

    [Tooltip("Objeto escondido quando o livro não tem vídeo. Se vazio, esconde só a RawImage.")]
    [SerializeField] private GameObject areaVideo;

    [Header("Eventos")]
    [SerializeField] private UnityEvent aoAbrirPopup = new UnityEvent();
    [SerializeField] private UnityEvent aoFecharPopup = new UnityEvent();

    [Tooltip("Chamado por IniciarMinigame(). Ligue aqui o que deve acontecer quando o minigame do livro começar (fase futura).")]
    [SerializeField] private UnityEvent aoIniciarMinigame = new UnityEvent();

    private XRBaseInteractable interactable;
    private RenderTexture texturaVideo;
    private bool abertoManualmente;
    private float horaDeFechar = -1f;

    public LivroData Dados => dados;
    public bool PopupAberto => canvasPopup != null && canvasPopup.activeSelf;
    public string NomeDoLivro => dados != null && !string.IsNullOrEmpty(dados.Titulo) ? dados.Titulo : gameObject.name;

    private bool UsaHover => quandoMostrar != QuandoMostrar.AoSegurar;
    private bool UsaSelect => quandoMostrar != QuandoMostrar.AoAproximarAMao;

    private bool TemVideo =>
        videoPlayer != null &&
        (videoPlayer.source == VideoSource.VideoClip ? videoPlayer.clip != null : !string.IsNullOrEmpty(videoPlayer.url));

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (interactable == null)
            Debug.LogWarning($"[LivroPopup] '{name}' não tem XR Grab Interactable: o popup só abre por código (MostrarPopup).", this);

        if (canvasPopup == null)
            Debug.LogWarning($"[LivroPopup] '{name}' está sem o Canvas do popup. Use o menu Sebraelab > Livros > Configurar livro selecionado.", this);

        AplicarDados();
        PrepararVideo();

        if (canvasPopup != null) canvasPopup.SetActive(false);
    }

    private void OnEnable()
    {
        // Se o script foi desligado com o popup aberto, confere no próximo Update se ele deve continuar aberto.
        if (PopupAberto) AgendarFechamento();

        if (interactable == null) return;
        interactable.hoverEntered.AddListener(AoAproximarMao);
        interactable.hoverExited.AddListener(AoAfastarMao);
        interactable.selectEntered.AddListener(AoSegurarLivro);
        interactable.selectExited.AddListener(AoSoltarLivro);
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(AoAproximarMao);
            interactable.hoverExited.RemoveListener(AoAfastarMao);
            interactable.selectEntered.RemoveListener(AoSegurarLivro);
            interactable.selectExited.RemoveListener(AoSoltarLivro);
        }

        abertoManualmente = false;
        horaDeFechar = -1f;
    }

    private void Update()
    {
        // O fechamento é feito aqui, com atraso, e não direto no evento de saída:
        // assim o popup não pisca quando a mão passa rápido pela borda do livro
        // e continua aberto se outra mão/raio ainda estiver no livro.
        if (horaDeFechar < 0f || Time.time < horaDeFechar) return;

        horaDeFechar = -1f;
        if (!DeveFicarAberto()) Fechar();
    }

    private void OnDestroy()
    {
        if (texturaVideo == null) return;
        texturaVideo.Release();
        Destroy(texturaVideo);
    }

    // ------------------------------------------------------------------
    // Eventos do XR Interaction Toolkit
    // ------------------------------------------------------------------

    private void AoAproximarMao(HoverEnterEventArgs args)
    {
        if (UsaHover) Abrir();
    }

    private void AoAfastarMao(HoverExitEventArgs args) => AgendarFechamento();

    private void AoSegurarLivro(SelectEnterEventArgs args)
    {
        if (UsaSelect) Abrir();
    }

    private void AoSoltarLivro(SelectExitEventArgs args) => AgendarFechamento();

    // ------------------------------------------------------------------
    // Funções públicas (podem ser chamadas por botões, UnityEvents e outros scripts)
    // ------------------------------------------------------------------

    /// <summary>Abre o popup e mantém aberto até EsconderPopup() ser chamado.</summary>
    public void MostrarPopup()
    {
        abertoManualmente = true;
        Abrir();
    }

    public void EsconderPopup()
    {
        abertoManualmente = false;
        Fechar();
    }

    public void AlternarPopup()
    {
        if (PopupAberto) EsconderPopup();
        else MostrarPopup();
    }

    /// <summary>
    /// Gancho para os minigames (fase futura do GDD). O botão "Iniciar Desafio" do popup chama esta função.
    /// Ligue o que deve acontecer no evento "Ao Iniciar Minigame" no Inspector.
    /// </summary>
    public void IniciarMinigame()
    {
        Debug.Log($"[LivroPopup] Iniciando minigame do livro: {NomeDoLivro}", this);
        aoIniciarMinigame.Invoke();
    }

    /// <summary>Copia os dados do asset LivroData para os textos e imagens do popup.</summary>
    public void AplicarDados()
    {
        if (dados != null)
        {
            if (textoTitulo != null) textoTitulo.text = dados.Titulo;
            if (textoAutor != null) textoAutor.text = dados.Autor;
            if (textoResumo != null) textoResumo.text = dados.Resumo;
            if (imagemCapa != null) imagemCapa.sprite = dados.Capa;

            if (videoPlayer != null && dados.Video != null)
            {
                videoPlayer.source = VideoSource.VideoClip;
                videoPlayer.clip = dados.Video;
            }
        }

        // Esconde o que estiver vazio para não aparecer caixa em branco no popup.
        if (textoAutor != null) textoAutor.gameObject.SetActive(!string.IsNullOrWhiteSpace(textoAutor.text));
        if (imagemCapa != null) imagemCapa.gameObject.SetActive(imagemCapa.sprite != null);
    }

    // ------------------------------------------------------------------
    // Abrir / fechar
    // ------------------------------------------------------------------

    private void Abrir()
    {
        horaDeFechar = -1f;
        if (canvasPopup == null || canvasPopup.activeSelf) return;

        canvasPopup.SetActive(true);

        if (TemVideo && videoPlayer.isActiveAndEnabled)
        {
            LimparTexturaVideo();
            videoPlayer.Play();
        }

        aoAbrirPopup.Invoke();
    }

    private void AgendarFechamento()
    {
        if (PopupAberto) horaDeFechar = Time.time + atrasoParaFechar;
    }

    private bool DeveFicarAberto()
    {
        if (abertoManualmente) return true;
        if (interactable == null) return false;
        return (UsaHover && interactable.isHovered) || (UsaSelect && interactable.isSelected);
    }

    private void Fechar()
    {
        horaDeFechar = -1f;
        if (!PopupAberto) return;

        if (videoPlayer != null) videoPlayer.Stop();
        canvasPopup.SetActive(false);

        aoFecharPopup.Invoke();
    }

    // ------------------------------------------------------------------
    // Vídeo
    // ------------------------------------------------------------------

    private void PrepararVideo()
    {
        var objetoVideo = areaVideo != null ? areaVideo : telaVideo != null ? telaVideo.gameObject : null;

        if (!TemVideo)
        {
            if (objetoVideo != null) objetoVideo.SetActive(false);
            return;
        }

        if (objetoVideo != null) objetoVideo.SetActive(true);

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true; // comporta-se como um GIF

        // Sem RawImage o VideoPlayer usa a configuração que já estiver nele.
        if (telaVideo == null) return;

        int largura = 640;
        int altura = 360;
        if (videoPlayer.source == VideoSource.VideoClip && videoPlayer.clip.width > 0 && videoPlayer.clip.height > 0)
        {
            largura = (int)videoPlayer.clip.width;
            altura = (int)videoPlayer.clip.height;
        }

        // Cada livro tem a própria textura, assim dois popups nunca mostram o mesmo vídeo por engano.
        texturaVideo = new RenderTexture(largura, altura, 0) { name = $"Video_{name}" };
        texturaVideo.Create();
        LimparTexturaVideo();

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = texturaVideo;
        telaVideo.texture = texturaVideo;

        var proporcao = telaVideo.GetComponent<AspectRatioFitter>();
        if (proporcao != null) proporcao.aspectRatio = (float)largura / altura;
    }

    private void LimparTexturaVideo()
    {
        if (texturaVideo == null) return;
        var anterior = RenderTexture.active;
        RenderTexture.active = texturaVideo;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = anterior;
    }
}
