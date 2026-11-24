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
///   o lugar dele no hub e libera a introdução da história no menu (<see cref="HubLivrosMenu"/>).
/// - Quando o último livro é encontrado, dispara "Ao Encontrar Todos".
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

    [Header("Eventos")]
    [SerializeField] private UnityEvent aoEncontrarLivro = new UnityEvent();
    [SerializeField] private UnityEvent aoEncontrarTodos = new UnityEvent();

    /// <summary>Para outros scripts: chamado a cada livro encontrado.</summary>
    public event Action<LivroColetavel> LivroEncontrado;

    private AudioSource fonte;
    private readonly HashSet<LivroColetavel> contados = new HashSet<LivroColetavel>();

    public IReadOnlyList<LivroColetavel> Livros => livros;
    public int Total => livros.Count;
    public int Encontrados => contados.Count;
    public bool TodosEncontrados => Total > 0 && Encontrados >= Total;
    public HubLivrosMenu Menu => menu;

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
            Debug.LogWarning("[Sebraelab] Há mais de um GerenciadorColeta na cena; o último a iniciar é o que vale.", this);
        Instancia = this;

        livros.RemoveAll(l => l == null);
        if (aviso == null) aviso = AvisoColeta.Criar();
        if (somLivroEncontrado == null) somLivroEncontrado = SonsGerados.Coleta();
        if (somTodosEncontrados == null) somTodosEncontrados = SonsGerados.Final();

        fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f; // o som da coleta toca "na cabeça", esteja o jogador onde estiver
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    private IEnumerator Start()
    {
        if (sortearEsconderijos) SortearEsconderijos();
        if (menu != null) menu.Configurar(this);

        if (string.IsNullOrWhiteSpace(mensagemInicial)) yield break;
        yield return new WaitForSeconds(1.2f);
        if (Encontrados == 0) aviso.Mostrar(Formatar(mensagemInicial), dicaInicial, 5f);
    }

    /// <summary>Coloca cada livro num esconderijo diferente, sorteado.</summary>
    public void SortearEsconderijos()
    {
        var pontos = new List<PontoDeLivro>();
        foreach (var ponto in esconderijos) if (ponto != null && ponto.isActiveAndEnabled) pontos.Add(ponto);
        if (pontos.Count == 0)
            foreach (var ponto in FindObjectsByType<PontoDeLivro>(FindObjectsSortMode.None)) if (ponto.isActiveAndEnabled) pontos.Add(ponto);

        var naoEncontrados = livros.FindAll(l => !l.Coletado);
        if (pontos.Count < naoEncontrados.Count)
        {
            Debug.LogWarning($"[Sebraelab] Só há {pontos.Count} esconderijos para {naoEncontrados.Count} livros: os livros ficam onde estão.", this);
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
        }
    }

    /// <summary>Chamado pelo <see cref="LivroColetavel"/> quando o jogador pega o livro.</summary>
    public void Registrar(LivroColetavel livro)
    {
        if (livro == null || !contados.Add(livro)) return;
        if (!livros.Contains(livro)) livros.Add(livro); // um livro colocado depois na cena também conta

        bool ultimo = TodosEncontrados;
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
            var marca = lugar.Find("Marca");
            if (marca != null) marca.gameObject.SetActive(false);
        }
        else
        {
            livro.ColocarNoHub(transform.position + Vector3.up * 0.15f + transform.right * (0.25f * (indice - 1)), transform.rotation);
        }

        yield return livro.Escalar(0f, 1f, 0.35f, escala);
        if (interactable != null) interactable.enabled = true;
    }

    private IEnumerator TodosOsLivros()
    {
        yield return new WaitForSeconds(4.5f);
        aviso.Mostrar("Você encontrou todos os livros!", "Volte ao hub para ler a introdução de cada história.", 6f);
        aoEncontrarTodos.Invoke();
    }

    private string Formatar(string texto)
    {
        try { return string.Format(texto, Total); }
        catch (FormatException) { return texto; }
    }
}
