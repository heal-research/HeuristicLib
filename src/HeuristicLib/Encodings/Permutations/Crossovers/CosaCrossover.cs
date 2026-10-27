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
/// Introduces a successor edge from the second parent by reversing a cyclic segment of the first parent.
/// </summary>
/// <remarks>Permutations with fewer than two elements are returned unchanged without random draws.</remarks>
public sealed record CosaCrossover : SingleCandidateCrossover<Permutation>
{
    public override Permutation CrossParents(Parents<Permutation> parents, IRandomNumberGenerator random) =>
        Cross(parents.Parent1, parents.Parent2, random);

    public static Permutation Cross(Permutation parent1, Permutation parent2, IRandomNumberGenerator random)
    {
        if (parent1.Count < 2)
            return parent1;

        var length = parent1.Count;
        var result = new int[length];
        int crossPoint, startIndex, endIndex;

        crossPoint = random.NextInt(length);
        startIndex = (crossPoint + 1) % length;

        var i = 0;
        while (i < parent2.Count && parent2[i] != parent1[crossPoint])
        {
            // find index of cross point in second permutation
            i++;
        }
        var newEdge = parent2[(i + 1) % length]; // the number that follows the cross point number in parent2 is the new edge that we want to insert
        endIndex = 0;
        while (endIndex < parent1.Count && parent1[endIndex] != newEdge)
        {
            // find index of the new edge in the first permutation
            endIndex++;
        }

        if (startIndex <= endIndex)
        {
            // copy parent1 to child and reverse the order in between startIndex and endIndex
            for (i = 0; i < parent1.Count; i++)
            {
                if (i >= startIndex && i <= endIndex)
                {
                    result[i] = parent1[endIndex - i + startIndex];
                }
                else
                {
                    result[i] = parent1[i];
                }
            }
        }
        else
        {
            // startIndex > endIndex
            for (i = 0; i < parent1.Count; i++)
            {
                if (i >= startIndex || i <= endIndex)
                {
                    result[i] = parent1[(endIndex - i + startIndex + length) % length]; // add length to wrap around when dropping below index 0
                }
                else
                {
                    result[i] = parent1[i];
                }
            }
        }
        return Permutation.FromOwnedArray(result);
    }
}
