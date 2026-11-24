using UnityEngine;

/// <summary>
/// Esconderijo possível para um livro. O configurador cria alguns em cima de móveis do Sebraelab (arquibancada,
/// recepção, cozinha, estações de trabalho...). A cada partida o <see cref="GerenciadorColeta"/> sorteia em quais
/// deles os livros aparecem, como as páginas do Slender. Para criar outro: duplique um esconderijo e arraste-o.
/// </summary>
[AddComponentMenu("Sebraelab/Esconderijo de Livro")]
public class PontoDeLivro : MonoBehaviour
{
    [Tooltip("Altura (m) em que o livro flutua acima deste ponto.")]
    [SerializeField, Min(0f)] private float alturaDoLivro = 0.14f;

    public Vector3 PosicaoDoLivro => transform.position + Vector3.up * alturaDoLivro;
    public Quaternion RotacaoDoLivro => transform.rotation;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.9f);
        Gizmos.DrawLine(transform.position, PosicaoDoLivro);
        Gizmos.matrix = Matrix4x4.TRS(PosicaoDoLivro, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.16f, 0.23f, 0.03f));
    }
}
