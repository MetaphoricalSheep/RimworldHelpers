namespace MetaphoricalSheep.Rimworld.Helpers
{
    public static class Random
    {
        private static readonly System.Random _random = new System.Random();

        /// <summary>
        /// Returns a random number between min and max, excluding the specified number
        /// </summary>
        /// <param name="min">The inclusive lower bound of the random number returned.</param>
        /// <param name="max">The exclusive upper bound of the random number returned.</param>
        /// <param name="exclude">The number to be excluded from the result</param>
        /// <returns></returns>
        public static int RandomExcluding(int min, int max, int exclude)
        {
            if (min == exclude)
            {
                max--;
            }

            if (max == exclude)
            {
                min++;
            }

            if (min > max || min == max)
            {
                return min;
            }

            var result = Next(min, max);

            if (result == exclude)
            {
                result = RandomExcluding(min, max, exclude);
            }

            return result;
        }

        /// <summary>
        /// Returns a random number between min and max
        /// </summary>
        /// <param name="min">The inclusive lower bound of the random number returned.</param>
        /// <param name="max">The exclusive upper bound of the random number returned.</param>
        /// <returns></returns>
        public static int Next(int min, int max)
        {
            return _random.Next(min, max);
        }
    }
}