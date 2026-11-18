using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Passeio automático pela cena, usado para gravar um vídeo real do jogo sem ninguém precisar jogar:
/// o jogador de PC olha a sala, vai até os livros, abre o painel de cada um e se afasta no final.
///
/// Normalmente é ligado pelo menu Sebraelab > Gravar vídeo de demonstração (que também grava o MP4),
/// mas também funciona sozinho: coloque no "Jogador PC" e dê Play (com o óculos conectado, o passeio
/// só acontece se este componente estiver no jogador antes do Play).
/// </summary>
[RequireComponent(typeof(DesktopPlayerController))]
[AddComponentMenu("Sebraelab/Demonstracao Guiada")]
public class DemonstracaoGuiada : MonoBehaviour
{
    /// <summary>Fica verdadeiro quando o passeio termina (o gravador usa isso para parar).</summary>
    public static bool Terminou { get; private set; }

    [SerializeField, Min(0.2f)] private float velocidade = 0.9f;
    [SerializeField, Min(0.5f)] private float tempoPorLivro = 4f;
    [Tooltip("Distância (m) entre o jogador e os livros quando ele para na frente da mesa.")]
    [SerializeField, Min(0.4f)] private float distanciaDosLivros = 0.95f;

    private DesktopPlayerController jogador;
    private CharacterController corpo;
    private Transform cameraJogador;
    private float inclinacao;

    private void Awake()
    {
        Terminou = false;
        DesktopPlayerController.ForcarModoPC = true; // grava no modo PC mesmo com óculos conectado
        jogador = GetComponent<DesktopPlayerController>();
        corpo = GetComponent<CharacterController>();
        var cam = GetComponentInChildren<Camera>(true);
        if (cam != null) cameraJogador = cam.transform;
    }

    private void OnDestroy() => DesktopPlayerController.ForcarModoPC = false;

    private IEnumerator Start()
    {
        jogador.AceitarEntrada = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
        yield return null; // espera o DesktopPlayerController se posicionar no XR Origin

        var livros = new List<LivroPopup>(FindObjectsByType<LivroPopup>(FindObjectsSortMode.None));
        livros.Sort((a, b) => Distancia(a).CompareTo(Distancia(b)));

        // 1. Olha a sala em volta.
        float yaw0 = transform.eulerAngles.y;
        yield return Girar(yaw0, yaw0 - 35f, 0f, 2.2f);
        yield return Girar(yaw0 - 35f, yaw0 + 35f, 0f, 3.2f);
        yield return Girar(yaw0 + 35f, yaw0, 0f, 1.6f);

        if (livros.Count > 0)
        {
            // 2. Vai até a frente dos livros.
            var centro = Vector3.zero;
            foreach (var livro in livros) centro += livro.transform.position;
            centro /= livros.Count;

            var direcao = transform.position - centro;
            direcao.y = 0f;
            if (direcao.sqrMagnitude < 0.01f) direcao = -transform.forward;
            direcao.Normalize();

            var destino = new Vector3(centro.x, transform.position.y, centro.z) + direcao * distanciaDosLivros;
            yield return Andar(destino, centro);

            // 3. Abre o painel de cada livro.
            LivroPopup anterior = null;
            foreach (var livro in livros)
            {
                yield return Mirar(livro.transform.position, 0.8f);
                if (anterior != null) anterior.EsconderPopup();
                livro.MostrarPopup();
                anterior = livro;
                yield return new WaitForSeconds(tempoPorLivro);
            }

            // 4. Dá alguns passos para trás, com o último painel aberto.
            yield return Andar(destino + direcao * 1.2f, centro, deFrente: true);
            yield return new WaitForSeconds(1.5f);
            if (anterior != null) anterior.EsconderPopup();
        }

        yield return new WaitForSeconds(1f);
        Terminou = true;
        jogador.AceitarEntrada = true;
    }

    private float Distancia(LivroPopup livro) => Vector3.Distance(livro.transform.position, transform.position);

    private IEnumerator Girar(float yawInicio, float yawFim, float pitch, float duracao)
    {
        float pitchInicio = inclinacao;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / duracao);
            Aplicar(Mathf.LerpAngle(yawInicio, yawFim, p), Mathf.Lerp(pitchInicio, pitch, p));
            yield return null;
        }
        Aplicar(yawFim, pitch);
    }

    private IEnumerator Mirar(Vector3 alvo, float duracao)
    {
        var olhar = alvo - (cameraJogador != null ? cameraJogador.position : transform.position);
        float yaw = Mathf.Atan2(olhar.x, olhar.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Atan2(olhar.y, new Vector2(olhar.x, olhar.z).magnitude) * Mathf.Rad2Deg;
        yield return Girar(transform.eulerAngles.y, yaw, Mathf.Clamp(pitch, -10f, 60f), duracao);
    }

    private IEnumerator Andar(Vector3 destino, Vector3 olharPara, bool deFrente = false)
    {
        var inicio = transform.position;
        float duracao = Mathf.Max(0.5f, Vector3.Distance(inicio, destino) / velocidade);
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / duracao);
            Mover(Vector3.Lerp(inicio, destino, p));
            var olhar = olharPara - (cameraJogador != null ? cameraJogador.position : transform.position);
            float yaw = Mathf.Atan2(olhar.x, olhar.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(olhar.y, new Vector2(olhar.x, olhar.z).magnitude) * Mathf.Rad2Deg;
            Aplicar(Mathf.LerpAngle(transform.eulerAngles.y, yaw, deFrente ? 0.15f : 0.08f),
                    Mathf.Lerp(inclinacao, Mathf.Clamp(pitch, -10f, 60f), 0.08f));
            yield return null;
        }
        Mover(destino);
    }

    private void Mover(Vector3 posicao)
    {
        bool estavaLigado = corpo.enabled;
        corpo.enabled = false;
        transform.position = posicao;
        corpo.enabled = estavaLigado;
    }

    private void Aplicar(float yaw, float pitch)
    {
        inclinacao = pitch;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cameraJogador != null) cameraJogador.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
