using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Aviso que aparece na frente do jogador, no estilo do contador de páginas do Slender ("2 de 3 livros").
/// Funciona em VR e no PC: segue a câmera que estiver ligada, sem grudar no rosto (vai atrás com suavidade),
/// e aparece por cima das paredes. O <see cref="GerenciadorColeta"/> cria um sozinho se a cena não tiver.
/// </summary>
[AddComponentMenu("Sebraelab/Aviso Coleta")]
public class AvisoColeta : MonoBehaviour
{
    [Tooltip("Distância (m) do aviso até os olhos.")]
    [SerializeField, Min(0.3f)] private float distancia = 1.1f;

    [Tooltip("Quanto o aviso fica abaixo da linha dos olhos (m).")]
    [SerializeField] private float abaixo = 0.18f;

    private CanvasGroup grupo;
    private TextMeshProUGUI titulo;
    private TextMeshProUGUI subtitulo;
    private float visivelAte = -1f;
    private bool reposicionar;

    /// <summary>Cria o aviso com a interface montada por código.</summary>
    public static AvisoColeta Criar() => new GameObject("Aviso de coleta").AddComponent<AvisoColeta>();

    private void Awake()
    {
        var rt = gameObject.GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(760f, 190f);
        rt.localScale = Vector3.one * 0.001f; // 1 unidade = 1 mm

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
        grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        // Material de UI que ignora a profundidade: o aviso aparece mesmo com uma parede na frente.
        var materialPorCima = new Material(Canvas.GetDefaultCanvasMaterial());
        materialPorCima.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);

        var fundo = new GameObject("Fundo", typeof(RectTransform)).AddComponent<Image>();
        fundo.transform.SetParent(transform, false);
        Esticar(fundo.rectTransform, 0f, 0f, 1f, 1f);
        fundo.color = new Color(0.03f, 0.05f, 0.1f, 0.82f);
        fundo.material = materialPorCima;
        fundo.raycastTarget = false;

        titulo = Texto("Titulo", 64f, FontStyles.Bold, Color.white, 0.42f, 0.96f);
        subtitulo = Texto("Subtitulo", 32f, FontStyles.Normal, new Color(0.8f, 0.87f, 1f), 0.06f, 0.44f);
    }

    private TextMeshProUGUI Texto(string nome, float tamanho, FontStyles estilo, Color cor, float baixo, float alto)
    {
        var texto = new GameObject(nome, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        texto.transform.SetParent(transform, false);
        Esticar(texto.rectTransform, 0.03f, baixo, 0.97f, alto);
        texto.fontSize = tamanho;
        texto.fontStyle = estilo;
        texto.color = cor;
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;
        if (texto.font != null) texto.fontMaterial.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
        return texto;
    }

    private static void Esticar(RectTransform rt, float x0, float y0, float x1, float y1)
    {
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>Mostra o aviso por alguns segundos.</summary>
    public void Mostrar(string textoTitulo, string textoSubtitulo, float segundos)
    {
        titulo.text = textoTitulo;
        subtitulo.text = textoSubtitulo;
        subtitulo.gameObject.SetActive(!string.IsNullOrWhiteSpace(textoSubtitulo));
        if (grupo.alpha < 0.05f) reposicionar = true; // aparece já na frente, sem "voar" até lá
        visivelAte = Time.time + segundos;
    }

    private void LateUpdate()
    {
        float alvo = Time.time < visivelAte ? 1f : 0f;
        grupo.alpha = Mathf.MoveTowards(grupo.alpha, alvo, Time.deltaTime * 4f);

        var cam = CameraAtiva();
        if (cam == null || grupo.alpha <= 0f) return;

        var olhos = cam.transform;
        var frente = Vector3.ProjectOnPlane(olhos.forward, Vector3.up);
        if (frente.sqrMagnitude < 0.01f) frente = olhos.up; // olhando reto para cima ou para baixo
        frente.Normalize();
        var posicao = olhos.position + frente * distancia + Vector3.down * abaixo;

        float k = reposicionar ? 1f : 1f - Mathf.Exp(-4f * Time.deltaTime);
        reposicionar = false;
        transform.position = Vector3.Lerp(transform.position, posicao, k);
        transform.rotation = Quaternion.LookRotation(transform.position - olhos.position, Vector3.up);
    }

    private static Camera CameraAtiva()
    {
        var principal = Camera.main;
        if (principal != null && principal.isActiveAndEnabled) return principal;
        foreach (var cam in Camera.allCameras) if (cam.isActiveAndEnabled && cam.targetTexture == null) return cam;
        return null;
    }
}
