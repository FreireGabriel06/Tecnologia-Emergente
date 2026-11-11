using UnityEngine;

/// <summary>
/// Interação com os livros no teste pelo PC: mire num livro e aperte E (ou clique) para abrir/fechar o popup.
/// Vai no mesmo objeto do DesktopPlayerController (o menu Sebraelab > Jogador já adiciona).
/// </summary>
[AddComponentMenu("Sebraelab/Desktop Interacao")]
public class DesktopInteracao : MonoBehaviour
{
    [Tooltip("Câmera do jogador. Se vazio, usa a primeira câmera encontrada nos filhos.")]
    [SerializeField] private Camera cameraJogador;

    [Tooltip("Distância máxima (metros) para alcançar um livro.")]
    [SerializeField, Min(0.1f)] private float alcance = 4f;

    [SerializeField] private LayerMask camadas = ~0;

    [Tooltip("Mostra um ponto no centro da tela para mirar.")]
    [SerializeField] private bool mostrarMira = true;

    private LivroPopup livroAberto;

    private void Awake()
    {
        if (cameraJogador == null) cameraJogador = GetComponentInChildren<Camera>(true);
    }

    private void Update()
    {
        if (cameraJogador == null || Cursor.lockState != CursorLockMode.Locked) return;
        if (!EntradaDesktop.InteragirPressionado()) return;

        var origem = cameraJogador.transform;
        LivroPopup livro = null;
        if (Physics.Raycast(origem.position, origem.forward, out RaycastHit acerto, alcance, camadas, QueryTriggerInteraction.Ignore))
            livro = acerto.collider.GetComponentInParent<LivroPopup>();

        // Só um popup aberto por vez.
        if (livroAberto != null && livroAberto != livro) livroAberto.EsconderPopup();

        if (livro != null)
        {
            livro.AlternarPopup();
            livroAberto = livro.PopupAberto ? livro : null;
        }
        else
        {
            livroAberto = null;
        }
    }

    private void OnGUI()
    {
        if (!mostrarMira || Cursor.lockState != CursorLockMode.Locked) return;

        const float tamanho = 6f;
        GUI.Box(new Rect((Screen.width - tamanho) / 2f, (Screen.height - tamanho) / 2f, tamanho, tamanho), GUIContent.none);
    }
}
