using System;
using System.Collections.Generic;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Where each seat sits on screen (INTERFACE.md 3.1): the viewer at the bottom, the opponents facing them in an
    /// arc, in turn order as around a real table played clockwise. The seat playing after the viewer is on the left,
    /// the one playing before on the right, so the viewer's neighbours are the two ends of the arc.
    /// </summary>
    public static class SeatLayout
    {
        /// <summary>The opponents of <paramref name="viewer"/>, from the left end of the arc to the right end.</summary>
        public static IReadOnlyList<int> Opponents(int seatCount, int viewer)
        {
            if (seatCount < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(seatCount), "A table has at least two seats.");
            }

            if (viewer < 0 || viewer >= seatCount)
            {
                throw new ArgumentOutOfRangeException(nameof(viewer));
            }

            var opponents = new List<int>(seatCount - 1);
            for (int step = 1; step < seatCount; step++)
            {
                opponents.Add((viewer + step) % seatCount);
            }

            return opponents;
        }

        /// <summary>
        /// Angle in degrees of each place of the arc, from the left end (180 - margin) to the right end (margin); a
        /// single opponent sits in the middle (90).
        /// </summary>
        public static float ArcAngle(int index, int opponentCount, float margin)
        {
            if (opponentCount < 1 || index < 0 || index >= opponentCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (opponentCount == 1)
            {
                return 90f;
            }

            float span = 180f - (2f * margin);
            return 180f - margin - (span * index / (opponentCount - 1));
        }
    }
}
