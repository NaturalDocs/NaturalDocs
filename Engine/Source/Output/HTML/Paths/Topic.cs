﻿/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Paths.Topic
 * ____________________________________________________________________________
 *
 * Path functions relating to a single topic.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Paths
	{
	static public class Topic
		{

		/* Function: HashPath
		 *
		 * Returns the hash path for the topic.  When appending this to the hash path of a file or class use a colon to separate
		 * them.
		 *
		 * Examples:
		 *
		 *		topic - Member
		 *		topic + includeClass - Module.Module.Member
		 */
		static public string HashPath (Engine.Topics.Topic topic, bool includeClass = true)
			{
			// We want to work from Topic.Title instead of Topic.Symbol so that we can use the separator characters as originally
			// written, as opposed to having them normalized and condensed in the anchor.

			int titleParametersIndex = ParameterString.GetParametersIndex(topic.Title);

			StringBuilder hashPath;
			if (titleParametersIndex == -1)
				{
				hashPath = new StringBuilder(topic.Title);
				}
			else
				{
				hashPath = new StringBuilder(titleParametersIndex);
				hashPath.Append(topic.Title, 0, titleParametersIndex);
				}

			hashPath.Replace('\t', ' ');

			// Remove all whitespace unless it separates two text characters.
			int i = 0;
			while (i < hashPath.Length)
				{
				if (hashPath[i] == ' ')
					{
					if (i == 0 || i == hashPath.Length - 1)
						{  hashPath.Remove(i, 1);  }
					else if (Tokenizer.FundamentalTypeOf(hashPath[i - 1]) == FundamentalType.Text &&
								 Tokenizer.FundamentalTypeOf(hashPath[i + 1]) == FundamentalType.Text)
						{  i++;  }
					else
						{  hashPath.Remove(i, 1);  }
					}
				else
					{  i++;  }
				}

			// Add parentheses to distinguish between multiple symbols in the same file.
			// xxx this will be a problem when doing class hash paths as symboldefnumber is only unique to a file
			if (topic.SymbolDefinitionNumber != 1)
				{
				hashPath.Append('(');
				hashPath.Append(topic.SymbolDefinitionNumber);
				hashPath.Append(')');
				}

			// Add class if present and desired.
			// xxx when class id is included in topic test for that here, maybe instead of having a flag
			if (includeClass)
				{
				// Find the part of the symbol that isn't generated by the title, if any.
				string ignore;
				string titleSymbol = SymbolString.FromPlainText(topic.Title, out ignore).ToString();
				string fullSymbol = topic.Symbol.ToString();

				if (titleSymbol.Length < fullSymbol.Length &&
					 fullSymbol.Substring(fullSymbol.Length - titleSymbol.Length) == titleSymbol)
					{
					string classSymbol = fullSymbol.Substring(0, fullSymbol.Length - titleSymbol.Length);
					classSymbol = classSymbol.Replace(SymbolString.SeparatorChar, '.');

					// The class symbol should already have a trailing member operator.
					hashPath.Insert(0, classSymbol);
					}
				}

			return Utilities.Sanitize(hashPath.ToString());
			}

		}
	}
