using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XRI 3.x. No XRI 2.x quem faz isto compilar é o XRICompat.cs

/// <summary>
/// Livro escondido pelo Sebraelab, no estilo das páginas do Slender: flutua, gira, brilha e faz um som baixinho para
/// ser achado. Conta como encontrado quando o jogador pega o livro DE QUALQUER JEITO:
/// - em VR, com a mão (grab direto) ou com o raio do controle (qualquer interactor do XR Interaction Toolkit);
/// - no PC, mirando e apertando E ou clicando (DesktopInteracao);
/// - por código, chamando <see cref="Coletar"/>.
/// Depois disso quem manda é o hub central (<see cref="GerenciadorColeta"/>): ele conta "1 de 3", leva o livro para o
/// hub e libera a introdução da história no menu. No hub, pegar o livro de novo abre a introdução dele no menu.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Sebraelab/Livro Coletavel")]
public class LivroColetavel : MonoBehaviour
{
    [Tooltip("Título, autor, introdução da história, capa e vídeo (asset \"Dados do Livro\").")]
    [SerializeField] private LivroData dados;

    [Tooltip("Cor do livro, usada na lista do menu do hub.")]
    [SerializeField] private Color cor = new Color(0.12f, 0.24f, 0.53f);

    [Header("Enquanto está escondido")]
    [Tooltip("Graus por segundo.")]
    [SerializeField] private float velocidadeGiro = 35f;

    [Tooltip("Quanto o livro sobe e desce (metros).")]
    [SerializeField, Min(0f)] private float flutuacao = 0.04f;

    [Tooltip("Luz que pulsa para o livro ser visto de longe (opcional).")]
    [SerializeField] private Light brilho;

    [Tooltip("Som baixinho em loop perto do livro (opcional). Sem clipe, um som é gerado na hora.")]
    [SerializeField] private AudioSource somProximidade;

    [Header("No hub")]
    [Tooltip("Graus por segundo depois de encontrado, em exposição no hub.")]
    [SerializeField] private float velocidadeGiroNoHub = 12f;

    [Header("Eventos")]
    [SerializeField] private UnityEvent aoSerEncontrado = new UnityEvent();

    private XRBaseInteractable interactable;
    private Vector3 posicaoBase;
    private Quaternion rotacaoBase;
    private float angulo;
    private float fase;
    private float intensidadeBrilho = 1f;
    private bool noHub;
    private bool parado;
    private bool posicaoDefinida;

    public LivroData Dados => dados;
    public Color Cor => cor;
    public bool Coletado { get; private set; }
    public string Titulo => dados != null && !string.IsNullOrWhiteSpace(dados.Titulo) ? dados.Titulo : name;
    public string Autor => dados != null ? dados.Autor : string.Empty;

    /// <summary>Componente do XR Interaction Toolkit que recebe o "pegar" (Grab ou Simple Interactable).</summary>
    public XRBaseInteractable Interactable => interactable;

    /// <summary>Verdadeiro enquanto alguma mão ou raio está segurando o livro.</summary>
    public bool Seguro => interactable != null && interactable.isSelected;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        fase = Random.value * 10f;
        if (brilho != null) intensidadeBrilho = brilho.intensity;
        if (!posicaoDefinida) DefinirPosicao(transform.position, transform.rotation);
    }

    private void OnEnable()
    {
        if (interactable != null) interactable.selectEntered.AddListener(AoSerPego);
    }

    private void OnDisable()
    {
        if (interactable != null) interactable.selectEntered.RemoveListener(AoSerPego);
    }

    private void Start()
    {
        if (somProximidade != null && !Coletado)
        {
            if (somProximidade.clip == null) somProximidade.clip = SonsGerados.Brilho();
            somProximidade.loop = true;
            somProximidade.spatialBlend = 1f;
            if (!somProximidade.isPlaying) somProximidade.Play();
        }
    }

    /// <summary>Lugar onde o livro fica (o esconderijo, ou o lugar dele no hub). O livro flutua em volta deste ponto.</summary>
    public void DefinirPosicao(Vector3 posicao, Quaternion rotacao)
    {
        posicaoBase = posicao;
        rotacaoBase = Quaternion.Euler(0f, rotacao.eulerAngles.y, 0f);
        angulo = 0f;
        posicaoDefinida = true;
        transform.SetPositionAndRotation(posicao, rotacaoBase);
    }

    private void AoSerPego(SelectEnterEventArgs args)
    {
        if (!Coletado) Coletar();
        else if (GerenciadorColeta.Instancia != null) GerenciadorColeta.Instancia.AbrirNoMenu(this);
    }

    /// <summary>Marca o livro como encontrado. Todo jeito de pegar o livro passa por aqui.</summary>
    public void Coletar()
    {
        if (Coletado) return;
        Coletado = true;

        if (somProximidade != null) somProximidade.Stop();
        if (brilho != null) brilho.enabled = false;
        aoSerEncontrado.Invoke();

        if (GerenciadorColeta.Instancia != null) GerenciadorColeta.Instancia.Registrar(this);
        else Debug.LogWarning($"[Sebraelab] O livro '{Titulo}' foi encontrado, mas a cena não tem um GerenciadorColeta (hub).", this);
    }

    /// <summary>Chamado pelo hub: o livro passa a ficar em exposição neste lugar.</summary>
    internal void ColocarNoHub(Vector3 posicao, Quaternion rotacao)
    {
        noHub = true;
        parado = false;
        DefinirPosicao(posicao, rotacao);
    }

    /// <summary>Para o livro onde ele está (o hub usa antes de guardá-lo, para ele sumir ali mesmo).</summary>
    internal void Parar() => parado = true;

    private void Update()
    {
        if (Seguro || parado) return; // na mão do jogador quem move é o XR Interaction Toolkit

        float t = Time.time + fase;
        angulo += (noHub ? velocidadeGiroNoHub : velocidadeGiro) * Time.deltaTime;
        float altura = noHub ? 0f : flutuacao * (0.5f + 0.5f * Mathf.Sin(t * 1.7f));
        var alvo = posicaoBase + Vector3.up * altura;
        var rotacaoAlvo = Quaternion.Euler(0f, angulo, 0f) * rotacaoBase;

        // Volta suavemente para o lugar (por exemplo, depois de ser solto no hub).
        float k = 1f - Mathf.Exp(-10f * Time.deltaTime);
        transform.SetPositionAndRotation(Vector3.Lerp(transform.position, alvo, k), Quaternion.Slerp(transform.rotation, rotacaoAlvo, k));

        if (brilho != null && brilho.enabled) brilho.intensity = intensidadeBrilho * (0.55f + 0.45f * Mathf.Sin(t * 2.6f));
    }

    /// <summary>Animação de "sumir" e "aparecer" usada pelo hub ao recolher o livro.</summary>
    internal IEnumerator Escalar(float de, float para, float duracao, Vector3 escalaNormal)
    {
        for (float tempo = 0f; tempo < duracao; tempo += Time.deltaTime)
        {
            float p = Mathf.Clamp01(tempo / duracao);
            // um pequeno "pulo" no fim, como um item sendo guardado
            float curva = para > de ? 1f + 0.12f * Mathf.Sin(p * Mathf.PI) : 1f;
            transform.localScale = escalaNormal * (Mathf.Lerp(de, para, p * p * (3f - 2f * p)) * curva);
            yield return null;
        }
        transform.localScale = escalaNormal * para;
    }
}
