/* 
 * Class: GregValure.NaturalDocs.Engine.ParsedPrototype
 * ____________________________________________________________________________
 * 
 * A class that wraps a <Tokenizer> for a prototype that's been marked with <PrototypeParsingTypes>, providing easier 
 * access to things like parameter lines.
 * 
 * Usage:
 * 
 *		The functions and properties obviously rely on the relevant tokens being set.  You cannot expect a proper result from
 *		<GetParameter()> or <NumberOfParameters> unless the tokens are marked with <PrototypeParsingType.StartOfParams>
 *		and <PrototypeParsingType.ParamSeparator>.  Likewise, you can't get anything from <GetParameterName()> unless
 *		you also have tokens marked with <PrototypeParsingType.Name>.  However, you can set the parameter divider tokens,
 *		call <GetParameter()>, and then use those bounds to further parse the parameter and set tokens like
 *		<PrototypeParsingType.Name>.
 * 
 *		An important thing to remember though is that the parameter divisions are calculated once and saved.  Only call
 *		functions like <GetParameter()> after *ALL* the separator tokens (<PrototypeParsingType.StartOfParams>,
 *		<PrototypeParsingType.ParamSeparator>, and <PrototypeParsingType.EndOfParams>) are set and will not change
 *		going forward.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Tokenization;


namespace GregValure.NaturalDocs.Engine
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
		 * BeforeParameters - The prototype prior to the parameters.  This will include the start of parameters symbol, such as
		 *							   an opening parenthesis in C#.  If there are no parameters, this will be the entire prototype.
		 *	Parameter - An individual parameter.  It may include the parameter separator symbol, such as a comma in C#.
		 *	AfterParameters - The prototype after the parameters.  This will include the end of parameters symbol, such as a
		 *							 closing parenthesis in C#.
		 */
		public enum SectionType : byte
			{  BeforeParameters, Parameter, AfterParameters  }



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


		/* Function: GetCompletePrototype
		 * Returns the bounds of the complete prototype, minus whitespace.
		 */
		public void GetCompletePrototype (out TokenIterator start, out TokenIterator end)
			{
			start = tokenizer.FirstToken;
			start.NextPastWhitespace();

			end = tokenizer.LastToken;
			end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds);
			}

			
		/* Function: GetBeforeParameters
		 * Returns the bounds of the section of the prototype prior to the parameters.  If it has parameters, it will include the 
		 * starting symbol of the parameter list such as the opening parenthesis.  If there are no parameters, this will return the 
		 * bounds of the entire prototype.
		 */
		public void GetBeforeParameters (out TokenIterator start, out TokenIterator end)
			{
			if (sections == null)
				{  CalculateSections();  }

			Section section = FindSection(SectionType.BeforeParameters);

			start = tokenizer.FirstToken;
			start.Next(section.StartIndex);

			end = start;
			end.Next(section.EndIndex - section.StartIndex);
			}


		/* Function: GetParameter
		 * Returns the bounds of the numbered parameter and whether or not it exists.  Numbers start at zero.
		 */
		public bool GetParameter (int index, out TokenIterator start, out TokenIterator end)
			{
			if (sections == null)
				{  CalculateSections();  }

			Section section = FindSection(SectionType.Parameter, index);

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
		 * Returns the bounds of the type of the passed parameter, or false if it couldn't find it.  This includes modifiers and type
		 * suffixes.  If there's any additional type information that appears after the name (int x[12]) that will be returned in the
		 * extension.
		 */
		public bool GetFullParameterType (int index, out TokenIterator start, out TokenIterator end, 
																			out TokenIterator extensionStart, out TokenIterator extensionEnd)
			{
			TokenIterator paramStart, paramEnd;

			if (!GetParameter(index, out paramStart, out paramEnd))
				{  
				start = paramEnd;
				end = paramEnd;
				extensionStart = paramEnd;
				extensionEnd = paramEnd;
				return false;  
				}

			start = paramStart;
			PrototypeParsingType type = start.PrototypeParsingType;

			while (start < paramEnd && 
						type != PrototypeParsingType.Type &&
						type != PrototypeParsingType.TypeModifier &&
						type != PrototypeParsingType.TypeQualifier &&
						type != PrototypeParsingType.NamePrefix_PartOfType)
				{  
				start.Next();  
				type = start.PrototypeParsingType;
				}

			if (start < paramEnd)
				{
				end = start;
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
							  type == PrototypeParsingType.NamePrefix_PartOfType ||
							  end.FundamentalType == FundamentalType.Whitespace) &&
							end < paramEnd);

				// Find an extension if there is one.
				extensionStart = end;

				while (extensionStart < paramEnd &&
							 extensionStart.PrototypeParsingType != PrototypeParsingType.NameSuffix_PartOfType)
					{  extensionStart.Next();  }

				extensionEnd = extensionStart;

				while (extensionEnd < paramEnd &&
							 extensionEnd.PrototypeParsingType == PrototypeParsingType.NameSuffix_PartOfType)
					{  extensionEnd.Next();  }

				// Trim trailing whitespace from the regular type
				end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

				return true;
				}
			else
				{  
				start = paramEnd;
				end = paramEnd;
				extensionStart = paramEnd;
				extensionEnd = paramEnd;
				return false;  
				}
			}


		/* Function: GetBaseParameterType
		 * Returns the bounds of the type of the passed parameter, or false if it couldn't find it.  This excludes modifiers and type
		 * suffixes.
		 */
		public bool GetBaseParameterType (int index, out TokenIterator start, out TokenIterator end)
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
				// If there's no explicit type, seach for a name prefix like "$", "@", or "%".
				start = paramStart;

				while (start < paramEnd && 
							start.PrototypeParsingType != PrototypeParsingType.NamePrefix_PartOfType)
					{  start.Next();  }

				if (start < paramEnd)
					{
					end = start;

					do
						{  end.Next();  }
					while (end.PrototypeParsingType == PrototypeParsingType.NamePrefix_PartOfType &&
								end < paramEnd);

					return true;
					}
				else
					{  
					start = paramEnd;
					end = paramEnd;
					return false;  
					}
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
			if (sections == null)
				{  CalculateSections();  }

			Section section = FindSection(SectionType.AfterParameters);

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
			

		/* Function: CalculateSections
		 */
		protected void CalculateSections ()
			{
			sections = new List<Section>();

			TokenIterator iterator = tokenizer.FirstToken;
			iterator.NextPastWhitespace();


			// Before parameters

			Section section = new Section();
			section.Type = SectionType.BeforeParameters;
			section.StartIndex = iterator.TokenIndex;

			while (iterator.IsInBounds && iterator.PrototypeParsingType != PrototypeParsingType.StartOfParams)
				{  iterator.Next();  }

			if (iterator.IsInBounds == false)
				{
				iterator.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds);
				section.EndIndex = iterator.TokenIndex;
				sections.Add(section);
				return;
				}

			// Include the StartOfParams symbol in the section
			iterator.Next();

			section.EndIndex = iterator.TokenIndex;
			sections.Add(section);

			iterator.NextPastWhitespace();


			// Parameters

			while (iterator.IsInBounds && iterator.PrototypeParsingType != PrototypeParsingType.EndOfParams)
				{
				section = new Section();
				section.Type = SectionType.Parameter;
				section.StartIndex = iterator.TokenIndex;

				while (iterator.IsInBounds &&
						 iterator.PrototypeParsingType != PrototypeParsingType.EndOfParams &&
						 iterator.PrototypeParsingType != PrototypeParsingType.ParamSeparator)
					{  iterator.Next();  }

				// Include the separator in the parameter block
				if (iterator.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
					{  iterator.Next();  }

				section.EndIndex = iterator.TokenIndex;
				sections.Add(section);

				iterator.NextPastWhitespace();
				}


			// After parameters

			if (iterator.IsInBounds)
				{
				section = new Section();
				section.Type = SectionType.AfterParameters;
				section.StartIndex = iterator.TokenIndex;

				iterator = tokenizer.LastToken;
				iterator.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds);

				section.EndIndex = iterator.TokenIndex;
				sections.Add(section);
				}
			}


		/* Function: FindSection
		 * Returns the first section with the passed type, or if you passed an index, the nth section with that type.  If there are
		 * none it will return null.  You must have called <CalculateSections()> at least once before using this function.
		 */
		protected Section FindSection (SectionType type, int index = 0)
			{
			#if DEBUG
			if (sections == null)
				{  throw new Exception("Tried to call FindSection() before calling CalculateSections().");  }
			#endif

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


		/* Property: NumberOfParameters
		 */
		public int NumberOfParameters
			{
			get
				{  
				if (sections == null)
					{  CalculateSections();  }

				int count = 0;

				foreach (Section section in sections)
					{
					if (section.Type == SectionType.Parameter)
						{  count++;  }
					}

				return count;
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
		 * Class: GregValure.NaturalDocs.Engine.ParsedPrototype.Section
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