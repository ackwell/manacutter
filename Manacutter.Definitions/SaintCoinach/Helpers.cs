namespace Manacutter.Definitions.SaintCoinach;

internal class Helpers {
	// TODO: Check if we need to look into optimisations e.g. suffix tree
	// Thanks, wikipedia
	internal static string LongestCommonSubsequence(string a, string b) {
		// Initalise table
		int[,] table = new int[a.Length + 1, b.Length + 1];

		// LCS algo
		int i;
		int j;
		for (i = 1; i <= a.Length; i++) {
			for (j = 1; j <= b.Length; j++) {
				table[i, j] = a[i - 1] == b[j - 1]
					? table[i, j] = table[i - 1, j - 1] + 1
					: table[i - 1, j] > table[i, j - 1]
						? table[i - 1, j]
						: table[i, j - 1];
			}
		}

		// Backtrack the table into a string
		var output = "";
		i = a.Length;
		j = b.Length;
		while (i > 0 && j > 0) {
			if (a[i - 1] == b[j - 1]) {
				output = a[i - 1] + output;
				i--;
				j--;
			} else if (table[i - 1, j] > table[i, j - 1]) {
				i--;
			} else {
				j--;
			}
		}

		return output;
	}
}
