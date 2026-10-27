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
/// Reorders selected values in the second parent according to their order in the first parent.
/// </summary>
/// <remarks>Permutations with fewer than two elements are returned unchanged without random draws.</remarks>
public sealed record OrderBasedCrossover : SingleCandidateCrossover<Permutation>
{
    public override Permutation CrossParents(Parents<Permutation> parents, IRandomNumberGenerator random) =>
        Cross(parents.Parent1, parents.Parent2, random);

    public static Permutation Cross(Permutation parent1, Permutation parent2, IRandomNumberGenerator random)
    {
        if (parent1.Count < 2)
            return parent1;

        var length = parent1.Count;
        var result = new int[length];
        var selectedNumbers = new int[random.NextInt(length + 1)];
        var numberSelected = new bool[length];
        int index, selectedIndex, currentIndex;

        for (var i = 0; i < selectedNumbers.Length; i++)
        {
            // select numbers for array
            index = 0;
            while (numberSelected[parent1[index]])
            {
                // find first valid index
                index++;
            }

            selectedIndex = random.NextInt(length - i);
            currentIndex = 0;
            while (index < parent1.Count && currentIndex != selectedIndex)
            {
                // find selected number
                index++;
                if (!numberSelected[parent1[index]])
                {
                    currentIndex++;
                }
            }
            numberSelected[parent1[index]] = true;
        }

        index = 0;
        for (var i = 0; i < parent1.Count; i++)
        {
            // copy selected numbers in array
            if (numberSelected[parent1[i]])
            {
                selectedNumbers[index] = parent1[i];
                index++;
            }
        }

        index = 0;
        for (var i = 0; i < result.Length; i++)
        {
            // copy rest of second permutation and selected numbers in order of first permutation
            if (numberSelected[parent2[i]])
            {
                result[i] = selectedNumbers[index];
                index++;
            }
            else
            {
                result[i] = parent2[i];
            }
        }
        return Permutation.FromOwnedArray(result);
    }
}
