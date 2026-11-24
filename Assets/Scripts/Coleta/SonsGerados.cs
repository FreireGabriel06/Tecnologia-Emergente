using System;
using UnityEngine;

/// <summary>
/// Sons simples gerados por código, para o jogo ter áudio sem depender de arquivos:
/// o "brilho" em loop perto de um livro escondido, o sino de livro encontrado e a fanfarra do último livro.
/// Para usar sons próprios, é só colocar um AudioClip nos campos do LivroColetavel e do GerenciadorColeta.
/// </summary>
public static class SonsGerados
{
    private const int Taxa = 44100;
    private static AudioClip brilho, coleta, final;

    /// <summary>Brilho suave em loop (2 s, emenda sem estalo).</summary>
    public static AudioClip Brilho() => brilho != null ? brilho : brilho = Gerar("Brilho (gerado)", 2f, t =>
    {
        // Frequências inteiras repetem exatamente a cada 2 s: o loop não tem emenda.
        float pulso = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
        return pulso * (0.5f * Seno(t, 880f) + 0.3f * Seno(t, 1320f) + 0.2f * Seno(t, 1761f)) * 0.6f;
    });

    /// <summary>Sino de três notas (livro encontrado).</summary>
    public static AudioClip Coleta() => coleta != null ? coleta : coleta = Gerar("Livro encontrado (gerado)", 1.6f, t =>
        Sino(t, 1046.5f, 0f) + Sino(t, 1318.5f, 0.1f) + Sino(t, 1568f, 0.2f));

    /// <summary>Arpejo mais longo (todos os livros encontrados).</summary>
    public static AudioClip Final() => final != null ? final : final = Gerar("Todos encontrados (gerado)", 2.8f, t =>
        Sino(t, 523.3f, 0f) + Sino(t, 659.3f, 0.14f) + Sino(t, 784f, 0.28f) + 1.2f * Sino(t, 1046.5f, 0.42f) + 0.6f * Sino(t, 1568f, 0.42f));

    private static float Seno(float t, float frequencia) => Mathf.Sin(2f * Mathf.PI * frequencia * t);

    private static float Sino(float t, float frequencia, float inicio)
    {
        if (t < inicio) return 0f;
        float x = t - inicio;
        float ataque = Mathf.Clamp01(x / 0.005f);
        return ataque * Mathf.Exp(-3.2f * x) * (Seno(x, frequencia) + 0.35f * Seno(x, frequencia * 2f) + 0.12f * Seno(x, frequencia * 3.01f));
    }

    private static AudioClip Gerar(string nome, float duracao, Func<float, float> onda)
    {
        int amostras = Mathf.RoundToInt(duracao * Taxa);
        var dados = new float[amostras];
        float pico = 0.0001f;
        for (int i = 0; i < amostras; i++)
        {
            dados[i] = onda(i / (float)Taxa);
            pico = Mathf.Max(pico, Mathf.Abs(dados[i]));
        }
        for (int i = 0; i < amostras; i++) dados[i] = dados[i] / pico * 0.8f;

        var clipe = AudioClip.Create(nome, amostras, 1, Taxa, false);
        clipe.SetData(dados, 0);
        return clipe;
    }
}
