/*
 * Class: CodeClear.NaturalDocs.Engine.Output.ResourceProcessors.CSS
 * ____________________________________________________________________________
 *
 * A class used to process CSS files, such as performing substitutions and removing whitespace.
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
	public class CSS : ResourceProcessor
		{

		// Group: Functions
		// __________________________________________________________________________

		static CSS ()
			{
			commentFinder = new Languages.CommentFinder("CSS");
			commentFinder.BlockCommentStringPairs = new string[] { "/*", "*/" };
			}


		override public string Process (string css, bool shrink = true)
			{
			source = new Tokenizer(css);
			output = new StringBuilder(css.Length / 2);  // Guess, but better than nothing.
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

			// We have to be more cautious than the JS shrinker.  You don't want something like "head .class" to become
			// "head.class".  Colon is a special case because we only want to remove spaces after it ("font-size: 12pt")
			// and not before ("body :link").
			string safeToCondenseAround = "{},;:+>[]=\0";

			while (iterator.IsInBounds)
				{
				TokenIterator prevIterator = iterator;
				char lastChar = (output.Length > 0 ? output[output.Length - 1] : '\0');

				if (TryToSkipWhitespace(ref iterator, true) == true)
					{
					if (!shrink)
						{  source.AppendTextBetweenTo(prevIterator, iterator, output);  }
					else
						{
						char nextChar = iterator.Character;

						if (nextChar == ':' ||
							  (safeToCondenseAround.IndexOf(lastChar) == -1 &&
								safeToCondenseAround.IndexOf(nextChar) == -1) )
							{  output.Append(' ');  }
						}
					}
				else if (TryToSkipString(ref iterator) == true)
					{
					source.AppendTextBetweenTo(prevIterator, iterator, output);
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

			return output.ToString();
			}



		// Group: Static Variables
		// __________________________________________________________________________

		protected static Languages.CommentFinder commentFinder;

		}
	}
