using System;

namespace DadaBot.Exceptions
{
    public class UnmatchedIdentifierException : Exception
    {
        public UnmatchedIdentifierException(string msg) : base (msg)
        { }

        public UnmatchedIdentifierException(string msg, Exception e) : base(msg, e)
        { }
    }
}
