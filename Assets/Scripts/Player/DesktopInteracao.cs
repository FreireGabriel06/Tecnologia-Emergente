using UnityEngine;

/// <summary>
/// Interação no teste pelo PC (sem óculos), sempre pela mira no centro da tela + tecla E (ou clique):
/// - livro escondido: pega o livro (conta na coleta, igual a pegar com a mão em VR);
/// - livro já encontrado, no hub: abre a introdução dele no menu;
/// - botão do menu do hub: clica no botão;
/// - livro com popup (modelo antigo, LivroPopup): abre/fecha o popup.
/// Vai no mesmo objeto do DesktopPlayerController (o menu Sebraelab > Jogador já adiciona).
/// </summary>
[AddComponentMenu("Sebraelab/Desktop Interacao")]
public class DesktopInteracao : MonoBehaviour
{
    [Tooltip("Câmera do jogador. Se vazio, usa a primeira câmera encontrada nos filhos.")]
    [SerializeField] private Camera cameraJogador;

    [Tooltip("Distância máxima (metros) para pegar um livro.")]
    [SerializeField, Min(0.1f)] private float alcance = 3.5f;

    [Tooltip("Distância máxima (metros) para usar os botões do menu do hub.")]
    [SerializeField, Min(0.1f)] private float alcanceMenu = 6f;

    [SerializeField] private LayerMask camadas = ~0;

    [Tooltip("Mostra um ponto no centro da tela para mirar e uma dica do que a tecla E faz.")]
    [SerializeField] private bool mostrarMira = true;

    private HubLivrosMenu[] menus = new HubLivrosMenu[0];
    private LivroPopup livroAberto;
    private string dica;

    private void Awake()
    {
        if (cameraJogador == null) cameraJogador = GetComponentInChildren<Camera>(true);
    }

    private void Start()
    {
        menus = FindObjectsByType<HubLivrosMenu>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }

    private void Update()
    {
        bool mirando = cameraJogador != null && cameraJogador.isActiveAndEnabled && Cursor.lockState == CursorLockMode.Locked;
        dica = null;

        // Botões do menu do hub na mira.
        HubLivrosMenu menuNaMira = null;
        foreach (var menu in menus)
        {
            if (menu == null) continue;
            menu.AtualizarMira(mirando ? cameraJogador : null, alcanceMenu);
            if (menu.TemBotaoNaMira) menuNaMira = menu;
        }
        if (!mirando) return;

        // Livro na mira.
        Component alvo = null;
        var olhos = cameraJogador.transform;
        if (Physics.Raycast(olhos.position, olhos.forward, out RaycastHit acerto, alcance, camadas, QueryTriggerInteraction.Ignore))
        {
            var coletavel = acerto.collider.GetComponentInParent<LivroColetavel>();
            alvo = coletavel != null ? coletavel : acerto.collider.GetComponentInParent<LivroPopup>();
        }

        if (menuNaMira != null) dica = "E: escolher";
        else if (alvo is LivroColetavel livro) dica = livro.Coletado ? "E: ler a introdução no menu" : "E: pegar o livro";
        else if (alvo is LivroPopup) dica = "E: abrir/fechar o painel";

        if (!EntradaDesktop.InteragirPressionado()) return;

        if (menuNaMira != null && menuNaMira.ClicarNaMira(cameraJogador, alcanceMenu)) return;

        if (alvo is LivroColetavel coletavelNaMira)
        {
            if (!coletavelNaMira.Coletado) coletavelNaMira.Coletar();
            else if (GerenciadorColeta.Instancia != null) GerenciadorColeta.Instancia.AbrirNoMenu(coletavelNaMira);
            return;
        }

        // Livros com popup (modelo antigo): só um popup aberto por vez.
        var popup = alvo as LivroPopup;
        if (livroAberto != null && livroAberto != popup) livroAberto.EsconderPopup();
        if (popup != null)
        {
            popup.AlternarPopup();
            livroAberto = popup.PopupAberto ? popup : null;
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

        if (string.IsNullOrEmpty(dica)) return;
        var estilo = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(14, Screen.height / 40) };
        GUI.Label(new Rect(0f, Screen.height / 2f + 18f, Screen.width, 40f), dica, estilo);
    }
}
