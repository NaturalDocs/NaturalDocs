/* 
 * Class: CodeClear.NaturalDocs.Engine.Links.Manager
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.CommentTypes;
using CodeClear.NaturalDocs.Engine.Errors;
using CodeClear.NaturalDocs.Engine.Languages;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Links
	{
	public partial class Manager
		{

		// Group: Link Targeting Functions
		// __________________________________________________________________________


		/* Function: Score
		 * 
		 * Generates a numeric score representing how well the <Topic> serves as a match for the <Link>.  Higher scores are
		 * better, and zero means they don't match at all.
		 * 
		 * If a score has to beat a certain threshold to be relevant, you can pass it to lessen the processing load.  This function 
		 * may be able to tell it can't beat the score early and return without performing later steps.  In these cases it will return 
		 * -1.
		 * 
		 * If scoring a Natural Docs link you must pass a list of interpretations.  It must include the literal form.
		 */
		public long Score (Link link, Topic topic, long minimumScore = 0, List<LinkInterpretation> interpretations = null)
			{
			// DEPENDENCY: These things depend on the score's internal format:
			//   - EngineTests.LinkScoring
			//   - Link.TargetInterepretationIndex

			// Other than that the score's format should be treated as opaque.  Nothing beyond this class should try to 
			// interpret the value other than to know that higher is better, zero is impossible, and -1 means we quit early.

			// It's a 64-bit value so we'll assign bits to the different characteristics.  Higher order bits obviously result in higher 
			// numeric values so the characteristics are ordered by priority.

			// Format:
			// 0LCETPPP PPPPPPPP PPPPPPPP PSSSSSSS SSSIIIII IBFFFFFF Rbbbbbbb brrrrrr1

			// 0 - The first bit is zero to make sure the number is positive.

			// L - Whether the topic matches the link's language.
			// C - Whether the topic and link's capitalization match if it matters to the language.
			// E - Whether the text is an exact match with no plural or possessive conversions applied.
			// T - Whether the link parameters exactly match the topic title parameters.
			// P - How well the parameters match.
			// S - How high on the scope list the symbol match is.
			// I - How high on the interpretation list (named/plural/possessive) the match is.
			// B - Whether the topic has a body
			// F - How high on the list of topics that define the same symbol in the same file this is.
			// R - Whether the topic has a prototype.
			// b - The length of the body divided by 16.
			// r - The length of the prototype divided by 16.

			// 1 - The final bit is one to make sure a match will never be zero.


			// For type and class parent links, the comment type MUST have the relevant attribute set to be possible.

			var commentType = EngineInstance.CommentTypes.FromID(topic.CommentTypeID);
			var language = EngineInstance.Languages.FromID(topic.LanguageID);

			if ( (link.Type == LinkType.ClassParent && commentType.Flags.ClassHierarchy == false) ||
				  (link.Type == LinkType.Type && commentType.Flags.VariableType == false) )
				{  return 0;  }


			// 0------- -------- -------- -------- -------- -------- -------- -------1
			// Our baseline.

			long score = 0x0000000000000001;


			// =L------ -------- -------- -------- -------- -------- -------- -------=
			// L - Whether the topic's language matches the link's language.  For type and class parent links this is mandatory.  For
			// Natural Docs links this is the highest priority criteria as links should favor any kind of match within their own language
			// over matches from another.

			if (link.LanguageID == topic.LanguageID)
				{  score |= 0x4000000000000000;  }
			else if (link.Type == LinkType.ClassParent || link.Type == LinkType.Type)
				{  return 0;  }
			else if (minimumScore > 0x3FFFFFFFFFFFFFFF)
				{  return -1;  }


			// ==CE---- -------- -------- -SSSSSSS SSSIIIII I------- -------- -------=
			// Now we have to go through the interpretations to figure out the fields that could change based on them.
			// C and S will be handled by ScoreInterpretation().  E and I will be handled here.

			// C - Whether the topic and link's capitalization match if it matters to the language.  This depends on the
			//		 interpretation because it can be affected by how named links are split.
			// E - Whether the text is an exact match with no plural or possessive conversions applied.  Named links are
			//		 okay.
			// S - How high on the scope list the symbol match is.
			// I - How high on the interpretation list (named/plural/possessive) the match is.

			long bestInterpretationScore = 0;
			int bestInterpretationIndex = 0;

			if (link.Type == LinkType.NaturalDocs)
				{
				for (int i = 0; i < interpretations.Count; i++)
					{
					long interpretationScore = ScoreInterpretation(topic, link, SymbolString.FromPlainText_NoParameters(interpretations[i].Target));

					if (interpretationScore != 0)
						{
						// Add E if there were no plurals or possessives.  Named links are okay.
						if (interpretations[i].PluralConversion == false && interpretations[i].PossessiveConversion == false)
							{  interpretationScore |= 0x1000000000000000;  }

						if (interpretationScore > bestInterpretationScore)
							{  
							bestInterpretationScore = interpretationScore;
							bestInterpretationIndex = i;
							}
						}
					}
				}

			else // type or class parent link
				{
				bestInterpretationScore = ScoreInterpretation(topic, link, link.Symbol);
				bestInterpretationIndex = 0;

				// Add E if there was a match.
				if (bestInterpretationScore != 0)
					{  bestInterpretationScore |= 0x1000000000000000;  }
				}

			// If none of the symbol interpretations matched the topic, we're done.
			if (bestInterpretationScore == 0)
				{  return 0;  }

			// Combine C, E, and S into the main score.
			score |= bestInterpretationScore;

			// Calculate I so that lower indexes are higher scores.  Since these are the lowest order bits it's okay to leave
			// this for the end instead of calculating it for every interpretation.
			if (bestInterpretationIndex > 63)
				{  bestInterpretationIndex = 63;  }

			long bestInterpretationBits = 63 - bestInterpretationIndex;
			bestInterpretationBits <<= 23;

			score |= bestInterpretationBits;

			if ((score | 0x0FFFFF80007FFFFF) < minimumScore)
				{  return -1;  }


			// ====TPPP PPPPPPPP PPPPPPPP P======= ======== =------- -------- -------=
			// T - Whether the link parameters exactly match the topic title parameters.
			// P - How well the parameters match.
			
			// Both of these only apply to Natural Docs links that have parameters.
			if (link.Type == LinkType.NaturalDocs)
				{  
				int parametersIndex = ParameterString.GetParametersIndex(link.Text);

				if (parametersIndex != -1)
					{
					string linkParametersString = link.Text.Substring(parametersIndex);
					ParameterString linkParameters = ParameterString.FromPlainText(linkParametersString);

					// If the topic title has parameters as well, the link parameters must match them exactly.  We
					// don't do fuzzy matching with topic title parameters.
					if (topic.HasTitleParameters && string.Compare(linkParameters, topic.TitleParameters, !language.CaseSensitive) == 0)
						{  
						score |= 0x0800000000000000;
						// We can skip the prototype match since this outweighs it.  Also, we don't want two link targets
						// where the topic title parameters are matched to be distinguished by the prototype parameters.
						// We'll let it fall through to lower properties in the score.
						}
					else
						{
						// Score the first nine parameters.
						for (int i = 0; i < 9; i++)
							{
							long paramScore = ScoreParameter(topic.ParsedPrototype, linkParameters, i, !language.CaseSensitive);

							if (paramScore == -1)
								{  return 0;  }

							paramScore <<= 39 + ((9 - i) * 2);
							score |= paramScore;
							}

						// The tenth is special.  It's possible that functions may have more than ten parameters, so we go
						// through the rest of them and use the lowest score we get.

						long lastParamScore = ScoreParameter(topic.ParsedPrototype, linkParameters, 9, !language.CaseSensitive);
						int maxParameters = linkParameters.NumberOfParameters;

						if (topic.ParsedPrototype != null && topic.ParsedPrototype.NumberOfParameters > maxParameters)
							{  maxParameters = topic.ParsedPrototype.NumberOfParameters;  }

						for (int i = 10; i < maxParameters; i++)
							{
							long paramScore = ScoreParameter(topic.ParsedPrototype, linkParameters, i, !language.CaseSensitive);

							if (paramScore < lastParamScore)
								{  lastParamScore = paramScore;  }
							}

						if (lastParamScore == -1)
							{  return 0;  }

						lastParamScore <<= 39;
						score |= lastParamScore;
						}
					}
				}


			// ======== ======== ======== ======== ======== =BFFFFFF Rbbbbbbb brrrrrr=
			// Finish off the score with the topic properties.

			// B - Whether the topic has a body
			// F - How high on the list of topics that define the same symbol in the same file this is.
			// R - Whether the topic has a prototype.
			// b - The length of the body divided by 16.
			// r - The length of the prototype divided by 16.

			score |= ScoreTopic(topic);

			return score;
			}


		/* Function: ScoreInterpretation
		 * A function used by <Score()> to determine the C and S fields of the score for the passed interpretation.  Only
		 * those fields and the trailing 1 will be set in the returned score.  If the interpretation doesn't match, it will return
		 * zero.
		 */
		private long ScoreInterpretation (Topic topic, Link link, SymbolString interpretation)
			{
			// --C----- -------- -------- -SSSSSSS SSS----- -------- -------- -------1
			// C - Whether the topic and link's capitalization match if it matters to the language.
			// S - How high on the scope list the symbol match is.

			long scopeScore = ScoreScopeInterpretation(topic, link, interpretation);

			// S is always going to be higher for scopes than for using statements, so if there's a match and C is set we can
			// quit early because there's no way a using statement is going to top it.
			if (scopeScore > 0x3000000000000000)
				{  return scopeScore;  }

			long usingScore = ScoreUsingInterpretation(topic, link, interpretation);

			if (scopeScore > usingScore)
				{  return scopeScore;  }
			else
				{  return usingScore;  }
			}


		/* Function: ScoreScopeInterpretation
		 * A function used by <ScoreInterpretation()> to determine the C and S fields of the score for the passed interpretation 
		 * using only the scope.  Only those fields and the trailing 1 will be set in the returned score.  If the interpretation doesn't 
		 * match using the scope, it will return zero.
		 */
		private long ScoreScopeInterpretation (Topic topic, Link link, SymbolString interpretation)
			{
			// --C----- -------- -------- -SSSSSSS SSS----- -------- -------- -------1
			// C - Whether the topic and link's capitalization match if it matters to the language.
			// S - How high on the scope list the symbol match is.

			Language topicLanguage = EngineInstance.Languages.FromID(topic.LanguageID);
			CommentType commentType = EngineInstance.CommentTypes.FromID(topic.CommentTypeID);


			// Values of C:
			//		Natural Docs links:
			//			1 - Topic is documentation, case matches
			//			1 - Topic is documentation, case differs
			//			1 - Topic is file, case matches
			//			1 - Topic is file, case differs
			//			1 - Topic is code, topic language is case sensitive, case matches
			//			0 - Topic is code, topic language is case sensitive, case differs
			//			1 - Topic is code, topic language is case insensitive, case matches
			//			1 - Topic is code, topic language is case insensitive, case differs
			//		Type/Class Parent links:
			//			Assuming they're the same language...
			//			X - Topic is documentation, case matches
			//			X - Topic is documentation, case differs
			//			X - Topic is file, case matches
			//			X - Topic is file, case differs
			//			1 - Topic is code, language is case sensitive, case matches
			//			X - Topic is code, language is case sensitive, case differs
			//			1 - Topic is code, language is case insensitive, case matches
			//			1 - Topic is code, language is case insensitive, case differs

			bool caseFlagged;
			bool caseRequired;
			
			if (link.Type == LinkType.NaturalDocs)
				{  
				caseRequired = false;
				caseFlagged = (commentType.Flags.Code && topicLanguage.CaseSensitive);
				}
			else
				{
				if (commentType.Flags.Code == false)
					{  return 0;  }

				caseRequired = topicLanguage.CaseSensitive;  
				caseFlagged = false;
				}


			// --C----- -------- -------- -SSSSSSS SSS----- -------- -------- -------1
			// Our baseline.

			long score = 0x0000000000000001;
				
			int scopeListIndex;


			// If we match as a global symbol...

			if (string.Compare(topic.Symbol, interpretation, !caseRequired) == 0)
				{
				if (link.Context.ScopeIsGlobal)
					{  scopeListIndex = 0;  }
				else
					{
					// Conceptually, we had to walk down the entire hierachy to get to global:
					//    Scope A.B.C = A.B.C.Name, A.B.Name, A.Name, Name = Index 3
					// so the scope list index is the number of dividers in the scope plus one.

					int linkScopeIndex, linkScopeLength;
					link.Context.GetRawTextScope(out linkScopeIndex, out linkScopeLength);

					int dividers = link.Context.RawText.Count(SymbolString.SeparatorChar, linkScopeIndex, linkScopeLength);
					scopeListIndex = dividers + 1;
					}

				// --C----- -------- -------- -SSSSSSS SSS----- -------- -------- -------=
				// Apply C
				if (!caseFlagged || string.Compare(topic.Symbol, interpretation, false) == 0)
					{  score |= 0x2000000000000000;  }
				}


			// If the topic ends with the interepretation, such as "A.B.C.Name" and "Name"...

			else if (topic.Symbol.EndsWith(interpretation, !caseRequired))
				{
				string topicSymbolString = topic.Symbol.ToString();
				int topicScopeIndex = 0;
				int topicScopeLength = topicSymbolString.Length - interpretation.ToString().Length - 1;

				// See if the link's scope can completely encompass the remaining scope:
				//    Topic A.B.C.Name + Link Name + Link Scope A.B.C = yes
				//    Topic A.B.C.Name + Link Name + Link Scope A.B = no
				//    Topic A.B.C.Name + Link Name + Link Scope A.B.C.D = yes, it can walk up the hierarchy
				//    Topic A.B.C.Name + Link Name + Link Scope A.B.CC = no, can't split a word
				//    Topic A.B.C.Name + Link Name + Link Scope X.Y.Z = no

				string linkContextString = link.Context.RawText;
				int linkScopeIndex, linkScopeLength;
				link.Context.GetRawTextScope(out linkScopeIndex, out linkScopeLength);

				// If the remaining topic scope is a substring or equal to the link scope...
				if (topicScopeLength <= linkScopeLength && 
					 string.Compare(linkContextString, linkScopeIndex, topicSymbolString, topicScopeIndex, topicScopeLength, !caseRequired) == 0)
					{
					if (topicScopeLength == linkScopeLength)
						{
						// If it's an exact match, this is considered the first entry on our conceptual scope list.
						scopeListIndex = 0;
						}

					else // topicScopeLength < linkScopeLength
						{
						// If the scope was a substring, the next character needs to be a separator so we don't split a word.
						if (linkContextString[topicScopeLength] != SymbolString.SeparatorChar)
							{  return 0;  }

						// The scope list index is the number of separators we trimmed off:
						//    Link scope: A.B.C.D
						//    Remaining topic scope: A.B
						//    Scope list:
						//       0 - A.B.C.D
						//       1 - A.B.C
						//       2 - A.B
						//       3 - A
						//       4 - global
						scopeListIndex = linkContextString.Count(SymbolString.SeparatorChar, linkScopeIndex + topicScopeLength,
																								  linkScopeLength - topicScopeLength);
						}

					// --C----- -------- -------- -SSSSSSS SSS----- -------- -------- -------=
					// Apply C
					if (!caseFlagged || 
						(topicSymbolString.EndsWith(interpretation, false, System.Globalization.CultureInfo.CurrentCulture) == true &&
						 string.Compare(linkContextString, linkScopeIndex, topicSymbolString, topicScopeIndex, topicScopeLength, false) == 0) )
						{  score |= 0x2000000000000000;  }
					}
				else
					{  return 0;  }
				}
			else
				{  return 0;  }


			// --=----- -------- -------- -SSSSSSS SSS----- -------- -------- -------=
			// Encode the scope index.  We want lower indexes to have a higher score.

			if (scopeListIndex > 1023)
				{  scopeListIndex = 1023;  }

			long scopeListBits = 1023 - scopeListIndex;
			scopeListBits <<= 29;

			score |= scopeListBits;

			return score;
			}


		/* Function: ScoreUsingInterpretation
		 * A function used by <ScoreInterpretation()> to determine the C and S fields of the score for the passed interpretation 
		 * using only the using statements.  Only those fields and the trailing 1 will be set in the returned score.  If the interpretation 
		 * doesn't match using the using statements, it will return zero.
		 */
		private long ScoreUsingInterpretation (Topic topic, Link link, SymbolString interpretation)
			{
			// --C----- -------- -------- -SSSSSSS SSS----- -------- -------- -------1
			// C - Whether the topic and link's capitalization match if it matters to the language.
			// S - How high on the scope list the symbol match is.

			IList<UsingString> usingStrings = link.Context.GetUsingStatements();

			if (usingStrings == null || usingStrings.Count == 0)
				{  return 0;  }

			Language topicLanguage = EngineInstance.Languages.FromID(topic.LanguageID);
			CommentType commentType = EngineInstance.CommentTypes.FromID(topic.CommentTypeID);


			// Values of C:
			//		Natural Docs links:
			//			1 - Topic is documentation, case matches
			//			1 - Topic is documentation, case differs
			//			1 - Topic is file, case matches
			//			1 - Topic is file, case differs
			//			1 - Topic is code, topic language is case sensitive, case matches
			//			0 - Topic is code, topic language is case sensitive, case differs
			//			1 - Topic is code, topic language is case insensitive, case matches
			//			1 - Topic is code, topic language is case insensitive, case differs
			//		Type/Class Parent links:
			//			Assuming they're the same language...
			//			X - Topic is documentation, case matches
			//			X - Topic is documentation, case differs
			//			X - Topic is file, case matches
			//			X - Topic is file, case differs
			//			1 - Topic is code, language is case sensitive, case matches
			//			X - Topic is code, language is case sensitive, case differs
			//			1 - Topic is code, language is case insensitive, case matches
			//			1 - Topic is code, language is case insensitive, case differs

			bool caseFlagged;
			bool caseRequired;
			
			if (link.Type == LinkType.NaturalDocs)
				{  
				caseRequired = false;
				caseFlagged = (commentType.Flags.Code && topicLanguage.CaseSensitive);
				}
			else
				{
				if (commentType.Flags.Code == false)
					{  return 0;  }

				caseRequired = topicLanguage.CaseSensitive;  
				caseFlagged = false;
				}


			// Find the scope list index to start at, since the actual scopes come before the using statements.
			//    Scope list:
			//       0 - A.B.C.Link
			//       1 - A.B.Link
			//       2 - A.Link
			//       3 - Link
			//       4 - Link + first using statement
			// So if there's a scope, the starting index is the number of separators in the scope + 2.  Otherwise it's one.
			//    Scope list:
			//       0 - Link
			//       1 - Link + first using statement

			int scopeListIndex;

			if (link.Context.ScopeIsGlobal)
				{  scopeListIndex = 1;  }
			else
				{
				int scopeIndex, scopeLength;
				link.Context.GetRawTextScope(out scopeIndex, out scopeLength);

				scopeListIndex = link.Context.RawText.Count(SymbolString.SeparatorChar, scopeIndex, scopeLength) + 2;
				}


			// Go through each using statement looking for the best score.

			long bestScore = 0;

			foreach (var usingString in usingStrings)
				{
				SymbolString newInterpretation;
				bool newInterpretationPossible;

				if (usingString.Type == UsingString.UsingType.AddPrefix)
					{
					newInterpretation = usingString.PrefixToAdd + interpretation;
					newInterpretationPossible = true;
					}
				else if (usingString.Type == UsingString.UsingType.ReplacePrefix)
					{
					SymbolString prefixToRemove = usingString.PrefixToRemove;
					string prefixToRemoveString = prefixToRemove.ToString();
					string interpretationString = interpretation.ToString();

					if (interpretationString.Length > prefixToRemoveString.Length &&
						interpretation.StartsWith(prefixToRemove, !caseRequired))
						{
						newInterpretation = usingString.PrefixToAdd + SymbolString.FromExportedString(interpretationString.Substring(prefixToRemoveString.Length + 1));
						newInterpretationPossible = true;
						}
					else
						{  
						newInterpretation = new SymbolString();  // to make the compiler shut up
						newInterpretationPossible = false;  
						}
					}
				else
					{  throw new NotImplementedException();  }


				if (newInterpretationPossible && string.Compare(newInterpretation, topic.Symbol, !caseRequired) == 0)
					{
					// --C----- -------- -------- -SSSSSSS SSS----- -------- -------- -------1
					// Our baseline.

					long score = 0x0000000000000001;


					// --C----- -------- -------- -SSSSSSS SSS----- -------- -------- -------=
					// Encode the scope index.  We want lower indexes to have a higher score.

					if (scopeListIndex > 1023)
						{  scopeListIndex = 1023;  }

					long scopeListBits = 1023 - scopeListIndex;
					scopeListBits <<= 29;

					score |= scopeListBits;


					// --C----- -------- -------- -======= ===----- -------- -------- -------=
					// Determine C.  If C is set we can quit early because it would be impossible for a later using statement to
					// generate a higher score.

					if (!caseFlagged || string.Compare(newInterpretation, topic.Symbol, false) == 0)
						{  
						score |= 0x2000000000000000;  
						bestScore = score;
						break;
						}
					else
						{
						if (score > bestScore)
							{  bestScore = score;  }
						}
					}

				scopeListIndex++;
				}

			return bestScore;
			}


		/* Function: ScoreParameter
		 * Returns a two bit value representing how well the parameters match, or -1 if they match so poorly that the link and
		 * the target shouldn't be considered a match at all.
		 */
		private long ScoreParameter (ParsedPrototype prototype, ParameterString linkParameters, int index, bool ignoreCase)
			{
			// -1 - The link has a parameter but the prototype does not.
			// 00 - The prototype has a parameter but the link does not.  This allows links on partial parameters.
			// 00 - They both have parameters but do not match at all.
			// 01 - The link doesn't have a parameter but the prototype has one with a default value set.
			// 10 - The parameters match except for qualifiers or modifiers like "unsigned".
			// 11 - The parameters match completely, by type or by name, or both don't exist.

			SimpleTokenIterator linkParamStart, linkParamEnd;
			TokenIterator prototypeParamStart, prototypeParamEnd;

			bool hasLinkParam = linkParameters.GetParameter(index, out linkParamStart, out linkParamEnd);
			bool hasPrototypeParam;
			
			if (prototype == null)
				{
				hasPrototypeParam = false;

				// To shut the compiler up.
				prototypeParamStart = new TokenIterator();
				prototypeParamEnd = new TokenIterator();
				}
			else
				{  hasPrototypeParam = prototype.GetParameter(index, out prototypeParamStart, out prototypeParamEnd);  }

			if (!hasLinkParam)
				{
				if (!hasPrototypeParam)
					{  return 3;  }
				else
					{  
					// There is a prototype parameter but not a link parameter.  This will be 0 or 1 depending on whether the 
					// prototype parameter has a default value.

					while (prototypeParamStart < prototypeParamEnd)
						{
						if (prototypeParamStart.PrototypeParsingType == PrototypeParsingType.DefaultValue)
							{  return 1;  }

						prototypeParamStart.Next();
						}

					return 0;
					}
				}

			else // hasLinkParam == true
				{
				if (hasPrototypeParam == false)
					{  return -1;  }

				// Both the link and the prototype have parameters at index.

				bool typeMatch = false;
				bool typeMismatch = false;
				bool typeModifierMismatch = false;
				bool nameMatch = false;
				bool nameMismatch = false;

				int suffixLevel = 0;

				while (prototypeParamStart < prototypeParamEnd)
					{
					var type = prototypeParamStart.PrototypeParsingType;

					// We want any mismatches that occur nested in type suffixes to be scored as a modifier mismatch.
					if (type == PrototypeParsingType.OpeningTypeSuffix)
						{  suffixLevel++;  }
					else if (type == PrototypeParsingType.ClosingTypeSuffix)
						{  suffixLevel--;  }
					else if (suffixLevel > 0)
						{  type = PrototypeParsingType.TypeSuffix;  }

					switch (type)
						{
						case PrototypeParsingType.TypeModifier:
						case PrototypeParsingType.TypeQualifier:
						case PrototypeParsingType.OpeningTypeSuffix:
						case PrototypeParsingType.ClosingTypeSuffix:
						case PrototypeParsingType.TypeSuffix:
						case PrototypeParsingType.NamePrefix_PartOfType:
						case PrototypeParsingType.NameSuffix_PartOfType: 

							if (linkParamStart < linkParamEnd && linkParamStart.MatchesToken(prototypeParamStart, ignoreCase))
								{  
								linkParamStart.Next();  
								linkParamStart.NextPastWhitespace();
								}
							else
								{  typeModifierMismatch = true;  }
							break;

						case PrototypeParsingType.Type:

							if (linkParamStart < linkParamEnd && linkParamStart.MatchesToken(prototypeParamStart, ignoreCase))
								{  
								typeMatch = true;  

								linkParamStart.Next();  
								linkParamStart.NextPastWhitespace();
								}
							else
								{  typeMismatch = true;  }
							break;

						case PrototypeParsingType.Name:

							if (linkParamStart < linkParamEnd && linkParamStart.MatchesToken(prototypeParamStart, ignoreCase))
								{  
								nameMatch = true;  

								linkParamStart.Next();  
								linkParamStart.NextPastWhitespace();
								}
							else
								{  nameMismatch = true;  }
							break;
						}

					prototypeParamStart.Next();
					prototypeParamStart.NextPastWhitespace();
					}

				if (linkParamStart < linkParamEnd)
					{  return 0;  }
				if (nameMatch && !nameMismatch)
					{  return 3;  }
				if (typeMatch && !typeMismatch)
					{
					if (!typeModifierMismatch)
						{  return 3;  }
					else
						{  return 2;  }
					}

				return 0;
				}
			}


		/* Function: ScoreTopic
		 * Generates the portions of the score which depend on the topic properties only and not how well they match a link.
		 * These are the B, F, R, b, and r components which are used for breaking ties when multiple topics would otherwise 
		 * satisfy a link equally.  This is also used in class views where there are multiple definitions of the same code element 
		 * and it must decide which one to display.  Using this function for that will make it more consistent with how links will
		 * resolve.
		 */
		private long ScoreTopic (Topic topic)
			{
			// -------- -------- -------- -------- -------- -BFFFFFF Rbbbbbbb brrrrrr1
			// B - Whether the topic has a body
			// F - How high on the list of topics that define the same symbol in the same file this is.
			// R - Whether the topic has a prototype.
			// b - The length of the body divided by 16.
			// r - The length of the prototype divided by 16.

			long score = 0x0000000000000001;


			// -------- -------- -------- -------- -------- --FFFFFF -------- -------=
			// F - How high on the list of topics that define the same symbol in the same file this is.

			long symbolDefinitionBits = topic.SymbolDefinitionNumber;

			if (symbolDefinitionBits > 63)
				{  symbolDefinitionBits = 63;  }

			symbolDefinitionBits = 63 - symbolDefinitionBits;
			symbolDefinitionBits <<= 16;

			score |= symbolDefinitionBits;


			// -------- -------- -------- -------- -------- -B====== -bbbbbbb b------=
			// B - Whether the topic has a body
			// b - The length of the body divided by 16.
			//    0-15 = 0
			//    16-31 = 1
			//    ...
			//		4064-4079 = 254
			//		4080+ = 255

			// Use BodyLength so we can exclude Body from the query.
			if (topic.BodyLength > 0)
				{
				long bodyBits = topic.BodyLength / 16;

				if (bodyBits > 255)
					{  bodyBits = 255;  }

				bodyBits <<= 7;
				bodyBits |= 0x0000000000400000;

				score |= bodyBits;
				}


			// -------- -------- -------- -------- -------- -======= R======= =rrrrrr=
			// R - Whether the topic has a prototype.
			// r - The length of the prototype divided by 16.
			//    0-15 = 0
			//    16-31 = 1
			//    ...
			//    992-1007 = 62
			//    1008+ = 63

			if (topic.Prototype != null)
				{
				long prototypeBits = topic.Prototype.Length / 16;

				if (prototypeBits > 63)
					{  prototypeBits = 63;  }

				prototypeBits <<= 1;
				prototypeBits |= 0x0000000000008000;

				score |= prototypeBits;
				}


			return score;
			}



		// Group: Definition Choosing Functions
		// __________________________________________________________________________
		//
		// These aren't always for links, but use similar logic so it's implemented here anyway.
		//


		/* Function: IsBetterTopicDefinition
		 * If two <Topics> both define the same thing, returns whether the second one serves as a better definition than the first,
		 * weighing things like the length of the body and whether they have prototypes.
		 */
		public bool IsBetterTopicDefinition (Topic currentDefinition, Topic toTest)
			{
			#if DEBUG
			if (EngineInstance.Languages.FromID(currentDefinition.LanguageID).IsSameCodeElement(currentDefinition, toTest) == false)
				{  throw new Exception("Tried to call IsBetterTopicDefinition() on two topics with different code elements.");  }
			#endif

			// Piggyback on ScoreTopic since iti already evaluates all the properties we want
			long currentScore = ScoreTopic(currentDefinition);
			long toTestScore = ScoreTopic(toTest);

			if (toTestScore > currentScore)
				{  return true;  }
			else if (toTestScore < currentScore)
				{  return false;  }

			// If the scores are equal, compare the paths.  Having a path that sorts higher isn't indicitive of anything, it just makes
			// sure the results of this function are consistent between runs.

			if (currentDefinition.FileID != toTest.FileID)
				{
				Path currentPath = EngineInstance.Files.FromID(currentDefinition.FileID).FileName;
				Path toTestPath = EngineInstance.Files.FromID(toTest.FileID).FileName;

				return ( Path.Compare(currentPath, toTestPath) > 0 );
				}

			// If they're in the same file, choose the one with the lower definition number.  If they're equal that means they're both 
			// the same topic and either topic is fine.

			else
				{  return (toTest.FilePosition < currentDefinition.FilePosition);  }
			}


		/* Function: IsBetterClassDefinition
		 * If two <Topics> both have the same <ClassString>, returns whether the second one serves as a better definition 
		 * than the first.  Is safe to use with topics that don't have <Topic.DefinesClass> set.
		 */
		public bool IsBetterClassDefinition (Topic currentDefinition, Topic toTest)
			{
			#if DEBUG
			if (currentDefinition.ClassString != toTest.ClassString)
				{  throw new Exception("Tried to call IsBetterClassDefinition() on two topics with different class strings.");  }
			#endif

			if (toTest.DefinesClass == false)
				{  return false;  }
			if (currentDefinition.DefinesClass == false)
				{  return true;  }

			return IsBetterTopicDefinition(currentDefinition, toTest);
			}

		}
	}
