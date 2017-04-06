/*
 * Class: CodeClear.NaturalDocs.Engine.Output.ResourceProcessors.JavaScript
 * ____________________________________________________________________________
 *
 * A class used to process JavaScript files, such as performing substitutions and removing whitespace.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Comments;
using CodeClear.NaturalDocs.Engine.Tokenization;

namespace CodeClear.NaturalDocs.Engine.Output.ResourceProcessors
	{
	public class JavaScript : ResourceProcessor
		{

		// Group: Functions
		// __________________________________________________________________________

		static JavaScript ()
			{
			commentFinder = new Languages.CommentFinder("JavaScript");
			commentFinder.LineCommentStrings = new string[] { "//" };
			commentFinder.BlockCommentStringPairs = new string[] { "/*", "*/" };
			}

		override public string Process (string javascript, bool shrink = true)
			{
			source = new Tokenizer(javascript);
			output = new StringBuilder(javascript.Length / 2);  // Guess, but better than nothing.
			substitutions = new StringToStringTable(KeySettingsForSubstitutions);


			// Search comments for sections to include in the output and substitution definitions.

			IList<PossibleDocumentationComment> comments = commentFinder.GetPossibleDocumentationComments(source);

			foreach (var comment in comments)
				{  ProcessComment(comment, shrink);  }

			if (output.Length > 0)
				{
				output.AppendLine(" */");
				output.AppendLine();
				}


			// Build the condensed output.

			TokenIterator iterator = source.FirstToken;

			string spaceSeparatedSymbols = "+-";
			string regexPrefixCharacters = "({[,;:=&|!?\0";
			char lastNonWSChar = '\0';

			while (iterator.IsInBounds)
				{
				TokenIterator prevIterator = iterator;
				char lastChar = (output.Length > 0 ? output[output.Length - 1] : '\0');

				if (lastChar != ' ' && lastChar != '\t')
					{  lastNonWSChar = lastChar;  }

				if (TryToSkipWhitespace(ref iterator, false) == true)
					{
					if (!shrink)
						{  source.AppendTextBetweenTo(prevIterator, iterator, output);  }
					else
						{
						char nextChar = iterator.Character;

						if ( nextChar == '`' ||
							  (spaceSeparatedSymbols.IndexOf(lastChar) != -1 &&
							   spaceSeparatedSymbols.IndexOf(nextChar) != -1) ||
							  (Tokenizer.FundamentalTypeOf(lastChar) == FundamentalType.Text &&
							   Tokenizer.FundamentalTypeOf(nextChar) == FundamentalType.Text) )
							{  output.Append(' ');  }
						}
					}
				else if (iterator.Character == '`')
					{
					string substitution = null;

					if (TryToGetSubstitution(ref iterator, out substitution))
						{
						output.Append(substitution);
						}
					else
						{
						output.Append('`');
						iterator.Next();
						}
					}
				else
					{
					if (TryToSkipString(ref iterator) == true ||
							(regexPrefixCharacters.IndexOf(lastNonWSChar) != -1 && TryToSkipRegex(ref iterator) == true) )
						{  }
					else
						{  iterator.Next();  }

					source.AppendTextBetweenTo(prevIterator, iterator, output);
					}
				}

			return output.ToString();
			}



		// Group: Static Variables
		// __________________________________________________________________________

		protected static Languages.CommentFinder commentFinder;

		}
	}
