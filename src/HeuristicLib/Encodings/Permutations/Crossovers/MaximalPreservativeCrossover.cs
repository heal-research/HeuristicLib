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
/// Preserves a cyclic segment, then follows unused successors from the second parent before the first.
/// </summary>
/// <remarks>Requires at least four elements.</remarks>
public sealed record MaximalPreservativeCrossover : SingleCandidateCrossover<Permutation>
{
    public override Permutation CrossParents(Parents<Permutation> parents, IRandomNumberGenerator random) =>
        Cross(parents.Parent1, parents.Parent2, random);

    public static Permutation Cross(Permutation parent1, Permutation parent2, IRandomNumberGenerator random)
    {
        if (parent1.Count < 4)
            throw new ArgumentException("MaximalPreservativeCrossover: The parent permutation must be at least of size 4.");
        var length = parent1.Count;
        var result = new int[length];
        var numberCopied = new bool[length];
        int breakPoint1, breakPoint2, subsegmentLength, index;

        subsegmentLength = random.NextInt(3, Math.Max(length / 3, 4)); // as mentioned in Pohlheim, H. Evolutionäre Algorithmen: Verfahren, Operatoren und Hinweise für die Praxis, 1999, p.44, Springer.
        breakPoint1 = random.NextInt(length);
        breakPoint2 = breakPoint1 + subsegmentLength;
        if (breakPoint2 >= length)
            breakPoint2 -= length;

        // copy string between position [breakPoint1, breakPoint2) from parent1 to the offspring
        index = breakPoint1;
        do
        {
            result[index] = parent1[index];
            numberCopied[result[index]] = true;
            index++;
            if (index >= length)
                index -= length;
        } while (index != breakPoint2);

        // calculate inverse permutation (number -> index) to help finding the follower of a given number
        var invParent1 = new int[length];
        var invParent2 = new int[length];

        for (var i = 0; i < length; i++)
        {
            invParent1[parent1[i]] = i;
            invParent2[parent2[i]] = i;
        }

        var prevIndex = index > 0 ? index - 1 : length - 1;
        do
        {
            // look for the follower of the last number in parent2
            var p2Follower = GetFollower(parent2, invParent2[result[prevIndex]]);
            if (!numberCopied[p2Follower])
            {
                result[index] = p2Follower;
            }
            else
            {
                // if that follower has already been added, look for the follower of the last number in parent1
                var p1Follower = GetFollower(parent1, invParent1[result[prevIndex]]);
                if (!numberCopied[p1Follower])
                {
                    result[index] = p1Follower;
                }
                else
                {
                    // if that has also been added, look for the next not already added number in parent2
                    var tempIndex = index;
                    for (var i = 0; i < parent2.Count; i++)
                    {
                        if (!numberCopied[parent2[tempIndex]])
                        {
                            result[index] = parent2[tempIndex];
                            break;
                        }
                        tempIndex++;
                        if (tempIndex >= parent2.Count)
                            tempIndex = 0;
                    }
                }
            }
            numberCopied[result[index]] = true;
            prevIndex = index;
            index++;
            if (index >= length)
                index -= length;
        } while (index != breakPoint1);

        return Permutation.FromOwnedArray(result);
    }

    private static int GetFollower(Permutation parent, int index) => parent[(index + 1) % parent.Count];
}
