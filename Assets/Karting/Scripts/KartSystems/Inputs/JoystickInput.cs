using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KartGame.KartSystems
{
    public class JoystickInput : MonoBehaviour, IInputFighter
    {
        public Joystick joystick;

        public DeltaMove deltaMove { get => this._deltaMove; }
        public bool IsPlayHitAction { get; }

        DeltaMove _deltaMove;
        // Update is called once per frame
        void Update()
        {
            _deltaMove = (DeltaMove)((int)joystick.Horizontal);
          //  m_Steering = (int)joystick.Horizontal;
        }
    }
}