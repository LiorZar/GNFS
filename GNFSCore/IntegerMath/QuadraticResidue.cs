using System;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GNFSCore.IntegerMath
{
	using Factors;

	public class QuadraticResidue
	{
		// a^(p-1)/2 ≡ 1 (mod p)
		public static BigInteger IsQuadraticResidue(BigInteger a, BigInteger p)
		{
			BigInteger quotient = BigInteger.Divide(p - 1, 2);
			BigInteger modPow = BigInteger.ModPow(a, quotient, p);

			return modPow;
		}

		public static bool GetQuadraticCharacter(Relation rel, FactorPair quadraticFactor)
		{
			// 			BigInteger ab = rel.A + rel.B;
			// 			BigInteger abp = BigInteger.Abs(BigInteger.Multiply(ab, quadraticFactor.P));
			// 
			// 			int legendreSymbol = Legendre.Symbol(abp, quadraticFactor.R);
			// 			return (legendreSymbol != 1);

			BigInteger a = rel.A;
			BigInteger b = rel.B;
			BigInteger a_plus_sb = BigInteger.Abs(a + BigInteger.Multiply(b, quadraticFactor.R));

			int legendreSymbol = Legendre.Symbol(a_plus_sb, quadraticFactor.P);
			BigInteger rv = IsQuadraticResidue(a_plus_sb, quadraticFactor.P);
			if ((1 == rv && 1 != legendreSymbol) || (1 != rv && 1 == legendreSymbol))
				legendreSymbol++;
			return (legendreSymbol != 1);
		}
	}
}
