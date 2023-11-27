using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

namespace GNFSCore.Matrix
{
	using IntegerMath;

	public class GaussianMatrix
	{
		public List<bool[]> Matrix { get { return M; } }
		public bool[] FreeVariables { get { return freeCols; } }

		public int RowCount { get { return M.Count; } }
		public int ColumnCount { get { return M.Any() ? M.First().Length : 0; } }

		public List<bool[]> A;
		public List<bool[]> M;
		private bool[] freeCols;
		private bool eliminationStep;

		private GNFS _gnfs;
		private List<Relation> relations;
		public Dictionary<int, Relation> ColumnIndexRelationDictionary;
		private List<Tuple<Relation, bool[]>> relationMatrixTuple;

		public GaussianMatrix(GNFS gnfs, List<Relation> rels)
		{
			_gnfs = gnfs;
			relationMatrixTuple = new List<Tuple<Relation, bool[]>>();
			eliminationStep = false;
			freeCols = new bool[0];
			M = new List<bool[]>();

			relations = rels;
			relations.Sort(delegate (Relation r1, Relation r2)
			{
				var a1 = Math.Abs((int)r1.A);
				var a2 = Math.Abs((int)r2.A);
				if (r1.B < r2.B) return -1;
				if (r1.B > r2.B) return 1;
				if (a1 < a2) return -1;
				if (a1 > a2) return 1;
				if (r1.A < r2.A) return 1;
				if (r1.A > r2.A) return -1;
				return 0;
			});

			List<GaussianRow> relationsAsRows = new List<GaussianRow>();

			foreach (Relation rel in relations)
			{
				GaussianRow row = new GaussianRow(_gnfs, rel);

				relationsAsRows.Add(row);
			}

			//List<GaussianRow> orderedRows = relationsAsRows.OrderBy(row1 => row1.LastIndexOfAlgebraic).ThenBy(row2 => row2.LastIndexOfQuadratic).ToList();

			List<GaussianRow> selectedRows = relationsAsRows.Take(_gnfs.CurrentRelationsProgress.SmoothRelationsRequiredForMatrixStep).ToList();

			/*int maxIndexRat = selectedRows.Select(row => row.LastIndexOfRational).Max();
			int maxIndexAlg = selectedRows.Select(row => row.LastIndexOfAlgebraic).Max();
			int maxIndexQua = selectedRows.Select(row => row.LastIndexOfQuadratic).Max();

			foreach (GaussianRow row in selectedRows)
			{
				row.ResizeRationalPart(maxIndexRat);
				row.ResizeAlgebraicPart(maxIndexAlg);
				row.ResizeQuadraticPart(maxIndexQua);
			}*/

			GaussianRow exampleRow = selectedRows.First();
			int newLength = exampleRow.GetBoolArray().Length;

			newLength++;

			//selectedRows = selectedRows.Take(newLength).ToList();


			foreach (GaussianRow row in selectedRows)
			{
				relationMatrixTuple.Add(new Tuple<Relation, bool[]>(row.SourceRelation, row.GetBoolArray()));
			}
		}

		public void PrintMatrix(List<bool[]> mat)
		{
			string s;
			for(int r = 0; r < mat.Count; ++r)
			{
				if (r % 8 == 0)
					GNFS.LogFunction("");

				s = GaussianRow.ToBinaryString(mat[r]);
				GNFS.LogFunction(s);
			}
			GNFS.LogFunction("");
			GNFS.LogFunction("");
		}
		public void NoTranspose()
		{
			List<bool[]> result = new List<bool[]>();
			
			int index = 0;
			int numRows = relationMatrixTuple.Count;
			while (index < numRows)
			{
				List<bool> newRow = relationMatrixTuple[index].Item2.ToList();
				result.Add(newRow.ToArray());

				index++;
			}
			A = result;
		}
		public void TransposeAppend()
		{
			List<bool[]> result = new List<bool[]>();
			ColumnIndexRelationDictionary = new Dictionary<int, Relation>();

			int index = 0;
			int numRows = relationMatrixTuple[0].Item2.Length;
			while (index < numRows)
			{
				ColumnIndexRelationDictionary.Add(index, relationMatrixTuple[index].Item1);

				List<bool> newRow = relationMatrixTuple.Select(bv => bv.Item2[index]).ToList();
				newRow.Add(false);
				result.Add(newRow.ToArray());

				index++;
			}

			M = result;
			freeCols = new bool[M.Count];
		}
		static bool[] ConvertBinaryStringToBoolArray(string binaryString)
		{
			bool[] boolArray = new bool[binaryString.Length];

			for (int i = 0; i < binaryString.Length; i++)
			{
				// Convert each character to a boolean value
				boolArray[i] = binaryString[i] == '1';
			}

			return boolArray;
		}
		public void CreateMat()
		{
			string m = "00000000000000100000000000000000000000000000000x11001000011100100000000100100000000100000100100x01100001000100111001100000000100001011001100000x01000100000000010010011010100000000011001011000x00000100000010100100000001000010000000000000010x00100000100000000000000001110000110000000000000x00000001000000000010000000010001100000000000000x00001000000000000000000010000000000010110000000x00000000000000000001011000000000011000010100010x00000000001001000000000000000000000000001010000x00010000000000000000100000000010000100000000000x00110011000010000000001001001010010010000000000x10000011011000000000000000000001100000011001000x00000100010000000000000000000100001000000000000x00000000000000010100000000001101010100100001000x00001000000011000000000000000000010000001000000x00000000100000000001000000000001000000100001000x00000000000000010010000001000000000000100010010x00000000010000001000000000000000000010000100100x01000000000000000000000000000011000000000000000x00000000001000000000000001001000000000000001000x00010000000000000000000000000000000000000000100x00000010000001100000000000000100000000000000000x00000000000100000000001000000010000000000010000x00000000000100000000000000010000000100000000000x00000000000000000000010000000000000000000000100x00000000000010000000000000100000000000000000000x00000000000000100000000000000000100001000000000x00000000000001000000010100000000000000000001000x00000000000000000000000010010001001001000010000x00000000000000100000000000000000000000000000010x10110000110110100111000011011100101111111010000x01101100010010010100101010000001100111100111100x10010100001101010101011101001010011100001000100x10111110100001000000000100000010010110010000010x01010010110100111100001001011111101111100100100x00110111111110110100101110111011010011111101100";
			string[] lines = m.Split('x');

			List<bool[]> result = new List<bool[]>();
			for(int i = 0; i < lines.Length; ++i)
			{
				var b = ConvertBinaryStringToBoolArray(lines[i]);
				result.Add(b);
			}
			M = result;
			freeCols = new bool[M.Count];
		}

		public void Elimination()
		{
			if (eliminationStep)
			{
				return;
			}

			int numRows = RowCount;
			int numCols = ColumnCount;

			freeCols = Enumerable.Repeat(false, numCols).ToArray();

			int h = 0;

			for (int i = 0; i < numRows && h < numCols; i++)
			{
				bool next = false;

				if (M[i][h] == false)
				{
					int t = i + 1;

					while (t < numRows && M[t][h] == false)
					{
						t++;
					}

					if (t < numRows)
					{
						//swap rows M[i] and M[t]

						bool[] temp = M[i];
						M[i] = M[t];
						M[t] = temp;
						temp = null;
					}
					else
					{
						freeCols[h] = true;
						i--;
						next = true;
					}
				}
				if (next == false)
				{
					for (int j = i + 1; j < numRows; j++)
					{
						if (M[j][h] == true)
						{
							// Add rows
							// M [j] ← M [j] + M [i]

							M[j] = Add(M[j], M[i]);
						}
					}
					for (int j = 0; j < i; j++)
					{
						if (M[j][h] == true)
						{
							// Add rows
							// M [j] ← M [j] + M [i]

							M[j] = Add(M[j], M[i]);
						}
					}
				}
				h++;
			}

			eliminationStep = true;
		}

		public List<Relation> GetSolutionSet(int numberOfSolutions)
		{
			bool[] solutionSet = GetSolutionFlags(numberOfSolutions);

			int index = 0;
			int max = ColumnIndexRelationDictionary.Count;

			List<Relation> result = new List<Relation>();
			while (index < max)
			{
				if (solutionSet[index] == true)
				{
					result.Add(ColumnIndexRelationDictionary[index]);
				}

				index++;
			}

			return result;
		}

		private bool[] GetSolutionFlags(int numSolutions)
		{
			if (!eliminationStep)
			{
				throw new Exception("Must call Elimination() method first!");
			}

			if (numSolutions < 1)
			{
				throw new ArgumentException($"{nameof(numSolutions)} must be greater than 1.");
			}

			int numRows = RowCount;
			int numCols = ColumnCount;

			if (numSolutions >= numCols)
			{
				throw new ArgumentException($"{nameof(numSolutions)} must be less than the column count.");
			}

			bool[] result = new bool[numCols];

			int j = -1;
			int i = numSolutions;

			while (i > 0)
			{
				j++;

				while (freeCols[j] == false)
				{
					j++;
				}

				i--;
			}

			result[j] = true;

			for (i = 0; i < numRows - 1; i++)
			{
				if (M[i][j] == true)
				{
					int h = i;
					while (h < j)
					{
						if (M[i][h] == true)
						{
							result[h] = true;
							break;
						}
						h++;
					}
				}
			}

			return result;
		}

		public static bool[] Add(bool[] left, bool[] right)
		{
			if (left.Length != right.Length) throw new ArgumentException($"Both vectors must have the same length.");

			int length = left.Length;
			bool[] result = new bool[length];

			int index = 0;
			while (index < length)
			{
				result[index] = left[index] ^ right[index];
				index++;
			}

			return result;
		}

		public static string VectorToString(bool[] vector)
		{
			return string.Join(",", vector.Select(b => b ? '1' : '0'));
		}

		public static string MatrixToString(List<bool[]> matrix)
		{
			return string.Join(Environment.NewLine, matrix.Select(i => VectorToString(i)));
		}

		public override string ToString()
		{
			return MatrixToString(M);
		}
	}
}