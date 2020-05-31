using System;

namespace DadaBot.Exceptions
{
    public class AmbiguousIdentifierException : Exception
    {
        public AmbiguousIdentifierException(string msg) : base(msg) { }
        public AmbiguousIdentifierException(string msg, Exception e) : base(msg, e) { }        
    }
}
