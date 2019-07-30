#pragma warning disable 1591

namespace Sanakan.Services.SlotMachine
{
    public class SlotWickedRandom : ISlotRandom
    {
        public int Next(int min, int max)
        {
            double sum = 0;
            double rMax = 100;
            double[] chance = new double[max];
            for (int i = 0; i < (max + 1); i++) sum += i;
            for (int i = 0; i < max; i++) chance[i] = (max-i)*(rMax/sum);

            int low = 0;
            int high = 0;
            int next = Services.Fun.GetRandomValue(min, (int)(rMax * 10));
            for (int i = 0; i < max; i++)
            {
                if (i > 0) low = (int)(chance[i-1] * 10);
                high += (int)(chance[i] * 10);

                if (next >= low && next < high)
                    return i;
            }
            return 0;
        }
    }
}