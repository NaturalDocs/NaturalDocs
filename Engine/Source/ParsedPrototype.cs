/* 
 * Class: CodeClear.NaturalDocs.Engine.ParsedPrototype
 * ____________________________________________________________________________
 * 
 * A class that wraps a <Tokenizer> for a prototype that's been marked with <PrototypeParsingTypes>, providing easier 
 * access to things like parameter lines.
 * 
 * Usage:
 * 
 *		The functions and properties obviously rely on the relevant tokens being set.  You cannot expect a proper result from
 *		<GetParameter()> or <NumberOfParameters> unless the tokens are marked with <PrototypeParsingType.StartOfParams>,
 *		<PrototypeParsingType.ParamSeparator>, etc.  Likewise, you can't get anything from <GetParameterName()> unless
 *		you also have tokens marked with <PrototypeParsingType.Name>.  However, you can set the parameter divider tokens,
 *		call <GetParameter()>, and then use those bounds to further parse the parameter and set tokens like
 *		<PrototypeParsingType.Name>.
 * 
 *		An important thing to remember though is that the parameter divisions are calculated once and saved.  Only call
 *		functions like <GetParameter()> after *ALL* the separator tokens (<PrototypeParsingType.StartOfParams>,
 *		<PrototypeParsingType.ParamSeparator>, and <PrototypeParsingType.EndOfParams>) are set and will not change
 *		going forward.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2016 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine
	{
	public class ParsedPrototype
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: ParameterStyle
		 * 
		 * C - A C-style prototype with parameters in a form similar to "int x = 12".
		 * Pascal - A Pascal-style prototype with parameters in a form similar to "x: int := 12".
		 * 
		 * Typeless prototypes will be returned as C-style.
		 */
		public enum ParameterStyle : byte
			{  C, Pascal  }


		/* Enum: SectionType
		 * 
		 * PrePrototypeLine - A line that should appear separately before the prototype.
		 * BeforeParameters - The prototype prior to the parameters.  This will include the start of parameters symbol, such as
		 *							   an opening parenthesis in C#.  If there are no parameters, this will be the entire prototype.
		 *	Parameter - An individual parameter.  It may include the parameter separator symbol, such as a comma in C#.
		 *	AfterParameters - The prototype after the parameters.  This will include the end of parameters symbol, such as a
		 *							 closing parenthesis in C#.
		 *	PostPrototypeLine - A line that should appear separately after the prototype.
		 */
		public enum SectionType : byte
			{  PrePrototypeLine, BeforeParameters, Parameter, AfterParameters, PostPrototypeLine  }



		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: ParsedPrototype
		 * Creates a new parsed prototype.
		 */
		public ParsedPrototype (Tokenizer prototype)
			{
			tokenizer = prototype;
			sections = null;
			style = null;
			}


		/* Function: GetPrePrototypeLine
		 * Returns the bounds of a numbered pre-prototype line.  Numbers start at zero.  It will return false if one does not
		 * exist at that number.
		 */
		public bool GetPrePrototypeLine (int lineNumber, out TokenIterator start, out TokenIterator end)
			{
			return GetSectionBounds(SectionType.PrePrototypeLine, lineNumber, out start, out end);
			}


		/* Function: GetCompletePrototype
		 * Returns the bounds of the complete prototype, minus whitespace.  This does NOT include pre-prototype lines.
		 */
		public void GetCompletePrototype (out TokenIterator start, out TokenIterator end)
			{
			Section beforeParameters = FindSection(SectionType.BeforeParameters);

			start = tokenizer.FirstToken;
			start.Next(beforeParameters.StartIndex);

			Section afterParameters = FindSection(SectionType.AfterParameters);

			end = start;

			if (afterParameters == null)
				{
				end.Next(beforeParameters.EndIndex - beforeParameters.StartIndex);
				}
			else
				{
				end.Next(afterParameters.EndIndex - beforeParameters.StartIndex);
				}
			}


		/* Function: GetAccessLevel
		 * Returns the <Languages.AccessLevel> if it can be determined by the prototype.  This should only be used with basic
		 * language support as it's not as reliable as the results from the dedicated language parsers.
		 */
		public Languages.AccessLevel GetAccessLevel ()
			{
			Languages.AccessLevel accessLevel = Languages.AccessLevel.Unknown;

			TokenIterator iterator, end;
			if (GetSectionBounds(SectionType.BeforeParameters, 0, out iterator, out end) == false)
				{  return accessLevel;  }

			bool previousWasUnderscore = false;

			while (iterator < end)
				{
				if (iterator.FundamentalType == FundamentalType.Text &&
					 iterator.PrototypeParsingType == PrototypeParsingType.TypeModifier &&
					 previousWasUnderscore == false)
					{
					if (iterator.MatchesToken("public"))
						{  accessLevel = Languages.AccessLevel.Public;  }
					else if (iterator.MatchesToken("private"))
						{  accessLevel = Languages.AccessLevel.Private;  }
					else if (iterator.MatchesToken("protected"))
						{
						if (accessLevel == Languages.AccessLevel.Internal)
							{  accessLevel = Languages.AccessLevel.ProtectedInternal;  }
						else
							{  accessLevel = Languages.AccessLevel.Protected;  }
						}
					else if (iterator.MatchesToken("internal"))
						{
						if (accessLevel == Languages.AccessLevel.Protected)
							{  accessLevel = Languages.AccessLevel.ProtectedInternal;  }
						else
							{  accessLevel = Languages.AccessLevel.Internal;  }
						}
					}
				else
					{
					previousWasUnderscore = (iterator.Character == '_');
					}

				iterator.Next();
				}

			return accessLevel;
			}

			
		/* Function: GetBeforeParameters
		 * Returns the bounds of the section of the prototype prior to the parameters.  If it has parameters, it will include the 
		 * starting symbol of the parameter list such as the opening parenthesis.  If there are no parameters, this will return the 
		 * bounds of the entire prototype.
		 */
		public void GetBeforeParameters (out TokenIterator start, out TokenIterator end)
			{
			GetSectionBounds(SectionType.BeforeParameters, 0, out start, out end);
			}


		/* Function: GetParameter
		 * Returns the bounds of a numbered parameter.  Numbers start at zero.  It will return false if one does not exist at that
		 * number.
		 */
		public bool GetParameter (int parameterNumber, out TokenIterator start, out TokenIterator end)
			{
			return GetSectionBounds(SectionType.Parameter, parameterNumber, out start, out end);
			}


		/* Function: GetParameterName
		 * Returns the bounds of the name of the passed parameter, or false if it couldn't find it.
		 */
		public bool GetParameterName (int index, out TokenIterator start, out TokenIterator end)
			{
			TokenIterator paramStart, paramEnd;

			if (!GetParameter(index, out paramStart, out paramEnd))
				{  
				start = paramEnd;
				end = paramEnd;
				return false;  
				}

			start = paramStart;

			while (start < paramEnd && start.PrototypeParsingType != PrototypeParsingType.Name)
				{  start.Next();  }

			if (start < paramEnd)
				{
				end = start;

				do
					{  end.Next();  }
				while (end.PrototypeParsingType == PrototypeParsingType.Name && end < paramEnd);

				return true;
				}
			else
				{  
				start = paramEnd;
				end = paramEnd;
				return false;  
				}
			}


		/* Function: GetFullParameterType
		 * 
		 * Returns the bounds of the type of the passed parameter, or false if it couldn't find it.  This includes modifiers and type
		 * suffixes.  Since the type token may not be continuous, it returns separate start and end iterators for type prefixes
		 * (* in "int *x") and suffixes ("[12]" in "int x[12]").
		 * 
		 * If the implied types flag is set this will return "int" for y in "int x, y".  If it is not then it will return false for y.
		 */
		public bool GetFullParameterType (int index, out TokenIterator start, out TokenIterator end, 
													  out TokenIterator prefixStart, out TokenIterator prefixEnd,
													  out TokenIterator suffixStart, out TokenIterator suffixEnd,
													  bool impliedTypes = true)
			{
			TokenIterator paramStart, paramEnd;

			if (!GetParameter(index, out paramStart, out paramEnd))
				{  
				start = paramEnd;
				end = paramEnd;
				prefixStart = paramEnd;
				prefixEnd = paramEnd;
				suffixStart = paramEnd;
				suffixEnd = paramEnd;
				return false;  
				}


			// Find the beginning of the type by finding the first type token

			start = paramStart;
			PrototypeParsingType type = start.PrototypeParsingType;

			while (start < paramEnd && 
						type != PrototypeParsingType.Type &&
						type != PrototypeParsingType.TypeModifier &&
						type != PrototypeParsingType.TypeQualifier)
				{  
				start.Next();  
				type = start.PrototypeParsingType;
				}


			// If we found one, find the end of the type

			bool foundType = (start < paramEnd);
			end = start;

			if (foundType)
				{
				int nestLevel = 0;

				do
					{  
					if (end.PrototypeParsingType == PrototypeParsingType.OpeningTypeSuffix)
						{  nestLevel++;  }
					else if (end.PrototypeParsingType == PrototypeParsingType.ClosingTypeSuffix)
						{  nestLevel--;  }

					end.Next();
					type = end.PrototypeParsingType;
					}
				while ( (nestLevel > 0 ||
							  type == PrototypeParsingType.Type ||
							  type == PrototypeParsingType.TypeModifier ||
							  type == PrototypeParsingType.TypeQualifier ||
							  type == PrototypeParsingType.OpeningTypeSuffix ||
							  type == PrototypeParsingType.ClosingTypeSuffix ||
							  type == PrototypeParsingType.TypeSuffix ||
							  end.FundamentalType == FundamentalType.Whitespace) &&
							end < paramEnd);

				end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);
				}


			// If we didn't find a type, see if it's implied

			else if (impliedTypes)
				{
				if (Style == ParameterStyle.C)
					{
					// Move backwards for "int x, y"
					for (int i = index - 1; i >= 0; i--)
						{
						if (GetFullParameterType(i, out start, out end, out prefixStart, out prefixEnd, out suffixStart, out suffixEnd, false))
							{  
							foundType = true;
							break;
							}
						}
					}
				else // Style == ParameterStyle.Pascal
					{
					// Move forwards for "x, y: integer"
					for (int i = index + 1; i <= NumberOfParameters; i++)
						{
						if (GetFullParameterType(i, out start, out end, out prefixStart, out prefixEnd, out suffixStart, out suffixEnd, false))
							{
							foundType = true;
							break;
							}
						}
					}
				}
			

			// If we found a type, explicit or implied, find the prefix and suffix.  We do this after checking for implied types because we 
			// want "int a[12], b" to work.  b should be int, not int[12].  In "int *x, y", y should be int, not int*.

			if (foundType)
				{
				prefixStart = paramStart;

				while (prefixStart < paramEnd &&
							 prefixStart.PrototypeParsingType != PrototypeParsingType.NamePrefix_PartOfType)
					{  prefixStart.Next();  }

				prefixEnd = prefixStart;

				while (prefixEnd < paramEnd &&
							 prefixEnd.PrototypeParsingType == PrototypeParsingType.NamePrefix_PartOfType)
					{  prefixEnd.Next();  }

				suffixStart = paramStart;

				while (suffixStart < paramEnd &&
							 suffixStart.PrototypeParsingType != PrototypeParsingType.NameSuffix_PartOfType)
					{  suffixStart.Next();  }

				suffixEnd = suffixStart;

				while (suffixEnd < paramEnd &&
							 suffixEnd.PrototypeParsingType == PrototypeParsingType.NameSuffix_PartOfType)
					{  suffixEnd.Next();  }

				return true;
				}

			else // didn't find a type
				{
				start = paramEnd;
				end = paramEnd;
				prefixStart = paramEnd;
				prefixEnd = paramEnd;
				suffixStart = paramEnd;
				suffixEnd = paramEnd;
				return false;  
				}
			}


		/* Function: GetBaseParameterType
		 * 
		 * Returns the bounds of the type of the passed parameter, or false if it couldn't find it.  This excludes modifiers and type
		 * suffixes.
		 * 
		 * If the implied types flag is set this will return "int" for y in "int x, y".  If it is not then it will return false for y.
		 */
		public bool GetBaseParameterType (int index, out TokenIterator start, out TokenIterator end, bool impliedTypes = true)
			{
			TokenIterator paramStart, paramEnd;

			if (!GetParameter(index, out paramStart, out paramEnd))
				{  
				start = paramEnd;
				end = paramEnd;
				return false;  
				}

			// First search for an explicit type, like "int".
			start = paramStart;

			while (start < paramEnd && 
						start.PrototypeParsingType != PrototypeParsingType.Type &&
						start.PrototypeParsingType != PrototypeParsingType.TypeQualifier)
				{  start.Next();  }

			if (start < paramEnd)
				{
				end = start;

				do
					{  end.Next();  }
				while ((end.PrototypeParsingType == PrototypeParsingType.Type ||
							 end.PrototypeParsingType == PrototypeParsingType.TypeQualifier) &&
							end < paramEnd);

				return true;
				}
			else
				{  
				if (impliedTypes)
					{
					if (Style == ParameterStyle.C)
						{
						// Move backwards for "int x, y"
						for (int i = index - 1; i >= 0; i--)
							{
							if (GetBaseParameterType(i, out start, out end, false))
								{  return true;  }
							}
						}
					else // Style == ParameterStyle.Pascal
						{
						// Move forwards for "x, y: integer"
						for (int i = index + 1; i <= NumberOfParameters; i++)
							{
							if (GetBaseParameterType(i, out start, out end, false))
								{  return true;  }
							}
						}
					}

				start = paramEnd;
				end = paramEnd;
				return false;  
				}
			}


		/* Function: GetDefaultValue
		 * Returns the bounds of the default value of the passed parameter, or false if it doesn't exist.
		 */
		public bool GetDefaultValue (int index, out TokenIterator start, out TokenIterator end)
			{
			TokenIterator paramStart, paramEnd;

			if (!GetParameter(index, out paramStart, out paramEnd))
				{  
				start = paramEnd;
				end = paramEnd;
				return false;  
				}

			start = paramStart;

			while (start < paramEnd && start.PrototypeParsingType != PrototypeParsingType.DefaultValue)
				{  start.Next();  }

			end = start;

			while (end < paramEnd && end.PrototypeParsingType == PrototypeParsingType.DefaultValue)
				{  end.Next();  }

			return (start != end);
			}


		/* Function: GetAfterParameters
		 * Returns the bounds of the section of the prototype after the parameters and whether it exists.  If it does
		 * exist, the bounds will include the closing symbol of the parameter list such as the closing parenthesis.
		 */
		public bool GetAfterParameters (out TokenIterator start, out TokenIterator end)
			{
			return GetSectionBounds(SectionType.AfterParameters, 0, out start, out end);
			}
			

		/* Function: GetPostPrototypeLine
		 * Returns the bounds of a numbered post-prototype line.  Numbers start at zero.  It will return false if one does not
		 * exist at that number.
		 */
		public bool GetPostPrototypeLine (int lineNumber, out TokenIterator start, out TokenIterator end)
			{
			return GetSectionBounds(SectionType.PostPrototypeLine, lineNumber, out start, out end);
			}


		/* Function: CalculateSections
		 */
		protected void CalculateSections ()
			{
			sections = new List<Section>();
			Section section = null;

			TokenIterator iterator = tokenizer.FirstToken;
			iterator.NextPastWhitespace();


			// Pre-Prototype Lines

			while (iterator.IsInBounds && 
					 iterator.PrototypeParsingType == PrototypeParsingType.StartOfPrePrototypeLine)
				{
				section = new Section();
				section.Type = SectionType.PrePrototypeLine;
				section.StartIndex = iterator.TokenIndex;

				do
					{  iterator.Next();  }
				while (iterator.IsInBounds && 
						 iterator.PrototypeParsingType == PrototypeParsingType.PrePrototypeLine);

				section.EndIndex = iterator.TokenIndex;
				sections.Add(section);

				iterator.NextPastWhitespace();
				}


			// Before Parameters

			section = new Section();
			section.Type = SectionType.BeforeParameters;
			section.StartIndex = iterator.TokenIndex;

			while (iterator.IsInBounds && 
					 iterator.PrototypeParsingType != PrototypeParsingType.StartOfParams &&
					 iterator.PrototypeParsingType != PrototypeParsingType.StartOfPostPrototypeLine)
				{  iterator.Next();  }


			if (iterator.PrototypeParsingType == PrototypeParsingType.StartOfParams)
				{
				// Include the StartOfParams symbol in the section
				iterator.Next();

				section.EndIndex = iterator.TokenIndex;
				sections.Add(section);

				iterator.NextPastWhitespace();


				// Parameters

				while (iterator.IsInBounds && 
						 iterator.PrototypeParsingType != PrototypeParsingType.EndOfParams &&
						 iterator.PrototypeParsingType != PrototypeParsingType.StartOfPostPrototypeLine)
					{
					section = new Section();
					section.Type = SectionType.Parameter;
					section.StartIndex = iterator.TokenIndex;

					while (iterator.IsInBounds &&
							 iterator.PrototypeParsingType != PrototypeParsingType.EndOfParams &&
							 iterator.PrototypeParsingType != PrototypeParsingType.ParamSeparator &&
							 iterator.PrototypeParsingType != PrototypeParsingType.StartOfPostPrototypeLine)
						{  iterator.Next();  }

					// Include the separator in the parameter block
					if (iterator.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
						{  
						iterator.Next();  
						section.EndIndex = iterator.TokenIndex;
						}
					else
						{
						TokenIterator lookbehind = iterator;
						lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.Iterator, iterator);
						section.EndIndex = lookbehind.TokenIndex;
						}

					sections.Add(section);

					iterator.NextPastWhitespace();
					}


				// After Parameters

				if (iterator.IsInBounds &&
					iterator.PrototypeParsingType != PrototypeParsingType.StartOfPostPrototypeLine)
					{
					section = new Section();
					section.Type = SectionType.AfterParameters;
					section.StartIndex = iterator.TokenIndex;

					do
						{  iterator.Next();  }
					while (iterator.IsInBounds &&
							 iterator.PrototypeParsingType != PrototypeParsingType.StartOfPostPrototypeLine);

					TokenIterator lookbehind = iterator;
					lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.Iterator, iterator);
					section.EndIndex = lookbehind.TokenIndex;

					sections.Add(section);
					}
				}

			else // there was no StartOfParams
				{
				// We still have to finish the BeforeParameters section.
				TokenIterator lookbehind = iterator;
				lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.Iterator, iterator);
				section.EndIndex = lookbehind.TokenIndex;

				sections.Add(section);
				}


			// Post-Prototype Lines

			while (iterator.IsInBounds && 
					 iterator.PrototypeParsingType == PrototypeParsingType.StartOfPostPrototypeLine)
				{
				section = new Section();
				section.Type = SectionType.PostPrototypeLine;
				section.StartIndex = iterator.TokenIndex;

				do
					{  iterator.Next();  }
				while (iterator.IsInBounds && 
						 iterator.PrototypeParsingType == PrototypeParsingType.PostPrototypeLine);

				section.EndIndex = iterator.TokenIndex;
				sections.Add(section);

				iterator.NextPastWhitespace();
				}
			}


		/* Function: GetSectionBounds
		 * Returns the bounds of the passed section and whether it exists.  An index of zero represents the first section of that
		 * type, 1 represents the second, etc.
		 */
		protected bool GetSectionBounds (SectionType type, int index, out TokenIterator start, out TokenIterator end)
			{
			Section section = FindSection(type, index);

			if (section == null)
				{
				start = tokenizer.LastToken;
				end = start;
				return false;
				}
			else
				{
				start = tokenizer.FirstToken;
				start.Next(section.StartIndex);

				end = start;
				end.Next(section.EndIndex - section.StartIndex);

				return true;
				}
			}


		/* Function: FindSection
		 * Returns the first section with the passed type, or if you passed an index, the nth section with that type.  If there are
		 * none it will return null.
		 */
		protected Section FindSection (SectionType type, int index = 0)
			{
			if (sections == null)
				{  CalculateSections();  }

			foreach (Section section in sections)
				{
				if (section.Type == type)
					{
					if (index == 0)
						{  return section;  }
					else
						{  index--;  }
					}
				}

			return null;
			}
			

		/* Function: CountSections
		 * Returns the number of sections with the passed type.
		 */
		protected int CountSections (SectionType type)
			{
			if (sections == null)
				{  CalculateSections();  }

			int count = 0;

			foreach (Section section in sections)
				{
				if (section.Type == type)
					{  count++;  }
				}

			return count;
			}



		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: Tokenizer
		 * The tokenized prototype.
		 */
		public Tokenizer Tokenizer
			{
			get
				{  return tokenizer;  }
			}


		/* Property: NumberOfPrePrototypeLines
		 */
		public int NumberOfPrePrototypeLines
			{
			get
				{  
				return CountSections(SectionType.PrePrototypeLine);
				}
			}


		/* Property: NumberOfParameters
		 */
		public int NumberOfParameters
			{
			get
				{  
				return CountSections(SectionType.Parameter);
				}
			}


		/* Property: NumberOfPostPrototypeLines
		 */
		public int NumberOfPostPrototypeLines
			{
			get
				{  
				return CountSections(SectionType.PostPrototypeLine);
				}
			}


		/* Property: Style
		 * The format of the prototype, such as C-style parameters ("int x") or Pascal-style ("x: int").  If it has no parameters or
		 * no types this will return C.  Tokens must be marked with <PrototypeParsingType.Name>, <PrototypeParsingType.Type>,
		 * and <PrototypeParsingType.NameTypeSeparator> for this to work.
		 */
		public ParameterStyle Style
			{
			get
				{
				if (style != null)
					{  return (ParameterStyle)style;  }

				int numberOfParameters = NumberOfParameters;

				for (int i = 0; i < numberOfParameters; i++)
					{
					bool foundName = false;
					bool foundType = false;
					bool foundSeparator = false;

					TokenIterator start, end;
					GetParameter(i, out start, out end);

					while (start < end)
						{
						PrototypeParsingType type = start.PrototypeParsingType;

						if (type == PrototypeParsingType.Name)
							{
							if (foundType)
								{  
								style = ParameterStyle.C;
								return ParameterStyle.C;
								}
							else
								{  foundName = true;  }
							}
						else if (type == PrototypeParsingType.Type)
							{
							if (foundName && foundSeparator)
								{  
								style = ParameterStyle.Pascal;
								return ParameterStyle.Pascal;
								}
							else
								{  foundType = true;  }
							}
						else if (type == PrototypeParsingType.NameTypeSeparator)
							{
							foundSeparator = true;
							}

						start.Next();
						}
					}
					
				style = ParameterStyle.C;
				return ParameterStyle.C;
				}
			}


		
		// Group: Variables
		// __________________________________________________________________________
		
		/* var: tokenizer
		 * The <Tokenizer> containing the full prototype.
		 */
		protected Tokenizer tokenizer;

		/* var: sections
		 * A list of <Sections> representing chunks of the prototype, or null if it hasn't been calculated yet.
		 */
		protected List<Section> sections;

		/* var: style
		 * The prototype format, or null if it hasn't been determined yet.
		 */
		protected ParameterStyle? style;



		/* ___________________________________________________________________________
		 * 
		 * Class: CodeClear.NaturalDocs.Engine.ParsedPrototype.Section
		 * ___________________________________________________________________________
		 */
		protected class Section
			{
			public Section ()
				{
				StartIndex = 0;
				EndIndex = 0;
				Type = SectionType.BeforeParameters;
				}
			
			public int StartIndex;
			public int EndIndex;
			public SectionType Type;
			}
		}
	}