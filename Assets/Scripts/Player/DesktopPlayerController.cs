using UnityEngine;

/// <summary>
/// Jogador para testar o Sebraelab no PC, sem óculos VR.
///   WASD / setas = andar      Mouse = olhar      Shift = correr
///   Esc = soltar o mouse      Clique = prender o mouse de novo
///
/// Com "Trocar Automaticamente" ligado, a mesma cena serve para PC e VR:
///   - headset conectado  -> usa o XR Origin e desliga este jogador;
///   - sem headset        -> desliga o XR Origin e usa este jogador.
///
/// Crie pelo menu Sebraelab > Jogador > Criar jogador de teste (teclado e mouse).
/// O movimento em VR (analógico, teleporte e giro) NÃO é feito aqui: ele vem pronto no
/// XR Origin dos Starter Assets do XR Interaction Toolkit (veja Docs/CONFIGURACAO_UNITY.md).
/// </summary>
[RequireComponent(typeof(CharacterController))]
[AddComponentMenu("Sebraelab/Desktop Player Controller")]
public class DesktopPlayerController : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Câmera do jogador (filha deste objeto). Se vazio, usa a primeira câmera encontrada nos filhos.")]
    [SerializeField] private Transform cameraJogador;

    [Tooltip("XR Origin da cena. Se vazio, é procurado automaticamente.")]
    [SerializeField] private GameObject xrOrigin;

    [Header("PC x VR")]
    [Tooltip("Usa este jogador só quando não há headset VR conectado. Desligue se for testar com o XR Device Simulator.")]
    [SerializeField] private bool trocarAutomaticamente = true;

    [Tooltip("Começa na posição do XR Origin (assim só é preciso posicionar o XR Origin na cena).")]
    [SerializeField] private bool comecarNoXROrigin = true;

    [Header("Movimento")]
    [SerializeField, Min(0f)] private float velocidadeAndar = 2f;
    [SerializeField, Min(0f)] private float velocidadeCorrer = 4f;
    [SerializeField] private float gravidade = -9.81f;

    [Header("Olhar com o mouse")]
    [Tooltip("Graus por pixel de movimento do mouse.")]
    [SerializeField, Min(0f)] private float sensibilidadeMouse = 0.1f;
    [SerializeField, Range(10f, 89f)] private float limiteOlharVertical = 80f;

    private CharacterController controller;
    private float rotacaoVertical;
    private float velocidadeQueda;

    private static bool MouseTravado => Cursor.lockState == CursorLockMode.Locked;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraJogador == null)
        {
            var cam = GetComponentInChildren<Camera>(true);
            if (cam != null) cameraJogador = cam.transform;
        }

        if (xrOrigin == null) xrOrigin = EncontrarXROrigin();
    }

    private void Start()
    {
        if (trocarAutomaticamente)
        {
            if (UnityEngine.XR.XRSettings.isDeviceActive)
            {
                // Headset conectado: fica só o XR Origin.
                if (xrOrigin != null) xrOrigin.SetActive(true);
                gameObject.SetActive(false);
                return;
            }

            if (xrOrigin != null) xrOrigin.SetActive(false);
        }

        if (comecarNoXROrigin && xrOrigin != null)
            IrPara(xrOrigin.transform.position, xrOrigin.transform.eulerAngles.y);

        TravarMouse(true);
    }

    private void Update()
    {
        if (EntradaDesktop.SoltarMousePressionado()) TravarMouse(false);
        else if (!MouseTravado && EntradaDesktop.CliquePressionado()) TravarMouse(true);

        if (MouseTravado) Olhar(EntradaDesktop.DeltaMouse());
        Mover(EntradaDesktop.Movimento(), EntradaDesktop.CorrerPressionado());
    }

    private void OnDisable()
    {
        if (MouseTravado) TravarMouse(false);
    }

    /// <summary>Teleporta o jogador (o CharacterController precisa ser desligado para isso).</summary>
    public void IrPara(Vector3 posicao, float rotacaoY)
    {
        controller.enabled = false;
        transform.SetPositionAndRotation(posicao, Quaternion.Euler(0f, rotacaoY, 0f));
        controller.enabled = true;
    }

    private void Olhar(Vector2 delta)
    {
        transform.Rotate(0f, delta.x * sensibilidadeMouse, 0f, Space.World);

        rotacaoVertical = Mathf.Clamp(rotacaoVertical - delta.y * sensibilidadeMouse, -limiteOlharVertical, limiteOlharVertical);
        if (cameraJogador != null) cameraJogador.localRotation = Quaternion.Euler(rotacaoVertical, 0f, 0f);
    }

    private void Mover(Vector2 entrada, bool correndo)
    {
        Vector3 direcao = transform.right * entrada.x + transform.forward * entrada.y;
        direcao = Vector3.ClampMagnitude(direcao, 1f) * (correndo ? velocidadeCorrer : velocidadeAndar);

        if (controller.isGrounded && velocidadeQueda < 0f) velocidadeQueda = -1f; // mantém colado no chão
        velocidadeQueda += gravidade * Time.deltaTime;
        direcao.y = velocidadeQueda;

        controller.Move(direcao * Time.deltaTime);
    }

    private static void TravarMouse(bool travar)
    {
        Cursor.lockState = travar ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !travar;
    }

    /// <summary>
    /// Procura o XR Origin da cena pelo nome do tipo, assim este script não depende do pacote XR Core Utils.
    /// </summary>
    public static GameObject EncontrarXROrigin()
    {
        foreach (var componente in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (componente != null && componente.GetType().Name == "XROrigin") return componente.gameObject;
        return null;
    }
}
