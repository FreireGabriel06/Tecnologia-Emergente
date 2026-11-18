using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Sebraelab > Gravar vídeo de demonstração: abre a cena do Sebraelab, dá Play, faz o passeio automático
/// (DemonstracaoGuiada) e grava a tela do jogo em MP4 com o Unity Recorder. No fim, sai do Play e abre a pasta do vídeo.
/// O vídeo fica em "Gravacoes/" na pasta do projeto (fora de Assets, não vai para o Git).
/// </summary>
[InitializeOnLoad]
public static class SebraelabGravador
{
    private const string ChaveGravando = "Sebraelab.Gravador.Ativo";
    private const string NomeJogador = "Jogador PC (teste sem oculos)";
    private static RecorderController gravador;
    private static string arquivo;

    static SebraelabGravador()
    {
        EditorApplication.playModeStateChanged += AoMudarModo;
    }

    [MenuItem("Sebraelab/Gravar vídeo de demonstração", false, -90)]
    private static void Gravar()
    {
        if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Sebraelab", "Saia do modo Play antes de gravar.", "OK"); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (File.Exists(SebraelabConfigurador.CaminhoCena)) EditorSceneManager.OpenScene(SebraelabConfigurador.CaminhoCena);

        var jogador = GameObject.Find(NomeJogador);
        if (jogador == null || jogador.GetComponent<DesktopPlayerController>() == null)
        {
            EditorUtility.DisplayDialog("Sebraelab",
                "Não achei o \"" + NomeJogador + "\" na cena. Rode antes Sebraelab > Configurar projeto VR (automático).", "OK");
            return;
        }

        // A DemonstracaoGuiada só é adicionada depois de entrar no Play, então nada muda na cena salva.
        SessionState.SetBool(ChaveGravando, true);
        EditorApplication.isPlaying = true;
    }

    private static void AoMudarModo(PlayModeStateChange estado)
    {
        if (!SessionState.GetBool(ChaveGravando, false)) return;

        if (estado == PlayModeStateChange.EnteredPlayMode)
        {
            var jogador = GameObject.Find(NomeJogador);
            if (jogador == null)
            {
                Debug.LogError("[Sebraelab] Jogador PC não encontrado no Play; gravação cancelada.");
                EditorApplication.isPlaying = false;
                return;
            }
            if (jogador.GetComponent<DemonstracaoGuiada>() == null) jogador.AddComponent<DemonstracaoGuiada>();
            Comecar();
            EditorApplication.update -= Acompanhar;
            EditorApplication.update += Acompanhar;
        }
        else if (estado == PlayModeStateChange.ExitingPlayMode)
        {
            Parar();
        }
        else if (estado == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(ChaveGravando, false);
            EditorApplication.update -= Acompanhar;

            var pasta = Path.GetFullPath("Gravacoes");
            if (Directory.Exists(pasta)) EditorUtility.RevealInFinder(Path.Combine(pasta, "Sebraelab_demo.mp4"));
            EditorUtility.DisplayDialog("Sebraelab",
                "Vídeo salvo em:\n" + Path.Combine(pasta, "Sebraelab_demo.mp4") + "\n\nEnvie esse arquivo para o Claude colocar no vídeo de apresentação.", "OK");
        }
    }

    private static void Comecar()
    {
        var pasta = Path.GetFullPath("Gravacoes");
        Directory.CreateDirectory(pasta);
        arquivo = Path.Combine(pasta, "Sebraelab_demo");

        var configuracao = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var video = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        video.name = "Sebraelab demo";
        video.Enabled = true;
        video.EncoderSettings = new CoreEncoderSettings
        {
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            Codec = CoreEncoderSettings.OutputCodec.MP4
        };
        video.CaptureAlpha = false;
        video.ImageInputSettings = new GameViewInputSettings { OutputWidth = 1920, OutputHeight = 1080 };
        video.OutputFile = arquivo;

        configuracao.AddRecorderSettings(video);
        configuracao.SetRecordModeToManual();
        configuracao.FrameRate = 30f;
        configuracao.CapFrameRate = true;

        RecorderOptions.VerboseMode = false;
        gravador = new RecorderController(configuracao);
        gravador.PrepareRecording();
        if (gravador.StartRecording()) Debug.Log("[Sebraelab] Gravando a demonstração em " + arquivo + ".mp4");
        else Debug.LogWarning("[Sebraelab] O Unity Recorder não conseguiu começar a gravar.");
    }

    private static void Acompanhar()
    {
        if (EditorApplication.isPlaying && DemonstracaoGuiada.Terminou) EditorApplication.isPlaying = false;
    }

    private static void Parar()
    {
        if (gravador == null) return;
        if (gravador.IsRecording()) gravador.StopRecording();
        gravador = null;
        Debug.Log("[Sebraelab] Gravação concluída: " + arquivo + ".mp4");
    }
}
