using System;
using System.Collections.Generic;
using System.Text;

namespace UnityServer
{

    [Serializable]
    class PlayerData
    {
        public float XPos { get; set; }
        public float YPos { get; set; }
        public float ZPos { get; set; }
        public float Rotation { get; set; }
        public string Username { get; set; }

        public int Health { get; set; }

        public int unitsLeft;
        public PlayerData()
        {
            XPos = 0;
            YPos = 0;
            ZPos = 0;
            Rotation = 0;
            Username = "";
            Health = 100;
        }
        public MoveData ToMoveData()
        {
            return new MoveData(this);
        }
        public void SetMoveData(MoveData moveData)
        {
            XPos = moveData.XPos;
            YPos = moveData.YPos;
            ZPos = moveData.ZPos;
            Rotation = moveData.Rotation;
        }
        public void TakeDamage(int amount)
        {
            if (amount >= Health)
            {
                Health = 0;
                unitsLeft = 0;
                return;
            }

            Health -= amount;

        }
    }
}