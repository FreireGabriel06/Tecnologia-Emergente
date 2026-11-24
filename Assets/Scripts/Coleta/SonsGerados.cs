using System;
using UnityEngine;

/// <summary>
/// Sons simples gerados por código, para o jogo ter áudio sem depender de arquivos:
/// o "brilho" em loop perto de um livro escondido, o sino de livro encontrado, a fanfarra do último livro,
/// o "ping" do detector de livros e uma música ambiente calma.
/// Para usar sons próprios, é só colocar um AudioClip nos campos do LivroColetavel e do GerenciadorColeta.
/// </summary>
public static class SonsGerados
{
    private const int Taxa = 44100;
    private static AudioClip brilho, coleta, final, ping, ambiente;

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

    /// <summary>"Ping" curto do detector de livros.</summary>
    public static AudioClip Ping() => ping != null ? ping : ping = Gerar("Ping do detector (gerado)", 0.22f, t =>
        Mathf.Clamp01(t / 0.004f) * Mathf.Exp(-22f * t) * (Seno(t, 1320f) + 0.3f * Seno(t, 2640f)));

    /// <summary>
    /// Música ambiente calma (16 s em loop, sem emenda): quatro acordes suaves (Dó, Lá menor, Fá, Sol) com
    /// algumas notas de sino por cima. Fica baixinha, só para a sala não ficar em silêncio.
    /// </summary>
    public static AudioClip Ambiente() => ambiente != null ? ambiente : ambiente = GerarAmbiente();

    private static AudioClip GerarAmbiente()
    {
        const int taxa = 22050;
        const float duracao = 16f, porAcorde = 4f;
        float[][] acordes =
        {
            new[] { 130.81f, 164.81f, 196.00f, 246.94f }, // Dó maior com sétima
            new[] { 110.00f, 130.81f, 164.81f, 196.00f }, // Lá menor com sétima
            new[] { 87.31f, 110.00f, 130.81f, 164.81f },  // Fá maior com sétima
            new[] { 98.00f, 123.47f, 146.83f, 164.81f },  // Sol com sexta
        };
        float[] sinos = { 523.25f, 587.33f, 659.25f, 783.99f, 880.00f }; // pentatônica
        var aleatorio = new System.Random(7);
        var notas = new (float inicio, float frequencia)[10];
        for (int i = 0; i < notas.Length; i++) notas[i] = (i * 1.6f + (float)aleatorio.NextDouble() * 0.6f, sinos[aleatorio.Next(sinos.Length)]);

        int amostras = Mathf.RoundToInt(duracao * taxa);
        var dados = new float[amostras];
        float pico = 0.0001f;
        for (int i = 0; i < amostras; i++)
        {
            float t = i / (float)taxa, valor = 0f;
            for (int a = 0; a < acordes.Length; a++)
            {
                // cada acorde entra e sai suavemente; o último emenda no primeiro (loop sem estalo)
                float centro = a * porAcorde + porAcorde / 2f;
                float d = Mathf.Abs(Mathf.Repeat(t - centro + duracao / 2f, duracao) - duracao / 2f);
                float envelope = Mathf.Clamp01((porAcorde / 2f + 0.9f - d) / 1.8f);
                if (envelope <= 0f) continue;
                envelope = envelope * envelope * (3f - 2f * envelope);
                foreach (float nota in acordes[a])
                {
                    float f = Mathf.Round(nota * duracao) / duracao; // ciclos inteiros em 16 s: o loop não estala
                    valor += envelope * (Seno(t, f) + 0.25f * Seno(t, 2f * f)) * 0.25f;
                }
            }
            foreach (var (inicio, f) in notas)
            {
                float x = Mathf.Repeat(t - inicio, duracao);
                if (x < 3f) valor += 0.18f * Mathf.Clamp01(x / 0.01f) * Mathf.Exp(-1.6f * x) * Seno(x, f);
            }
            valor *= 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * t / 8f); // respiração lenta (2 ciclos por volta)
            dados[i] = valor;
            pico = Mathf.Max(pico, Mathf.Abs(valor));
        }
        for (int i = 0; i < amostras; i++) dados[i] = dados[i] / pico * 0.7f;

        var clipe = AudioClip.Create("Música ambiente (gerada)", amostras, 1, taxa, false);
        clipe.SetData(dados, 0);
        return clipe;
    }

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
