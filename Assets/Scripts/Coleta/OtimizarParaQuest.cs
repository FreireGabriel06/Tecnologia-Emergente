using UnityEngine;

/// <summary>
/// No Meta Quest (Android) desliga o que pesa mais para o óculos, como o pós-processamento e partículas extras.
/// No PC tudo continua ligado. O configurador coloca este componente no grupo "Iluminação".
/// </summary>
[AddComponentMenu("Sebraelab/Otimizar Para Quest")]
public class OtimizarParaQuest : MonoBehaviour
{
    [Tooltip("Objetos desligados quando o jogo roda no Quest.")]
    [SerializeField] private GameObject[] desligarNoQuest = new GameObject[0];

    private void Awake()
    {
        if (Application.platform != RuntimePlatform.Android) return;
        foreach (var objeto in desligarNoQuest) if (objeto != null) objeto.SetActive(false);
    }
}
