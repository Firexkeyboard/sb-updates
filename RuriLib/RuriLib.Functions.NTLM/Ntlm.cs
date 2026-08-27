namespace RuriLib.Functions.NTLM;

public static class Ntlm
{
	public static string Generate(string key)
	{
		char[] array = new char[16]
		{
			'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
			'A', 'B', 'C', 'D', 'E', 'F'
		};
		uint[] array2 = new uint[16];
		uint[] array3 = new uint[4];
		char[] array4 = new char[32];
		int i = 0;
		int length;
		for (length = key.Length; i < length / 2; i++)
		{
			array2[i] = key[2 * i] | ((uint)key[2 * i + 1] << 16);
		}
		if (length % 2 == 1)
		{
			array2[i] = (uint)(key[length - 1] | 0x800000);
		}
		else
		{
			array2[i] = 128u;
		}
		array2[14] = (uint)(length << 4);
		uint num = 1732584193u;
		uint num2 = 4023233417u;
		uint num3 = 2562383102u;
		uint num4 = 271733878u;
		num += (num4 ^ (num2 & (num3 ^ num4))) + array2[0];
		num = (num << 3) | (num >> 29);
		num4 += (num3 ^ (num & (num2 ^ num3))) + array2[1];
		num4 = (num4 << 7) | (num4 >> 25);
		num3 += (num2 ^ (num4 & (num ^ num2))) + array2[2];
		num3 = (num3 << 11) | (num3 >> 21);
		num2 += (num ^ (num3 & (num4 ^ num))) + array2[3];
		num2 = (num2 << 19) | (num2 >> 13);
		num += (num4 ^ (num2 & (num3 ^ num4))) + array2[4];
		num = (num << 3) | (num >> 29);
		num4 += (num3 ^ (num & (num2 ^ num3))) + array2[5];
		num4 = (num4 << 7) | (num4 >> 25);
		num3 += (num2 ^ (num4 & (num ^ num2))) + array2[6];
		num3 = (num3 << 11) | (num3 >> 21);
		num2 += (num ^ (num3 & (num4 ^ num))) + array2[7];
		num2 = (num2 << 19) | (num2 >> 13);
		num += (num4 ^ (num2 & (num3 ^ num4))) + array2[8];
		num = (num << 3) | (num >> 29);
		num4 += (num3 ^ (num & (num2 ^ num3))) + array2[9];
		num4 = (num4 << 7) | (num4 >> 25);
		num3 += (num2 ^ (num4 & (num ^ num2))) + array2[10];
		num3 = (num3 << 11) | (num3 >> 21);
		num2 += (num ^ (num3 & (num4 ^ num))) + array2[11];
		num2 = (num2 << 19) | (num2 >> 13);
		num += (num4 ^ (num2 & (num3 ^ num4))) + array2[12];
		num = (num << 3) | (num >> 29);
		num4 += (num3 ^ (num & (num2 ^ num3))) + array2[13];
		num4 = (num4 << 7) | (num4 >> 25);
		num3 += (num2 ^ (num4 & (num ^ num2))) + array2[14];
		num3 = (num3 << 11) | (num3 >> 21);
		num2 += (num ^ (num3 & (num4 ^ num))) + array2[15];
		num2 = (num2 << 19) | (num2 >> 13);
		num += ((num2 & (num3 | num4)) | (num3 & num4)) + array2[0] + 1518500249;
		num = (num << 3) | (num >> 29);
		num4 += ((num & (num2 | num3)) | (num2 & num3)) + array2[4] + 1518500249;
		num4 = (num4 << 5) | (num4 >> 27);
		num3 += ((num4 & (num | num2)) | (num & num2)) + array2[8] + 1518500249;
		num3 = (num3 << 9) | (num3 >> 23);
		num2 += ((num3 & (num4 | num)) | (num4 & num)) + array2[12] + 1518500249;
		num2 = (num2 << 13) | (num2 >> 19);
		num += ((num2 & (num3 | num4)) | (num3 & num4)) + array2[1] + 1518500249;
		num = (num << 3) | (num >> 29);
		num4 += ((num & (num2 | num3)) | (num2 & num3)) + array2[5] + 1518500249;
		num4 = (num4 << 5) | (num4 >> 27);
		num3 += ((num4 & (num | num2)) | (num & num2)) + array2[9] + 1518500249;
		num3 = (num3 << 9) | (num3 >> 23);
		num2 += ((num3 & (num4 | num)) | (num4 & num)) + array2[13] + 1518500249;
		num2 = (num2 << 13) | (num2 >> 19);
		num += ((num2 & (num3 | num4)) | (num3 & num4)) + array2[2] + 1518500249;
		num = (num << 3) | (num >> 29);
		num4 += ((num & (num2 | num3)) | (num2 & num3)) + array2[6] + 1518500249;
		num4 = (num4 << 5) | (num4 >> 27);
		num3 += ((num4 & (num | num2)) | (num & num2)) + array2[10] + 1518500249;
		num3 = (num3 << 9) | (num3 >> 23);
		num2 += ((num3 & (num4 | num)) | (num4 & num)) + array2[14] + 1518500249;
		num2 = (num2 << 13) | (num2 >> 19);
		num += ((num2 & (num3 | num4)) | (num3 & num4)) + array2[3] + 1518500249;
		num = (num << 3) | (num >> 29);
		num4 += ((num & (num2 | num3)) | (num2 & num3)) + array2[7] + 1518500249;
		num4 = (num4 << 5) | (num4 >> 27);
		num3 += ((num4 & (num | num2)) | (num & num2)) + array2[11] + 1518500249;
		num3 = (num3 << 9) | (num3 >> 23);
		num2 += ((num3 & (num4 | num)) | (num4 & num)) + array2[15] + 1518500249;
		num2 = (num2 << 13) | (num2 >> 19);
		num += (num4 ^ num3 ^ num2) + array2[0] + 1859775393;
		num = (num << 3) | (num >> 29);
		num4 += (num3 ^ num2 ^ num) + array2[8] + 1859775393;
		num4 = (num4 << 9) | (num4 >> 23);
		num3 += (num2 ^ num ^ num4) + array2[4] + 1859775393;
		num3 = (num3 << 11) | (num3 >> 21);
		num2 += (num ^ num4 ^ num3) + array2[12] + 1859775393;
		num2 = (num2 << 15) | (num2 >> 17);
		num += (num4 ^ num3 ^ num2) + array2[2] + 1859775393;
		num = (num << 3) | (num >> 29);
		num4 += (num3 ^ num2 ^ num) + array2[10] + 1859775393;
		num4 = (num4 << 9) | (num4 >> 23);
		num3 += (num2 ^ num ^ num4) + array2[6] + 1859775393;
		num3 = (num3 << 11) | (num3 >> 21);
		num2 += (num ^ num4 ^ num3) + array2[14] + 1859775393;
		num2 = (num2 << 15) | (num2 >> 17);
		num += (num4 ^ num3 ^ num2) + array2[1] + 1859775393;
		num = (num << 3) | (num >> 29);
		num4 += (num3 ^ num2 ^ num) + array2[9] + 1859775393;
		num4 = (num4 << 9) | (num4 >> 23);
		num3 += (num2 ^ num ^ num4) + array2[5] + 1859775393;
		num3 = (num3 << 11) | (num3 >> 21);
		num2 += (num ^ num4 ^ num3) + array2[13] + 1859775393;
		num2 = (num2 << 15) | (num2 >> 17);
		num += (num4 ^ num3 ^ num2) + array2[3] + 1859775393;
		num = (num << 3) | (num >> 29);
		num4 += (num3 ^ num2 ^ num) + array2[11] + 1859775393;
		num4 = (num4 << 9) | (num4 >> 23);
		num3 += (num2 ^ num ^ num4) + array2[7] + 1859775393;
		num3 = (num3 << 11) | (num3 >> 21);
		num2 += (num ^ num4 ^ num3) + array2[15] + 1859775393;
		num2 = (num2 << 15) | (num2 >> 17);
		array3[0] = num + 1732584193;
		array3[1] = num2 + 4023233417u;
		array3[2] = num3 + 2562383102u;
		array3[3] = num4 + 271733878;
		for (i = 0; i < 4; i++)
		{
			int j = 0;
			uint num5 = array3[i];
			for (; j < 4; j++)
			{
				uint num6 = num5 % 256;
				array4[i * 8 + j * 2 + 1] = array[num6 % 16];
				num6 /= 16;
				array4[i * 8 + j * 2] = array[num6 % 16];
				num5 /= 256;
			}
		}
		return string.Join(string.Empty, array4);
	}
}
