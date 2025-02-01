/*
 * Class: CodeClear.NaturalDocs.Engine.Output.ResourceProcessors.CSS
 * ____________________________________________________________________________
 *
 * A class used to process CSS files, such as performing substitutions and removing whitespace.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
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
	public class CSS : ResourceProcessor
		{

		// Group: Functions
		// __________________________________________________________________________

		public CSS () : base ()
			{
			this.BlockCommentStringPairs = new string[] { "/*", "*/" };
			this.QuoteCharacters = new char[] { '"', '\'' };
			}


		override public string Process (string css, bool shrink = true)
			{
			Tokenizer source = new Tokenizer(css);
			StringToStringTable substitutions = FindSubstitutions(source);

			source = ApplySubstitutions(source, substitutions);

			if (!shrink)
				{  return source.RawText;  }


			// Search comments for sections to include in the output

			StringBuilder output = new StringBuilder(css.Length);

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

			// We have to be more cautious than the JavaScript shrinker.  You don't want something like "head .class" to become
			// "head.class".  Colon is a special case because we only want to remove spaces after it ("font-size: 12pt") and not
			// before ("body :link").
			string safeToCondenseAround = "{},;:+>[]= \0\n\r";

			while (iterator.IsInBounds)
				{
				char lastChar = (output.Length > 0 ? output[output.Length - 1] : '\0');

				if (TryToSkipWhitespace(ref iterator)) // includes comments
					{
					char nextChar = iterator.Character;

					if (nextChar == ':' ||
						(safeToCondenseAround.IndexOf(lastChar) == -1 &&
						safeToCondenseAround.IndexOf(nextChar) == -1) )
						{  output.Append(' ');  }
					}
				else
					{
					TokenIterator prevIterator = iterator;

					if (TryToSkipString(ref iterator))
						{
						source.AppendTextBetweenTo(prevIterator, iterator, output);
						}
					else
						{
						if (iterator.Character == '}' && lastChar == ';')
							{
							// Semicolons are unnecessary at the end of blocks.  However, we have to do this here instead of in a
							// global search and replace for ";}" because we don't want to alter that sequence if it appears in a string.
							output[output.Length - 1] = '}';
							}
						else
							{  iterator.AppendTokenTo(output);  }

						iterator.Next();
						}
					}
				}

			return output.ToString();
			}

		}
	}
