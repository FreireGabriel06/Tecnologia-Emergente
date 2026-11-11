using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Área de gatilho de um estande de livros (GDD 3.2): quando a cabeça do jogador entra na área,
/// liga os objetos de destaque (luzes, setas, placas) e toca um som. Desliga ao sair.
///
/// Como usar: crie um objeto vazio na frente do estande, adicione um Box Collider do tamanho da área
/// e adicione este script (se não tiver Collider, ele cria um Box Collider). O Collider vira "Is Trigger"
/// sozinho, então não bloqueia o jogador.
/// Funciona com o XR Origin e com o jogador de teste do PC (usa a posição da câmera principal).
/// </summary>
[AddComponentMenu("Sebraelab/Estande Zona Destaque")]
public class EstandeZonaDestaque : MonoBehaviour
{
    [Tooltip("Objetos ligados enquanto o jogador está na área (ex.: Spot Light em cima do estande). Começam desligados.")]
    [SerializeField] private GameObject[] objetosDestaque = new GameObject[0];

    [Tooltip("Som tocado quando o jogador entra (opcional).")]
    [SerializeField] private AudioSource somAoEntrar;

    [SerializeField] private bool pararSomAoSair = true;

    [SerializeField] private UnityEvent aoEntrar = new UnityEvent();
    [SerializeField] private UnityEvent aoSair = new UnityEvent();

    [Tooltip("De quanto em quanto tempo (segundos) verifica se o jogador está na área.")]
    [SerializeField, Min(0.02f)] private float intervaloVerificacao = 0.1f;

    private Collider area;
    private Transform cabecaJogador;
    private bool jogadorDentro;
    private bool sincronizarDestaque;
    private float proximaVerificacao;

    public bool JogadorDentro => jogadorDentro;

    private void Reset()
    {
        var colisor = GetComponent<Collider>();
        if (colisor == null) colisor = gameObject.AddComponent<BoxCollider>();
        colisor.isTrigger = true;
    }

    private void Awake()
    {
        area = GetComponent<Collider>();
        if (area == null)
        {
            Debug.LogWarning($"[EstandeZonaDestaque] '{name}' precisa de um Box Collider marcando a área do estande.", this);
            enabled = false;
            return;
        }

        area.isTrigger = true;
        LigarDestaque(false);
    }

    private void Update()
    {
        if (sincronizarDestaque)
        {
            sincronizarDestaque = false;
            LigarDestaque(jogadorDentro);
        }

        if (Time.time < proximaVerificacao) return;
        proximaVerificacao = Time.time + intervaloVerificacao;

        if (cabecaJogador == null || !cabecaJogador.gameObject.activeInHierarchy)
        {
            var cam = Camera.main;
            cabecaJogador = cam != null ? cam.transform : null;
        }

        bool dentro = cabecaJogador != null && EstaDentroDaArea(cabecaJogador.position);
        if (dentro == jogadorDentro) return;

        jogadorDentro = dentro;
        LigarDestaque(dentro);

        if (dentro)
        {
            if (somAoEntrar != null) somAoEntrar.Play();
            aoEntrar.Invoke();
        }
        else
        {
            if (somAoEntrar != null && pararSomAoSair) somAoEntrar.Stop();
            aoSair.Invoke();
        }
    }

    private void OnDisable()
    {
        if (jogadorDentro) aoSair.Invoke();
        jogadorDentro = false;

        // Objetos filhos não podem ser ligados/desligados enquanto este objeto está sendo desativado;
        // eles são acertados no primeiro Update depois que o script voltar a ficar ativo.
        foreach (var objeto in objetosDestaque)
            if (objeto != null && !objeto.transform.IsChildOf(transform)) objeto.SetActive(false);
        sincronizarDestaque = true;
    }

    // ClosestPoint devolve o próprio ponto quando ele está dentro do Collider.
    // Funciona com Box, Sphere, Capsule e Mesh Collider convexo.
    private bool EstaDentroDaArea(Vector3 ponto) =>
        area != null && area.enabled && (area.ClosestPoint(ponto) - ponto).sqrMagnitude < 0.0001f;

    private void LigarDestaque(bool ligar)
    {
        foreach (var objeto in objetosDestaque)
            if (objeto != null) objeto.SetActive(ligar);
    }
}
