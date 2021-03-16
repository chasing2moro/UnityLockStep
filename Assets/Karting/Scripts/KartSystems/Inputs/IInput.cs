using UnityEngine;

namespace KartGame.KartSystems
{
    /// <summary>
    /// An interface representing the input controls a kart needs.
    /// </summary>
    public interface IInput
    {
        /// <summary>
        /// Used for determining whether the kart should increase its forward speed.
        /// </summary>
        int Acceleration { get; }

        /// <summary>
        /// Used for turning the kart left and right.
        /// </summary>
        int Steering { get; }

        bool IsPlayHitAction { get; }

        // TODO: LZ:
        //      not support the following controls right now
#if false

        /// <summary>
        /// Not implemented by this template.  Potentially used for activating a boost.
        /// </summary>
        bool BoostPressed { get; }

        /// <summary>
        /// Not implemented by this template.  Potentially used for activating weapons/pickups.
        /// </summary>
        bool FirePressed { get; }

        /// <summary>
        /// Used for determining when the kart should hop.  Also used to initiate a drift.
        /// </summary>
        bool HopPressed { get; }

        /// <summary>
        /// Used to determine when a drift should continue.
        /// </summary>
        bool HopHeld { get; }
#endif
    }
}