// Compatibilidade entre o XR Interaction Toolkit 2.x e 3.x. Não precisa mexer neste arquivo.
//
// No XRI 3.x classes como XRGrabInteractable foram movidas para o namespace
// "UnityEngine.XR.Interaction.Toolkit.Interactables". No XRI 2.x esse namespace não existe,
// então ele é declarado vazio aqui para que o "using" dos scripts compile nas duas versões.
// (A pasta Editor tem uma cópia deste arquivo, porque ela é compilada separadamente.)
namespace UnityEngine.XR.Interaction.Toolkit.Interactables
{
}
