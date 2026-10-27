#region License Information
/* HeuristicLab
 * Copyright (C) Heuristic and Evolutionary Algorithms Laboratory (HEAL)
 *
 * This file is part of HeuristicLab.
 *
 * HeuristicLab is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * HeuristicLab is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with HeuristicLab. If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.Permutations;

/// <summary>
/// Copies a segment from the first parent by exchanging mapped values in the second parent.
/// </summary>
/// <remarks>Requires at least four elements.</remarks>
public sealed record PartiallyMatchedCrossover : SingleCandidateCrossover<Permutation>
{
    public override Permutation CrossParents(Parents<Permutation> parents, IRandomNumberGenerator random) =>
        Cross(parents.Parent1, parents.Parent2, random);

    public static Permutation Cross(Permutation parent1, Permutation parent2, IRandomNumberGenerator random)
    {
        if (parent1.Count < 4)
            throw new ArgumentException("PartiallyMatchedCrossover: The parent permutation must be at least of size 4.");
        var length = parent1.Count;
        var result = new int[length];
        var invResult = new int[length];

        var breakPoint1 = random.NextInt(length - 1);
        // Keep at least two positions outside the copied segment without rejection sampling.
        var breakPoint2 = random.NextInt(breakPoint1 + 1, Math.Min(length, breakPoint1 + length - 2));

        // clone parent2 and calculate inverse permutation (number -> index)

        for (var j = 0; j < length; j++)
        {
            result[j] = parent2[j];
            invResult[result[j]] = j;
        }

        for (var j = breakPoint1; j <= breakPoint2; j++)
        {
            var orig = result[j]; // save the former value
            result[j] = parent1[j]; // overwrite the former value with the new value
            var index = invResult[result[j]]; // look where the new value is in the offspring
            result[index] = orig; // write the former value to this position
            invResult[orig] = index; // update the inverse mapping
                                     // it's not necessary to do 'invResult[result[j]] = j' as this will not be needed again
        }

        return Permutation.FromOwnedArray(result);
    }
}
