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
/// Copies position cycles alternately from the two parents, choosing the first parent randomly.
/// </summary>
/// <remarks>Permutations with fewer than two elements are returned unchanged without random draws.</remarks>
public sealed record CyclicCrossover : SingleCandidateCrossover<Permutation>
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
        var invParent1 = new int[length];
        var invParent2 = new int[length];

        // calculate inverse mappings (number -> index) for parent1 and parent2

        for (var i = 0; i < length; i++)
        {
            invParent1[parent1[i]] = i;
            invParent2[parent2[i]] = i;
        }

        // randomly choose whether to start copying from parent1 or parent2
        var copyFromParent1 = random.NextDouble() >= 0.5;

        var j = 0;
        do
        {
            do
            {
                if (copyFromParent1)
                {
                    result[j] = parent1[j]; // copy number at position j from parent1
                    indexCopied[j] = true;
                    j = invParent2[result[j]]; // set position j to the position of the copied number in parent2
                }
                else
                {
                    result[j] = parent2[j]; // copy number at position j from parent2
                    indexCopied[j] = true;
                    j = invParent1[result[j]]; // set position j to the position of the copied number in parent1
                }
            } while (!indexCopied[j]);
            copyFromParent1 = !copyFromParent1;
            j = 0;
            while (j < length && indexCopied[j])
                j++;
        } while (j < length);

        return Permutation.FromOwnedArray(result);
    }
}
