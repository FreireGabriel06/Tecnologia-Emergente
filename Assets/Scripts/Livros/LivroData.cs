using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Dados de um livro da exposição (título, autor, resumo, capa e vídeo).
///
/// Crie um pelo menu: Assets > Create > Sebraelab > Dados do Livro
/// e arraste o asset para o campo "Dados" do componente LivroPopup do livro.
/// Assim o texto do popup fica num lugar só e é fácil de editar.
/// </summary>
[CreateAssetMenu(fileName = "NovoLivro", menuName = "Sebraelab/Dados do Livro", order = 0)]
public class LivroData : ScriptableObject
{
    [SerializeField] private string titulo = "Título do livro";

    [SerializeField] private string autor = "";

    [SerializeField, TextArea(4, 15)] private string resumo = "";

    [Tooltip("Imagem da capa. Importe a imagem com Texture Type = Sprite (2D and UI).")]
    [SerializeField] private Sprite capa;

    [Tooltip("GIF convertido para .mp4 (o Unity não toca .gif). Deixe vazio se o livro não tiver animação.")]
    [SerializeField] private VideoClip video;

    public string Titulo => titulo;
    public string Autor => autor;
    public string Resumo => resumo;
    public Sprite Capa => capa;
    public VideoClip Video => video;
}
