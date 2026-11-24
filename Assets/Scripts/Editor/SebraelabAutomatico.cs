using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Automação pela linha de comando, sem ninguém clicar em nada (é o que o FAZER_TUDO.bat usa):
///
///   1) Configurar o projeto, sem janela da Unity:
///      Unity.exe -batchmode -projectPath "PASTA" -executeMethod SebraelabAutomatico.Configurar -logFile "PASTA\Gravacoes\1_configurar.log"
///      Instala os pacotes, liga o OpenXR, cria a cena com o hub e os livros escondidos e fecha a Unity.
///
///   2) Gravar o vídeo real do jogo (com a janela da Unity, que abre e fecha sozinha):
///      Unity.exe -projectPath "PASTA" -executeMethod SebraelabAutomatico.GravarDemonstracao -logFile "PASTA\Gravacoes\2_gravar.log"
///      Dá Play, faz o passeio automático (acha os 3 livros, volta ao hub e abre o menu) e salva Gravacoes/Sebraelab_demo.mp4.
///
/// O código de saída da Unity é 0 quando deu tudo certo.
/// </summary>
[InitializeOnLoad]
public static class SebraelabAutomatico
{
    private const string ChaveAtivo = "Sebraelab.Automatico.Ativo";
    private const string ChaveInicio = "Sebraelab.Automatico.Inicio";
    private const string ChaveGravar = "Sebraelab.Automatico.GravarAgendado";
    private const double LimiteEmSegundos = 40 * 60;

    /// <summary>Verdadeiro durante uma execução automática (as janelas de aviso viram mensagens no log).</summary>
    internal static bool Ativo => SessionState.GetBool(ChaveAtivo, false);

    static SebraelabAutomatico()
    {
        // Depois de cada recompilação (domain reload) os ganchos precisam ser religados.
        if (!Ativo) return;
        EditorApplication.update -= Vigiar;
        EditorApplication.update += Vigiar;
    }

    /// <summary>Linha de comando: configura o projeto e fecha a Unity.</summary>
    public static void Configurar()
    {
        Comecar("configurar o projeto");
        SebraelabConfigurador.Iniciar();
    }

    /// <summary>Linha de comando: grava o vídeo real do jogo e fecha a Unity.</summary>
    public static void GravarDemonstracao()
    {
        Comecar("gravar o vídeo de demonstração");
        if (!File.Exists(SebraelabConfigurador.CaminhoCena))
        {
            Log("A cena " + SebraelabConfigurador.CaminhoCena + " não existe. Rode antes a configuração (SebraelabAutomatico.Configurar).");
            Terminar(3);
            return;
        }

        EditorSceneManager.OpenScene(SebraelabConfigurador.CaminhoCena);
        if (GarantirColetaNaCena()) EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        // Espera a janela da Unity terminar de abrir antes de dar Play.
        SessionState.SetBool(ChaveGravar, true);
    }

    private static void Comecar(string tarefa)
    {
        SessionState.SetBool(ChaveAtivo, true);
        SessionState.SetFloat(ChaveInicio, (float)EditorApplication.timeSinceStartup);
        EditorApplication.update -= Vigiar;
        EditorApplication.update += Vigiar;
        Log("Automático: " + tarefa + ".");
    }

    private static void Vigiar()
    {
        if (!Ativo) return;

        double decorrido = EditorApplication.timeSinceStartup - SessionState.GetFloat(ChaveInicio, 0f);
        if (decorrido > LimiteEmSegundos)
        {
            Log($"Tempo esgotado ({LimiteEmSegundos / 60:0} min). Veja os erros acima neste log.");
            Terminar(2);
            return;
        }

        // Gravação agendada: espera a Unity ficar livre e uns segundos de janela aberta.
        if (SessionState.GetBool(ChaveGravar, false) && decorrido > 8.0 &&
            !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetBool(ChaveGravar, false);
            if (!SebraelabGravador.GravarAgora())
            {
                Log("Não foi possível começar a gravação (detalhes acima).");
                Terminar(4);
            }
        }
    }

    /// <summary>Chamado pelo configurador quando termina.</summary>
    internal static void ConfiguracaoTerminou(string mensagem, List<string> avisos)
    {
        if (!Ativo) return;
        Log(mensagem.Replace("\n\n", "\n"));
        if (GarantirColetaNaCena()) EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Terminar(0);
    }

    /// <summary>Chamado pelo configurador se uma etapa der erro.</summary>
    internal static void ConfiguracaoFalhou(System.Exception erro)
    {
        if (!Ativo) return;
        Log("A configuração parou por causa de um erro: " + erro.Message);
        Terminar(1);
    }

    /// <summary>Chamado pelo gravador quando o vídeo termina.</summary>
    internal static void GravacaoTerminou(string arquivo)
    {
        if (!Ativo) return;
        bool existe = File.Exists(arquivo);
        Log(existe ? "Vídeo gravado: " + arquivo : "A gravação terminou, mas o arquivo não apareceu: " + arquivo);
        Terminar(existe ? 0 : 5);
    }

    /// <summary>Cena criada antes da coleta de livros: monta o hub e os livros escondidos nela.</summary>
    private static bool GarantirColetaNaCena()
    {
        if (Object.FindFirstObjectByType<GerenciadorColeta>(FindObjectsInactive.Include) != null) return false;
        Log("A cena não tinha a coleta de livros; montando o hub e os livros escondidos.");
        return SebraelabColeta.MontarNaCena() != null;
    }

    private static void Terminar(int codigo)
    {
        SessionState.SetBool(ChaveAtivo, false);
        SessionState.SetBool(ChaveGravar, false);
        EditorApplication.update -= Vigiar;
        Log(codigo == 0 ? "Automático: concluído." : "Automático: terminou com erro (código " + codigo + ").");
        EditorApplication.delayCall += () => EditorApplication.Exit(codigo);
    }

    private static void Log(string mensagem) => Debug.Log("[Sebraelab] " + mensagem);
}
