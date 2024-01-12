/*
 * Class: CodeClear.NaturalDocs.Engine.Output.ResourceProcessors.JavaScript
 * ____________________________________________________________________________
 *
 * A class used to process JavaScript files, such as performing substitutions and removing whitespace.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Tokenization;

namespace CodeClear.NaturalDocs.Engine.Output.ResourceProcessors
	{
	public class JavaScript : ResourceProcessor
		{

		// Group: Functions
		// __________________________________________________________________________

		public JavaScript()  : base ()
			{
			this.LineCommentStrings = new string[] { "//" };
			this.BlockCommentStringPairs = new string[] { "/*", "*/" };
			this.QuoteCharacters = new char[] { '"', '\'' };
			}

		override public string Process (string javascript, bool shrink = true)
			{
			Tokenizer source = new Tokenizer(javascript);
			StringToStringTable substitutions = FindSubstitutions(source);

			source = ApplySubstitutions(source, substitutions);

			if (!shrink)
				{  return source.RawText;  }


			// Search comments for sections to include in the output

			StringBuilder output = new StringBuilder(javascript.Length);

			string includeInOutput = FindIncludeInOutput( GetPossibleDocumentationComments(source) );

			if (includeInOutput != null)
				{
				output.AppendLine("/*");
				output.Append(includeInOutput);
				output.AppendLine("*/");
				output.AppendLine();
				}


			// Shrink the source

			TokenIterator iterator = source.FirstToken;
			string spaceSeparatedSymbols = "+-";

			while (iterator.IsInBounds)
				{
				char lastChar = (output.Length > 0 ? output[output.Length - 1] : '\0');

				if (TryToSkipWhitespace(ref iterator) == true) // includes comments
					{
					char nextChar = iterator.Character;

					if ( (spaceSeparatedSymbols.IndexOf(lastChar) != -1 &&
						  spaceSeparatedSymbols.IndexOf(nextChar) != -1) ||
						 (Tokenizer.FundamentalTypeOf(lastChar) == FundamentalType.Text &&
						  Tokenizer.FundamentalTypeOf(nextChar) == FundamentalType.Text) )
						{  output.Append(' ');  }
					}
				else
					{
					TokenIterator prevIterator = iterator;

					if (TryToSkipString(ref iterator) ||
						TryToSkipRegex(ref iterator) )
						{  source.AppendTextBetweenTo(prevIterator, iterator, output);  }
					else
						{
						iterator.AppendTokenTo(output);
						iterator.Next();
						}
					}
				}

			return output.ToString();
			}


		/* Function: GenericSkip
		 * Extends <ResourceProcessor.GenericSkip()> to handle regular expressions.
		 */
		override protected void GenericSkip (ref TokenIterator iterator)
			{
			if (!TryToSkipRegex(ref iterator))
				{  base.GenericSkip(ref iterator);  }
			}


		/* Function: TryToSkipRegex
		 * If the iterator is on the opening symbol of a regular expression, skips over it and returns true.  Otherwise leaves the iterator
		 * alone and returns false.
		 */
		protected bool TryToSkipRegex (ref TokenIterator iterator)
			{
			if (iterator.Character != '/')
				{  return false;  }

			// A plain / can be the start of a regex or a division symbol.  See if the previous non-whitespace character is acceptable before
			// treating it as a regex.

			string rawText = iterator.Tokenizer.RawText;
			int rawTextIndex = iterator.RawTextIndex - 1;

			while (rawTextIndex >= 0 && (rawText[rawTextIndex] == ' ' || rawText[rawTextIndex] == '\t'))
				{  rawTextIndex--;  }

			if (rawTextIndex < 0 || RegexPrefixCharacters.IndexOf(rawText[rawTextIndex]) != -1)
				{
				// Starts and ends with a slash except when escaped.  Just like a string right?
				return TryToSkipString(ref iterator, '/');
				}
			else
				{  return false;  }
			}


		// Group: Static Variables
		// __________________________________________________________________________

		protected static string RegexPrefixCharacters = "({[,;:=&|!?\0";

		}
	}
