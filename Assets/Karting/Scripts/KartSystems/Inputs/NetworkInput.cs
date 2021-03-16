using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KartGame.KartSystems
{
    public class NetworkInput : MonoBehaviour
    {
        public int m_Acceleration;
        public int m_Steering;


        public int Acceleration => m_Acceleration;
        public int Steering => m_Steering;
    }
}