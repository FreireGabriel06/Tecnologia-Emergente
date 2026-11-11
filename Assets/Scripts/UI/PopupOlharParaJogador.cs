using UnityEngine;

/// <summary>
/// Coloque no Canvas do popup. Faz duas coisas:
/// 1. Mantém o popup acima do livro, mas sem girar junto quando o livro é girado na mão.
/// 2. Vira o popup sempre de frente para o jogador, para o texto ficar legível.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Sebraelab/Popup Olhar Para Jogador")]
public class PopupOlharParaJogador : MonoBehaviour
{
    [Tooltip("Objeto que o popup acompanha (normalmente o livro). Se vazio, usa o objeto pai.")]
    [SerializeField] private Transform alvo;

    [SerializeField] private bool seguirAlvo = true;

    [Tooltip("Posição do popup em relação ao alvo, em metros, no espaço do mundo (Y = para cima).")]
    [SerializeField] private Vector3 deslocamento = new Vector3(0f, 0.25f, 0f);

    [Tooltip("Gira só no eixo vertical, assim o popup fica sempre em pé.")]
    [SerializeField] private bool somenteEixoVertical = true;

    private Transform cameraJogador;

    private void Awake()
    {
        if (alvo == null && transform.parent != null) alvo = transform.parent;
    }

    // Posiciona já no frame em que o popup abre, para ele não aparecer torto por um instante.
    private void OnEnable() => Atualizar();

    private void LateUpdate() => Atualizar();

    private void Atualizar()
    {
        if (seguirAlvo && alvo != null) transform.position = alvo.position + deslocamento;

        if (cameraJogador == null || !cameraJogador.gameObject.activeInHierarchy)
        {
            var cam = Camera.main;
            if (cam == null) return;
            cameraJogador = cam.transform;
        }

        Vector3 direcao = transform.position - cameraJogador.position;
        if (somenteEixoVertical) direcao.y = 0f;
        if (direcao.sqrMagnitude < 0.0001f) return;

        // A frente de um Canvas é vista olhando na direção +Z dele, por isso o popup "olha" para longe da câmera.
        transform.rotation = Quaternion.LookRotation(direcao.normalized, Vector3.up);
    }
}
