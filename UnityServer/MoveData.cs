using System;
using System.Collections.Generic;
using System.Text;

namespace UnityServer
{
    [Serializable]
    class MoveData
    {
        public float XPos { get; set; }
        public float YPos { get; set; }
        public float ZPos { get; set; }
        public float Rotation { get; set; }
        public MoveData(int id)
        {
            XPos = 2*(id-1);
            YPos = 0.5f;
            ZPos = 0;
        }
        public MoveData(float x, float y, float z)
        {
            XPos = x;
            YPos = y;
            ZPos = z;
        }
        public MoveData(PlayerData data)
        {
            XPos = data.XPos;
            YPos = data.YPos;
            ZPos = data.ZPos;
            Rotation = data.Rotation;
        }
        public MoveData()
        {

        }
    }
}
