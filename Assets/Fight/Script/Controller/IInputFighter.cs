public interface IInputFighter
{
    /// <summary>
    /// Used for determining whether the kart should increase its forward speed.
    /// </summary>
    DeltaMove deltaMove { get; }
    bool IsPlayHitAction { get; }
}