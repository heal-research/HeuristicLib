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
/// Preserves a segment of the first parent and fills gaps in the linear order of the second parent.
/// </summary>
/// <remarks>Permutations with fewer than two elements are returned unchanged without random draws.</remarks>
public sealed record OrderCrossover2 : SingleCandidateCrossover<Permutation>
{
    public override Permutation CrossParents(Parents<Permutation> parents, IRandomNumberGenerator random) =>
        Cross(parents.Parent1, parents.Parent2, random);

    public static Permutation Cross(Permutation parent1, Permutation parent2, IRandomNumberGenerator random)
    {
        if (parent1.Count < 2)
            return parent1;

        var result = new int[parent1.Count];
        var copied = new bool[result.Length];

        var breakPoint1 = random.NextInt(result.Length - 1);
        var breakPoint2 = random.NextInt(breakPoint1 + 1, result.Length);

        for (var i = breakPoint1; i <= breakPoint2; i++)
        {
            // copy part of first permutation
            result[i] = parent1[i];
            copied[parent1[i]] = true;
        }

        var index = 0;
        for (var i = 0; i < parent2.Count; i++)
        {
            // copy remaining part of second permutation
            if (index == breakPoint1)
            {
                // skip already copied part
                index = breakPoint2 + 1;
            }
            if (!copied[parent2[i]])
            {
                result[index] = parent2[i];
                index++;
            }
        }
        return Permutation.FromOwnedArray(result);
    }
}
