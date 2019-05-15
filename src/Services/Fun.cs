#pragma warning disable 1591

using System;
using System.Security.Cryptography;

namespace Sanakan.Services
{
    public enum CoinSide
    {
        Head, Tail
    }

    public class Fun
    {
        private static RNGCryptoServiceProvider _rand = new RNGCryptoServiceProvider();

        public static int GetRandomValue(int max) => GetRandomValue(0, max);

        public static int GetRandomValue(int min, int max)
        {
            uint scale = uint.MaxValue;
            while (scale == uint.MaxValue)
            {
                byte[] bytes = new byte[4];
                _rand.GetBytes(bytes);

                scale = BitConverter.ToUInt32(bytes, 0);
            }

            return (int)(min + (max - min) * (scale / (double)uint.MaxValue));
        }

        public CoinSide RandomizeSide()
            => (CoinSide) GetRandomValue(2);
    }
}