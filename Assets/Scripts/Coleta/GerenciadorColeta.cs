using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Hub central da coleta de livros (estilo Slender: "1 de 3", "2 de 3"...). Fica no objeto do hub.
///
/// - No começo da partida, sorteia em quais esconderijos (<see cref="PontoDeLivro"/>) os livros aparecem.
/// - Quando um livro é pego, não importa como (mão, raio do controle, tecla E no PC), o <see cref="LivroColetavel"/>
///   avisa este gerenciador. Ele conta o livro, mostra o aviso na frente do jogador, toca o som, leva o livro para
///   o lugar dele no hub (com um brilho) e libera a introdução da história no menu (<see cref="HubLivrosMenu"/>).
/// - Ajuda o jogador: um detector toca um "ping" que acelera perto de um livro escondido, e depois de um tempo sem
///   achar nada o menu do hub passa a dar dicas de onde procurar.
/// - Quando o último livro é encontrado: comemoração, tempo total e o evento "Ao Encontrar Todos".
/// </summary>
[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
[AddComponentMenu("Sebraelab/Gerenciador Coleta (Hub)")]
public class GerenciadorColeta : MonoBehaviour
{
    public static GerenciadorColeta Instancia { get; private set; }

    [Tooltip("Livros que o jogador precisa encontrar (a princípio, 3).")]
    [SerializeField] private List<LivroColetavel> livros = new List<LivroColetavel>();

    [Tooltip("Lugar de cada livro no hub depois de encontrado (mesma ordem da lista de livros).")]
    [SerializeField] private List<Transform> lugaresNoHub = new List<Transform>();

    [Header("Esconderijos")]
    [Tooltip("Sorteia em quais esconderijos os livros aparecem a cada partida. Desligado: os livros ficam onde estão na cena.")]
    [SerializeField] private bool sortearEsconderijos = true;

    [Tooltip("Esconderijos possíveis. Vazio = todos os Esconderijos de Livro da cena.")]
    [SerializeField] private List<PontoDeLivro> esconderijos = new List<PontoDeLivro>();

    [Header("Hub")]
    [SerializeField] private HubLivrosMenu menu;

    [Tooltip("Aviso na frente do jogador. Se vazio, é criado sozinho.")]
    [SerializeField] private AvisoColeta aviso;

    [Tooltip("Som de livro encontrado. Vazio = som gerado.")]
    [SerializeField] private AudioClip somLivroEncontrado;

    [Tooltip("Som do último livro. Vazio = som gerado.")]
    [SerializeField] private AudioClip somTodosEncontrados;

    [Tooltip("Mensagem no começo da partida ({0} = quantidade de livros).")]
    [SerializeField] private string mensagemInicial = "Encontre os {0} livros";

    [SerializeField] private string dicaInicial = "Eles estão escondidos pelo Sebraelab.";

    [Header("Efeitos")]
    [Tooltip("Brilho tocado no lugar do hub quando um livro chega.")]
    [SerializeField] private ParticleSystem efeitoChegada;

    [Tooltip("Comemoração quando todos os livros são encontrados.")]
    [SerializeField] private ParticleSystem efeitoComemoracao;

    [Tooltip("Objetos ligados quando todos os livros são encontrados (luzes, letreiros...).")]
    [SerializeField] private List<GameObject> ligarNoFinal = new List<GameObject>();

    [Header("Ajuda ao jogador")]
    [Tooltip("Segundos sem achar livro até o menu do hub começar a dar dicas de onde procurar (0 = nunca).")]
    [SerializeField, Min(0f)] private float segundosParaDica = 75f;

    [Tooltip("Detector de livros: um \"ping\" baixinho que fica mais rápido e mais agudo perto de um livro escondido.")]
    [SerializeField] private bool detectorDeLivros = true;

    [SerializeField, Range(0f, 1f)] private float volumeDetector = 0.16f;

    [Header("Som ambiente")]
    [SerializeField] private bool musicaAmbiente = true;
    [SerializeField, Range(0f, 1f)] private float volumeMusica = 0.1f;

    [Tooltip("Música própria (opcional). Vazio = uma música calma gerada por código.")]
    [SerializeField] private AudioClip musica;

    [Header("Eventos")]
    [SerializeField] private UnityEvent aoEncontrarLivro = new UnityEvent();
    [SerializeField] private UnityEvent aoEncontrarTodos = new UnityEvent();

    /// <summary>Para outros scripts: chamado a cada livro encontrado.</summary>
    public event Action<LivroColetavel> LivroEncontrado;

    private AudioSource fonte;
    private AudioSource fonteDetector;
    private AudioClip ping;
    private readonly HashSet<LivroColetavel> contados = new HashSet<LivroColetavel>();
    private readonly Dictionary<LivroColetavel, PontoDeLivro> esconderijoDoLivro = new Dictionary<LivroColetavel, PontoDeLivro>();
    private float inicioDaPartida;
    private float fimDaPartida = -1f;
    private float ultimaDescoberta;
    private float proximoPing;

    public IReadOnlyList<LivroColetavel> Livros => livros;
    public int Total => livros.Count;
    public int Encontrados => contados.Count;
    public bool TodosEncontrados => Total > 0 && Encontrados >= Total;
    public HubLivrosMenu Menu => menu;

    /// <summary>Segundos desde o começo da partida (para de contar quando o último livro é encontrado).</summary>
    public float TempoDePartida => (fimDaPartida >= 0f ? fimDaPartida : Time.time) - inicioDaPartida;

    /// <summary>Verdadeiro quando o jogador está há um tempo sem achar livro: o menu do hub mostra dicas.</summary>
    public bool DicasLiberadas => segundosParaDica > 0f && !TodosEncontrados && Time.time - ultimaDescoberta >= segundosParaDica;

    /// <summary>Onde o livro está escondido, em palavras (ex.: "perto da bancada da cozinha").</summary>
    public string DicaDe(LivroColetavel livro) =>
        livro != null && esconderijoDoLivro.TryGetValue(livro, out var ponto) && ponto != null ? ponto.Dica : string.Empty;

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
            Debug.LogWarning("[Sebraelab] Há mais de um GerenciadorColeta na cena; o último a iniciar é o que vale.", this);
        Instancia = this;

        livros.RemoveAll(l => l == null);
        if (aviso == null) aviso = AvisoColeta.Criar();
        if (somLivroEncontrado == null) somLivroEncontrado = SonsGerados.Coleta();
        if (somTodosEncontrados == null) somTodosEncontrados = SonsGerados.Final();

        fonte = NovaFonte2D();
        fonteDetector = NovaFonte2D();
        ping = SonsGerados.Ping();

        if (musicaAmbiente)
        {
            var fonteMusica = NovaFonte2D();
            fonteMusica.clip = musica != null ? musica : SonsGerados.Ambiente();
            fonteMusica.loop = true;
            fonteMusica.volume = volumeMusica;
            fonteMusica.Play();
        }
    }

    private AudioSource NovaFonte2D()
    {
        var nova = gameObject.AddComponent<AudioSource>();
        nova.playOnAwake = false;
        nova.spatialBlend = 0f; // toca "na cabeça", esteja o jogador onde estiver
        return nova;
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    private IEnumerator Start()
    {
        inicioDaPartida = ultimaDescoberta = Time.time;
        if (sortearEsconderijos) SortearEsconderijos();
        else LembrarEsconderijos();
        if (menu != null) menu.Configurar(this);

        if (string.IsNullOrWhiteSpace(mensagemInicial)) yield break;
        yield return new WaitForSeconds(1.2f);
        if (Encontrados == 0) aviso.Mostrar(Formatar(mensagemInicial), dicaInicial, 5f);
    }

    private void Update()
    {
        if (!detectorDeLivros || TodosEncontrados || Time.time < proximoPing) return;

        var ouvido = CameraAtiva();
        if (ouvido == null) return;

        float maisPerto = float.MaxValue;
        foreach (var livro in livros)
            if (livro != null && !livro.Coletado)
                maisPerto = Mathf.Min(maisPerto, Vector3.Distance(livro.transform.position, ouvido.transform.position));
        if (maisPerto == float.MaxValue) return;

        // 0 = longe (15 m ou mais), 1 = do lado do livro
        float perto = Mathf.InverseLerp(15f, 1.5f, maisPerto);
        fonteDetector.pitch = Mathf.Lerp(0.85f, 1.55f, perto);
        fonteDetector.PlayOneShot(ping, volumeDetector * Mathf.Lerp(0.3f, 1f, perto));
        proximoPing = Time.time + Mathf.Lerp(2.6f, 0.35f, perto);
    }

    /// <summary>Coloca cada livro num esconderijo diferente, sorteado.</summary>
    public void SortearEsconderijos()
    {
        var pontos = Esconderijos();
        var naoEncontrados = livros.FindAll(l => !l.Coletado);
        if (pontos.Count < naoEncontrados.Count)
        {
            Debug.LogWarning($"[Sebraelab] Só há {pontos.Count} esconderijos para {naoEncontrados.Count} livros: os livros ficam onde estão.", this);
            LembrarEsconderijos();
            return;
        }

        for (int i = pontos.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (pontos[i], pontos[j]) = (pontos[j], pontos[i]);
        }

        for (int i = 0; i < naoEncontrados.Count; i++)
        {
            var giro = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            naoEncontrados[i].DefinirPosicao(pontos[i].PosicaoDoLivro, giro * pontos[i].RotacaoDoLivro);
            esconderijoDoLivro[naoEncontrados[i]] = pontos[i];
        }
    }

    /// <summary>Sem sorteio: cada livro fica associado ao esconderijo mais perto dele (para as dicas).</summary>
    private void LembrarEsconderijos()
    {
        var pontos = Esconderijos();
        foreach (var livro in livros)
        {
            PontoDeLivro melhor = null;
            float menor = 3f; // mais longe que isso, o livro não está num esconderijo
            foreach (var ponto in pontos)
            {
                float distancia = Vector3.Distance(ponto.PosicaoDoLivro, livro.transform.position);
                if (distancia >= menor) continue;
                menor = distancia;
                melhor = ponto;
            }
            if (melhor != null) esconderijoDoLivro[livro] = melhor;
        }
    }

    private List<PontoDeLivro> Esconderijos()
    {
        var pontos = new List<PontoDeLivro>();
        foreach (var ponto in esconderijos) if (ponto != null && ponto.isActiveAndEnabled) pontos.Add(ponto);
        if (pontos.Count == 0)
            foreach (var ponto in FindObjectsByType<PontoDeLivro>(FindObjectsSortMode.None)) if (ponto.isActiveAndEnabled) pontos.Add(ponto);
        return pontos;
    }

    /// <summary>Chamado pelo <see cref="LivroColetavel"/> quando o jogador pega o livro.</summary>
    public void Registrar(LivroColetavel livro)
    {
        if (livro == null || !contados.Add(livro)) return;
        if (!livros.Contains(livro)) livros.Add(livro); // um livro colocado depois na cena também conta

        ultimaDescoberta = Time.time;
        bool ultimo = TodosEncontrados;
        if (ultimo) fimDaPartida = Time.time;

        fonte.PlayOneShot(ultimo ? somTodosEncontrados : somLivroEncontrado);
        aviso.Mostrar($"{Encontrados} de {Total} livros", $"Você encontrou: {livro.Titulo}", 4f);

        StartCoroutine(LevarParaOHub(livro, livros.IndexOf(livro)));
        if (menu != null) menu.Atualizar();

        LivroEncontrado?.Invoke(livro);
        aoEncontrarLivro.Invoke();

        if (ultimo) StartCoroutine(TodosOsLivros());
    }

    /// <summary>Mostra no menu do hub a introdução deste livro (se ele já foi encontrado).</summary>
    public void AbrirNoMenu(LivroColetavel livro)
    {
        if (menu != null && livro != null && livro.Coletado) menu.Selecionar(livros.IndexOf(livro));
    }

    private IEnumerator LevarParaOHub(LivroColetavel livro, int indice)
    {
        // Deixa o livro um instante na mão (ou no raio) antes de guardá-lo.
        yield return new WaitForSeconds(livro.Seguro ? 0.45f : 0.15f);

        // Desligar o interactable solta o livro de qualquer mão ou raio que esteja segurando.
        var interactable = livro.Interactable;
        if (interactable != null) interactable.enabled = false;
        livro.Parar();

        var escala = livro.transform.localScale;
        yield return livro.Escalar(1f, 0f, 0.25f, escala);

        var lugar = indice >= 0 && indice < lugaresNoHub.Count ? lugaresNoHub[indice] : null;
        if (lugar != null)
        {
            livro.ColocarNoHub(lugar.position, lugar.rotation);
            // Some o que marcava o lugar vazio ("?" e afins).
            foreach (Transform filho in lugar)
                if (filho.name == "Marca" || filho.name == "Vazio") filho.gameObject.SetActive(false);
        }
        else
        {
            livro.ColocarNoHub(transform.position + Vector3.up * 0.15f + transform.right * (0.25f * (indice - 1)), transform.rotation);
        }

        if (efeitoChegada != null)
        {
            efeitoChegada.transform.position = livro.transform.position;
            efeitoChegada.Play(true);
        }

        yield return livro.Escalar(0f, 1f, 0.35f, escala);
        if (interactable != null) interactable.enabled = true;
    }

    private IEnumerator TodosOsLivros()
    {
        yield return new WaitForSeconds(4.5f);
        int segundos = Mathf.RoundToInt(TempoDePartida);
        aviso.Mostrar($"Você encontrou os {Total} livros!", $"Tempo: {segundos / 60}:{segundos % 60:00}. Leia a introdução de cada um no hub.", 7f);
        if (efeitoComemoracao != null) efeitoComemoracao.Play(true);
        foreach (var objeto in ligarNoFinal) if (objeto != null) objeto.SetActive(true);
        if (menu != null) menu.Atualizar();
        aoEncontrarTodos.Invoke();
    }

    private string Formatar(string texto)
    {
        try { return string.Format(texto, Total); }
        catch (FormatException) { return texto; }
    }

    private static Camera CameraAtiva()
    {
        var principal = Camera.main;
        if (principal != null && principal.isActiveAndEnabled) return principal;
        foreach (var cam in Camera.allCameras) if (cam.isActiveAndEnabled && cam.targetTexture == null) return cam;
        return null;
    }
}
