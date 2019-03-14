﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Numerics;
using Newtonsoft.Json;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GNFSCore
{
	using IntegerMath;

	[DataContract]
	public class PolyRelationsSieveProgress
	{
		[DataMember]
		public int A { get; private set; }
		[DataMember]
		public int B { get; private set; }
		[DataMember]
		public int Quantity { get; private set; }
		[DataMember]
		public int ValueRange { get; private set; }

		public CancellationToken CancelToken;

		public List<List<Relation>> FreeRelations { get { return Relations.FreeRelations; } }
		public List<Relation> SmoothRelations { get { return Relations.SmoothRelations; } }
		public List<Relation> UnFactored { get { return Relations.UnFactoredRelations; } }
		public List<Relation> RoughRelations { get { return Relations.RoughRelations; } }

		public RelationContainer Relations { get; set; }

		internal GNFS _gnfs;

		#region Constructors

		public PolyRelationsSieveProgress()
		{
			Relations = new RelationContainer();
		}

		public PolyRelationsSieveProgress(GNFS gnfs)
		{
			_gnfs = gnfs;

			using (CancellationTokenSource cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(_gnfs.CancelToken))
			{
				CancelToken = cancellationSource.Token;
			}

			Relations = new RelationContainer();
		}

		public PolyRelationsSieveProgress(GNFS gnfs, int quantity, int valueRange)
			: this(gnfs)
		{
			A = 0;
			B = 3;
			Quantity = quantity;
			ValueRange = valueRange;
		}

		public PolyRelationsSieveProgress(GNFS gnfs, int a, int b, int quantity, int valueRange, List<List<Relation>> free, List<Relation> smooth, List<Relation> rough, List<Relation> unfactored)
			: this(gnfs, quantity, valueRange)
		{
			A = a;
			B = b;

			Relations.FreeRelations = free;
			Relations.SmoothRelations = smooth;
			Relations.RoughRelations = rough;
			Relations.UnFactoredRelations = unfactored;
		}

		#endregion

		#region Processing / Computation

		public void GenerateRelations(CancellationToken cancelToken)
		{
			if (Quantity == -1)
			{
				Quantity = _gnfs.RationalFactorPairCollection.Count + _gnfs.AlgebraicFactorPairCollection.Count + _gnfs.QuadradicFactorPairCollection.Count + 1;
			}
			else if (Relations.SmoothRelations.Count >= Quantity)
			{
				Quantity += 2000;
			}

			if (A >= ValueRange)
			{
				ValueRange += 100;
			}

			int adjustedRange = ValueRange % 2 == 0 ? ValueRange + 1 : ValueRange;
			A = (A % 2 == 0) ? A + 1 : A;

			int maxB = Math.Min(adjustedRange * 2, Quantity);

			if (B >= maxB)
			{
				maxB += 100;
			}

			while (Relations.SmoothRelations.Count < Quantity)
			{
				if (cancelToken.IsCancellationRequested)
				{
					break;
				}

				if (B > maxB)
				{
					break;
				}

				foreach (int a in SieveRange.GetSieveRangeContinuation(A, ValueRange))
				{
					if (cancelToken.IsCancellationRequested)
					{
						break;
					}

					A = a;
					if (GCD.AreCoprime(A, B))
					{
						Relation rel = new Relation(_gnfs, A, B);

						rel.Sieve(_gnfs.CurrentRelationsProgress);

						bool smooth = rel.IsSmooth;
						if (smooth)
						{
							_gnfs.CurrentRelationsProgress.Relations.SmoothRelations.Add(rel);
						}
						else
						{
							_gnfs.CurrentRelationsProgress.Relations.RoughRelations.Add(new Relation(rel));
						}
					}
				}

				if (cancelToken.IsCancellationRequested)
				{
					break;
				}

				B += 2;
				A = 1;
			}
		}

		#endregion

		#region Misc

		public void PurgePrimeRoughRelations()
		{
			List<Relation> roughRelations = Relations.RoughRelations.ToList();

			IEnumerable<Relation> toRemoveAlg = roughRelations
				.Where(r => r.AlgebraicQuotient != 1 && FactorizationFactory.IsProbablePrime(r.AlgebraicQuotient));

			roughRelations = roughRelations.Except(toRemoveAlg).ToList();

			Relations.RoughRelations = roughRelations;

			IEnumerable<Relation> toRemoveRational = roughRelations
				.Where(r => r.RationalQuotient != 1 && FactorizationFactory.IsProbablePrime(r.RationalQuotient));

			roughRelations = roughRelations.Except(toRemoveRational).ToList();

			Relations.RoughRelations = roughRelations;
		}

		public void AddFreeRelations(List<List<Relation>> freeRelations)
		{
			Relations.FreeRelations.AddRange(freeRelations);
		}

		#endregion

		#region ToString

		public string FormatRelations(IEnumerable<Relation> relations)
		{
			StringBuilder result = new StringBuilder();

			result.AppendLine($"Smooth relations:");
			result.AppendLine("\t_______________________________________________");
			result.AppendLine($"\t|   A   |  B | ALGEBRAIC_NORM | RATIONAL_NORM | \t\tQuantity: {Relations.SmoothRelations.Count} Target quantity: {(_gnfs.RationalFactorPairCollection.Count + _gnfs.AlgebraicFactorPairCollection.Count + _gnfs.QuadradicFactorPairCollection.Count + 1).ToString()}");
			result.AppendLine("\t```````````````````````````````````````````````");
			foreach (Relation rel in relations.OrderByDescending(rel => rel.A * rel.B))
			{
				result.AppendLine(rel.ToString());
				result.AppendLine("Algebraic " + rel.AlgebraicFactorization.FormatStringAsFactorization());
				result.AppendLine("Rational  " + rel.RationalFactorization.FormatStringAsFactorization());
				result.AppendLine();
			}
			result.AppendLine();

			return result.ToString();
		}

		public override string ToString()
		{
			if (Relations.FreeRelations.Any())
			{
				StringBuilder result = new StringBuilder();

				List<Relation> relations = Relations.FreeRelations.First();

				result.AppendLine(FormatRelations(relations));

				BigInteger algebraic = relations.Select(rel => rel.AlgebraicNorm).Product();
				BigInteger rational = relations.Select(rel => rel.RationalNorm).Product();

				bool isAlgebraicSquare = algebraic.IsSquare();
				bool isRationalSquare = rational.IsSquare();

				CountDictionary algCountDict = new CountDictionary();
				foreach (var rel in relations)
				{
					algCountDict.Combine(rel.AlgebraicFactorization);
				}

				result.AppendLine("---");
				result.AppendLine($"Rational  ∏(a+mb): IsSquare? {isRationalSquare} : {rational}");
				result.AppendLine($"Algebraic ∏ƒ(a/b): IsSquare? {isAlgebraicSquare} : {algebraic}");
				result.AppendLine();
				result.AppendLine($"Algebraic factorization (as prime ideals): {algCountDict.FormatStringAsFactorization()}");
				result.AppendLine();

				result.AppendLine();
				result.AppendLine("");
				result.AppendLine(string.Join(Environment.NewLine,
					relations.Select(rel =>
					{
						BigInteger f = _gnfs.CurrentPolynomial.Evaluate((BigInteger)rel.A);
						if (rel.B == 0)
						{
							return "";
						}
						return $"ƒ({rel.A}) ≡ {f} ≡ {(f % rel.B)} (mod {rel.B})";
					}
					)));
				result.AppendLine();



				return result.ToString();
			}
			else
			{
				return FormatRelations(Relations.SmoothRelations);
			}
		}

		#endregion
	}
}
