using System;

namespace DadaBot.Exceptions
{
    public class WrongChannelTypeException : Exception
    {
        public WrongChannelTypeException(string msg) : base(msg) { }
    }
}
