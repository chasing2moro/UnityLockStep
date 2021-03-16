using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KartGame.KartSystems
{
    /// <summary>
    /// A basic keyboard implementation of the IInput interface for all the input information a kart needs.
    /// </summary>
    public class KeyboardInput : MonoBehaviour
    {
        public int Acceleration
        {
            get { return m_Acceleration; }
        }
        public int Steering
        {
            get { return m_Steering; }
        }
        public bool BoostPressed
        {
            get { return m_BoostPressed; }
        }
        public bool FirePressed
        {
            get { return m_FirePressed; }
        }
        public bool HopPressed
        {
            get { return m_HopPressed; }
        }
        public bool HopHeld
        {
            get { return m_HopHeld; }
        }

        int m_Acceleration;
        int m_Steering;
        bool m_HopPressed;
        bool m_HopHeld;
        bool m_BoostPressed;
        bool m_FirePressed;

        bool m_FixedUpdateHappened;

        void Update ()
        {
            if (Input.GetKey (KeyCode.UpArrow))
                m_Acceleration = 1;
            else if (Input.GetKey (KeyCode.DownArrow))
                m_Acceleration = -1;
            else
                m_Acceleration = 0;

            if (Input.GetKey (KeyCode.LeftArrow) && !Input.GetKey (KeyCode.RightArrow))
                m_Steering = -1;
            else if (!Input.GetKey (KeyCode.LeftArrow) && Input.GetKey (KeyCode.RightArrow))
                m_Steering = 1;
            else
                m_Steering = 0;

            // TODO: LZ:
            //      not support the following controls right now
#if false
            m_HopHeld = Input.GetKey (KeyCode.Space);

            if (m_FixedUpdateHappened)
            {
                m_FixedUpdateHappened = false;

                m_HopPressed = false;
                m_BoostPressed = false;
                m_FirePressed = false;
            }

            m_HopPressed |= Input.GetKeyDown (KeyCode.Space);
            m_BoostPressed |= Input.GetKeyDown (KeyCode.RightShift);
            m_FirePressed |= Input.GetKeyDown (KeyCode.RightControl);
#endif
        }

        void FixedUpdate ()
        {
            m_FixedUpdateHappened = true;
        }
    }
}