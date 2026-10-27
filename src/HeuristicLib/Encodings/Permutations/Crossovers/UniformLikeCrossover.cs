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
/// Chooses unused parental values at each position and fills unresolved positions with unused values randomly.
/// </summary>
/// <remarks>Permutations with fewer than two elements are returned unchanged without random draws.</remarks>
public sealed record UniformLikeCrossover : SingleCandidateCrossover<Permutation>
{
    public override Permutation CrossParents(Parents<Permutation> parents, IRandomNumberGenerator random) =>
        Cross(parents.Parent1, parents.Parent2, random);

    public static Permutation Cross(Permutation parent1, Permutation parent2, IRandomNumberGenerator random)
    {
        if (parent1.Count < 2)
            return parent1;

        var length = parent1.Count;
        var child = new int[length];
        var numberCopied = new bool[length];
        var unoccupied = new List<int>();

        for (var i = 0; i < length; i++)
        {
            var copied = true;
            if (parent1[i] == parent2[i])
            {
                child[i] = parent1[i];
            }
            else if (!numberCopied[parent1[i]] && !numberCopied[parent2[i]])
            {
                child[i] = random.NextDouble() < 0.5 ? parent1[i] : parent2[i];
            }
            else if (!numberCopied[parent1[i]])
            {
                child[i] = parent1[i];
            }
            else if (!numberCopied[parent2[i]])
            {
                child[i] = parent2[i];
            }
            else
            {
                copied = false;
                unoccupied.Add(i);
            }
            if (copied)
                numberCopied[child[i]] = true;
        }

        if (unoccupied.Count > 0)
        {
            for (var i = 0; i < length; i++)
            {
                if (!numberCopied[i])
                {
                    var r = random.NextInt(unoccupied.Count);
                    child[unoccupied[r]] = i;
                    unoccupied.RemoveAt(r);
                }
                if (unoccupied.Count == 0)
                    break;
            }
        }

        return Permutation.FromOwnedArray(child);
    }
}
