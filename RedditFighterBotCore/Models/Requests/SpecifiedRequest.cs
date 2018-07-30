using System;
using System.Collections.Generic;

namespace RedditFighterBot
{
    public class SpecifiedRequest : IRequest
    {        
        private const int MAX_ROWS = 50;
        private int _UserRequestedSize;

        public bool IsPartialTable { get; private set; }

        public List<string> FighterNames { get; private set; }

        public int RequestSize
        {
            get
            {
                return _UserRequestedSize;
            }
            private set
            {
                if (value <= 0)
                {
                    _UserRequestedSize = 5;
                }
                else
                {
                    _UserRequestedSize = value;
                }

                if (_UserRequestedSize * FighterNames.Count > MAX_ROWS)
                {
                    _UserRequestedSize = (int)Math.Floor((double)(MAX_ROWS / FighterNames.Count));
                }
            }
        }

        public SpecifiedRequest(List<string> fighters, int requestSize)
        {
            IsPartialTable = true;
            FighterNames = fighters;
            RequestSize = requestSize;
        }
    }
}