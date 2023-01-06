﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Waher.Persistence.Filters;

namespace Waher.Persistence.FullTextSearch.Keywords
{
	/// <summary>
	/// Represents a wildcard keyword.
	/// </summary>
	public class RegexKeyword : Keyword
	{
		/// <summary>
		/// Represents a wildcard keyword.
		/// </summary>
		/// <param name="Keyword">Keyword</param>
		public RegexKeyword(string Expression)
		{
			this.Expression = Expression;
			this.Parsed = new Regex(Expression, RegexOptions.IgnoreCase | RegexOptions.Singleline);
		}

		/// <summary>
		/// String representation of regex expression.
		/// </summary>
		public string Expression { get; }

		/// <summary>
		/// Keyword
		/// </summary>
		public Regex Parsed { get; }

		/// <summary>
		/// Order complexity (within category) of keyword
		/// </summary>
		public override int OrderComplexity => this.Expression.Length;

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			return obj is RegexKeyword k && this.Expression == k.Expression;
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.Expression;
		}

		/// <summary>
		/// Gets available token references.
		/// </summary>
		/// <param name="Process">Current search process.</param>
		/// <returns>Enumerable set of token references.</returns>
		public override async Task<IEnumerable<KeyValuePair<string, TokenReferences>>> GetTokenReferences(SearchProcess Process)
		{
			string Preamble = FilterFieldLikeRegEx.GetRegExConstantPrefix(this.Expression, this.Parsed);
			LinkedList<KeyValuePair<string, TokenReferences>> Result = new LinkedList<KeyValuePair<string, TokenReferences>>();

			if (string.IsNullOrEmpty(Preamble))
			{
				int c = Process.Index.Count;
				string[] Keys = new string[c];
				Process.Index.Keys.CopyTo(Keys, 0);

				foreach (string Key in Keys)
				{
					Match M = this.Parsed.Match(Key);
					if (M.Success && M.Index == 0 && M.Length == Key.Length)
					{
						KeyValuePair<bool, object> P = await Process.Index.TryGetValueAsync(Key);
						if (P.Key && P.Value is TokenReferences References)
							Result.AddLast(new KeyValuePair<string, TokenReferences>(Key, References));
					}
				}
			}
			else
			{
				char[] Characters = Preamble.ToCharArray();
				Characters[Characters.Length - 1]++;
				string ToExclusive = new string(Characters);

				KeyValuePair<string, object>[] Records = await Process.Index.GetEntriesAsync(Preamble, ToExclusive);

				foreach (KeyValuePair<string, object> Rec in Records)
				{
					Match M = this.Parsed.Match(Rec.Key);
					if (M.Success && M.Index == 0 && M.Length == Rec.Key.Length && Rec.Value is TokenReferences References)
						Result.AddLast(new KeyValuePair<string, TokenReferences>(Rec.Key, References));
				}
			}

			return Result;
		}
	}
}
