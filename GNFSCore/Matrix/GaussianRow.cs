using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

namespace GNFSCore.Matrix
{
	using Factors;
	using IntegerMath;

	public class GaussianRow
	{
		public bool Sign { get; set; }

		public List<bool> RationalPart { get; set; }
		public List<bool> AlgebraicPart { get; set; }
		public List<bool> QuadraticPart { get; set; }

		public int RationalCount { get { return RationalPart.Count; } }
		public int AlgebraicCount { get { return AlgebraicPart.Count; } }
		public int QuadraticCount { get { return QuadraticPart.Count; } }
		public int LastIndexOfRational { get { return RationalPart.LastIndexOf(true); } }
		public int LastIndexOfAlgebraic { get { return AlgebraicPart.LastIndexOf(true); } }
		public int LastIndexOfQuadratic { get { return QuadraticPart.LastIndexOf(true); } }

		public Relation SourceRelation { get; private set; }

		public GaussianRow(GNFS gnfs, Relation relation)
		{
			SourceRelation = relation;

			if (relation.RationalNorm.Sign == -1)
			{
				Sign = true;
			}
			else
			{
				Sign = false;
			}

			FactorPairCollection qfb = gnfs.QuadraticFactorPairCollection;

			FactorPairCollection rational = gnfs.RationalFactorPairCollection;
			FactorPairCollection algebraic = gnfs.AlgebraicFactorPairCollection;

			RationalPart = GetVector(relation.RationalFactorization, rational).ToList();
			AlgebraicPart = GetVector(relation.AlgebraicFactorization, algebraic).ToList();
			QuadraticPart = qfb.Select(qf => QuadraticResidue.GetQuadraticCharacter(relation, qf)).ToList();

// 			var s = ToBinaryString(GetBoolArray());
// 			GNFS.LogFunction(s);
		}
		public static string ToBinaryString(bool[] row)
		{
			string s = "";
			for (int c = 0; c < row.Length; ++c)
			{
				if (c % 8 == 0)
					s += " ";
				if (row[c])
					s += "1";
				else
					s += "0";
			}
			return s;
		}
		//protected static bool[] GetVector(CountDictionary primeFactorizationDict, BigInteger maxValue)
		protected static bool[] GetVector(CountDictionary primeFactorizationDict, FactorPairCollection pairs)
		{
			bool[] result = new bool[pairs.Count];

			if (primeFactorizationDict.Any())
			{
				foreach (KeyValuePair<BigInteger, BigInteger> kvp in primeFactorizationDict)
				{
					if (kvp.Key == -1)
					{
						continue;
					}
					
					int index = pairs.PrimeIndex(kvp.Key);
					if (-1 == index)
						continue;
// 					if (kvp.Key > maxValue)
// 					{
// 						continue;
// 					}					
					if (kvp.Value % 2 == 0)
					{
						continue;
					}

					//int index = PrimeFactory.GetIndexFromValue(kvp.Key);
					result[index] = true;
				}
			}

			return result;
		}
				
		public bool[] GetBoolArray()
		{
			List<bool> result = new List<bool>() { Sign };
			result.AddRange(RationalPart);
			result.AddRange(AlgebraicPart);
			result.AddRange(QuadraticPart);
			//result.Add(false);
			return result.ToArray();
		}

		public void ResizeRationalPart(int size)
		{
			RationalPart = RationalPart.Take(size + 1).ToList();
		}

		public void ResizeAlgebraicPart(int size)
		{
			AlgebraicPart = AlgebraicPart.Take(size + 1).ToList();
		}

		public void ResizeQuadraticPart(int size)
		{
			QuadraticPart = QuadraticPart.Take(size + 1).ToList();
		}
	}
}

