#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;

/// <summary>
/// Lê teclado e mouse para os scripts de teste no PC. Funciona tanto com o Input System novo
/// (que o XR Interaction Toolkit usa) quanto com o Input Manager antigo.
/// </summary>
public static class EntradaDesktop
{
#if ENABLE_INPUT_SYSTEM
    /// <summary>WASD ou setas. X = lados, Y = frente/trás.</summary>
    public static Vector2 Movimento()
    {
        var teclado = Keyboard.current;
        if (teclado == null) return Vector2.zero;

        float x = Eixo(teclado.dKey.isPressed || teclado.rightArrowKey.isPressed,
                       teclado.aKey.isPressed || teclado.leftArrowKey.isPressed);
        float y = Eixo(teclado.wKey.isPressed || teclado.upArrowKey.isPressed,
                       teclado.sKey.isPressed || teclado.downArrowKey.isPressed);
        return new Vector2(x, y);
    }

    /// <summary>Quanto o mouse andou neste frame, em pixels.</summary>
    public static Vector2 DeltaMouse() => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;

    public static bool CorrerPressionado() =>
        Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

    public static bool CliquePressionado() => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

    /// <summary>Tecla E ou clique esquerdo.</summary>
    public static bool InteragirPressionado() =>
        (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) || CliquePressionado();

    public static bool SoltarMousePressionado() => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

    private static float Eixo(bool positivo, bool negativo) => (positivo ? 1f : 0f) - (negativo ? 1f : 0f);

#elif ENABLE_LEGACY_INPUT_MANAGER
    public static Vector2 Movimento() => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

    // O Input Manager antigo devolve o movimento do mouse dividido por ~10; multiplica para ficar igual ao Input System.
    public static Vector2 DeltaMouse() => new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f;

    public static bool CorrerPressionado() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

    public static bool CliquePressionado() => Input.GetMouseButtonDown(0);

    public static bool InteragirPressionado() => Input.GetKeyDown(KeyCode.E) || CliquePressionado();

    public static bool SoltarMousePressionado() => Input.GetKeyDown(KeyCode.Escape);

#else
    public static Vector2 Movimento() => Vector2.zero;
    public static Vector2 DeltaMouse() => Vector2.zero;
    public static bool CorrerPressionado() => false;
    public static bool CliquePressionado() => false;
    public static bool InteragirPressionado() => false;
    public static bool SoltarMousePressionado() => false;
#endif
}
