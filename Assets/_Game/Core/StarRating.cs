using System;

namespace Blocktrotter.Core
{
    /// <summary>
    /// Stars for a solved level: 3 at or under the optimal move count, 2 within
    /// <c>optimal * twoStarRatio</c> (rounded up), otherwise 1.
    /// </summary>
    /// <example><code>int stars = StarRating.Calculate(movesUsed: 9, optimalMoves: 8, twoStarRatio: 1.5f); // 2</code></example>
    public static class StarRating
    {
        /// <summary>Returns 1–3 stars. The ratio comes from GameConfig, not a constant here.</summary>
        public static int Calculate(int movesUsed, int optimalMoves, float twoStarRatio)
        {
            if (movesUsed <= optimalMoves)
                return 3;
            int twoStarLimit = (int)Math.Ceiling(optimalMoves * twoStarRatio);
            return movesUsed <= twoStarLimit ? 2 : 1;
        }
    }
}
