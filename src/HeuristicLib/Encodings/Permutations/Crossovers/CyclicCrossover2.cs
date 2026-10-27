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
/// Copies one randomly selected position cycle from the first parent and all other positions from the second.
/// </summary>
/// <remarks>Permutations with fewer than two elements are returned unchanged without random draws.</remarks>
public sealed record CyclicCrossover2 : SingleCandidateCrossover<Permutation>
{
    public override Permutation CrossParents(Parents<Permutation> parents, IRandomNumberGenerator random) =>
        Cross(parents.Parent1, parents.Parent2, random);

    public static Permutation Cross(Permutation parent1, Permutation parent2, IRandomNumberGenerator random)
    {
        if (parent1.Count < 2)
            return parent1;

        var length = parent1.Count;
        var result = new int[length];
        var indexCopied = new bool[length];
        int j, number;

        j = random.NextInt(length);  // get number to start cycle
        while (!indexCopied[j])
        {
            // copy whole cycle to new permutation
            result[j] = parent1[j];
            number = parent2[j];
            indexCopied[j] = true;

            j = 0;
            while (j < length && parent1[j] != number)
            {
                // search copied number in second permutation
                j++;
            }
        }

        for (var i = 0; i < length; i++)
        {
            // copy rest of second permutation to new permutation
            if (!indexCopied[i])
            {
                result[i] = parent2[i];
            }
        }
        return Permutation.FromOwnedArray(result);
    }
}
