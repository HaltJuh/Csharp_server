using System;
using System.Collections.Generic;
using System.Text;

namespace UnityServer
{
    //Acts like an enum, avoids the usage of magic strings.
    public class SendType
    {
        SendType(string _value) { value = _value; }
        public string value;

        public static SendType Move { get { return new SendType("MOVE"); } }
        public static SendType Connect { get { return new SendType("CONNECT"); } }
        public static SendType Instantiate { get { return new SendType("INSTANTIATE"); } }
        public static SendType Disconnect { get { return new SendType("DISCONNECT"); } }
        public static SendType Drop { get { return new SendType("DROP"); } }
        public static SendType Take { get { return new SendType("TAKE"); } }
        public static SendType Place { get { return new SendType("PLACE"); } }
        public static SendType PickUp { get { return new SendType("PICKUP"); } }
        public static SendType EndTurn { get { return new SendType("ENDTURN"); } }
        public static SendType YourTurn { get { return new SendType("YOURTURN"); } }
        public static SendType Attack { get { return new SendType("ATTACK"); } }
        public static SendType Damage { get { return new SendType("DAMAGE"); } }
        public static SendType Destroy { get { return new SendType("DESTROY"); } }
    }
}
