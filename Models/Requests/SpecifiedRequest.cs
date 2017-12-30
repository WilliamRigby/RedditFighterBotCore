using RedditSharp.Things;
using System;
using System.Collections.Generic;

namespace RedditFighterBot
{
    public class SpecifiedRequest : IRequest
    {

        private const int MAX_ROWS = 50;

        public SpecifiedRequest(List<string> fighters, int request_size)
        {
            IsPartialTable = true;
            FighterNames = fighters;
            RequestSize = request_size;
        }

        private int _UserRequestedSize;

        public int RequestSize
        {
            get
            {
                return _UserRequestedSize;
            }
            set
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

        public bool IsPartialTable { get; set; }

        public List<string> FighterNames { get; set; }

    }
}