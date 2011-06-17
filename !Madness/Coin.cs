using System;

namespace Madness
{
    class Coin
    {
        private static readonly Random Random = new Random(DateTime.Now.Millisecond);

        public static bool Toss()
        {
            return (Random.Next(2) == 0);
        }
    }
}
