/*
 * Class: CodeClear.NaturalDocs.Engine.Comments.NaturalDocs.Parser
 * ____________________________________________________________________________
 *
 * A parser to handle Natural Docs' native comment format.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CodeClear.NaturalDocs.Engine.Languages;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Comments.NaturalDocs
	{
	public partial class Parser : Comments.Parser
		{

		// Group: Types
		// __________________________________________________________________________


		/* enum: LinkInterpretationFlags
		 *
		 * Options you can pass to <LinkInterpretations()>.
		 *
		 * ExcludeLiteral - If set, the unaltered input string will not be added as one of the interpretations.  Only alternative
		 *								 interpretations such as named links or plural/possessive conversions will be included, provided the
		 *								 relevant flags are set.
		 *	 AllowNamedLinks - If set, it will see if the input string can be interpreted as a named link such as
		 *										"<web site at http://www.naturaldocs.org>" or "<web site: http://www.naturaldocs.org>",
		 *										and if so will add any possibilities to the list.  There may be more than one, or there may be none.
		 *	 AllowPluralsAndPossessives - If set, it will see if the input string can be interpreted as a plural form of another
		 *														 word, and if so will add possible singular forms to the list.  There may be more than
		 *														 one, or there may be none.
		 *	 FromOriginalText - If set, specifies that the input string comes from the originaltext property of a <NDMarkup>
		 *									  link and is surrounded by angle brackets which should be ignored.  If not set, it assumes the
		 *									  input string is just the content of the link.
		 */
		[Flags]
		public enum LinkInterpretationFlags : byte
			{
			ExcludeLiteral = 0x01,
			AllowNamedLinks = 0x02,
			AllowPluralsAndPossessives = 0x04,
			FromOriginalText = 0x08
			}


		/* enum: BlockType
		 *
		 * The type of block started by lines like "(start code)".
		 *
		 * Generic - Generic block with no special behavior.
		 * Code - The block should be formatted as source code.
		 * Prototype - The block should be used as a prototype.
		 */
		public enum BlockType : byte
			{  Generic, Code, Prototype  }


		/* enum: HeadingType
		 *
		 * The type of block started by a recognized heading.
		 *
		 * Generic - A generic heading with no special behavior.
		 * Parameters - The content under the heading is interpreted as a parameter list.
		 */
		public enum HeadingType : byte
			{  Generic, Parameters  }



		// Group: Functions
		// __________________________________________________________________________


		/* Function: Parser
		 */
		public Parser (Comments.Manager manager) : base (manager)
			{
			config = null;
			}


		/* Function: Start
		 *
		 * Dependencies:
		 *
		 *		- <Config.Manager> and <CommentTypes.Manager> must be started before using this class.
		 */
		public override bool Start (Errors.ErrorList errors)
		    {
			StartupIssues newStartupIssues = StartupIssues.None;


			// Load configuration files

			ConfigFiles.TextFileParser textFileParser = new ConfigFiles.TextFileParser();

			bool loadTextFileResult = textFileParser.Load(EngineInstance.Config.SystemConfigFolder + "/Parser.txt",
																			 Engine.Config.PropertySource.ParserConfigurationFile,
																			 errors, out config);

			if (loadTextFileResult == false )
				{  return false;  }


			ConfigFiles.BinaryFileParser binaryFileParser = new ConfigFiles.BinaryFileParser();
			Config binaryConfig = null;

			if (!EngineInstance.HasIssues( StartupIssues.NeedToStartFresh ))
				{
				binaryFileParser.Load(EngineInstance.Config.WorkingDataFolder + "/Parser.nd", out binaryConfig);
				}


			// Compare to previous settings

			if (binaryConfig == null)
				{  newStartupIssues |= StartupIssues.NeedToReparseAllFiles;  }
			else if (binaryConfig != config)
				{  newStartupIssues |= StartupIssues.NeedToReparseAllFiles;  }

			ConfigFile.TryToRemoveErrorAnnotations(EngineInstance.Config.SystemConfigFolder + "/Parser.txt");

			binaryFileParser.Save(EngineInstance.Config.WorkingDataFolder + "/Parser.nd", config);


			if (newStartupIssues != StartupIssues.None)
				{  EngineInstance.AddStartupIssues(newStartupIssues);  }

			return true;
			}


		/* Function: Parse
		 *
		 * Attempts to parse the passed comment into <Topics>.  Returns whether it was successful, and if so, adds them
		 * to the list.  These fields will be set:
		 *
		 *		- CommentLineNumber
		 *		- Title, unless doesn't require header
		 *		- Body, if present
		 *		- Summary, if available
		 *		- CommentTypeID, unless doesn't require header
		 *		- AccessLevel, if specified
		 *		- Tags, if specified
		 *		- UsesPluralKeyword, unless doesn't require header
		 *		- IsEmbedded
		 */
		public bool Parse (PossibleDocumentationComment comment, int languageID, List<Topic> topics, bool requireHeader)
			{
			// Skip initial blank and horizontal lines.

			LineIterator lineIterator = comment.Start;

			while (lineIterator < comment.End &&
					  ( lineIterator.IsEmpty(LineBoundsMode.CommentContent) ||
						LineFinder.IsHorizontalLine(lineIterator) ))
				{
				lineIterator.Next();
				}

			if (lineIterator >= comment.End)
				{  return false;  }


			// First topic line, if required.

			Topic currentTopic;
			LineIterator firstContentLine;

			if (IsTopicLine(lineIterator, languageID, out currentTopic))
				{
				lineIterator.Next();
				firstContentLine = lineIterator;
				}
			else
				{
				if (requireHeader)
					{  return false;  }
				else
					{
					currentTopic = new Topic(EngineInstance.CommentTypes);
					currentTopic.CommentLineNumber = lineIterator.LineNumber;

					firstContentLine = lineIterator;
					}
				}


			// Rest of comment.

			Topic nextTopic;
			char blockChar;
			BlockType blockType;
			Language language;
			bool prevLineBlank = false;

			while (lineIterator < comment.End)
				{
				if (prevLineBlank && IsTopicLine(lineIterator, languageID, out nextTopic))
					{
					if (firstContentLine < lineIterator)
						{  ParseBody(firstContentLine, lineIterator, currentTopic);  }

					topics.Add(currentTopic);
					ExtractEmbeddedTopics(currentTopic, topics);

					currentTopic = nextTopic;
					nextTopic = null;

					lineIterator.Next();
					firstContentLine = lineIterator;
					prevLineBlank = false;
					}

				else if (IsStartBlockLine(lineIterator, out blockChar, out blockType, out language))
					{
					lineIterator.Next();

					// Skip rest of code block so nothing in its content can be interpreted as a topic line.
					while (lineIterator < comment.End)
						{
						char endBlockChar;
						if (IsEndBlockLine(lineIterator, out endBlockChar) && endBlockChar == blockChar)
							{
							lineIterator.Next();
							break;
							}
						else
							{  lineIterator.Next();  }
						}

					prevLineBlank = false;
					}

				else
					{
					prevLineBlank = ( lineIterator.IsEmpty(LineBoundsMode.CommentContent) ||
											   LineFinder.IsHorizontalLine(lineIterator) );

					lineIterator.Next();
					}
				}

			if (firstContentLine < lineIterator)
				{  ParseBody(firstContentLine, lineIterator, currentTopic);  }

			topics.Add(currentTopic);
			ExtractEmbeddedTopics(currentTopic, topics);

			return true;
			}


		/* Function: Parse
		 *
		 * Attempts to parse the passed inline comment into a single <Topic>.  Returns it if it was successful, or null if not.  Only
		 * these fields will be set:
		 *
		 *		- CommentLineNumber
		 *		- Body
		 *		- Summary
		 */
		public Topic Parse (InlineDocumentationComment comment)
			{
			// Create a separate Tokenizer from the comment bounds so we can use LineIterators and such.  This will filter out any
			// non-comment tokens before the start of the comment.
			Tokenizer tokenizer = Tokenizer.CreateFromIterators(comment.Start, comment.End);

			LineIterator firstLine = tokenizer.FirstLine;

			Topic topic = new Topic(EngineInstance.CommentTypes);
			topic.CommentLineNumber = firstLine.LineNumber;

			ParseBody(firstLine, tokenizer.EndOfLines, topic, inlineFormattingOnly: true);

			return topic;
			}


		/* Function: LinkInterpretations
		 *
		 * Generates a list of possible interpretations for the passed target of a Natural Docs link, or null if there are none.  If
		 * <LinkInterpretationFlags.ExcludeLiteral> is not set it will always return a list of at least one interpretation.
		 *
		 * If <LinkInterpretationFlags.ExcludeLiteral> is not set, the literal interpretation will always appear as the first entry
		 * in the list.  Following entries are not guaranteed to be in any particular order but they are guaranteed to be in a
		 * consistent order, meaning every call with the same input will generate the same list in the same order.
		 */
		public List<LinkInterpretation> LinkInterpretations (string linkText, LinkInterpretationFlags flags, out string parameters)
			{
			string input = linkText.Trim();

			if ((flags & LinkInterpretationFlags.FromOriginalText) != 0 && input.Length > 2 &&
				 input[0] == '<' && input[input.Length - 1] == '>')
				{
				input = input.Substring(1, input.Length - 2);

				// Remove the flag so we can pass the rest of them to LinkInterpretations_NoParameters().
				flags &= ~LinkInterpretationFlags.FromOriginalText;
				}

			int parametersIndex = ParameterString.GetParametersIndex(input);
			bool spaceBeforeParameters;
			string inputWithoutParameters;

			if (parametersIndex == -1)
				{
				inputWithoutParameters = input;
				parameters = null;
				spaceBeforeParameters = false;
				}
			else
				{
				inputWithoutParameters = input.Substring(0, parametersIndex);
				parameters = input.Substring(parametersIndex);
				spaceBeforeParameters = (input[parametersIndex - 1] == ' ');
				}

			List<LinkInterpretation> result = LinkInterpretations_NoParameters(inputWithoutParameters, flags);

			// Put the parameters back on the literal.
			if (parameters != null)
				{
				foreach (LinkInterpretation interpretation in result)
					{
					if (interpretation.IsLiteral)
						{
						if (spaceBeforeParameters)
							{  interpretation.Text += ' ';  }

						interpretation.Text += parameters;
						}
					}
				}

			return result;
			}


		/* Function: LinkInterpretations_NoParameters
		 *
		 * Generates a list of possible interpretations for the passed target of a Natural Docs link, or null if there are none.  If
		 * <LinkInterpretationFlags.ExcludeLiteral> is not set it will always return a list of at least one interpretation.
		 *
		 * If <LinkInterpretationFlags.ExcludeLiteral> is not set, the literal interpretation will always appear as the first entry
		 * in the list.  Following entries are not guaranteed to be in any particular order but they are guaranteed to be in a
		 * consistent order, meaning every call with the same input will generate the same list in the same order.
		 *
		 * We use this awkward function name because 90% of the time you need to handle parameters, or at least strip them
		 * off.  If we just made an overload of <LinkInterpretations()> without the out parameter people would use this one by
		 * accident.  By attaching _NoParameters it forces you to only use this one if you know what you're doing.
		 */
		public List<LinkInterpretation> LinkInterpretations_NoParameters (string linkText, LinkInterpretationFlags flags)
			{
			List<LinkInterpretation> interpretations = null;
			string input = linkText.CondenseWhitespace();

			if ((flags & LinkInterpretationFlags.FromOriginalText) != 0 && input.Length > 2 &&
				 input[0] == '<' && input[input.Length - 1] == '>')
				{
				input = input.Substring(1, input.Length - 2);
				}


			if ((flags & LinkInterpretationFlags.ExcludeLiteral) == 0)
				{
				interpretations = new List<LinkInterpretation>();

				LinkInterpretation interpretation = new LinkInterpretation();
				interpretation.Target = input;
				interpretations.Add(interpretation);
				}


			if ((flags & LinkInterpretationFlags.AllowNamedLinks) != 0)
				{
				int colon = input.IndexOf(':');

				while (colon != -1)
					{
					// Don't interpret it as a named link if the character before or after it is also a colon, so <Package::Name> works.
					if (colon != 0 && colon != input.Length - 1 && input[colon + 1] != ':' &&
						(colon > 0 && input[colon - 1] == ':') == false)
						{
						// Need to check for URL protocols so the colon in <http://www.naturaldocs.org> doesn't make it get interpreted
						// as a named link.  Same with the colon in <web site at http://www.naturaldocs.org>.
						int space = input.LastIndexOf(' ', colon - 1);
						string wordBeforeColon;

						if (space == -1)
							{  wordBeforeColon = input.Substring(0, colon);  }
						else
							{  wordBeforeColon = input.Substring(space + 1, colon - (space + 1));  }

						if (!IsURLProtocol(wordBeforeColon) && String.Compare(wordBeforeColon, "mailto", true) != 0)
							{
							if (interpretations == null)
								{  interpretations = new List<LinkInterpretation>();  }

							LinkInterpretation interpretation = new LinkInterpretation();
							interpretation.Text = input.Substring(0, colon).TrimEnd();
							interpretation.Target = input.Substring(colon + 1).TrimStart();
							interpretation.NamedLink = true;

							interpretations.Add(interpretation);
							}
						}

					colon = input.IndexOf(':', colon + 1);
					}

				for (int firstSpace = input.IndexOf(' '); firstSpace != -1; firstSpace = input.IndexOf(' ', firstSpace + 1))
					{
					for (int secondSpace = input.IndexOf(' ', firstSpace + 1); secondSpace != -1; secondSpace = input.IndexOf(' ', secondSpace + 1))
						{
						string keyword = input.Substring(firstSpace + 1, secondSpace - (firstSpace + 1));

						if (IsAtLinkKeyword(keyword))
							{
							if (interpretations == null)
								{  interpretations = new List<LinkInterpretation>();  }

							LinkInterpretation interpretation = new LinkInterpretation();
							interpretation.Text = input.Substring(0, firstSpace);
							interpretation.Target = input.Substring(secondSpace + 1);
							interpretation.NamedLink = true;

							interpretations.Add(interpretation);
							}
						}
					}
				}


			// We only generate plural and possessive interpretations from the input string because it doesn't make sense to use both a named
			// link and a plural or possessive form in the same link.

			if ((flags & LinkInterpretationFlags.AllowPluralsAndPossessives) != 0)
				{
				string nInput = input.Normalize(System.Text.NormalizationForm.FormC);
				string lcnInput = nInput.ToLower(CultureInfo.InvariantCulture);

				// We use -1 to signify none, since we also want to test each plural conversion without any possessive conversion applied.
				for (int possessiveIndex = -1; possessiveIndex < config.PossessiveConversions.Count; possessiveIndex++)
					{
					string nWithoutPossessive, lcnWithoutPossessive;

					if (possessiveIndex == -1)
						{
						nWithoutPossessive = nInput;
						lcnWithoutPossessive = lcnInput;
						}
					else if (lcnInput.EndsWith(config.PossessiveConversions[possessiveIndex].Key))
						{
						var possessiveConversion = config.PossessiveConversions[possessiveIndex];

						nWithoutPossessive = nInput.Substring(0, nInput.Length - possessiveConversion.Key.Length);
						lcnWithoutPossessive = lcnInput.Substring(0, lcnInput.Length - possessiveConversion.Key.Length);

						if (possessiveConversion.Value != null)
							{
							nWithoutPossessive += possessiveConversion.Value;
							lcnWithoutPossessive += possessiveConversion.Value;
							}
						}
					else
						{
						nWithoutPossessive = null;
						lcnWithoutPossessive = null;
						}

					if (nWithoutPossessive != null)
						{
						// Again -1 signifies none, since we also want each possessive conversion without any plural conversion applied.
						for (int pluralIndex = -1; pluralIndex < config.PluralConversions.Count; pluralIndex++)
							{
							string nWithoutEither;

							if (pluralIndex == -1)
								{
								// Skip when we're missing both.  We have that on the list already.
								if (possessiveIndex == -1)
									{  nWithoutEither = null;  }
								else
									{  nWithoutEither = nWithoutPossessive;  }
								}

							else if (lcnWithoutPossessive.EndsWith(config.PluralConversions[pluralIndex].Key))
								{
								var pluralConversion = config.PluralConversions[pluralIndex];

								nWithoutEither = nWithoutPossessive.Substring(0, nWithoutPossessive.Length - pluralConversion.Key.Length);

								if (pluralConversion.Value != null)
									{  nWithoutEither += pluralConversion.Value;  }
								}

							else
								{  nWithoutEither = null;  }

							// We also check for empty because a conversion may render it so.  Think of removing the trailing S from a link that was
							// only an S.
							if (!String.IsNullOrEmpty(nWithoutEither))
								{
								if (interpretations == null)
									{  interpretations = new List<LinkInterpretation>();  }

								LinkInterpretation interpretation = new LinkInterpretation();

								interpretation.Text = input;
								interpretation.Target = nWithoutEither;

								if (pluralIndex != -2)
									{  interpretation.PluralConversion = true;  }
								if (possessiveIndex != -2)
									{  interpretation.PossessiveConversion = true;  }

								interpretations.Add(interpretation);
								}
							}
						}
					}
				}


			return interpretations;
			}



		// Group: Protected Functions
		// __________________________________________________________________________


		/* Function: IsAccessLevelTag
		 * Returns whether the passed string is an access level tag, and if so, also returns the <Languages.AccessLevel>
		 * associated with it.
		 */
		protected bool IsAccessLevelTag (string tag, out Languages.AccessLevel accessLevel)
			{
			if (config.AccessLevel.ContainsKey(tag))
				{
				accessLevel = config.AccessLevel[tag];
				return true;
				}
			else
				{
				accessLevel = Languages.AccessLevel.Unknown;
				return false;
				}
			}

		/* Function: IsTopicLine
		 *
		 * Returns whether the passed <LineIterator> is on a topic line, and if so, returns a <Topic> with the following fields filled
		 * in:
		 *
		 *		- CommentLineNumber
		 *		- Title
		 *		- CommentTypeID
		 *		- IsList
		 *		- LanguageID, if specified as a tag
		 *		- AccessLevel, if specified as a tag
		 *		- Tags, if specified
		 *
		 *	Note that even though a language ID is passed to this function, <Topic.LanguageID> will not be filled in unless a language
		 *	name was specified as a tag, such as "JavaScript Class: MyClass".  This allows language-tagged topics to be distringuished
		 *	from ones that inherit the default language.
		 */
		protected bool IsTopicLine (LineIterator lineIterator, int languageID, out Topic topic)
			{
			topic = null;


			// The topic line must contain a colon.

			TokenIterator colon;

			if (lineIterator.FindToken(":", false, LineBoundsMode.CommentContent, out colon) == false)
				{  return false;  }


			// The colon must be followed by whitespace and at least one non-whitespace token.

			TokenIterator afterColon = colon;

			afterColon.Next();
			if (afterColon.FundamentalType != FundamentalType.Whitespace)
				{  return false;  }

			do
				{  afterColon.Next();  }
			while (afterColon.FundamentalType == FundamentalType.Whitespace);

			if ( (afterColon.FundamentalType != FundamentalType.Text && afterColon.FundamentalType != FundamentalType.Symbol) ||
				 afterColon.CommentParsingType == CommentParsingType.CommentSymbol ||
				 afterColon.CommentParsingType == CommentParsingType.CommentDecoration)
				{  return false;  }


			// The colon can't be preceded by whitespace.

			TokenIterator beforeColon = colon;
			beforeColon.Previous();

			if (beforeColon.FundamentalType == FundamentalType.Whitespace)
				{  return false;  }


			// Gather everything before the colon, condensing whitespace.

			int lineStartingIndex, lineEndingIndex;
			lineIterator.GetRawTextBounds(LineBoundsMode.CommentContent, out lineStartingIndex, out lineEndingIndex);

			Tokenizer tokenizer = lineIterator.Tokenizer;
			string keywordsAndTags = tokenizer.RawText.Substring(lineStartingIndex, colon.RawTextIndex - lineStartingIndex);
			keywordsAndTags = keywordsAndTags.CondenseWhitespace();


			// If there are spaces, then there are a lot of potential interpretations for keywords and tags.  For example,
			// "JavaScript Function" can keyword "Function" with tag "JavaScript", but it can be also be just keyword
			// "JavaScript Function".  The latter is possible because people may have used them as workarounds before tags
			// were supported, such as using "Private Function" as a keyword that's defined in one set of documentation but
			// not in another.  So we test all possible permutations until we find one that makes sense or we run out of
			// possibilities.

			List<int> tagIDs = null;
			int languageIDFromTags = 0;
			Languages.AccessLevel accessLevelFromTags = Languages.AccessLevel.Unknown;


			// First we choose our keyword.  We start with the longest one possible for the first permutation.

			int keywordStartingIndex = 0;
			int keywordEndingIndex = keywordsAndTags.Length;

			for (;;)
				{
				// We can't test whether it's an actual keyword yet because it may be a language-dependent one, and we won't
				// know which language to use until we parse all the tags.

				// So now we parse out the tags, language, and access level.  We start with the full string before the keyword
				// and work our way down to the first word.  This allows longer tags and access levels like "Protected Internal"
				// to apply before shorter ones like "Protected" and "Internal".  One word can serve as both, so "Private" can be
				// both a tag and a protection level.

				int tagSectionStartingIndex = 0;
				int tagSectionEndingIndex = (keywordStartingIndex == 0 ? 0 : keywordStartingIndex - 1);  // -1 to skip separating space

				bool tagsAreValid = true;

				if (tagSectionEndingIndex > tagSectionStartingIndex)
					{
					int candidateStartingIndex = tagSectionStartingIndex;
					int candidateEndingIndex = tagSectionEndingIndex;

					for (;;)
						{
						bool foundInterpretation = false;
						string candidate = keywordsAndTags.Substring(candidateStartingIndex, candidateEndingIndex - candidateStartingIndex);

						CommentTypes.Tag candidateAsTag = EngineInstance.CommentTypes.TagFromName(candidate);

						if (candidateAsTag != null)
							{
							if (tagIDs == null)
								{  tagIDs = new List<int>();  }

							tagIDs.Add(candidateAsTag.ID);
							foundInterpretation = true;
							}

						Languages.AccessLevel candidateAsAccessLevel;

						if (IsAccessLevelTag(candidate, out candidateAsAccessLevel))
							{
							accessLevelFromTags = candidateAsAccessLevel;
							foundInterpretation = true;
							}

						Language candidateAsLanguage = EngineInstance.Languages.FromName(candidate);

						if (candidateAsLanguage != null)
							{
							languageIDFromTags = candidateAsLanguage.ID;
							foundInterpretation = true;
							}

						if (foundInterpretation)
							{
							// We found an interpretation for this tag candidate.  It might not encompass the entire tag section so move
							// on to the next part.
							candidateStartingIndex = candidateEndingIndex + 1;  // +1 to skip separating space.
							candidateEndingIndex = tagSectionEndingIndex;

							// If there's no more to the tag section then we're done.
							if (candidateStartingIndex >= candidateEndingIndex)
								{  break;  }
							}
						else
							{
							// We didn't find an interpretation for this tag canditate.  If it contains a space there are still other permutations
							// we can try, so shave off the last word and try again.
							candidateEndingIndex = keywordsAndTags.LastIndexOf(' ', candidateEndingIndex - 1,
																												 candidateEndingIndex - candidateStartingIndex);

							// If there aren't any more spaces then this entire tag section is invalid.
							if (candidateEndingIndex == -1)
								{
								tagsAreValid = false;
								break;
								}
							}
						}
					}


				if (tagsAreValid)
					{
					// If the tags are valid in this permutation then we can test the keyword.

					string keyword = keywordsAndTags.Substring(keywordStartingIndex, keywordEndingIndex - keywordStartingIndex);
					bool keywordIsPlural;

					var commentType = EngineInstance.CommentTypes.FromKeyword(keyword,
																													(languageIDFromTags != 0 ? languageIDFromTags : languageID),
																													out keywordIsPlural);

					// If we found a comment type then we're done.

					if (commentType != null)
						{
						topic = new Topic(EngineInstance.CommentTypes);
						topic.CommentLineNumber = lineIterator.LineNumber;
						topic.Title = tokenizer.RawText.Substring( afterColon.RawTextIndex, lineEndingIndex - afterColon.RawTextIndex );
						topic.CommentTypeID = commentType.ID;
						topic.IsList = keywordIsPlural;

						if (accessLevelFromTags != AccessLevel.Unknown)
							{  topic.DeclaredAccessLevel = accessLevelFromTags;  }
						if (languageIDFromTags != 0)
							{  topic.LanguageID = languageIDFromTags;  }
						if (tagIDs != null)
							{
							foreach (int tagID in tagIDs)
								{  topic.AddTagID(tagID);  }
							}

						return true;
						}
					}


				// If we're here then either the keyword or the tags weren't valid for this permutation.  Try shaving one word off
				// the front of the keyword and trying again.

				int firstKeywordSpaceIndex = keywordsAndTags.IndexOf(' ', keywordStartingIndex);

				if (firstKeywordSpaceIndex != -1)
					{  keywordStartingIndex = firstKeywordSpaceIndex + 1;  }
				else
					{
					// If there's no more spaces in the keyword section then there's no more possibilities so we're done.
					topic = null;
					return false;
					}
				}
			}


		/* Function: IsParenTagLine
		 * Returns true if the entire line is enclosed in parentheses and satisfies a few other requirements to be suitable for a
		 * parenthetical tag like "(start code)" or "(see image.jpg)".  Will return the contents of the parentheses with all whitespace
		 * condensed.
		 */
		protected bool IsParenTagLine (LineIterator lineIterator, out string content)
			{
			TokenIterator firstToken, lastToken;
			lineIterator.GetBounds(LineBoundsMode.CommentContent, out firstToken, out lastToken);

			lastToken.Previous();

			if (firstToken.Character != '(' || lastToken.Character != ')')
				{
				content = null;
				return false;
				}

			firstToken.Next();
			lastToken.Previous();

			if (firstToken > lastToken || firstToken.FundamentalType == FundamentalType.Whitespace ||
				lastToken.FundamentalType == FundamentalType.Whitespace)
				{
				content = null;
				return false;
				}

			lastToken.Next();

			string betweenParens = firstToken.TextBetween(lastToken);

			if (betweenParens.IndexOfAny(ParenthesesChars) != -1)
				{
				content = null;
				return false;
				}

			betweenParens = betweenParens.CondenseWhitespace();

			content = betweenParens;
			return true;
			}


		/* Function: IsHorizontalLineTagLine
		 * Returns true if the line starts with at least three dashes, underscores, or equals signs and satisfies a few other requirements
		 * to be suitable for a line block tag like "--- code" or "==== Perl ====".  Will return the contents of the tag with all whitespace
		 * condensed as well as which character was used.
		 */
		protected bool IsHorizontalLineTagLine (LineIterator lineIterator, out string content, out char character)
			{
			TokenIterator firstToken, lastToken;
			lineIterator.GetBounds(LineBoundsMode.CommentContent, out firstToken, out lastToken);

			if (firstToken.Character != '-' && firstToken.Character != '=' && firstToken.Character != '_')
				{
				content = null;
				character = '\0';
				return false;
				}

			character = firstToken.Character;
			int prefixCount = 0;

			while (firstToken < lastToken && firstToken.Character == character)
				{
				firstToken.Next();
				prefixCount++;
				}

			if (prefixCount < 3)
				{
				content = null;
				character = '\0';
				return false;
				}

			firstToken.NextPastWhitespace(lastToken);

			lastToken.Previous();  // change limit to iterator

			while (lastToken >= firstToken && lastToken.Character == character)
				{  lastToken.Previous();  }

			lastToken.Next();  // iterator back to limit
			lastToken.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds);

			if (lastToken <= firstToken)
				{
				content = null;
				character = '\0';
				return false;
				}

			content = firstToken.TextBetween(lastToken).CondenseWhitespace();
			return true;
			}


		/* Function: IsHorizontalLineEnderLine
		 * Returns true if the line only contains at least three dashes, underscores, or equals signs so that it's suitable to end a
		 * horizontal line tag line of the same character.
		 */
		protected bool IsHorizontalLineEnderLine (LineIterator lineIterator, out char character)
			{
			TokenIterator firstToken, lastToken;
			lineIterator.GetBounds(LineBoundsMode.CommentContent, out firstToken, out lastToken);

			if (firstToken.Character != '-' && firstToken.Character != '=' && firstToken.Character != '_')
				{
				character = '\0';
				return false;
				}

			character = firstToken.Character;
			int characterCount = 0;

			while (firstToken < lastToken && firstToken.Character == character)
				{
				firstToken.Next();
				characterCount++;
				}

			if (characterCount < 3 || firstToken != lastToken)
				{
				character = '\0';
				return false;
				}

			return true;
			}


		/* Function: IsStartBlockKeyword
		 * Returns whether the passed string is a start block keyword, which would be the first word in "(start code)".
		 */
		protected bool IsStartBlockKeyword (string keyword)
			{
			return config.StartBlockKeywords.Contains(keyword);
			}


		/* Function: IsEndBlockKeyword
		 * Returns whether the passed string is an end block keyword, which would be the first word in "(end code)".
		 */
		protected bool IsEndBlockKeyword (string keyword)
			{
			return config.EndBlockKeywords.Contains(keyword);
			}


		/* Function: IsBlockType
		 * Returns whether the passed string represents a block type, which would be the second word in "(start code)", and
		 * which <BlockType> it is if so.
		 */
		protected bool IsBlockType (string keyword, out BlockType blockType)
			{
			if (config.BlockTypes.ContainsKey(keyword))
				{
				blockType = config.BlockTypes[keyword];
				return true;
				}
			else
				{
				blockType = BlockType.Generic;
				return false;
				}
			}


		/* Function: IsStartBlockLine
		 * Returns whether the <LineIterator> is on a start block line like "(start code)" or "--- code", and if so, which <BlockType> and
		 * character it uses.  If it was a code block and a language was specified, also returns that <Language>.
		 */
		protected bool IsStartBlockLine (LineIterator lineIterator, out char character, out BlockType blockType, out Language language)
			{
			string tagString;

			if (IsParenTagLine(lineIterator, out tagString))
				{  character = '(';  }
			else if (IsHorizontalLineTagLine(lineIterator, out tagString, out character))
				{  }
			else
				{
				character = '\0';
				blockType = BlockType.Generic;
				language = null;
				return false;
				}


			// (code)

			if (IsBlockType(tagString, out blockType))
				{
				language = null;
				return true;
				}


			// (Perl)

			language = EngineInstance.Languages.FromName(tagString);
			if (language != null)
				{
				blockType = BlockType.Code;
				return true;
				}



			// Since there may be multiple spaces in the parentheses and some may belong to the keyword or language,
			// we have to test all permutations of spaces as dividers.

			for (int firstSpace = tagString.IndexOf(' ');
				  firstSpace != -1;
				  firstSpace = tagString.IndexOf(' ', firstSpace + 1))
				{
				string firstPart = tagString.Substring(0, firstSpace);


				// (start _____)

				if (IsStartBlockKeyword(firstPart))
					{
					string secondPart = tagString.Substring(firstSpace + 1);


					// (start code)

					if (IsBlockType(secondPart, out blockType))
						{
						language = null;
						return true;
						}


					// (start Perl)

					language = EngineInstance.Languages.FromName(secondPart);
					if (language != null)
						{
						blockType = BlockType.Code;
						return true;
						}


					// (start Perl code)

					for (int secondSpace = secondPart.IndexOf(' ');
						  secondSpace != -1;
						  secondSpace = secondPart.IndexOf(' ', secondSpace + 1))
						{
						language = EngineInstance.Languages.FromName( secondPart.Substring(0, secondSpace) );

						if (language != null &&
							IsBlockType( secondPart.Substring(secondSpace+1), out blockType) &&
							blockType == BlockType.Code)
							{  return true;  }
						}
					}

				else // firstPart isn't a start block keyword
					{

					// (Perl code)

					language = EngineInstance.Languages.FromName(firstPart);

					if (language != null &&
						IsBlockType( tagString.Substring(firstSpace + 1), out blockType ) &&
						blockType == BlockType.Code)
						{  return true;  }
					}
				}

			character = '\0';
			blockType = BlockType.Generic;
			language = null;
			return false;
			}


		/* Function: IsEndBlockLine
		 * Returns if the <LineIterator> is on an end block line like "(end code)", "--- end", or "---", and what character it uses.
		 */
		protected bool IsEndBlockLine (LineIterator lineIterator, out char character)
			{
			string tagString;

			if (IsParenTagLine(lineIterator, out tagString))
				{  character = '(';  }
			else if (IsHorizontalLineEnderLine(lineIterator, out character))
				{  return true;  }
			else if (IsHorizontalLineTagLine(lineIterator, out tagString, out character))
				{  }
			else
				{
				character = '\0';
				return false;
				}


			// (end)

			if (IsEndBlockKeyword(tagString))
				{  return true;  }


			// Since there may be multiple spaces in the parentheses and some may belong to the keyword or language,
			// we have to test all permutations of spaces as dividers.

			for (int firstSpace = tagString.IndexOf(' ');
				  firstSpace != -1;
				  firstSpace = tagString.IndexOf(' ', firstSpace + 1))
				{
				string firstPart = tagString.Substring(0, firstSpace);


				// (end _____)

				if (IsEndBlockKeyword(firstPart))
					{
					string secondPart = tagString.Substring(firstSpace + 1);


					// (end code)

					BlockType blockType;
					if (IsBlockType(secondPart, out blockType))
						{  return true;  }


					// (end Perl)

					Language language = EngineInstance.Languages.FromName(secondPart);
					if (language != null)
						{  return true;  }


					// (end Perl code)

					for (int secondSpace = secondPart.IndexOf(' ');
						  secondSpace != -1;
						  secondSpace = secondPart.IndexOf(' ', secondSpace + 1))
						{
						language = EngineInstance.Languages.FromName( secondPart.Substring(0, secondSpace) );

						if (language != null &&
							IsBlockType(secondPart.Substring(secondSpace+1), out blockType) &&
							blockType == BlockType.Code)
							{  return true;  }
						}
					}
				}

			character = '\0';
			return false;
			}


		/* Function: IsHeading
		 * Returns whether the passed <LineIterator> is on a heading, and if so, returns the heading's raw text and what
		 * type of heading it was.
		 */
		protected bool IsHeading (LineIterator lineIterator, out string heading, out HeadingType headingType)
			{
			TokenIterator start, end;
			lineIterator.GetBounds(LineBoundsMode.CommentContent, out start, out end);

			bool definiteHeading = false;

			heading = null;
			headingType = HeadingType.Generic;


			// The line must end with a colon and be right up against content.  If it ends with a double colon we can skip
			// IsHeadingContent().

			end.Previous();

			if (end.Character != ':')
				{  return false;  }

			end.Previous();

			if (end.FundamentalType == FundamentalType.Whitespace)
				{  return false;  }

			else if (end.Character == ':')
				{  definiteHeading = true;  }

			else
				{  end.Next();  }


			if (start >= end)
				{  return false;  }

			if (!definiteHeading && IsHeadingContent(start, end) == false)
				{  return false;  }


			heading = start.TextBetween(end);

			if (config.SpecialHeadings.ContainsKey(heading))
				{  headingType = config.SpecialHeadings[heading];  }
			else
				{  headingType = HeadingType.Generic;  }

			return true;
			}


		/* Function: IsHeadingContent
		 * Tests the capitalization of the text between the iterators to see whether the content should be interpreted as a
		 * heading or left as plain text.  The iterators should not include the colon.
		 */
		protected bool IsHeadingContent (TokenIterator iterator, TokenIterator endOfContent)
			{
			bool isFirstWord = true;
			TokenIterator wordStart = iterator;
			TokenIterator wordEnd = iterator;

			while (iterator < endOfContent)
				{
				if (iterator.FundamentalType == FundamentalType.Text)
					{
					wordStart = iterator;
					wordEnd = wordStart;
					wordEnd.Next();

					while ( wordEnd < endOfContent &&
							 (wordEnd.FundamentalType == FundamentalType.Text || wordEnd.Character == '_') )
						{  wordEnd.Next();  }

					if (IsHeadingWord(wordStart, wordEnd, isFirstWord, false) == false)
						{  return false;  }

					iterator = wordEnd;
					isFirstWord = false;
					}
				else
					{  iterator.Next();  }
				}

			// If there were no words it's not a heading
			if (isFirstWord)
				{  return false;  }

			// Retest the last word with the flag set
			if (IsHeadingWord(wordStart, wordEnd, false, true) == false)
				{  return false;  }

			return true;
			}


		/* Function: IsHeadingWord
		 * Tests the capitalization of the single word between the iterators to see whether it should be interpreted as part of
		 * a heading or plain text.  All words in a heading should pass this test for it to be seen as a heading.
		 */
		protected bool IsHeadingWord (TokenIterator start, TokenIterator end, bool isFirstWord, bool isLastWord)
			{
			// If it starts with an uppercase letter, number, symbol, or underscore it's okay.  We only need to do tests if it starts
			// with a lowercase letter.
			if (Char.IsLower(start.Character) == false)
				{  return true;  }

			// We'll accept short words as long as it's not the first or the last.  This lets through "A and the B", "X or Y", etc.
			if (!isFirstWord && !isLastWord && (end.RawTextIndex - start.RawTextIndex) <= 4)
				{  return true;  }

			return false;
			}


		/* Function: GetPreformattedLine
		 * Returns the line specified by the <LineIterator> as a preformatted line, meaning all leading whitespace will be preserved
		 * and all tabs will expanded.  Trailing whitespace will be removed, and empty lines will return empty strings with indent 0.
		 * Comment symbols and decoration will be replaced by spaces.
		 *
		 * This will NOT check for or remove the first character if this is a standalone code line.  Use <IsStandalonePreformattedLine()>
		 * for that.
		 */
		protected void GetPreformattedLine (LineIterator lineIterator, out string line, out int indent)
			{
			TokenIterator token, end;
			lineIterator.GetBounds(LineBoundsMode.Everything, out token, out end);

			StringBuilder lineBuilder = new StringBuilder(end.RawTextIndex - token.RawTextIndex);

			int lineLength = 0;
			string rawText = lineIterator.Tokenizer.RawText;
			int tabWidth = EngineInstance.Config.TabWidth;


			// Calculate indent first.

			while (token < end)
				{
				if (token.FundamentalType == FundamentalType.Whitespace)
					{
					int tokenEndIndex = token.RawTextIndex + token.RawTextLength;

					for (int i = token.RawTextIndex; i < tokenEndIndex; i++)
						{
						if (rawText[i] == '\t')
							{
							lineLength += tabWidth;
							lineLength -= (lineLength % tabWidth);
							}
						else
							{  lineLength++;  }
						}

					token.Next();
					}

				else if (token.CommentParsingType == CommentParsingType.CommentSymbol ||
							 token.CommentParsingType == CommentParsingType.CommentDecoration)
					{
					lineLength += token.RawTextLength;
					token.Next();
					}

				else
					{  break;  }
				}

			if (lineLength > 0)
				{  lineBuilder.Append(' ', lineLength);  }

			indent = lineLength;


			// Finish off the line, still expanding tabs.

			TokenIterator ignore;
			lineIterator.GetBounds(LineBoundsMode.CommentContent, out ignore, out end);

			while (token < end)
				{
				if (token.FundamentalType == FundamentalType.Whitespace)
					{
					int tokenEndIndex = token.RawTextIndex + token.RawTextLength;
					int oldLineLength = lineLength;

					for (int i = token.RawTextIndex; i < tokenEndIndex; i++)
						{
						if (rawText[i] == '\t')
							{
							lineLength += tabWidth;
							lineLength -= (lineLength % tabWidth);
							}
						else
							{  lineLength++;  }
						}

					lineBuilder.Append(' ', lineLength - oldLineLength);
					token.Next();
					}

				else
					{
					lineBuilder.Append(rawText, token.RawTextIndex, token.RawTextLength);
					lineLength += token.RawTextLength;
					token.Next();
					}
				}

			if (lineLength == indent)
				{
				indent = 0;
				line = "";
				}
			else
				{
				line = lineBuilder.ToString();
				}
			}


		/* Function: IsStandalonePreformattedLine
		 * Returns whether the line specified by the <LineIterator> is a standalone preformatted line, meaning it starts with :, >, or |.
		 * If so, returns it as a string with leading whitespace preserved and all tabs expanded.  Any comment symbols, decoration,
		 * and the leading :, >, or | will be replaced by spaces.  Trailing whitespace will be removed, and empty lines will return empty
		 * strings with indent 0.
		 */
		protected bool IsStandalonePreformattedLine (LineIterator lineIterator, out string line, out int indent, out char leadingCharacter)
			{
			TokenIterator token, end;
			lineIterator.GetBounds(LineBoundsMode.CommentContent, out token, out end);


			// Must start with :, |, or > and be followed by whitespace or be the only thing on the line.

			if (token.Character != ':' && token.Character != '|' && token.Character != '>')
				{
				line = null;
				indent = 0;
				leadingCharacter = '\0';
				return false;
				}

			token.Next();

			if (token < end && token.FundamentalType != FundamentalType.Whitespace)
				{
				line = null;
				indent = 0;
				leadingCharacter = '\0';
				return false;
				}

			token.Previous();


			// Grab it and strip out the leading symbol.

			CommentParsingType oldType = token.CommentParsingType;
			token.CommentParsingType = CommentParsingType.CommentDecoration;

			GetPreformattedLine(lineIterator, out line, out indent);
			leadingCharacter = token.Character;

			token.CommentParsingType = oldType;

			return true;
			}


		/* Function: IsImageKeyword
		 * Returns whether the passed string is an image keyword, which would be the first word in "(see image.jpg)".
		 */
		protected bool IsImageKeyword (string keyword)
			{
			return config.SeeImageKeywords.Contains(keyword);
			}


		/* Function: IsImageTagContent
		 * Returns whether the passed string is the content of an image tag, like "see image.jpg".  It validates the file name against
		 * the registered extensions in <Files.Manager.ImageExtensions>.  The string must not contain the parentheses.  If it is tag
		 * content it will also returns the keyword and file name.
		 */
		protected bool IsImageTagContent (string betweenParens, out string keyword, out Path file)
		    {
			// The compiler doesn't believe keyword is always set before returning otherwise.
			keyword = null;


			// Search for the image keyword.  Check all the spaces as dividers since there may be one in the keyword.

			int spaceIndex = betweenParens.IndexOf(' ');

			while (spaceIndex != -1)
				{
				keyword = betweenParens.Substring(0, spaceIndex);

				if (IsImageKeyword(keyword))
					{  break;  }

				spaceIndex = betweenParens.IndexOf(' ', spaceIndex + 1);
				}

			if (spaceIndex == -1)
				{
				keyword = null;
				file = null;
				return false;
				}

			file = betweenParens.Substring(spaceIndex + 1);

			if (file.Extension != null && Files.Manager.ImageExtensions.Contains(file.Extension))
				{
				return true;
				}
			else
				{
				keyword = null;
				file = null;
				return false;
				}
		    }


		/* Function: IsStandaloneImage
		 * Returns whether the line specified by <LineIterator> is a standalone image line, like "(see image.jpg)".  If so also returns
		 * the file name.
		 */
		protected bool IsStandaloneImage (LineIterator lineIterator, out Path file)
			{
			string betweenParens, ignore;
			if (!IsParenTagLine(lineIterator, out betweenParens))
				{
				file = null;
				return false;
				}

			return IsImageTagContent(betweenParens, out ignore, out file);
			}


		/* Function: IsBulletLine
		 * Returns whether the line specified by <LineIterator> starts with a bullet, and if so, returns the raw content and indent.
		 */
		public bool IsBulletLine (LineIterator lineIterator, out string content, out char bulletChar, out int indent)
			{
			TokenIterator start, end;
			lineIterator.GetBounds(LineBoundsMode.CommentContent, out start, out end);

			if (start.Character != '-' && start.Character != '*' && start.Character != '+' && !start.MatchesToken("o", false))
				{
				content = null;
				bulletChar = '\0';
				indent = 0;
				return false;
				}

			bulletChar = start.Character;
			start.Next();

			if (start.FundamentalType != FundamentalType.Whitespace)
				{
				content = null;
				bulletChar = '\0';
				indent = 0;
				return false;
				}

			start.Next();

			if (start >= end || (start.FundamentalType != FundamentalType.Text && start.FundamentalType != FundamentalType.Symbol) )
				{
				content = null;
				bulletChar = '\0';
				indent = 0;
				return false;
				}

			content = start.TextBetween(end);
			indent = lineIterator.Indent(LineBoundsMode.CommentContent);
			return true;
			}


		/* Function: IsDefinitionLine
		 * Returns whether the line specified by <LineIterator> is a line from a definition list, and if so, returns the content and
		 * indent.
		 */
		public bool IsDefinitionLine (LineIterator lineIterator, out string leftSide, out string rightSide, out int indent)
			{
			TokenIterator start, end, token;
			lineIterator.GetBounds(LineBoundsMode.CommentContent, out start, out end);

			token = start;
			FundamentalType lastType = token.FundamentalType;
			token.Next();

			while (token < end)
				{
				if (token.Character == '-' && lastType == FundamentalType.Whitespace)
					{
					TokenIterator next = token;
					next.Next();

					if (next.FundamentalType == FundamentalType.Whitespace)
						{  break;  }
					}

				lastType = token.FundamentalType;
				token.Next();
				}

			if (token >= end)
				{
				leftSide = null;
				rightSide = null;
				indent = 0;
				return false;
				}

			TokenIterator endOfLeftSide = token;
			TokenIterator startOfRightSide = token;

			endOfLeftSide.Previous(2);
			while (endOfLeftSide.FundamentalType == FundamentalType.Whitespace)
				{  endOfLeftSide.Previous();  }
			endOfLeftSide.Next();

			startOfRightSide.Next(2);
			while (startOfRightSide.FundamentalType == FundamentalType.Whitespace)
				{  startOfRightSide.Next();  }

			if (endOfLeftSide <= start || startOfRightSide >= end)
				{
				leftSide = null;
				rightSide = null;
				indent = 0;
				return false;
				}

			leftSide = start.TextBetween(endOfLeftSide);
			rightSide = startOfRightSide.TextBetween(end);
			indent = lineIterator.Indent(LineBoundsMode.CommentContent);
			return true;
			}


		/* Function: IsURLProtocol
		 * Returns whether the passed string is a valid URL protocol.  Must not include the colon.
		 */
		public bool IsURLProtocol (string input)
			{
			return config.URLProtocols.Contains(input);
			}


		/* Function: IsAtLinkKeyword
		 * Returns whether the passed string is an "at" link keyword, such as in "<e-mail me at email@address.com>".
		 */
		protected bool IsAtLinkKeyword (string keyword)
			{
			return config.AtLinkKeywords.Contains(keyword);
			}


		/* Function: ParseBody
		 * Parses the content between two <LineIterators> and adds its content to the <Topic> in <NDMarkup> as its body.
		 * Also extracts the summary from it and adds it to the <Topic>.
		 */
		protected void ParseBody (LineIterator firstContentLine, LineIterator endOfContent, Topic topic, bool inlineFormattingOnly = false)
			{
			LineIterator line = firstContentLine;
			StringBuilder body = new StringBuilder();

			bool prevLineBlank = true;
			StringBuilder paragraph = null;
			bool prevParagraphLineEndsSentence = false;
			int definitionIndent = -1;
			List<int> bulletIndents = null;
			HeadingType lastHeadingType = HeadingType.Generic;

			// Temp storage for the Is functions.
			char blockChar;
			BlockType blockType;
			int indent;
			char leadingCharacter;
			string tempString, tempString2;
			Path filePath;
			Language language;
			HeadingType headingType;

			while (line < endOfContent)
				{

				// Preformatted blocks
				// (start code)
				// --- code

				if (!inlineFormattingOnly &&
					IsStartBlockLine(line, out blockChar, out blockType, out language))
					{
					CloseAllBlocks(ref paragraph, ref definitionIndent, ref bulletIndents, body);

					List<string> codeLines = new List<string>();
					int sharedIndent = -1;
					line.Next();

					string rawCodeLine;
					int rawCodeLineIndent;
					char newBlockChar;
					BlockType newBlockType;
					Language newLanguage;

					while (line < endOfContent)
						{
						if (IsEndBlockLine(line, out newBlockChar) && newBlockChar == blockChar)
							{
							line.Next();
							break;
							}
						else if (IsStartBlockLine(line, out newBlockChar, out newBlockType, out newLanguage) && newBlockChar == blockChar)
							{
							// Stay on this line so we pick it up again on the next iteration.
							break;
							}
						else
							{
							GetPreformattedLine(line, out rawCodeLine, out rawCodeLineIndent);
							AddRawCodeLineToList(rawCodeLine, rawCodeLineIndent, codeLines, ref sharedIndent);
							line.Next();
							}
						}

					if (sharedIndent != -1)
						{
						body.Append("<pre");

						if (blockType == BlockType.Code)
							{  body.Append(" type=\"code\"");  }
						else if (blockType == BlockType.Prototype)
							{  body.Append(" type=\"prototype\"");  }
						else
							{  language = null;  }

						if (language != null)
							{
							body.Append(" language=\"");
							body.EntityEncodeAndAppend(language.Name);
							body.Append('"');
							}

						body.Append('>');

						AddRawCodeLinesToNDMarkup(codeLines, body, sharedIndent);

						body.Append("</pre>");
						}

					prevLineBlank = false;
					}


				// Standalone preformatted blocks
				// > code

				else if (!inlineFormattingOnly &&
						  IsStandalonePreformattedLine(line, out tempString, out indent, out leadingCharacter))
					{
					CloseAllBlocks(ref paragraph, ref definitionIndent, ref bulletIndents, body);

					List<string> codeLines = new List<string>();
					int sharedIndent = -1;
					char firstLeadingCharacter = leadingCharacter;

					do
						{
						AddRawCodeLineToList(tempString, indent, codeLines, ref sharedIndent);
						line.Next();
						}
					while (line < endOfContent &&
							 IsStandalonePreformattedLine(line, out tempString, out indent, out leadingCharacter) &&
							 leadingCharacter == firstLeadingCharacter);

					if (sharedIndent != -1)
						{
						body.Append("<pre>");
						AddRawCodeLinesToNDMarkup(codeLines, body, sharedIndent);
						body.Append("</pre>");
						}

					prevLineBlank = false;
					}


				// Standalone image lines
				// (see image.jpg)

				else if (!inlineFormattingOnly &&
						  IsStandaloneImage(line, out filePath))
					{
					CloseAllBlocks(ref paragraph, ref definitionIndent, ref bulletIndents, body);

					int rawTextStart, rawTextEnd;
					line.GetRawTextBounds(LineBoundsMode.CommentContent, out rawTextStart, out rawTextEnd);

					body.Append("<image type=\"standalone\" originaltext=\"");
					body.EntityEncodeAndAppend(line.Tokenizer.RawText, rawTextStart, rawTextEnd - rawTextStart);
					body.Append("\" target=\"");
					body.EntityEncodeAndAppend(filePath);
					body.Append("\">");

					prevLineBlank = false;
					line.Next();
					}


				// Bullet lists
				// - bullet

				else if ( !inlineFormattingOnly &&
						   (prevLineBlank || (bulletIndents != null && bulletIndents.Count > 0)) &&
						   IsBulletLine(line, out tempString, out leadingCharacter, out indent) )
					{
					if (bulletIndents == null)
						{  bulletIndents = new List<int>();  }

					if (bulletIndents.Count == 0)
						{
						CloseAllBlocks(ref paragraph, ref definitionIndent, ref bulletIndents, body);
						bulletIndents.Add(indent);
						body.Append("<ul><li><p>");
						}
					else
						{
						CloseParagraph(ref paragraph, body);

						// Increase indent if we're at least two spaces ahead of the top one.
						if (indent >= bulletIndents[ bulletIndents.Count-1 ] + 2)
							{
							body.Append("<ul><li><p>");
							bulletIndents.Add(indent);
							}

						else
							{
							// Decrease indent until there's only one left or we're between the top two.
							while (bulletIndents.Count >= 2 && indent <= bulletIndents[ bulletIndents.Count-2 ])
								{
								body.Append("</li></ul>");
								bulletIndents.RemoveAt( bulletIndents.Count-1 );
								}

							// Decrease the indent one last time if there's at least two and we're closer to the lower
							// one than the higher one.  Tie goes to the lower.
							if (bulletIndents.Count >= 2 &&
								indent - bulletIndents[ bulletIndents.Count-2 ] <= bulletIndents[ bulletIndents.Count-1 ] - indent)
								{
								body.Append("</li></ul>");
								bulletIndents.RemoveAt( bulletIndents.Count-1 );
								}

							// Update the top position so that future bullets are always relative.
							bulletIndents[ bulletIndents.Count-1 ] = indent;

							body.Append("</li><li><p>");
							}
						}

					if (paragraph == null)
						{  paragraph = new StringBuilder();  }

					paragraph.Append(tempString);
					prevParagraphLineEndsSentence = LineEndProbablyEndsSentenceRegex().IsMatch(tempString);

					prevLineBlank = false;
					line.Next();
					}


				// Definition Lists
				// item - definition

				else if (!inlineFormattingOnly &&
						   (prevLineBlank || definitionIndent != -1) &&
						   IsDefinitionLine(line, out tempString, out tempString2, out indent) )
					{
					if (definitionIndent == -1)
						{
						CloseAllBlocks(ref paragraph, ref definitionIndent, ref bulletIndents, body);
						definitionIndent = indent;
						body.Append("<dl>");
						}
					else
						{
						CloseParagraph(ref paragraph, body);
						body.Append("</dd>");
						}

					bool isSymbol = ( topic.IsEnum || (topic.IsList && lastHeadingType != HeadingType.Parameters) ) ;

					if (isSymbol)
						{  body.Append("<ds>");  }
					else
						{  body.Append("<de>");  }

					ParseTextBlock(tempString, body);

					if (isSymbol)
						{  body.Append("</ds>");  }
					else
						{  body.Append("</de>");  }

					body.Append("<dd><p>");

					if (paragraph == null)
						{  paragraph = new StringBuilder();  }

					paragraph.Append(tempString2);
					prevParagraphLineEndsSentence = LineEndProbablyEndsSentenceRegex().IsMatch(tempString2);

					prevLineBlank = false;
					line.Next();
					}


				// Headings
				// heading:

				else if (!inlineFormattingOnly &&
						  prevLineBlank && IsHeading(line, out tempString, out headingType))
					{
					CloseAllBlocks(ref paragraph, ref definitionIndent, ref bulletIndents, body);

					body.Append("<h");

					if (headingType == HeadingType.Parameters)
						{  body.Append(" type=\"parameters\"");  }

					body.Append('>');
					ParseTextBlock(tempString, body);
					body.Append("</h>");

					// We want to be able to start new things directly under headings as if they were blank lines.
					prevLineBlank = true;  // Deliberate!

					// We need a separate variable for lastHeadingType because IsHeading has to set the "out" variable whether
					// it's successful or not.  If we used one variable it would be overwritten every time we checked a line to see
					// if it's a heading.
					lastHeadingType = headingType;

					line.Next();
					}


				// Blank or horizontal lines

				else if (line.IsEmpty(LineBoundsMode.CommentContent) || LineFinder.IsHorizontalLine(line))
					{
					CloseParagraph(ref paragraph, body, inlineFormattingOnly);
					prevLineBlank = true;
					line.Next();
					}


				// Any other line of content.

				else
					{
					if (paragraph == null)
						{  paragraph = new StringBuilder();  }

					if (paragraph.Length > 0)
						{
						if (prevParagraphLineEndsSentence)
							{  paragraph.Append("  ");  }
						else
							{  paragraph.Append(' ');  }
						}

					else // no previous paragraph
						{
						if (definitionIndent != -1)
							{
							indent = line.Indent(LineBoundsMode.CommentContent);
							if (indent < definitionIndent + 2)
								{  CloseAllBlocks(ref paragraph, ref definitionIndent, ref bulletIndents, body);  }
							}

						else if (bulletIndents != null)
							{
							indent = line.Indent(LineBoundsMode.CommentContent);
							while (bulletIndents.Count > 0 && indent < bulletIndents[ bulletIndents.Count-1 ] + 2)
								{
								body.Append("</li></ul>");
								bulletIndents.RemoveAt( bulletIndents.Count-1 );
								}
							}

						body.Append("<p>");
						}

					string lineString = line.String(LineBoundsMode.CommentContent);
					paragraph.Append(lineString);
					prevParagraphLineEndsSentence = LineEndProbablyEndsSentenceRegex().IsMatch(lineString);

					prevLineBlank = false;
					line.Next();
					}
				}


			CloseAllBlocks(ref paragraph, ref definitionIndent, ref bulletIndents, body, inlineFormattingOnly);

			if (body.Length > 0)
				{
				topic.Body = body.ToString();
				MakeSummaryFromBody(topic);
				}
			}


		/* Function: AddRawCodeLineToList
		 * Adds the line of text to the list of code lines, keeping the shared indent updated as it goes.  It does not convert its content
		 * into <NDMarkup>.  The shared indent should start at -1 for a block, and it will remain there if there's never been a line of content.
		 */
		protected void AddRawCodeLineToList (string rawCodeLine, int rawCodeLineIndent, List<string> codeLines, ref int sharedIndent)
			{
			if (rawCodeLine.Length == 0)
				{
				// Ignore leading empty lines.  Also don't let any empty lines affect the shared indent.
				if (codeLines.Count != 0)
					{  codeLines.Add(rawCodeLine);  }
				}

			else
				{
				if (sharedIndent == -1 || rawCodeLineIndent < sharedIndent)
					{  sharedIndent = rawCodeLineIndent;  }

				codeLines.Add(rawCodeLine);
				}
			}


		/* Function: AddRawCodeLinesToNDMarkup
		 * Adds the list of raw code lines to NDMarkup, removing the shared indent as it goes.
		 */
		protected void AddRawCodeLinesToNDMarkup (List<string> rawCodeLines, StringBuilder ndMarkup, int sharedIndent)
			{
			// Remove leading and trailing line breaks.

			while (rawCodeLines.Count > 0 && rawCodeLines[0].Length == 0)
				{  rawCodeLines.RemoveAt(0);  }

			while (rawCodeLines.Count > 0 && rawCodeLines[ rawCodeLines.Count - 1 ].Length == 0)
				{  rawCodeLines.RemoveAt( rawCodeLines.Count - 1 );  }


			// Add to list, converting entity chars, removing shared indent, and adding <br>s between lines.

			for (int i = 0; i < rawCodeLines.Count; i++)
				{
				if (i > 0)
					{  ndMarkup.Append("<br>");  }

				if (rawCodeLines[i].Length != 0)
					{  ndMarkup.EntityEncodeAndAppend(rawCodeLines[i], sharedIndent, rawCodeLines[i].Length - sharedIndent);  }
				}
			}


		/* Function: CloseParagraph
		 * If the paragraph isn't null or empty, convert its contents to <NDMarkup>, add it to the body along with a closing </p>,
		 *	and empty it.
		 */
		protected void CloseParagraph (ref StringBuilder paragraph, StringBuilder body, bool inlineFormattingOnly = false)
			{
			if (paragraph != null && paragraph.Length > 0)
				{
				ParseTextBlock(paragraph.ToString(), body, inlineFormattingOnly);
				body.Append("</p>");

				paragraph.Remove(0, paragraph.Length);
				}
			}


		/* Function: CloseAllBlocks
		 *
		 * If any of the blocks are open it will close them and add the content to the body.
		 *
		 * Parameters:
		 *
		 *		paragraph - If this isn't null or empty, converts its contents to <NDMarkup>, adds it to the body along with a closing </p>,
		 *						and empties it.
		 *		definitionIndent - If this isn't -1, adds a closing </dd></dl> to the body and sets it to -1.
		 *		bulletIndents - If this isn't null or empty, adds a closing </li></ul> for every entry on the list and empties it.
		 *
		 */
		protected void CloseAllBlocks (ref StringBuilder paragraph, ref int definitionIndent, ref List<int> bulletIndents, StringBuilder body,
													bool inlineFormattingOnly = false)
			{
			CloseParagraph(ref paragraph, body, inlineFormattingOnly);

			if (definitionIndent != -1)
				{
				body.Append("</dd></dl>");
				definitionIndent = -1;
				}

			if (bulletIndents != null && bulletIndents.Count > 0)
				{
				for (int i = 0; i < bulletIndents.Count; i++)
					{  body.Append("</li></ul>");  }

				bulletIndents.Clear();
				}
			}


		/* Function: ParseTextBlock
		 * Parses a block of text for inline tags and adds it to the StringBuilder as <NDMarkup>.
		 */
		protected void ParseTextBlock (string input, StringBuilder output, bool inlineFormattingOnly = false)
			{
			Tokenizer tokenizer = new Tokenizer(input, tabWidth: EngineInstance.Config.TabWidth);

			// The order of these function calls is important.  Read each of their descriptions.
			MarkPossibleFormattingTags(tokenizer);
			MarkPossibleLinkTags(tokenizer);

			if (!inlineFormattingOnly)
				{  MarkPossibleInlineImageTags(tokenizer);  }

			MarkEMailAddresses(tokenizer);
			MarkURLs(tokenizer);

			FinalizeLinkTags(tokenizer);

			if (!inlineFormattingOnly)
				{  FinalizeInlineImageTags(tokenizer);  }

			FinalizeFormattingTags(tokenizer);

			MarkedTokensToNDMarkup(tokenizer, output);
			}


		/* Function: MarkPossibleFormattingTags
		 * Goes through the passed <Tokenizer> and marks asterisks and underscores with <CommentParsingType.PossibleOpeningTag> and
		 * <CommentParsingType.PossibleClosingTag> if they can possibly be interpreted as bold and underline formatting.
		 */
		protected void MarkPossibleFormattingTags (Tokenizer tokenizer)
			{
			for (TokenIterator token = tokenizer.FirstToken; token.IsInBounds; token.Next())
				{
				char character = token.Character;

				if (character == '_' || character == '*')
					{
					// Possible opening symbols

					TokenIterator next = token;
					next.Next();

					TokenIterator prev = token;
					prev.Previous();

					// Prevent *=, __, ** from counting.
					if ( (character == '*' && (next.Character == '=' || next.Character == '*' || prev.Character == '*')) ||
						  character == '_' && (next.Character == '_' || prev.Character == '_') )
						{  goto ClosingSymbols;  }

					// The next token must also be non-whitespace.
					if (next.FundamentalType == FundamentalType.Whitespace)
						{  goto ClosingSymbols;  }

					// Move past the content immediately before it.
					while (prev.FundamentalType != FundamentalType.Whitespace &&
							 prev.FundamentalType != FundamentalType.Null)
						{
						prev.Previous();
						}

					prev.Next();

					// If there's still intervening content, it must be entirely acceptable characters like ( and ".
					if (prev < token)
						{
						if ( tokenizer.MatchTextBetween(IsAcceptableBeforeOpeningTagRegex(), prev, token).Success == false )
							{  goto ClosingSymbols;  }
						}

					token.CommentParsingType = CommentParsingType.PossibleOpeningTag;
					continue;


					// Possible closing symbols

					ClosingSymbols:

					prev = token;
					prev.Previous();

					// Prevent *=, **, __ from counting.
					if ( (character == '*' && (next.Character == '=' || next.Character == '*' || prev.Character == '*')) ||
						  character == '_' && (next.Character == '_' || prev.Character == '_') )
						{  continue;  }

					// The previous token must also be non-whitespace.
					if (prev.FundamentalType == FundamentalType.Whitespace)
						{  continue;  }

					// Skip past the content immediately after it.
					TokenIterator end = next;

					while (end.FundamentalType != FundamentalType.Whitespace &&
							 end.FundamentalType != FundamentalType.Null)
						{  end.Next();  }

					// If there's still intervening content, it must be entirely acceptable characters like ) and ".
					if (end > next)
						{
						if ( tokenizer.MatchTextBetween(IsAcceptableAfterClosingTagRegex(), next, end).Success == false )
							{  continue;  }
						}

					token.CommentParsingType = CommentParsingType.PossibleClosingTag;
					}
				}
			}


		/* Function: MarkPossibleLinkTags
		 * Goes through the passed <Tokenizer> and marks angle brackets with <CommentParsingType.PossibleOpeningTag> and
		 * <CommentParsingType.PossibleClosingTag> if they can possibly be interpreted as links.  Call <MarkPossibleFormattingTags()>
		 * prior to this in order to allow links to be tolerant of formatting tags surrounding them.  Call
		 * <MarkPossibleInlineImageTags()> after this so marked parentheses don't screw it up.
		 */
		protected void MarkPossibleLinkTags (Tokenizer tokenizer)
			{
			for (TokenIterator token = tokenizer.FirstToken; token.IsInBounds; token.Next())
				{
				char character = token.Character;


				// Possible opening symbols

				if (character == '<')
					{
					TokenIterator next = token;
					next.Next();

					TokenIterator prev = token;
					prev.Previous();

					// Prevent <-, <=, <<, <> from counting.
					if (next.Character == '-' || next.Character == '=' || next.Character == '<' || prev.Character == '<' || next.Character == '>')
						{  continue;  }

					// The next token must also be non-whitespace.
					if (next.FundamentalType == FundamentalType.Whitespace)
						{  continue;  }

					bool slashOrDash = (prev.Character == '/' || prev.Character == '-');

					// Move past the content immediately before it.
					while (prev.FundamentalType != FundamentalType.Whitespace &&
							 prev.FundamentalType != FundamentalType.Null &&
							 prev.CommentParsingType != CommentParsingType.PossibleOpeningTag)
						{
						prev.Previous();
						}

					// An opening link tag can only be preceded by another opening tag without intervening whitespace if
					// it's a bold or underline tag.
					if (prev.CommentParsingType == CommentParsingType.PossibleOpeningTag)
						{
						if (prev.Character != '*' && prev.Character != '_')
							{  continue;  }
						}

					// Move back past the null, whitespace, or opening tag.
					prev.Next();

					// If there's still intervening content, it must be entirely acceptable characters like ( and ", or it must end with a
					// slash or a dash.
					if (prev < token)
						{
						if (!slashOrDash &&
							tokenizer.MatchTextBetween(IsAcceptableBeforeOpeningTagRegex(), prev, token).Success == false)
							{  continue;  }
						}

					token.CommentParsingType = CommentParsingType.PossibleOpeningTag;
					}


				// Possible closing symbols

				else if (character == '>')
					{
					TokenIterator next = token;
					next.Next();

					TokenIterator prev = token;
					prev.Previous();

					// Prevent >>, ->, =>, <> from counting.
					if (prev.Character == '-' || prev.Character == '=' || prev.Character == '>' || next.Character == '>' || prev.Character == '<')
						{  continue;  }

					// The previous token must also be non-whitespace.
					if (prev.FundamentalType == FundamentalType.Whitespace)
						{  continue;  }

					// Skip any acceptable suffixes, like 's.  We pick the longest match we can find.
					int longestSuffix = 0;
					foreach (string suffix in config.AcceptableLinkSuffixes)
						{
						if (suffix.Length > longestSuffix && next.MatchesAcrossTokens(suffix, true))
							{  longestSuffix = suffix.Length;  }
						}

					if (longestSuffix > 0)
						{  next.NextByCharacters(longestSuffix);  }

					// Skip past the content immediately after it.
					TokenIterator end = next;

					while (end.FundamentalType != FundamentalType.Whitespace &&
								end.FundamentalType != FundamentalType.Null &&
								end.CommentParsingType != CommentParsingType.PossibleClosingTag)
						{  end.Next();  }

					// A closing link tag can only be followed by another closing tag without intervening whitespace if
					// it's a bold or underline tag.
					if (end.CommentParsingType == CommentParsingType.PossibleClosingTag)
						{
						if (end.Character != '*' && end.Character != '_')
							{  continue;  }
						}

					// If there's still intervening content, it must be entirely acceptable characters like ) and ", or it must
					// start with a slash or dash.
					if (next < end)
						{
						if (next.Character != '/' && next.Character != '-' &&
							tokenizer.MatchTextBetween(IsAcceptableAfterClosingTagRegex(), next, end).Success == false)
							{  continue;  }
						}

					token.CommentParsingType = CommentParsingType.PossibleClosingTag;
					}
				}

			}


		/* Function: MarkPossibleInlineImageTags
		 * Goes through the passed <Tokenizer> and marks parentheses with <CommentParsingType.PossibleOpeningTag> and
		 * <CommentParsingType.PossibleClosingTag> if they can possibly be used for inline images.  This does NOT validate
		 * the content of the parentheses, merely that they are acceptable candidates.
		 */
		protected void MarkPossibleInlineImageTags (Tokenizer tokenizer)
			{
			for (TokenIterator token = tokenizer.FirstToken; token.IsInBounds; token.Next())
				{
				char character = token.Character;


				// Possible opening symbols

				if (character == '(')
					{
					TokenIterator next = token;
					next.Next();

					TokenIterator prev = token;
					prev.Previous();

					// The preceding token must be whitespace.
					if (prev.FundamentalType != FundamentalType.Whitespace && prev.FundamentalType != FundamentalType.Null)
						{  continue;  }

					// The next token must be non-whitespace.
					if (next.FundamentalType == FundamentalType.Whitespace)
						{  continue;  }

					token.CommentParsingType = CommentParsingType.PossibleOpeningTag;
					}


				// Possible closing symbols

				else if (character == ')')
					{
					TokenIterator next = token;
					next.Next();

					TokenIterator prev = token;
					prev.Previous();

					// The previous token must be non-whitespace.
					if (prev.FundamentalType == FundamentalType.Whitespace)
						{  continue;  }

					// There may be a single acceptable non-whitespace token after it.
					if ( next.IsInBounds &&
						 IsAcceptableAfterInlineImageRegex().Match(tokenizer.RawText, next.RawTextIndex,
																					   next.RawTextLength).Success == true )
						{  next.Next();  }

					// After that it must be whitespace.
					if (next.FundamentalType != FundamentalType.Whitespace && next.FundamentalType != FundamentalType.Null)
						{  continue;  }

					token.CommentParsingType = CommentParsingType.PossibleClosingTag;
					}
				}

			}


		/* Function: FinalizeLinkTags
		 * Goes through the passed <Tokenizer> and converts all angle brackets marked as <CommentParsingType.PossibleOpeningTag> and
		 * <CommentParsingType.PossibleClosingTag> to <CommentParsingType.OpeningTag>, <CommentParsingType.ClosingTag>, or back
		 * to <CommentParsingType.Null>.  It makes sure every opening tag has a closing tag and removes possible tag markings on other
		 * symbols between them.  Call this before <FinalizeInlineImageTags()> and <FinalizeFormattingTags()> because parentheses, asterisks,
		 * and underscores can be part of a link's content.
		 */
		protected void FinalizeLinkTags (Tokenizer tokenizer)
			{
			TokenIterator token = tokenizer.FirstToken;

			while (token.IsInBounds)
				{
				if (token.CommentParsingType == CommentParsingType.PossibleOpeningTag && token.Character == '<')
					{
					TokenIterator lookahead = token;

					for (;;)
						{
						lookahead.Next();

						// If there's another opening angle bracket or there's no closing one, this one is unacceptable.
						if (!lookahead.IsInBounds ||
							 (lookahead.CommentParsingType == CommentParsingType.PossibleOpeningTag && lookahead.Character == '<'))
							{
							token.CommentParsingType = CommentParsingType.Null;
							token.Next();
							break;
							}

						// If there is a closing tag, mark the start and close and eat any possible tags between them.
						else if (lookahead.CommentParsingType == CommentParsingType.PossibleClosingTag && lookahead.Character == '>')
							{
							token.CommentParsingType = CommentParsingType.OpeningTag;
							lookahead.CommentParsingType = CommentParsingType.ClosingTag;

							while (token < lookahead)
								{
								if (token.CommentParsingType == CommentParsingType.PossibleOpeningTag ||
									 token.CommentParsingType == CommentParsingType.PossibleClosingTag)
									{  token.CommentParsingType = CommentParsingType.Null;  }

								token.Next();
								}

							break;
							}
						}
					}

				else if (token.CommentParsingType == CommentParsingType.PossibleClosingTag && token.Character == '>')
					{
					// Closing tag without an opening tag preceding it.
					token.CommentParsingType = CommentParsingType.Null;
					token.Next();
					}

				else
					{  token.Next();  }
				}
			}


		/* Function: FinalizeInlineImageTags
		 * Goes through the passed <Tokenizer> and converts all parentheses marked as <CommentParsingType.PossibleOpeningTag> and
		 * <CommentParsingType.PossibleClosingTag> to <CommentParsingType.OpeningTag>, <CommentParsingType.ClosingTag>, or
		 * back to <CommentParsingType.Null>.  It makes sure every opening tag has a closing tag, the content is in the right format, and
		 * removes possible tag markings on other symbols between them.  Call this before <FinalizeFormattingTags()> because asterisks
		 * and underscores may be part of a tag's content.
		 */
		protected void FinalizeInlineImageTags (Tokenizer tokenizer)
			{
			TokenIterator token = tokenizer.FirstToken;

			while (token.IsInBounds)
				{
				if (token.CommentParsingType == CommentParsingType.PossibleOpeningTag && token.Character == '(')
					{
					TokenIterator lookahead = token;

					for (;;)
						{
						lookahead.Next();

						// If there's another opening paren or there's no closing one, this one is unacceptable.
						if (!lookahead.IsInBounds ||
							 (lookahead.CommentParsingType == CommentParsingType.PossibleOpeningTag && lookahead.Character == '('))
							{
							token.CommentParsingType = CommentParsingType.Null;
							token.Next();
							break;
							}

						// If there is a closing tag, try to validate the content between them.
						else if (lookahead.CommentParsingType == CommentParsingType.PossibleClosingTag && lookahead.Character == ')')
							{
							TokenIterator contentStart = token;
							contentStart.Next();

							bool acceptable = (contentStart < lookahead);

							if (acceptable)
								{
								string betweenParens = tokenizer.TextBetween(contentStart, lookahead);

								Path ignoreFile;
								string ignoreString;
								acceptable = IsImageTagContent(betweenParens, out ignoreString, out ignoreFile);
								}

							// Eat all the other tags between them.
							if (acceptable)
								{
								token.CommentParsingType = CommentParsingType.OpeningTag;
								lookahead.CommentParsingType = CommentParsingType.ClosingTag;

								while (token < lookahead)
									{
									if (token.CommentParsingType == CommentParsingType.PossibleOpeningTag ||
										 token.CommentParsingType == CommentParsingType.PossibleClosingTag)
										{  token.CommentParsingType = CommentParsingType.Null;  }

									token.Next();
									}
								}
							else
								{
								token.CommentParsingType = CommentParsingType.Null;
								token.Next();
								}

							break;
							}
						}
					}

				else if (token.CommentParsingType == CommentParsingType.PossibleClosingTag && token.Character == ')')
					{
					// Closing tag without an opening tag preceding it.
					token.CommentParsingType = CommentParsingType.Null;
					token.Next();
					}

				else
					{  token.Next();  }
				}
			}


		/* Function: FinalizeFormattingTags
		 * Goes through the passed <Tokenizer> and converts all asterisks and underscores marked as <CommentParsingType.PossibleOpeningTag>
		 * and <CommentParsingType.PossibleClosingTag> to <CommentParsingType.OpeningTag>, <CommentParsingType.ClosingTag>, or back
		 * to <CommentParsingType.Null>.  It makes sure every opening tag has a closing tag.  Call this after <FinalizeLinkTags()> and
		 * <FinalizeInlineImageTags()> so that these are the only tokens marked as possible tags left.
		 */
		protected void FinalizeFormattingTags (Tokenizer tokenizer)
			{
			TokenIterator token = tokenizer.FirstToken;

			while (token.IsInBounds)
				{
				if (token.CommentParsingType == CommentParsingType.PossibleOpeningTag)
					{
					TokenIterator lookahead = token;

					for (;;)
						{
						lookahead.Next();

						// If there's another opening symbol of the same type or there's no closing one, this one is unacceptable.
						if (!lookahead.IsInBounds || (lookahead.CommentParsingType == CommentParsingType.PossibleOpeningTag &&
							 lookahead.Character == token.Character))
							{
							token.CommentParsingType = CommentParsingType.Null;
							token.Next();
							break;
							}

						// If we hit a definite tag, skip it.
						else if (lookahead.CommentParsingType == CommentParsingType.OpeningTag)
							{
							do
								{  lookahead.Next();  }
							while (lookahead.CommentParsingType != CommentParsingType.ClosingTag);

							// The first line of the loop will advance past the closing tag.
							}

						// If we hit a definite closing tag without hitting an opening tag first, it means this tag can't have an end while being nested
						// properly, like the first asterisk in "_startunderline *startbold endunderline_".
						else if (lookahead.CommentParsingType == CommentParsingType.ClosingTag)
							{
							token.CommentParsingType = CommentParsingType.Null;
							token.Next();
							break;
							}

						// Success if we hit a closing tag of the same type before breaking out of this loop.
						else if (lookahead.CommentParsingType == CommentParsingType.PossibleClosingTag && lookahead.Character == token.Character)
							{
							token.CommentParsingType = CommentParsingType.OpeningTag;
							lookahead.CommentParsingType = CommentParsingType.ClosingTag;
							break;
							}
						}
					}

				else if (token.CommentParsingType == CommentParsingType.PossibleClosingTag)
					{
					// Closing tag without an opening tag preceding it.
					token.CommentParsingType = CommentParsingType.Null;
					token.Next();
					}

				else
					{  token.Next();  }
				}
			}


		/* Function: MarkURLs
		 * Goes through the passed <Tokenizer> and marks all tokens than are part of an URL with <CommentParsingType.URL>.
		 * This should be called after the MarkPossibleTags functions so it can reclaim any of their characters if it needs to,
		 * but before the FinalizeTags functions so it's not invalidating them.
		 */
		protected void MarkURLs (Tokenizer tokenizer)
			{
			TokenIterator token = tokenizer.FirstToken;

			while (token.IsInBounds)
				{
				Match match = FindURLAnywhereInLineRegex().Match(tokenizer.RawText, token.RawTextIndex);

				if (match.Success == false)
					{  break;  }


				// Has to land on token boundaries.

				if (match.Index > token.RawTextIndex)
					{
					int tokensToMatch = token.TokensInCharacters( match.Index - token.RawTextIndex );
					if (tokensToMatch == -1)
						{
						token.Next();
						continue;
						}
					else
						{
						token.Next(tokensToMatch);
						}
					}

				int tokensInMatch = token.TokensInCharacters( match.Length );
				if (tokensInMatch == -1)
					{
					token.Next();
					continue;
					}


				// Has to have a registered protocol.

				if (IsURLProtocol(match.Groups[1].Value) == false)
					{
					token.Next();
					continue;
					}


				// All okay.  Mark it.

				token.SetCommentParsingTypeByCharacters(CommentParsingType.URL, match.Length);
				token.Next(tokensInMatch);
				}
			}


		/* Function: MarkEMailAddresses
		 * Goes through the passed <Tokenizer> and marks all tokens than are part of an e-mail address with
		 * <CommentParsingType.EMail>.  This should be called after the MarkPossibleTags functions so it can reclaim any of their
		 * characters if it needs to, but before the FinalizeTags functions so it's not invalidating them.
		 */
		protected void MarkEMailAddresses (Tokenizer tokenizer)
			{
			TokenIterator token = tokenizer.FirstToken;

			while (token.IsInBounds)
				{
				Match match = FindEMailAnywhereInLineRegex().Match(tokenizer.RawText, token.RawTextIndex);

				if (match.Success == false)
					{  break;  }


				// Has to land on token boundaries.

				if (match.Index > token.RawTextIndex)
					{
					int tokensToMatch = token.TokensInCharacters( match.Index - token.RawTextIndex );
					if (tokensToMatch == -1)
						{
						token.Next();
						continue;
						}
					else
						{
						token.Next(tokensToMatch);
						}
					}

				int tokensInMatch = token.TokensInCharacters( match.Length );
				if (tokensInMatch == -1)
					{
					token.Next();
					continue;
					}


				// All okay.  Mark it.

				token.SetCommentParsingTypeByCharacters(CommentParsingType.EMail, match.Length);
				token.Next(tokensInMatch);
				}
			}


		/* Function: MarkedTokensToNDMarkup
		 * Appends the tokenizer's content to the StringBuilder as NDMarkup.  All tokens marked with types like <CommentParsingType.URL>
		 * and <CommentParsingType.OpeningTag> will be converted to tags.
		 */
		protected void MarkedTokensToNDMarkup(Tokenizer tokenizer, StringBuilder output)
			{
			TokenIterator tokenIterator = tokenizer.FirstToken;
			bool eatUnderscores = false;

			// We can assume all tags are valid and paired correctly so we don't need to track their state to make sure everything
			// gets closed correctly.

			while (tokenIterator.IsInBounds)
				{
				if (tokenIterator.CommentParsingType == CommentParsingType.OpeningTag)
					{
					if (tokenIterator.Character == '*')
						{  output.Append("<b>");  }

					else if (tokenIterator.Character == '_')
						{
						output.Append("<u>");

						// Check if we need to convert internal underscores to spaces.
						TokenIterator lookahead = tokenIterator;
						lookahead.Next();

						while ( !(lookahead.CommentParsingType == CommentParsingType.ClosingTag && lookahead.Character == '_') )
							{
							if (lookahead.Character == '_')
								{  eatUnderscores = true;  }
							else if (lookahead.FundamentalType == FundamentalType.Whitespace)
								{
								eatUnderscores = false;
								break;
								}

							lookahead.Next();
							}
						}

					else if (tokenIterator.Character == '(' || tokenIterator.Character == '<')
						{
						TokenIterator startOfContent = tokenIterator;
						startOfContent.Next();

						TokenIterator closingTag = startOfContent;

						// We don't have to check the character because there are no other tags allowed in links or inline images.
						do
							{  closingTag.Next();  }
						while (closingTag.CommentParsingType != CommentParsingType.ClosingTag);

						string tagContent = tokenizer.TextBetween(startOfContent, closingTag);

						if (tokenIterator.Character == '(')
							{
							Path file;
							string keyword;
							IsImageTagContent(tagContent, out keyword, out file);
							string name = file.NameWithoutPathOrExtension;

							output.Append("<image type=\"inline\" originaltext=\"(");
							output.EntityEncodeAndAppend(tagContent);
							output.Append(")\" linktext=\"(");
							output.EntityEncodeAndAppend(keyword);
							output.Append(' ');
							output.EntityEncodeAndAppend(name);
							output.Append(")\" target=\"");
							output.EntityEncodeAndAppend(file);
							output.Append("\" caption=\"");
							output.EntityEncodeAndAppend(name);
							output.Append("\">");
							}

						else // character == '<'
							{
							Match match = StartsWithURLProtocolRegex().Match(tagContent);

							if (match.Success && IsURLProtocol(match.Groups[1].Value))
								{
								output.Append("<link type=\"url\" target=\"");
								output.EntityEncodeAndAppend(tagContent);
								output.Append("\">");
								}

							else
								{
								match = IsEMailRegex().Match(tagContent);

								if (match.Success)
									{
									output.Append("<link type=\"email\" target=\"");
									output.EntityEncodeAndAppend(match.Groups[1].Value);
									output.Append("\">");
									}

								else
									{
									// See if we can interpret the link as a named URL or e-mail address.  We can accept the first interpretation we find.
									List<LinkInterpretation> interpretations = LinkInterpretations_NoParameters(tagContent,
																																		  LinkInterpretationFlags.AllowNamedLinks |
																																		  LinkInterpretationFlags.ExcludeLiteral);
									bool found = false;

									if (interpretations != null)
										{
										for (int i = 0; i < interpretations.Count && !found; i++)
											{
											match = StartsWithURLProtocolRegex().Match(interpretations[i].Target);

											if (match.Success && IsURLProtocol(match.Groups[1].Value))
												{
												output.Append("<link type=\"url\" target=\"");
												output.EntityEncodeAndAppend(interpretations[i].Target);
												output.Append("\" text=\"");
												output.EntityEncodeAndAppend(interpretations[i].Text);
												output.Append("\">");

												found = true;
												}
											else
												{
												match = IsEMailRegex().Match(interpretations[i].Target);

												if (match.Success)
													{
													output.Append("<link type=\"email\" target=\"");
													output.EntityEncodeAndAppend(match.Groups[1].Value);
													output.Append("\" text=\"");
													output.EntityEncodeAndAppend(interpretations[i].Text);
													output.Append("\">");

													found = true;
													}
												}
											}
										}

									// If not, it's a Natural Docs link
									if (found == false)
										{
										output.Append("<link type=\"naturaldocs\" originaltext=\"&lt;");
										output.EntityEncodeAndAppend(tagContent);
										output.Append("&gt;\">");
										}
									}
								}
							}

						tokenIterator = closingTag;
						}

					tokenIterator.Next();
					}

				else if (tokenIterator.CommentParsingType == CommentParsingType.ClosingTag)
					{
					if (tokenIterator.Character == '*')
						{  output.Append("</b>");  }
					else if (tokenIterator.Character == '_')
						{
						output.Append("</u>");
						eatUnderscores = false;
						}

					tokenIterator.Next();
					}

				else if (tokenIterator.CommentParsingType == CommentParsingType.URL)
					{
					TokenIterator startOfURL = tokenIterator;

					do
						{  tokenIterator.Next();  }
					while (tokenIterator.CommentParsingType == CommentParsingType.URL);

					output.Append("<link type=\"url\" target=\"");
					output.EntityEncodeAndAppend( tokenizer.RawText, startOfURL.RawTextIndex,
																						 tokenIterator.RawTextIndex - startOfURL.RawTextIndex );
					output.Append("\">");
					}

				else if (tokenIterator.CommentParsingType == CommentParsingType.EMail)
					{
					TokenIterator startOfURL = tokenIterator;

					do
						{  tokenIterator.Next();  }
					while (tokenIterator.CommentParsingType == CommentParsingType.EMail);

					Match match = tokenizer.MatchTextBetween(IsEMailRegex(), startOfURL, tokenIterator);

					output.Append("<link type=\"email\" target=\"");
					output.EntityEncodeAndAppend( match.Groups[1].Value );
					output.Append("\">");
					}

				else if (eatUnderscores && tokenIterator.Character == '_')
					{
					output.Append(' ');
					tokenIterator.Next();
					}

				else
					{
					TokenIterator lookahead = tokenIterator;

					do
						{
						lookahead.Next();
						}
					while (lookahead.IsInBounds && lookahead.CommentParsingType == CommentParsingType.Null &&
								!(eatUnderscores && lookahead.Character == '_'));

					output.EntityEncodeAndAppend(tokenizer.RawText, tokenIterator.RawTextIndex,
																				 lookahead.RawTextIndex - tokenIterator.RawTextIndex);
					tokenIterator = lookahead;
					}
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: config
		 */
		protected Config config;

		/* var: ParenthesesChars
		 * An array of the parentheses characters, for use with IndexOfAny(char[]).
		 */
		protected static char[] ParenthesesChars = { '(', ')' };



		// Group: Regular Expressions
		// __________________________________________________________________________


		/* Regex: LineEndProbablyEndsSentenceRegex
		 * Will match the string if the end of it probably ends a sentence as well.
		 */
		[GeneratedRegex("""[\.\?\!][\*_]?[\)\"\u201d]?$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex LineEndProbablyEndsSentenceRegex();


		/* Regex: IsAcceptableBeforeOpeningTagRegex
		 * Will match the string if all of its characters are acceptable before the beginning of a formatting tag.
		 */
		[GeneratedRegex("""^[\p{Ps}\p{Pi}\"\'\/\-\u00bf\u00a1\*_]+$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsAcceptableBeforeOpeningTagRegex();


		/* Regex: IsAcceptableAfterClosingTagRegex
		 * Will match the string if all of its characters are acceptable after the end of a formatting tag.
		 */
		[GeneratedRegex("""^[\p{Pe}\p{Pf}\"\'\.\,\?\!\:\;\/\-\*_]+$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsAcceptableAfterClosingTagRegex();


		/* Regex: IsAcceptableAfterInlineImageRegex
		 * Will match the string if all of its characters are acceptable after an inline image.
		 */
		[GeneratedRegex("""^[\.\,\?\!\:\;]$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsAcceptableAfterInlineImageRegex();


		/* Regex: StartsWithURLProtocolRegex
		 * Will match the string if it starts with an URL protocol.
		 */
		[GeneratedRegex("""^([a-z0-9\.\-\+]+):""",
								  RegexOptions.Singleline |  RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static private partial Regex StartsWithURLProtocolRegex();


		/* Regex: FindURLAnywhereInLineRegex
		 * Will match instances of an URL appearing without surrounding tags in a line of text.
		 */
		[GeneratedRegex("""
			([a-z0-9\.\-\+]+):

			[a-z0-9_\-\=\~\@\#\%\&\+\/\\\|\*\;\:\?\.\,]+
			[a-z0-9_\-\=\~\@\#\%\&\+\/\\\|\*]
			""",
								  RegexOptions.Singleline |  RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant)]
		static private partial Regex FindURLAnywhereInLineRegex();


		/* Regex: FindEMailAnywhereInLineRegex
		 * Will match instances of an e-mail link or address appearing without surrounding tags in a line of text.
		 */
		[GeneratedRegex("""(?:mailto:)?([a-z0-9_\-\.\+]+\@(?:[a-z0-9_\-]+\.)+[a-z]{2,})""",
								  RegexOptions.Singleline |  RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static private partial Regex FindEMailAnywhereInLineRegex();


		/* Regex: IsEMailRegex
		 * Will match the string if it is an e-mail link or address.
		 */
		[GeneratedRegex("""^(?:mailto:)?([a-z0-9_\-\.\+]+\@(?:[a-z0-9_\-]+\.)+[a-z]{2,})$""",
								  RegexOptions.Singleline |  RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static private partial Regex IsEMailRegex();

		}
	}
