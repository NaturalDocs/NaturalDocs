﻿/* 
 * Class: CodeClear.NaturalDocs.Engine.Comments.Parsers.NaturalDocs
 * ____________________________________________________________________________
 * 
 * A parser to handle Natural Docs' native comment format.
 * 
 * 
 * File: Parser.txt
 * 
 *		A configuration file to handle various non-topic keywords in Natural Docs formatted comments.  The file follows the
 *		standard conventions in <ConfigFile>.  Identifier and value whitespace is condensed.
 *		
 * 
 *		Sets:
 *		
 *		> Set: [identifier]
 *		>    [value]
 *		>    [value]
 *		>    ...
 *		
 *		A string set, meaning the code is interested in only the existence of a string in that set.
 *		
 *		Start Block Keywords - The first word for lines like "(start code)".
 *		End Block Keywords - The first word for lines like "(end code)" or just "(end)".
 * 
 *		See Image Keywords - The first word for lines like "(see image.jpg)".
 *		At Link Keywords - The middle word for lines like "<reference at http://www.website.com>".
 *		URL Protocols - The protocol strings in external URLs like http.
 *		
 *		Acceptable Link Suffixes - The s after links like "<object>s".
 * 
 * 
 *		Tables:
 * 
 *		> Table: [identifier]
 *		>    [key] -> [value]
 *		>    [key] -> [value]
 *		>    ...
 *		
 *		A table mapping one string to another.  Each key can only have one value, so anything specified multiple times
 *		will overwrite the previous value.
 * 
 * 	Block Types - The second word for lines like "(start code)" or the only word for lines like "(code)".  Possible values
 * 						   are "generic" for when there is no special behavior, "code" for source code and any additional
 * 						   formatting that may entail, and "prototype" for manually specifying prototypes.
 *		
 *		Special Headings - Headings that have special behavior associated with them.  The only possible value at this point
 *								  is "parameters", meaning the section is dedicated to a function's parameters.
 *								  
 *		Access Level - Modifiers that can be placed before a Natural Docs keyword to set the access level if it is not specified
 *							  in the code itself.  Possible values are "public", "private", "protected", "internal".
 *								   
 * 
 *		Conversions:
 *		
 *		> Conversion List: [identifier]
 *		>    [key] -> [value]
 *		>    [key] ->
 *		>    ...
 *
 *		A list of string pairs mapping one to another.  There can be multiple values per key, and the value can also be null.
 * 
 *		Plural Conversions - A series of endings where the words ending with the key can have it replaced by the value to form
 *									  a possible singular form.  There may be multiple combinations that can be applied to a word, and 
 *									  not all of them will be valid.  "Leaves" converts to "Leave", "Leav", "Leaf", and "Leafe".  All that 
 *									  matters however is that the valid form be present in the possibilities.
 *									  
 *		Possessive Conversions - A series of endings where the words ending with the key can have it replaced by the value to
 *											  form a possible non-possessive form.
 *
 * 
 *		Revisions:
 *			
 *			2.0:
 *				- The file was introduced.
 *		
 * 
 * 
 * File: Parser.nd
 * 
 *		A binary file to store the last version of <Parser.txt> used in order to detect changes.
 *		
 *		> [[Binary Header]]
 *		>
 *		> [Byte: number of sets]
 *		> [[Set 0]] [[Set 1]] ...
 *		>
 *		> [Byte: number of tables]
 *		> [[Table 0]] [[Table 1]] ...
 *		>
 *		> [Byte: number of conversion lists]
 *		> [[Conversion List 0]] [[Conversion List 1]] ...
 *		
 *		Sets:
 *		
 *			> [String: value] [] ... [String: null]
 *			
 *		Tables:
 *		
 *			> [String: key] [Byte: value] [] [] ... [String: null]
 *			
 *		Conversion Lists:
 *		
 *			> [String: key] [String: value] [] [] ... [String: null]
 *		
 *		Revisions:
 *			
 *			2.0:
 *				- The file was introduced.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2018 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Languages;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Regex.Comments.NaturalDocs;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Comments.Parsers
	{
	public class NaturalDocs : Parser
		{
		
		// Group: Types
		// __________________________________________________________________________


		/* enum: LinkInterpretationFlags
		 * 
		 * Options you can pass to <LinkInterpretations()>.
		 * 
		 * ExcludeLiteral - If set, the unaltered input string will not be added as one of the interpretations.  Only alternate
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
		
			
		/* enum: SetIndex
		 * The index into <sets> for each item.  The values must start at zero and proceed sequentially.  MaxValue must be set
		 * to the highest used value.  The value names are used by <LoadFile()>, so they must match the possible set names in
		 * <Parser.txt> exactly with the exception of spaces.
		 */
		protected enum SetIndex 
			{  
			StartBlockKeywords = 0, 
			EndBlockKeywords = 1, 
			SeeImageKeywords = 2, 
			AtLinkKeywords = 3,
			URLProtocols = 4,
			AcceptableLinkSuffixes = 5,
			MaxValue = AcceptableLinkSuffixes
			}
			
		/* enum: TableIndex
		 * The index into <tables> for each item.  The values must start at zero and proceed sequentially.  MaxValue must be set
		 * to the highest used value.  The value names are used by <LoadFile()>, so they must match the possible table names in
		 * <Parser.txt> exactly with the exception of spaces.
		 */
		protected enum TableIndex
			{  
			BlockTypes = 0, 
			SpecialHeadings = 1, 
			AccessLevel = 2,
			MaxValue = AccessLevel
			}
			
		/* enum: ConversionListIndex
		 * The index into <conversionLists> for each item.  The values must start at zero and proceed sequentially.  MaxValue must
		 * be set to the highest used value.  The value names are used by <LoadFile()>, so they must match the possible list names
		 * in <Parser.txt> exactly with the exception of spaces.
		 */
		protected enum ConversionListIndex
			{  
			PluralConversions = 0, 
			PossessiveConversions = 1,
			MaxValue = PossessiveConversions
			}
			
			
			
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: NaturalDocs
		 */
		public NaturalDocs (Comments.Manager manager) : base (manager)
			{
			sets = null;
			tables = null;
			conversionLists = null;
			}
			
			
		/* Function: Start
		 * 
		 * Dependencies:
		 * 
		 *		- <Config.Manager> and <CommentTypes.Manager> must be started before using this class.
		 */
		public override bool Start (Errors.ErrorList errors)
		    {

			 // Load configuration files

			bool loadFileResult = LoadFile( EngineInstance.Config.SystemConfigFolder + "/Parser.txt", errors,
														out sets, out tables, out conversionLists);
													 
			if (loadFileResult == false )
				{  return false;  }
								
			if (EngineInstance.Config.ReparseEverything == false)
				{
				StringSet[] binarySets;
				StringTable<byte>[] binaryTables;
				List<string>[] binaryConversionLists;

				bool loadBinaryFileResult = LoadBinaryFile( EngineInstance.Config.WorkingDataFolder + "/Parser.nd",
																			 out binarySets, out binaryTables, out binaryConversionLists );
																		 
				if (loadBinaryFileResult == false)
					{  EngineInstance.Config.ReparseEverything = true;  }
					
				// Try quick compares before full ones
				else if (sets.Length != binarySets.Length ||
							 tables.Length != binaryTables.Length ||
							 conversionLists.Length != binaryConversionLists.Length)
					{  EngineInstance.Config.ReparseEverything = true;  }

				else
					{
					bool equal = true;

					for (int i = 0; i < sets.Length && equal; i++)	
						{
						if (sets[i].Count != binarySets[i].Count)
							{  equal = false;  }
						else
							{
							foreach (string setMember in sets[i])
								{
								if (!binarySets[i].Contains(setMember))
									{
									equal = false;
									break;
									}
								}
							}
						}
						
					for (int i = 0; i < tables.Length && equal; i++)
						{
						if (tables[i].Count != binaryTables[i].Count)
							{  equal = false;  }
						else
							{
							foreach (KeyValuePair<string, byte> pair in tables[i])
								{
								if (binaryTables[i][pair.Key] != pair.Value)	
									{
									equal = false;
									break;
									}
								}
							}
						}
						
					for (int i = 0; i < conversionLists.Length && equal; i++)
						{
						if (conversionLists[i].Count != binaryConversionLists[i].Count)
							{  equal = false;  }
						else
							{
							for (int x = 0; x < conversionLists[i].Count; x++)
								{
								if (conversionLists[i][x] != binaryConversionLists[i][x])
									{
									equal = false;
									break;
									}
								}
							}
						}
						
					if (equal == false)
						{  EngineInstance.Config.ReparseEverything = true;  }
					}
				}
		        
			ConfigFile.TryToRemoveErrorAnnotations( EngineInstance.Config.SystemConfigFolder + "/Parser.txt" );
				
			SaveBinaryFile( EngineInstance.Config.WorkingDataFolder + "/Parser.nd",
								  sets, tables, conversionLists );

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
		public bool Parse (PossibleDocumentationComment comment, List<Topic> topics, bool requireHeader)
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
			
			if (IsTopicLine(lineIterator, out currentTopic))
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
				if (prevLineBlank && IsTopicLine(lineIterator, out nextTopic))
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
				string lcnInput = nInput.ToLower();
				
				List<String> pluralConversions = conversionLists[(int)ConversionListIndex.PluralConversions];
				List<String> possessiveConversions = conversionLists[(int)ConversionListIndex.PossessiveConversions];

				// We use -2 to signify none, since we also want to test each plural conversion without any possessive conversion applied.
				for (int possessiveIndex = -2; possessiveIndex < possessiveConversions.Count; possessiveIndex += 2)
					{
					string nWithoutPossessive, lcnWithoutPossessive;
					
					if (possessiveIndex == -2)
						{  
						nWithoutPossessive = nInput;
						lcnWithoutPossessive = lcnInput;
						}
					else if (lcnInput.EndsWith(possessiveConversions[possessiveIndex]))
						{
						nWithoutPossessive = nInput.Substring(0, nInput.Length - possessiveConversions[possessiveIndex].Length);
						lcnWithoutPossessive = lcnInput.Substring(0, lcnInput.Length - possessiveConversions[possessiveIndex].Length);
						
						if (possessiveConversions[possessiveIndex+1] != null)
							{  
							nWithoutPossessive += possessiveConversions[possessiveIndex+1];
							lcnWithoutPossessive += possessiveConversions[possessiveIndex+1];  
							}
						}
					else
						{  
						nWithoutPossessive = null;
						lcnWithoutPossessive = null;  
						}
						
					if (nWithoutPossessive != null)
						{
						// Again -2 signifies none, since we also want each possessive conversion without any plural conversion applied.
						for (int pluralIndex = -2; pluralIndex < pluralConversions.Count; pluralIndex += 2)
							{
							string nWithoutEither;
							
							if (pluralIndex == -2)
								{
								// Skip when we're missing both.  We have that on the list already.
								if (possessiveIndex == -2)
									{  nWithoutEither = null;  }
								else
									{  nWithoutEither = nWithoutPossessive;  }
								}	
														
							else if (lcnWithoutPossessive.EndsWith(pluralConversions[pluralIndex]))
								{
								nWithoutEither = nWithoutPossessive.Substring(0, nWithoutPossessive.Length - pluralConversions[pluralIndex].Length);
								
								if (pluralConversions[pluralIndex+1] != null)
									{  nWithoutEither += pluralConversions[pluralIndex+1];  }
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
			if (tables[(int)TableIndex.AccessLevel].ContainsKey(tag))
				{  
				accessLevel = (Languages.AccessLevel)tables[(int)TableIndex.AccessLevel][tag];
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
		 *		- LanguageID, if specified
		 *		- AccessLevel, if specified
		 *		- Tags, if specified
		 */
		protected bool IsTopicLine (LineIterator lineIterator, out Topic topic)
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
			

			// Parse out the keyword.  Since they can have spaces in them, we start with the full string and work our way
			// down to the last word.  So if there's a keyword "Private Function" it will be seen as such before "Function"
			// with the modifier "Private".
			
			int keywordStartingIndex = 0;
			int keywordEndingIndex = keywordsAndTags.Length;
			
			bool pluralKeyword;
			CommentTypes.CommentType commentType = EngineInstance.CommentTypes.FromKeyword(keywordsAndTags, out pluralKeyword);
			
			while (commentType == null)
				{
				keywordStartingIndex = keywordsAndTags.IndexOf(' ', keywordStartingIndex);
				
				if (keywordStartingIndex == -1)
					{  break;  }
					
				keywordStartingIndex++;
				
				commentType = EngineInstance.CommentTypes.FromKeyword( 
											keywordsAndTags.Substring (keywordStartingIndex, keywordEndingIndex - keywordStartingIndex),
											out pluralKeyword);
				}
				
			if (commentType == null)
				{  return false;  }
				

			// Parse out the language, access level, and tags.  Start with the full string before the keyword and work our way down 
			// to the first word.  This allows longer tags and access levels like "Protected Internal" to apply before shorter ones like
			// "Protected" and "Internal".  One word can serve as both, so "Private" can be both a tag and a protection level.			
			
			int languageID = 0;
			List<int> tagIDs = null;
			Languages.AccessLevel accessLevel = Languages.AccessLevel.Unknown;
			
			if (keywordStartingIndex >= 2)
				{
				int tagStartingIndex = 0;
				int tagEndingIndex = keywordStartingIndex - 1;  // -1 to skip separating space.
				
				for (;;)
					{
					bool success = false;
					string substring = keywordsAndTags.Substring(tagStartingIndex, tagEndingIndex - tagStartingIndex);
					
					CommentTypes.Tag tag = EngineInstance.CommentTypes.TagFromName(substring);
														
					if (tag != null)
						{
						if (tagIDs == null)
							{  tagIDs = new List<int>();  }
						
						tagIDs.Add(tag.ID);
						success = true;
						}
						
					Languages.AccessLevel tempAccessLevel;
					
					if (IsAccessLevelTag(substring, out tempAccessLevel))
						{
						accessLevel = (Languages.AccessLevel)tempAccessLevel;
						success = true;
						}

					Language language = EngineInstance.Languages.FromName(substring);

					if (language != null)
						{
						languageID = language.ID;
						success = true;
						}
						
					if (success)
						{
						// Move on to the next tag
						tagStartingIndex = tagEndingIndex + 1;  // +1 to skip separating space.
						tagEndingIndex = keywordStartingIndex - 1;  // -1 to skip separating space.
						
						// Break if there are no more
						if (tagStartingIndex >= tagEndingIndex)
							{  break;  }
						}
					else // failed
						{
						// Trim the last word off the section we're checking.  Fail if there are no more spaces.
						tagEndingIndex = keywordsAndTags.LastIndexOf(' ', tagEndingIndex - 1, tagEndingIndex - tagStartingIndex);
						
						if (tagEndingIndex == -1)
							{  return false;  }
						}
					}
				}
				
				
			// If we made it this far, we're okay.  Set any topic properties.
			
			topic = new Topic(EngineInstance.CommentTypes);
			topic.CommentLineNumber = lineIterator.LineNumber;
			topic.Title = tokenizer.RawText.Substring( afterColon.RawTextIndex, lineEndingIndex - afterColon.RawTextIndex );
			topic.CommentTypeID = commentType.ID;
			topic.IsList = pluralKeyword;
			
			topic.DeclaredAccessLevel = accessLevel;
			topic.LanguageID = languageID;

			if (tagIDs != null)
				{  
				foreach (int tagID in tagIDs)
					{  topic.AddTagID(tagID);  }
				}
				
			return true;
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
			return sets[(int)SetIndex.StartBlockKeywords].Contains(keyword);
			}
			
			
		/* Function: IsEndBlockKeyword
		 * Returns whether the passed string is an end block keyword, which would be the first word in "(end code)".
		 */
		protected bool IsEndBlockKeyword (string keyword)
			{
			return sets[(int)SetIndex.EndBlockKeywords].Contains(keyword);
			}
			
			
		/* Function: IsBlockType
		 * Returns whether the passed string represents a block type, which would be the second word in "(start code)", and
		 * which <BlockType> it is if so.
		 */
		protected bool IsBlockType (string keyword, out BlockType blockType)
			{
			if (tables[(int)TableIndex.BlockTypes].ContainsKey(keyword))
				{  
				blockType = (BlockType)tables[(int)TableIndex.BlockTypes][keyword];  
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

			if (tables[(int)TableIndex.SpecialHeadings].ContainsKey(heading))
				{  headingType = (HeadingType)tables[(int)TableIndex.SpecialHeadings][heading];  }
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
			return sets[(int)SetIndex.SeeImageKeywords].Contains(keyword);
			}
			
			
		/* Function: IsImageTagContent
		 * Returns whether the passed string is the content of an image tag, like "see image.jpg".  It validates the file name against
		 * the registered extensions in <Files.FileSources.Folder>.  The string must not contain the parentheses.  If it is tag content 
		 * it will also returns the keyword and file name.
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
			return sets[(int)SetIndex.URLProtocols].Contains(input);
			}


		/* Function: IsAtLinkKeyword
		 * Returns whether the passed string is an "at" link keyword, such as in "<e-mail me at email@address.com>".
		 */
		protected bool IsAtLinkKeyword (string keyword)
			{
			return sets[(int)SetIndex.AtLinkKeywords].Contains(keyword);
			}
			

		/* Function: ParseBody
		 * Parses the content between two <LineIterators> and adds its content to the <Topic> in <NDMarkup> as its body.
		 * Also extracts the summary from it and adds it to the <Topic>.
		 */
		protected void ParseBody (LineIterator firstContentLine, LineIterator endOfContent, Topic topic)
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
				
				if (IsStartBlockLine(line, out blockChar, out blockType, out language))
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
				
				else if (IsStandalonePreformattedLine(line, out tempString, out indent, out leadingCharacter))
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
				
				else if (IsStandaloneImage(line, out filePath))
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
					
					
				// Definition Lists
				// item - definition
				
				else if ( (prevLineBlank || definitionIndent != -1) && 
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

					bool isSymbol = ( (topic.IsList || topic.IsEnum) && lastHeadingType != HeadingType.Parameters);
						
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
					prevParagraphLineEndsSentence = LineEndProbablyEndsSentenceRegex.IsMatch(tempString2);
					
					prevLineBlank = false;
					line.Next();
					}
					
					
				// Bullet lists
				// - bullet
				
				else if ( (prevLineBlank || (bulletIndents != null && bulletIndents.Count > 0)) &&
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
					prevParagraphLineEndsSentence = LineEndProbablyEndsSentenceRegex.IsMatch(tempString);
					
					prevLineBlank = false;
					line.Next();
					}
					
					
				// Headings
				// heading:
				
				else if (prevLineBlank && IsHeading(line, out tempString, out headingType))
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
					CloseParagraph(ref paragraph, body);
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
					prevParagraphLineEndsSentence = LineEndProbablyEndsSentenceRegex.IsMatch(lineString);

					prevLineBlank = false;
					line.Next();  
					}
				}
				
				
			CloseAllBlocks(ref paragraph, ref definitionIndent, ref bulletIndents, body);
				
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
		protected void CloseParagraph (ref StringBuilder paragraph, StringBuilder body)
			{
			if (paragraph != null && paragraph.Length > 0)
				{
				ParseTextBlock(paragraph.ToString(), body);
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
		protected void CloseAllBlocks (ref StringBuilder paragraph, ref int definitionIndent, ref List<int> bulletIndents, StringBuilder body)
			{
			CloseParagraph(ref paragraph, body);
				
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
		protected void ParseTextBlock (string input, StringBuilder output)
			{
			Tokenizer tokenizer = new Tokenizer(input, tabWidth: EngineInstance.Config.TabWidth);
			
			// The order of these function calls is important.  Read each of their descriptions.
			MarkPossibleFormattingTags(tokenizer);
			MarkPossibleLinkTags(tokenizer);
			MarkPossibleInlineImageTags(tokenizer);

			MarkEMailAddresses(tokenizer);
			MarkURLs(tokenizer);

			FinalizeLinkTags(tokenizer);
			FinalizeInlineImageTags(tokenizer);
			FinalizeFormattingTags(tokenizer);

			MarkedTokensToNDMarkup(tokenizer, output);
			}
			
			
		/* Function: MarkPossibleFormattingTags
		 * Goes through the passed <Tokenizer> and marks asterisks, underscores, slashes, and backticks with
		 * <CommentParsingType.PossibleOpeningTag> and  <CommentParsingType.PossibleClosingTag> if they can possibly be interpreted as
		 * bold, underline, italic, or in-line code formatting (respectively).
		 */
		protected void MarkPossibleFormattingTags (Tokenizer tokenizer)
			{
			for (TokenIterator token = tokenizer.FirstToken; token.IsInBounds; token.Next())
				{
				char character = token.Character;
				
				if (character == '_' || character == '*' || character == '/' || character == '`')
					{					
					// Possible opening symbols

					TokenIterator next = token;
					next.Next();

					TokenIterator prev = token;
					prev.Previous();

					// Prevent *=, __, **, /= from counting.					
					if ( (character == '*' && (next.Character == '=' || next.Character == '*' || prev.Character == '*')) ||
						 (character == '_' && (next.Character == '_' || prev.Character == '_')) ||
						 (character == '/' && next.Character == '=') )
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
						if ( tokenizer.MatchTextBetween(AcceptableBeforeOpeningTagRegex, prev, token).Success == false )
							{  goto ClosingSymbols;  }
						}
							
					token.CommentParsingType = CommentParsingType.PossibleOpeningTag;
					continue;
						
					
					// Possible closing symbols
					
					ClosingSymbols:
					
					prev = token;
					prev.Previous();
					
					// Prevent *=, **, __, /= from counting.						
					if ( (character == '*' && (next.Character == '=' || next.Character == '*' || prev.Character == '*')) ||
						 (character == '_' && (next.Character == '_' || prev.Character == '_')) ||
						 (character == '/' && next.Character == '=') )
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
						if ( tokenizer.MatchTextBetween(AcceptableAfterClosingTagRegex, next, end).Success == false )
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
						
					// Move past the content immediately before it.
					while (prev.FundamentalType != FundamentalType.Whitespace &&
								 prev.FundamentalType != FundamentalType.Null &&
								 prev.CommentParsingType != CommentParsingType.PossibleOpeningTag)
						{
						prev.Previous();
						}
							
					// An opening link tag can only be preceded by another opening tag without intervening whitespace if
					// it's a bold, underline, or italic tag; code tags cannot be further formatted.
					if (prev.CommentParsingType == CommentParsingType.PossibleOpeningTag)
						{
						if (prev.Character != '*' && prev.Character != '_' && prev.Character != '/')
							{  continue;  }
						}

					// Move back past the null, whitespace, or opening tag.
					prev.Next();

					// If there's still intervening content, it must be entirely acceptable characters like ( and ".						
					if (prev < token)
						{
						if ( tokenizer.MatchTextBetween(AcceptableBeforeOpeningTagRegex, prev, token).Success == false )
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
					foreach (string suffix in sets[(int)SetIndex.AcceptableLinkSuffixes])
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
					// it's a bold, underline, or italic tag; code tags cannot be further formatted.
					if (end.CommentParsingType == CommentParsingType.PossibleClosingTag)
						{
						if (end.Character != '*' && end.Character != '_' && end.Character != '/')
							{  continue;  }
						}

					// If there's still intervening content, it must be entirely acceptable characters like ( and ".						
					if (next < end)
						{
						if ( tokenizer.MatchTextBetween(AcceptableAfterClosingTagRegex, next, end).Success == false )
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
						 AcceptableAfterInlineImageRegex.Match(tokenizer.RawText, next.RawTextIndex, 
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
				Match match = URLAnywhereInLineRegex.Match(tokenizer.RawText, token.RawTextIndex);
				
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
				Match match = EMailAnywhereInLineRegex.Match(tokenizer.RawText, token.RawTextIndex);
				
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
					else if (tokenIterator.Character == '/')
						{ output.Append("<i>"); }
					else if (tokenIterator.Character == '`')
						{ output.Append("<code>"); }
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
							Match match = StartsWithURLProtocolRegex.Match(tagContent);
							
							if (match.Success && IsURLProtocol(match.Groups[1].Value))
								{
								output.Append("<link type=\"url\" target=\"");
								output.EntityEncodeAndAppend(tagContent);
								output.Append("\">");
								}
								
							else
								{
								match = EMailRegex.Match(tagContent);
								
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
											match = StartsWithURLProtocolRegex.Match(interpretations[i].Target);
											
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
												match = EMailRegex.Match(interpretations[i].Target);
												
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
					else if (tokenIterator.Character == '/')
						{
						output.Append("</i>");
						}
					else if (tokenIterator.Character == '`')
						{
						output.Append("</code>");
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
					
					Match match = tokenizer.MatchTextBetween(EMailRegex, startOfURL, tokenIterator);
					
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


		/* Function: ExtractEmbeddedTopics
		 * Goes through the topic body to find any definition list symbols and adds them to the list as separate topics.
		 * Since embedded topics must appear immediately after their parent topic, this must be called while the passed
		 * topic is at the end of the list.
		 */
		protected void ExtractEmbeddedTopics (Topic topic, IList<Topic> topicList)
			{
			#if DEBUG
			if (topic != topicList[topicList.Count - 1])
				{  throw new Exception ("ExtractEmbeddedTopics requires the topic to be the last on the list.");  }
			#endif

			if (topic.Body == null)
				{  return;  }

			if (topic.IsList == false && topic.IsEnum == false)
				{
				#if DEBUG
				if (topic.Body.IndexOf("<ds>") != -1)
					{  
					throw new Exception ("ExtractEmbeddedTopics found definition symbols in topic " + topic.Title + " even though it isn't " +
															"a list or an enum.");
					}
				#endif

				return;
				}

			int symbolIndex = topic.Body.IndexOf("<ds>");
			int lineNumberOffset = 1;

			int embeddedCommentTypeID = 0;

			if (topic.IsEnum)
				{  embeddedCommentTypeID = EngineInstance.CommentTypes.IDFromKeyword("constant");  }

			// We do it this way in case there is no type that uses the "constant" keyword.
			if (embeddedCommentTypeID == 0)
				{  embeddedCommentTypeID = topic.CommentTypeID;  }

			while (symbolIndex != -1)
				{
				int endSymbolIndex = topic.Body.IndexOf("</ds>", symbolIndex + 4);
				int definitionIndex = endSymbolIndex + 5;

				#if DEBUG
				if (topic.Body.Substring(definitionIndex, 4) != "<dd>")
					{  throw new Exception ("The assumption that a <dd> would appear immediately after a </ds> failed for some reason.");  }
				#endif

				int endDefinitionIndex = topic.Body.IndexOf("</dd>", definitionIndex + 4);

				Topic embeddedTopic = new Topic(EngineInstance.CommentTypes);
				embeddedTopic.Title = topic.Body.Substring(symbolIndex + 4, endSymbolIndex - (symbolIndex + 4)).EntityDecode();
				embeddedTopic.Body = topic.Body.Substring(definitionIndex + 4, endDefinitionIndex - (definitionIndex + 4));
				embeddedTopic.IsEmbedded = true;
				embeddedTopic.CommentTypeID = embeddedCommentTypeID;
				embeddedTopic.DeclaredAccessLevel = topic.DeclaredAccessLevel;
				embeddedTopic.TagString = topic.TagString;
				embeddedTopic.CommentLineNumber = topic.CommentLineNumber + lineNumberOffset;

				MakeSummaryFromBody(embeddedTopic);

				topicList.Add(embeddedTopic);

				lineNumberOffset++;

				symbolIndex = topic.Body.IndexOf("<ds>", endDefinitionIndex + 5);
				}
			}
			
			
			
		// Group: Static Functions
		// __________________________________________________________________________
		
		
		/* Function: LoadFile
		 * 
		 * Loads <Parser.txt> and puts the results in the various out parameters, returning whether it was successful or not.
		 * If it wasn't, the out structures will still exist but be empty and all errors will be added to the error list.
		 * 
		 * Keywords are matched to the value names in <SetIndex>, <TableIndex>, and <ConversionListIndex> so all possible
		 * keywords must be represented there sans spaces.  Likewise table values must be represented in <BlockType>,
		 * <HeadingType>, and <Languages.AccessLevel>.
		 */
		static public bool LoadFile (Path filename, Errors.ErrorList errors,
											   out StringSet[] sets, out StringTable<byte>[] tables, out List<string>[] conversionLists)
		    {
		    sets = new StringSet[ (int)SetIndex.MaxValue + 1 ];
		    tables = new StringTable<byte>[ (int)TableIndex.MaxValue + 1 ];
		    conversionLists = new List<string>[ (int)ConversionListIndex.MaxValue + 1 ];

		    int previousErrorCount = errors.Count;
		    Regex.CondensedWhitespaceArrowSeparator arrowSeparatorRegex = new Regex.CondensedWhitespaceArrowSeparator();
			
		    using (ConfigFile file = new ConfigFile())
		        {
		        bool openResult = file.Open(filename, 
														 ConfigFile.FileFormatFlags.MakeIdentifiersLowercase |
		                                                 ConfigFile.FileFormatFlags.CondenseValueWhitespace |
		                                                 ConfigFile.FileFormatFlags.SupportsRawValueLines,
		                                                 errors);
														 
		        if (openResult == false)
		            {  return false;  }
					
		        string identifier = null;
		        string value = null;
				
		        // If this is true, identifier and value are already filled but not processed, so Get shouldn't be called again on the next
		        // iteration.
		        bool alreadyHaveNextLine = false;				
				
		        while (alreadyHaveNextLine || file.Get(out identifier, out value))
		            {
		            alreadyHaveNextLine = false;
					
		            if (identifier == null)
		                {
		                file.AddError(
		                    Locale.Get("NaturalDocs.Engine", "ConfigFile.LineNotInIdentifierValueFormat")
		                    );
		                continue;
		                }
		                
		            
		            //
		            // Sets
		            //

					if (identifier == "set")
						{
						string nsValue = value.RemoveWhitespace();
						StringSet set = new StringSet (KeySettings.IgnoreCase | KeySettings.NormalizeUnicode);
						bool urlProtocol = false;
						
						try
							{  
							SetIndex setIndex = (SetIndex)Enum.Parse(typeof(SetIndex), nsValue, true);  
							sets[ (int)setIndex ] = set;
							
							urlProtocol = (setIndex == SetIndex.URLProtocols);
							}
						catch
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidIdentifier(identifier)", value)
								);
							// Continue anyway.
							}
							
						while (file.Get(out identifier, out value))
							{
							if (identifier != null)
								{
								alreadyHaveNextLine = true;
								break;
								}
								
							if (urlProtocol && AcceptableURLProtocolCharactersRegex.IsMatch(value) == false)
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidValue(value)", value)
									);
								}
								
							set.Add(value);
							}
						}
						
						
					//
					// Tables
					//
					
					else if (identifier == "table")
						{
						StringTable<byte> table = new StringTable<byte>(KeySettings.IgnoreCase | KeySettings.NormalizeUnicode);
						System.Type type = null;

						// We take the spaces out anyway because if one's not defined, the error message will use the
						// enum name.  Following that message would make people define it without spaces.						
						string nslcValue = value.RemoveWhitespace().ToLower();
						
						if (nslcValue == "blocktypes")
							{
							type = typeof(BlockType);
							tables[ (int)TableIndex.BlockTypes ] = table;
							}
						else if (nslcValue == "specialheadings")
							{
							type = typeof(HeadingType);
							tables[ (int)TableIndex.SpecialHeadings ] = table;
							}
						else if (nslcValue == "accesslevel")
							{
							type = typeof(Languages.AccessLevel);
							tables[ (int)TableIndex.AccessLevel ] = table;
							}
						else
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidIdentifier(identifier)", value)
								);
							// Continue anyway.
							}
							
						while (file.Get(out identifier, out value))
							{
							if (identifier != null)
								{
								alreadyHaveNextLine = true;
								break;
								}
								
							string[] split = arrowSeparatorRegex.Split(value, 2);
							byte byteValue = 0;
							
							try
								{  
								if (type != null)
									{  byteValue = (byte)Enum.Parse(type, split[1], true);  }
								}
							catch
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidValue(value)", split[1])
									);
								// Continue anyway.
								}

							table.Add(split[0], byteValue);
							}
						}
						
						
					//
					// Conversion List
					//
					
					else if (identifier == "conversion list")
						{
						string nsValue = value.RemoveWhitespace();
						List<string> conversionList = new List<string>();
						
						try
							{  
							ConversionListIndex index = (ConversionListIndex)Enum.Parse( typeof(ConversionListIndex), nsValue, true);  
							conversionLists[ (int)index ] = conversionList;
							}
						catch
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidIdentifier(identifier)", value)
								);
							// Continue anyway.
							}

						while (file.Get(out identifier, out value))
							{
							if (identifier != null)
								{
								alreadyHaveNextLine = true;
								break;
								}
								
							string[] split = arrowSeparatorRegex.Split(value, 2);
							
							conversionList.Add( split[0].ToLower().Normalize(System.Text.NormalizationForm.FormC) );
							
							if (String.IsNullOrEmpty(split[1]))
								{  conversionList.Add(null);  }
							else
								{  conversionList.Add( split[1].ToLower().Normalize(System.Text.NormalizationForm.FormC) );  }
							}
						}
						
						
						
		            else
		                {
		                file.AddError(
		                    Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidIdentifier(identifier)", identifier)
		                    );
							
		                // Skip to the next identifier
		                while (file.Get(out identifier, out value))
		                    {
		                    if (identifier != null)
		                        {
		                        alreadyHaveNextLine = true;
		                        break;
		                        }
		                    }
		                }						
		            }
		            
		            
		            
				List<string> missingIdentifiers = new List<string>();
		        
		        for (int i = 0; i <= (int)SetIndex.MaxValue; i++)
					{
					if (sets[i] == null)
						{
						string name = Enum.GetName( typeof(SetIndex), i);
						
						if (!String.IsNullOrEmpty(name))
							{  missingIdentifiers.Add(name);  }
						}
					}
					
		        for (int i = 0; i <= (int)TableIndex.MaxValue; i++)
					{
					if (tables[i] == null)
						{
						string name = Enum.GetName( typeof(TableIndex), i);

						if (!String.IsNullOrEmpty(name))
							{  missingIdentifiers.Add(name);  }
						}
					}
					
		        for (int i = 0; i <= (int)ConversionListIndex.MaxValue; i++)
					{
					if (conversionLists[i] == null)
						{
						string name = Enum.GetName( typeof(ConversionListIndex), i);
						
						if (!String.IsNullOrEmpty(name))
							{  missingIdentifiers.Add(name);  }
						}
					}
					
				if (missingIdentifiers.Count == 1)
					{
					file.AddError(
						Locale.Get("NaturalDocs.Engine", "ConfigFile.RequiredIdentifierNotDefined(identifier)", missingIdentifiers[0])
						);
					}
				else if (missingIdentifiers.Count > 1)
					{
					file.AddError(
						Locale.Get("NaturalDocs.Engine", "ConfigFile.RequiredIdentifiersNotDefined(identifiers)", 
										string.Join(", ", missingIdentifiers.ToArray()))
						);
					}

		        }
		        
				
		    if (errors.Count == previousErrorCount)
		        {  return true;  }
		    else
		        {
		        sets = null;
		        tables = null;
		        conversionLists = null;

		        return false;
		        }
		    }
			
			
		/* Function: LoadBinaryFile
		 * Loads <Parser.nd> and puts the results in the various out parameters, returning whether it was successful or not.
		 * If it wasn't, the out structures will still exist but be empty.
		 */
		static public bool LoadBinaryFile (Path filename,
		                                            out StringSet[] binarySets,
		                                            out StringTable<byte>[] binaryTables,
		                                            out List<string>[] binaryConversionLists)
		    {
		    // To get the compiler to shut up
		    binarySets = null;
		    binaryTables = null;
		    binaryConversionLists = null;
		    
		    BinaryFile file = new BinaryFile();
		    bool result = true;
			
		    try
		        {
		        if (file.OpenForReading(filename, "2.0") == false)
		            {
		            result = false;
		            }
		        else
		            {
					// [Byte: number of sets]
					// [[Set 0]] [[Set 1]] ...

		            byte numberOfSets = file.ReadByte();
					binarySets = new StringSet[numberOfSets];		            
						
					for (byte i = 0; i < numberOfSets; i++)
						{
						StringSet set = new StringSet (KeySettings.IgnoreCase | KeySettings.NormalizeUnicode);
						LoadBinaryFile_GetSet(file, set);
						binarySets[i] = set;
						}


					// [Byte: number of tables]
					// [[Table 0]] [[Table 1]] ...
					
					byte numberOfTables = file.ReadByte();
					binaryTables = new StringTable<byte>[numberOfTables];
				
					for (byte i = 0; i < numberOfTables; i++)
						{
						StringTable<byte> table = new StringTable<byte>(KeySettings.IgnoreCase | KeySettings.NormalizeUnicode);
						LoadBinaryFile_GetTable(file, table);
						binaryTables[i] = table;
						}
						
						
					// [Byte: number of conversion lists]
					// [[Conversion List 0]] [[Conversion List 1]] ...
					
					byte numberOfConversionLists = file.ReadByte();
					binaryConversionLists = new List<string>[numberOfConversionLists];

					for (byte i = 0; i < numberOfConversionLists; i++)
						{
						List<string> conversionList = new List<string>();
						LoadBinaryFile_GetConversionList(file, conversionList);
						binaryConversionLists[i] = conversionList;
						}
		            }
		        }
		    catch
		        {  result = false;  }
		    finally
		        {  file.Close();  }
				
		    if (result == false)
		        {
				binarySets = new StringSet[ (int)SetIndex.MaxValue + 1 ];
				binaryTables = new StringTable<byte>[ (int)TableIndex.MaxValue + 1 ];
				binaryConversionLists = new List<string>[ (int)ConversionListIndex.MaxValue + 1 ];
				
				for (byte i = 0; i <= (byte)SetIndex.MaxValue; i++)
					{  binarySets[i] = new StringSet (KeySettings.IgnoreCase | KeySettings.NormalizeUnicode);  }
					
				for (byte i = 0; i <= (byte)TableIndex.MaxValue; i++)
					{  binaryTables[i] = new StringTable<byte> (KeySettings.IgnoreCase | KeySettings.NormalizeUnicode);  }
					
				for (byte i = 0; i <= (byte)ConversionListIndex.MaxValue; i++)
					{  binaryConversionLists[i] = new List<string>();  }
		        }
				
		    return result;
		    }
			
			
		/* Function: LoadBinaryFile_GetSet
		 * A helper function used only by <LoadBinaryFile()> which loads values into the passed <StringSet> until it reaches a null string.
		 */
		static private void LoadBinaryFile_GetSet(BinaryFile file, StringSet set)
		    {
		    for (;;)
		        {
		        string value = file.ReadString();
				
		        if (value == null)
		            {  return;  }
					
		        set.Add(value);
		        }
		    }
		
		
		/* Function: LoadBinaryFile_GetTable
		 * A helper function used only by <LoadBinaryFile()> which loads values into the passed <StringTable> until it reaches a null string.
		 */
		static private void LoadBinaryFile_GetTable(BinaryFile file, StringTable<byte> table)
		    {
		    for (;;)
		        {
		        string key = file.ReadString();
				
		        if (key == null)
		            {  return;  }
					
		        byte value = file.ReadByte();
					
		        table.Add(key, value);
		        }
		    }
		
		
		/* Function: LoadBinaryFile_GetConversionList
		 * A helper function used only by <LoadBinaryFile()> which loads values into the passed conversion list until it reaches a null string.
		 */
		static private void LoadBinaryFile_GetConversionList(BinaryFile file, List<string> conversionList)
		    {
		    for (;;)
		        {
		        string key = file.ReadString();
				
		        if (key == null)
		            {  return;  }
					
		        string value = file.ReadString();
					
		        conversionList.Add(key);
		        conversionList.Add(value);
		        }
		    }
		
		
		/* Function: SaveBinaryFile
		 * Saves <Parser.nd>.  Throws an exception if unsuccessful.
		 */
		static public void SaveBinaryFile (Path filename,
		                                            StringSet[] sets,
		                                            StringTable<byte>[] tables,
		                                            List<string>[] conversionLists)
		    {
		    BinaryFile file = new BinaryFile();
		    file.OpenForWriting(filename);

		    try
		        {
				// [Byte: number of sets]
				// [[Set 0]] [[Set 1]] ...
				
				file.WriteByte((byte)sets.Length);
				foreach (StringSet set in sets)
					{  SaveBinaryFile_WriteSet(file, set);  }

				// [Byte: number of tables]
				// [[Table 0]] [[Table 1]] ...
				
				file.WriteByte((byte)tables.Length);
				foreach (StringTable<byte> table in tables)
					{  SaveBinaryFile_WriteTable(file, table);  }

				// [Byte: number of conversion lists]
				// [[Conversion List 0]] [[Conversion List 1]] ...
				
				file.WriteByte((byte)conversionLists.Length);
				foreach (List<string> conversionList in conversionLists)
					{  SaveBinaryFile_WriteConversionList(file, conversionList);  }
		        }
				
		    finally
		        {
		        file.Close();
		        }
		    }
			
			
		/* Function: SaveBinaryFile_WriteSet
		 * A helper function used only by <SaveBinaryFile()> which writes out the <StringSet> values followed by a null string.
		 */
		static private void SaveBinaryFile_WriteSet(BinaryFile file, StringSet set)
		    {
		    foreach (string value in set)
		        {  file.WriteString(value);  }
				
		    file.WriteString(null);
		    }
		
		
		/* Function: SaveBinaryFile_WriteTable
		 * A helper function used only by <SaveBinaryFile()> which writes out the <StringTable> values followed by a null string.
		 */
		static private void SaveBinaryFile_WriteTable(BinaryFile file, StringTable<byte> table)
		    {
		    foreach (System.Collections.Generic.KeyValuePair<string, byte> pair in table)
		        {  
		        file.WriteString(pair.Key);
		        file.WriteByte(pair.Value);
		        }
				
		    file.WriteString(null);
		    }
		
		
		/* Function: SaveBinaryFile_WriteConversionList
		 * A helper function used only by <SaveBinaryFile()> which writes out the conversion list values followed by a null string.
		 */
		static private void SaveBinaryFile_WriteConversionList(BinaryFile file, List<string> conversionList)
		    {
		    foreach (string entry in conversionList)
		        {  file.WriteString(entry);  }
				
		    file.WriteString(null);
		    }
		
		
		
		// Group: Variables
		// __________________________________________________________________________


		/* var: sets
		 * An array of <StringSets> corresponding to the sets in <Parser.txt>.  Use <SetIndex> for indexes to get particular
		 * sets.
		 */
		protected StringSet[] sets;

		/* var: tables
		 * An array of <StringTables> corresponding to the tables in <Parser.txt>.  The values are bytes but will be safe to blindly
		 * cast to their respective enums because the values will have been validated when the files were loaded.
		 */
		protected StringTable<byte>[] tables;
		
		/* var: conversionLists
		 * An array of string lists corresponding to the conversion lists in <Parser.txt>.  Each string list is made up of string pairs 
		 * where the first are the keys and the second are the values or null.  Everything will be in lowercase and canonically
		 * composed in Unicode (FormC).  Use <ConversionListIndex> for indexes to get particular tables.
		 */
		protected List<string>[] conversionLists;

		/* var: ParenthesesChars 
		 * An array of the parentheses characters, for use with IndexOfAny(char[]).
		 */
		protected static char[] ParenthesesChars = { '(', ')' };


		protected static LineEndProbablyEndsSentence LineEndProbablyEndsSentenceRegex = new LineEndProbablyEndsSentence();
			
		protected static AcceptableBeforeOpeningTag AcceptableBeforeOpeningTagRegex = new AcceptableBeforeOpeningTag();
		protected static AcceptableAfterClosingTag AcceptableAfterClosingTagRegex = new AcceptableAfterClosingTag();
		protected static AcceptableAfterInlineImage AcceptableAfterInlineImageRegex = new AcceptableAfterInlineImage();
	
		protected static AcceptableURLProtocolCharacters AcceptableURLProtocolCharactersRegex = new AcceptableURLProtocolCharacters();
		protected static StartsWithURLProtocol StartsWithURLProtocolRegex = new StartsWithURLProtocol();
		
		protected static URLAnywhereInLine URLAnywhereInLineRegex = new URLAnywhereInLine();
		protected static EMailAnywhereInLine EMailAnywhereInLineRegex = new EMailAnywhereInLine();
		protected static EMail EMailRegex = new EMail();
		}
	}