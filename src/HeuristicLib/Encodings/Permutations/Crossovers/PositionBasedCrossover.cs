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
/// Preserves randomly selected positions from the second parent and fills gaps in first-parent order.
/// </summary>
/// <remarks>Permutations with fewer than two elements are returned unchanged without random draws.</remarks>
public sealed record PositionBasedCrossover : SingleCandidateCrossover<Permutation>
{
    public override Permutation CrossParents(Parents<Permutation> parents, IRandomNumberGenerator random) =>
        Cross(parents.Parent1, parents.Parent2, random);

    public static Permutation Cross(Permutation parent1, Permutation parent2, IRandomNumberGenerator random)
    {
        if (parent1.Count < 2)
            return parent1;

        var length = parent1.Count;
        var result = new int[length];
        var randomPosition = new bool[length];
        var numberCopied = new bool[length];
        var randomPosNumber = random.NextInt(length);

        for (var i = 0; i < randomPosNumber; i++)
        {
            // generate random bit mask
            randomPosition[random.NextInt(length)] = true;
        }

        for (var i = 0; i < length; i++)
        {
            // copy numbers masked as true from second permutation
            if (randomPosition[i])
            {
                result[i] = parent2[i];
                numberCopied[parent2[i]] = true;
            }
        }

        var index = 0;
        for (var i = 0; i < length; i++)
        {
            // copy numbers masked as false from first permutation
            if (!numberCopied[parent1[i]])
            {
                if (randomPosition[index])
                {
                    while (randomPosition[index])
                    {
                        index++;
                    }
                }
                result[index] = parent1[i];
                index++;
            }
        }

        return Permutation.FromOwnedArray(result);
    }
}
