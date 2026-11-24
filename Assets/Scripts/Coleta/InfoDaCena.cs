using UnityEngine;

/// <summary>
/// Guarda a versão da cena gerada pelo configurador. Quando o configurador fica mais novo, ele sabe que precisa
/// recriar a cena (a antiga vai para Assets/Scenes/Antigas).
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Sebraelab/Info da Cena")]
public class InfoDaCena : MonoBehaviour
{
    [SerializeField] private int versao;

    public int Versao => versao;
}
