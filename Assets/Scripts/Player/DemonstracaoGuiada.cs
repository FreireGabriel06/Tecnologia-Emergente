using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Passeio automático pela cena, usado para gravar um vídeo real do jogo sem ninguém precisar jogar:
/// o jogador de PC olha o hub, vai atrás de cada livro escondido e pega, volta ao hub e abre a introdução de cada
/// livro no menu (e o botão Desafios, que por enquanto só avisa "em breve").
///
/// Normalmente é ligado pelo menu Sebraelab > Gravar vídeo de demonstração (que também grava o MP4),
/// mas também funciona sozinho: coloque no "Jogador PC" e dê Play.
/// </summary>
[RequireComponent(typeof(DesktopPlayerController))]
[AddComponentMenu("Sebraelab/Demonstracao Guiada")]
public class DemonstracaoGuiada : MonoBehaviour
{
    /// <summary>Fica verdadeiro quando o passeio termina (o gravador usa isso para parar).</summary>
    public static bool Terminou { get; private set; }

    [SerializeField, Min(0.2f)] private float velocidade = 1.4f;
    [Tooltip("Segundos lendo cada livro no menu do hub.")]
    [SerializeField, Min(0.5f)] private float tempoPorLivro = 4.5f;
    [Tooltip("Distância (m) em que o jogador para na frente de um livro escondido.")]
    [SerializeField, Min(0.5f)] private float distanciaDoLivro = 1.3f;

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
        yield return new WaitForSeconds(0.5f); // e o hub sortear os esconderijos

        var inicio = transform.position;
        float yawInicial = transform.eulerAngles.y;
        var hub = GerenciadorColeta.Instancia;

        // 1. Olha o hub e a sala em volta.
        yield return Girar(yawInicial, yawInicial - 40f, 0f, 2.2f);
        yield return Girar(yawInicial - 40f, yawInicial + 40f, 0f, 3.2f);
        yield return Girar(yawInicial + 40f, yawInicial, 0f, 1.6f);

        if (hub != null)
        {
            // 2. Vai atrás de cada livro escondido (o mais perto primeiro) e pega.
            var faltam = new List<LivroColetavel>();
            foreach (var livro in hub.Livros) if (livro != null && !livro.Coletado) faltam.Add(livro);
            while (faltam.Count > 0)
            {
                faltam.Sort((a, b) => Distancia(a.transform.position).CompareTo(Distancia(b.transform.position)));
                var livro = faltam[0];
                faltam.RemoveAt(0);

                var lugar = LugarPerto(livro);
                yield return IrPara(lugar, livro.transform.position);
                yield return Mirar(livro.transform.position, 0.8f);
                yield return new WaitForSeconds(0.5f);
                livro.Coletar();
                yield return new WaitForSeconds(2.6f);
            }

            // 3. Volta ao hub e lê a introdução de cada livro no menu.
            yield return IrPara(inicio, hub.transform.position);
            var menu = hub.Menu;
            if (menu != null)
            {
                yield return Mirar(menu.transform.position, 1f);
                for (int i = 0; i < hub.Livros.Count; i++)
                {
                    menu.Selecionar(i);
                    yield return new WaitForSeconds(tempoPorLivro);
                }
                menu.AbrirDesafios();
                yield return new WaitForSeconds(2.5f);
            }
        }

        yield return new WaitForSeconds(1f);
        Terminou = true;
        jogador.AceitarEntrada = true;
    }

    private float Distancia(Vector3 ponto) => Vector3.Distance(ponto, transform.position);

    /// <summary>Um lugar livre no chão, perto do livro e de onde dá para vê-lo.</summary>
    private Vector3 LugarPerto(LivroColetavel livro)
    {
        var alvo = livro.transform.position;
        var paraOJogador = transform.position - alvo;
        paraOJogador.y = 0f;
        float anguloInicial = Mathf.Atan2(paraOJogador.x, paraOJogador.z) * Mathf.Rad2Deg;

        foreach (float distancia in new[] { distanciaDoLivro, distanciaDoLivro + 0.6f, distanciaDoLivro + 1.2f })
        {
            for (int i = 0; i < 16; i++)
            {
                // alterna para os dois lados a partir da direção de onde o jogador vem
                float angulo = anguloInicial + (i % 2 == 0 ? 1f : -1f) * 22.5f * ((i + 1) / 2);
                var direcao = Quaternion.Euler(0f, angulo, 0f) * Vector3.forward;
                var candidato = new Vector3(alvo.x, transform.position.y + 0.6f, alvo.z) + direcao * distancia;
                if (!Physics.Raycast(candidato, Vector3.down, out RaycastHit chao, 2.5f, ~0, QueryTriggerInteraction.Ignore)) continue;
                var pe = chao.point;
                if (Mathf.Abs(pe.y - transform.position.y) > 0.4f) continue; // outro andar, degrau ou móvel
                if (Physics.CheckCapsule(pe + Vector3.up * 0.4f, pe + Vector3.up * 1.4f, 0.25f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (Bloqueado(pe + Vector3.up * 1.5f, alvo, livro)) continue;
                return pe;
            }
        }
        return new Vector3(alvo.x, transform.position.y, alvo.z) + (paraOJogador.sqrMagnitude > 0.01f ? paraOJogador.normalized : Vector3.back) * distanciaDoLivro;
    }

    private static bool Bloqueado(Vector3 de, Vector3 para, LivroColetavel ignorar)
    {
        foreach (var acerto in Physics.RaycastAll(de, para - de, Vector3.Distance(de, para), ~0, QueryTriggerInteraction.Ignore))
            if (ignorar == null || acerto.collider.GetComponentInParent<LivroColetavel>() != ignorar) return true;
        return false;
    }

    /// <summary>Anda até o destino; se houver parede ou móvel no caminho, corta direto para lá (como um teleporte).</summary>
    private IEnumerator IrPara(Vector3 destino, Vector3 olharPara)
    {
        var inicio = transform.position;
        var caminho = destino - inicio;
        bool livre = !Physics.CapsuleCast(inicio + Vector3.up * 0.4f, inicio + Vector3.up * 1.4f, 0.25f, caminho.normalized,
                                          caminho.magnitude, ~0, QueryTriggerInteraction.Ignore);
        if (!livre)
        {
            var antes = destino - caminho.normalized * Mathf.Min(1.5f, caminho.magnitude);
            Mover(antes);
            yield return Mirar(olharPara, 0.4f);
            inicio = antes;
        }

        float duracao = Mathf.Max(0.5f, Vector3.Distance(inicio, destino) / velocidade);
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / duracao);
            Mover(Vector3.Lerp(inicio, destino, p));
            OlharSuave(olharPara, 0.08f);
            yield return null;
        }
        Mover(destino);
    }

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
        Angulos(alvo, out float yaw, out float pitch);
        yield return Girar(transform.eulerAngles.y, yaw, pitch, duracao);
    }

    private void OlharSuave(Vector3 alvo, float fator)
    {
        Angulos(alvo, out float yaw, out float pitch);
        Aplicar(Mathf.LerpAngle(transform.eulerAngles.y, yaw, fator), Mathf.Lerp(inclinacao, pitch, fator));
    }

    private void Angulos(Vector3 alvo, out float yaw, out float pitch)
    {
        var olhar = alvo - (cameraJogador != null ? cameraJogador.position : transform.position);
        yaw = Mathf.Atan2(olhar.x, olhar.z) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(-Mathf.Atan2(olhar.y, new Vector2(olhar.x, olhar.z).magnitude) * Mathf.Rad2Deg, -30f, 60f);
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
