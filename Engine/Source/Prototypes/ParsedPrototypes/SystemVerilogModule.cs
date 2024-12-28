/*
 * Class: CodeClear.NaturalDocs.Engine.Prototypes.ParsedPrototypes.SystemVerilogModule
 * ____________________________________________________________________________
 *
 * A specialized <ParsedPrototype> for SystemVerilog modules.  It differs from <ParsedPrototype> in the following ways:
 *
 *
 * Multiple Parameter Sections:
 *
 *		Multiple parameter sections will be used for things like <GetParameterName()> and <NumberOfParameters>.  This
 *		is because modules can have both #() and () parameters and we want to include them both.
 *
 *		This class treats them as if they are one continuous set of parameters, so in:
 *
 *		--- SV Code ---
 *		module MyModule #(int A, int B) (int C);
 *		---
 *
 *		it behaves as if there are three parameters, with B at parameter index 1 and C at parameter index 2.  The benefit
 *		of this is it allows types to be automatically retrieved from both sets of parameters when documenting them in
 *		definition lists.
 *
 *
 *	Attribute Combining:
 *
 *		<BuildFullParameterType()> will handle how unspecified attributes combine, both for ANSI and non-ANSI parameter
 *		sections.  For example:
 *
 *		--- SV Code ---
 *		module MyModule (input bit A[2], B, unsigned C);
 *		---
 *
 *		it knows the "[2]" is not inherited by B, and "bit" doesn't get added to "unsigned" in C.  However, "input" _does_ get
 *		added to both B and C.  The rules for how attributes combine are described in <SystemVerilog Parser Notes>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Reflection.Emit;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Prototypes.ParsedPrototypes
	{
	public class SystemVerilogModule : ParsedPrototype
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: PortFlags
		 *
		 * Flags to show which parts of a port definition exist.
		 *
		 *	Primary Flags:
		 *
		 *		These can all be set individually.
		 *
		 *		HasDirection - Whether the port has a direction defined, such as "input".
		 *		HasParameterkeyword - Whether the port has a parameter keyword defined, such as "localparam".
		 *		HasBaseDataType - Whether the port has a base data type defined, such as "logic".  This doesn't include
		 *									 the signing or packed dimension parts, which are denoted separately, hence _base_
		 *									 data type.
		 *		HasOtherModifiers - Whether the port has modifiers defined that aren't signing or packed dimensions.  This
		 *									 includes modports on interfaces, struct/union modifiers, and struct/union definitions.
		 *		HasSigning - Whether the port has signing defined, such as "unsigned".
		 *		HasPackedDimensions - Whether the port has one or more packed dimensions defined, such as "[7:0]".
		 *		HasName - Whether the port's name was defined.
		 *		HasUnpackedDimensions - Whether the port has one or more unpacked dimensions defined, such as "[2]".
		 *
		 * Combination Flags:
		 *
		 *		These are combinations of the above flags that are only used for testing the value against multiple flags
		 *		at once.
		 *
		 *		HasDataTypeOrProperties - A combination of <HasBaseDataType>, <HasSigning>, and <HasPackedDimensions>.
		 *											   Does *not* include <HasUnpackedDimensions>.
		 *
		 */
		[Flags]
		protected enum PortFlags : byte
			{
			HasDirection = 0x01,
			HasParameterKeyword = 0x02,
			HasBaseDataType = 0x04,
			HasOtherModifiers = 0x08,
			HasSigning = 0x10,
			HasPackedDimensions = 0x20,
			HasName = 0x40,
			HasUnpackedDimensions = 0x80,

			HasDataTypeOrProperties = HasBaseDataType | HasSigning | HasPackedDimensions
			}


		/* Enum: ParameterSectionType
		 *
		 * ANSIParameterPorts - ANSI ports appearing in #() parentheses.
		 * ANSIPorts - ANSI ports appearing in regular parentheses.
		 * NonANSIPorts - Ports and parameter ports appearing in a non-ANSI section.
		 */
		protected enum ParameterSectionType
			{  ANSIParameterPorts, ANSIPorts, NonANSIPorts  }



		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: SystemVerilogModule
		 * Static constructor
		 */
		static SystemVerilogModule ()
			{
			// I debated between "x" and "_".  "x[2]" works, as does "logic [7:0] x[2]".  Underscore draws your eye to it a little
			// less and makes it clearer something's missing: "_[2]" and "logic [7:0] _[2]".  It's debatable though.
			NameStandInToken = new Tokenizer("_");

			TokenIterator iterator = NameStandInToken.FirstToken;
			iterator.PrototypeParsingType = PrototypeParsingType.ParamModifier;
			}


		/* Constructor: SystemVerilogModule
		 */
		public SystemVerilogModule (Tokenizer prototype, int languageID, int commentTypeID)
			: base (prototype, languageID, commentTypeID, parameterStyle: ParameterStyle.SystemVerilog, supportsImpliedTypes: true)
			{
			}


		/* Function: ConvertParameterIndex
		 * Takes an index that applies across all parameter sections and finds the <ParameterSection> containing it.  Also
		 * returns the index within that section that corresponds to it.  Returns whether it was successful.
		 */
		protected bool ConvertParameterIndex (int parameterIndex, out ParameterSection containingSection,
																 out int containingSectionParameterIndex)
			{
			for (int i = mainSectionIndex; i < sections.Count; i++)
				{
				if (sections[i] is ParameterSection)
					{
					ParameterSection parameterSection = (sections[i] as ParameterSection);

					if (parameterIndex < parameterSection.NumberOfParameters)
						{
						containingSection = parameterSection;
						containingSectionParameterIndex = parameterIndex;
						return true;
						}
					else
						{
						parameterIndex -= parameterSection.NumberOfParameters;
						// Continue looking
						}
					}
				}

			containingSection = null;
			containingSectionParameterIndex = -1;
			return false;
			}


		/* Function: GetParameterSectionType
		 * Returns the <ParameterSectionType> of the passed section.
		 */
		protected ParameterSectionType GetParameterSectionType (ParameterSection parameterSection)
			{
			TokenIterator start, end;
			if (parameterSection.GetBeforeParameters(out start, out end))
				{
				end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds);
				end.Previous();

				if (end.Character == '(')
					{
					end.Previous();

					if (end.Character == '#')
						{  return ParameterSectionType.ANSIParameterPorts;  }
					else
						{  return ParameterSectionType.ANSIPorts;  }
					}
				}

			// Everything else we treat as non-ANSI
			return ParameterSectionType.NonANSIPorts;
			}


		/* Function: BuildFullANSIParameterPortType
		 */
		protected TypeBuilder BuildFullANSIParameterPortType (ParameterSection parameterSection, int parameterIndex,
																					   bool impliedTypes = true)
			{
			TypeBuilder typeBuilder = new TypeBuilder();
			PortFlags portFlags = 0;


			// Parameter Keyword

			if (AppendParameterKeyword(parameterSection, parameterIndex, typeBuilder))
				{  portFlags |= PortFlags.HasParameterKeyword;  }
			// Parameter keywords are treated as if they don't inherit.  See SystemVerilog Notes.


			// Data Type

			if (AppendBaseDataType(parameterSection, parameterIndex, typeBuilder))
				{
				portFlags |= PortFlags.HasBaseDataType;

				if (AppendOtherModifiers(parameterSection, parameterIndex, typeBuilder))
					{  portFlags |= PortFlags.HasOtherModifiers;  }
				}
			if (AppendSigning(parameterSection, parameterIndex, typeBuilder))
				{  portFlags |= PortFlags.HasSigning;  }
			if (AppendPackedDimensions(parameterSection, parameterIndex, typeBuilder))
				{  portFlags |= PortFlags.HasPackedDimensions;  }

			// The data type only inherits if nothing is specified.  If the base type or any properties are set the rest does
			// not inherit, they revert to the default.  This includes if signing or packed data types appear alone.
			if (impliedTypes && (portFlags & PortFlags.HasDataTypeOrProperties) == 0)
				{
				for (int i = parameterIndex - 1; i >= 0; i--)
					{
					if (AppendBaseDataType(parameterSection, i, typeBuilder))
						{
						portFlags |= PortFlags.HasBaseDataType;

						if (AppendOtherModifiers(parameterSection, i, typeBuilder))
							{  portFlags |= PortFlags.HasOtherModifiers;  }
						}
					if (AppendSigning(parameterSection, i, typeBuilder))
						{  portFlags |= PortFlags.HasSigning;  }
					if (AppendPackedDimensions(parameterSection, i, typeBuilder))
						{  portFlags |= PortFlags.HasPackedDimensions;  }

					if (impliedTypes && (portFlags & PortFlags.HasDataTypeOrProperties) != 0)
						{  break;  }
					}

				// xxx inherit from non-ANSI too?
				}


			// Unpacked Dimensions

			if (AppendUnpackedDimensions(parameterSection, parameterIndex, typeBuilder, addStandInForName: true))
				{  portFlags |= PortFlags.HasUnpackedDimensions;  }
			// Unpacked dimensions don't inherit from previous parameters


			return typeBuilder;
			}


		/* Function: BuildFullANSIPortType
		 */
		protected TypeBuilder BuildFullANSIPortType (ParameterSection parameterSection, int parameterIndex,
																		 bool impliedTypes = true)
			{
			TypeBuilder typeBuilder = new TypeBuilder();
			PortFlags portFlags = 0;


			// Direction

			if (AppendDirection(parameterSection, parameterIndex, typeBuilder))
				{  portFlags |= PortFlags.HasDirection;  }
			else
				{
				// The direction is always inherited if it's not specified
				for (int i = parameterIndex - 1; i >= 0; i--)
					{
					if (AppendDirection(parameterSection, i, typeBuilder))
						{
						portFlags |= PortFlags.HasDirection;
						break;
						}
					}

				// xxx inherit from non-ANSI too?
				}


			// Data Type

			if (AppendBaseDataType(parameterSection, parameterIndex, typeBuilder))
				{
				portFlags |= PortFlags.HasBaseDataType;

				if (AppendOtherModifiers(parameterSection, parameterIndex, typeBuilder))
					{  portFlags |= PortFlags.HasOtherModifiers;  }
				}
			if (AppendSigning(parameterSection, parameterIndex, typeBuilder))
				{  portFlags |= PortFlags.HasSigning;  }
			if (AppendPackedDimensions(parameterSection, parameterIndex, typeBuilder))
				{  portFlags |= PortFlags.HasPackedDimensions;  }

			// The data type only inherits if nothing is specified.  If the base type or any properties are set the rest does
			// not inherit, they revert to the default.  This includes if signing or packed data types appear alone.
			if (impliedTypes && (portFlags & PortFlags.HasDataTypeOrProperties) == 0)
				{
				for (int i = parameterIndex - 1; i >= 0; i--)
					{
					if (AppendBaseDataType(parameterSection, i, typeBuilder))
						{
						portFlags |= PortFlags.HasBaseDataType;

						if (AppendOtherModifiers(parameterSection, i, typeBuilder))
							{  portFlags |= PortFlags.HasOtherModifiers;  }
						}
					if (AppendSigning(parameterSection, i, typeBuilder))
						{  portFlags |= PortFlags.HasSigning;  }
					if (AppendPackedDimensions(parameterSection, i, typeBuilder))
						{  portFlags |= PortFlags.HasPackedDimensions;  }

					if (impliedTypes && (portFlags & PortFlags.HasDataTypeOrProperties) != 0)
						{  break;  }
					}

				// xxx inherit from non-ANSI too?
				}


			// Unpacked Dimensions

			if (AppendUnpackedDimensions(parameterSection, parameterIndex, typeBuilder, addStandInForName: true))
				{  portFlags |= PortFlags.HasUnpackedDimensions;  }
			// Unpacked dimensions don't inherit from previous parameters


			return typeBuilder;
			}


		/* Function: GetBaseANSIParameterPortType
		 */
		protected bool GetBaseANSIParameterPortType (ParameterSection parameterSection, int parameterIndex,
																			  out TokenIterator baseTypeStart, out TokenIterator baseTypeEnd,
																			  bool impliedTypes = true)
			{
			// Find the start of the base type, and whether we have one

			bool foundBaseType = false;
			TokenIterator endOfParameter;

			if (FindBaseDataType(parameterSection, parameterIndex, out baseTypeStart, out endOfParameter))
				{
				foundBaseType = true;
				}

			else if (impliedTypes)
				{
				// If kind, signing, or packed dimensions are defined the parameter does not inherit the base data type from a previous
				// parameter.  It reverts to a default.
				if (HasDirection(parameterSection, parameterIndex) ||
					HasSigning(parameterSection, parameterIndex) ||
					HasPackedDimensions(parameterSection, parameterIndex))
					{
					foundBaseType = false;
					}
				else
					{
					for (int i = parameterIndex - 1; i >= 0; i--)
						{
						if (FindBaseDataType(parameterSection, i, out baseTypeStart, out endOfParameter))
							{
							foundBaseType = true;
							break;
							}

						if (HasDirection(parameterSection, i) ||
							HasSigning(parameterSection, i) ||
							HasPackedDimensions(parameterSection, i))
							{
							foundBaseType = false;
							break;
							}
						}
					}
				}

			if (!foundBaseType)
				{
				baseTypeStart = tokenizer.EndOfTokens;
				baseTypeEnd = tokenizer.EndOfTokens;
				return false;
				}


			// Since we have a base type, find its end.  It could contain multiple words ("tri0 logic") so we want look for additional type
			// tokens after whitespace.

			baseTypeEnd = baseTypeStart;
			baseTypeEnd.Next();

			TokenIterator lookahead = baseTypeEnd;

			for (;;)
				{
				if (lookahead.PrototypeParsingType == PrototypeParsingType.Type ||
					lookahead.PrototypeParsingType == PrototypeParsingType.TypeQualifier)
					{
					lookahead.Next();
					baseTypeEnd = lookahead;
					}
				else if (lookahead.FundamentalType == FundamentalType.Whitespace)
					{
					lookahead.Next();
					}
				else
					{  break;  }
				}

			return true;
			}


		/* Function: GetBaseANSIPortType
		 */
		protected bool GetBaseANSIPortType (ParameterSection parameterSection, int parameterIndex,
															   out TokenIterator baseTypeStart, out TokenIterator baseTypeEnd,
															   bool impliedTypes = true)
			{
			// Find the start of the base type, and whether we have one

			bool foundBaseType = false;
			TokenIterator endOfParameter;

			if (FindBaseDataType(parameterSection, parameterIndex, out baseTypeStart, out endOfParameter))
				{
				foundBaseType = true;
				}

			else if (impliedTypes)
				{
				// If kind, signing, or packed dimensions are defined the parameter does not inherit the base data type from a previous
				// parameter.  It reverts to a default.
				if (HasDirection(parameterSection, parameterIndex) ||
					HasSigning(parameterSection, parameterIndex) ||
					HasPackedDimensions(parameterSection, parameterIndex))
					{
					foundBaseType = false;
					}
				else
					{
					for (int i = parameterIndex - 1; i >= 0; i--)
						{
						if (FindBaseDataType(parameterSection, i, out baseTypeStart, out endOfParameter))
							{
							foundBaseType = true;
							break;
							}

						if (HasDirection(parameterSection, i) ||
							HasSigning(parameterSection, i) ||
							HasPackedDimensions(parameterSection, i))
							{
							foundBaseType = false;
							break;
							}
						}
					}
				}

			if (!foundBaseType)
				{
				baseTypeStart = tokenizer.EndOfTokens;
				baseTypeEnd = tokenizer.EndOfTokens;
				return false;
				}


			// Since we have a base type, find its end.  It could contain multiple words ("tri0 logic") so we want look for additional type
			// tokens after whitespace.

			baseTypeEnd = baseTypeStart;
			baseTypeEnd.Next();

			TokenIterator lookahead = baseTypeEnd;

			for (;;)
				{
				if (lookahead.PrototypeParsingType == PrototypeParsingType.Type ||
					lookahead.PrototypeParsingType == PrototypeParsingType.TypeQualifier)
					{
					lookahead.Next();
					baseTypeEnd = lookahead;
					}
				else if (lookahead.FundamentalType == FundamentalType.Whitespace)
					{
					lookahead.Next();
					}
				else
					{  break;  }
				}

			return true;
			}



		// Group: Port Component Functions
		// __________________________________________________________________________


		/* Function: HasDirection
		 * Returns whether the passed parameter contains a direction keyword (input, output, etc.)  Direction keywords must be
		 * marked with <PrototypeParsingType.TypeModifier>.
		 */
		protected bool HasDirection (ParameterSection parameterSection, int parameterIndex)
			{
			TokenIterator ignore1, ignore2;
			return FindDirection(parameterSection, parameterIndex, out ignore1, out ignore2);
			}


		/* Function: HasParameterKeyword
		 * Returns whether the passed parameter contains a parameter keyword (parameter, localparam).  Parameter keywords
		 * must be marked with <PrototypeParsingType.TypeModifier>.
		 */
		protected bool HasParameterKeyword (ParameterSection parameterSection, int parameterIndex)
			{
			TokenIterator ignore1, ignore2;
			return FindParameterKeyword(parameterSection, parameterIndex, out ignore1, out ignore2);
			}


		/* Function: HasBaseDataType
		 * Returns whether the passed parameter contains a base data type.  Base data type tokens must be marked
		 * with <PrototypeParsingType.Type>, <PrototypeParsingType.TypeQualifier>, or for type references like "type(x)",
		 * <PrototypeParsingType.OpeningTypeModifier> and <PrototypeParsingType.ClosingTypeModifier>.
		 */
		protected bool HasBaseDataType (ParameterSection parameterSection, int parameterIndex)
			{
			TokenIterator ignore1, ignore2;
			return FindBaseDataType(parameterSection, parameterIndex, out ignore1, out ignore2);
			}


		/* Function: HasOtherModifiers
		 * Returns whether the passed parameter contains modifiers that aren't signing or packed dimensions.
		 */
		protected bool HasOtherModifiers (ParameterSection parameterSection, int parameterIndex)
			{
			TokenIterator ignore1, ignore2;
			return FindOtherModifiers(parameterSection, parameterIndex, out ignore1, out ignore2);
			}


		/* Function: HasSigning
		 *
		 * Returns whether the passed parameter contains a signing keyword (signed, unsigned)  Signing keywords
		 * must be marked with <PrototypeParsingType.TypeModifier>.
		 *
		 * Note that this will return false for signed structs and unions.  This is because those appear in the middle of
		 * "other modifiers" so are treated as part of them.
		 */
		protected bool HasSigning (ParameterSection parameterSection, int parameterIndex)
			{
			TokenIterator ignore1, ignore2;
			return FindSigning(parameterSection, parameterIndex, out ignore1, out ignore2);
			}


		/* Function: HasPackedDimensions
		 * Returns whether the passed parameter contains one or more packed dimensions.  Packed dimensions must
		 * be marked with <PrototypeParsingType.OpeningTypeModifier> and <PrototypeParsingType.ClosingTypeModifier>.
		 * They also cannot appear after a <PrototypeParsingType.Name> token.
		 */
		protected bool HasPackedDimensions (ParameterSection parameterSection, int parameterIndex)
			{
			TokenIterator ignore1, ignore2;
			return FindPackedDimensions(parameterSection, parameterIndex, out ignore1, out ignore2);
			}


		/* Function: HasUnpackedDimensions
		 * Returns whether the passed parameter contains one or more unpacked dimensions.  Unpacked dimensions
		 * must be marked with <PrototypeParsingType.OpeningParamModifier> and
		 * <PrototypeParsingType.ClosingParamModifier>.  They also must appear after a <PrototypeParsingType.Name>
		 * token.
		 */
		protected bool HasUnpackedDimensions (ParameterSection parameterSection, int parameterIndex)
			{
			TokenIterator ignore1, ignore2;
			return FindUnpackedDimensions(parameterSection, parameterIndex, out ignore1, out ignore2);
			}


		/* Function: FindDirection
		 * If the passed parameter contains a direction keyword (input, output, etc.) it will return a <TokenIterator> at
		 * its position and return true.  Returns false otherwise.  Direction keywords must be marked with
		 * <PrototypeParsingType.TypeModifier>.
		 */
		protected bool FindDirection (ParameterSection parameterSection, int parameterIndex,
												  out TokenIterator directionPosition, out TokenIterator endOfParameter)
			{
			TokenIterator iterator;
			parameterSection.GetParameterBounds(parameterIndex, out iterator, out endOfParameter);

			while (iterator < endOfParameter)
				{
				if (iterator.PrototypeParsingType == PrototypeParsingType.TypeModifier &&
					Languages.Parsers.SystemVerilog.IsOnDirectionKeyword(iterator))
					{
					directionPosition = iterator;
					return true;
					}

				if (!TryToSkipBlock(ref iterator, endOfParameter))
					{  iterator.Next();  }
				}

			directionPosition = endOfParameter;
			return false;
			}


		/* Function: FindParameterKeyword
		 * If the passed parameter contains a parameter keyword (parameter, localparam) it will return a <TokenIterator> at
		 * its position and return true.  Returns false otherwise.  Parameter keywords must be marked with
		 * <PrototypeParsingType.TypeModifier>.
		 */
		protected bool FindParameterKeyword (ParameterSection parameterSection, int parameterIndex,
																out TokenIterator keywordPosition, out TokenIterator endOfParameter)
			{
			TokenIterator iterator;
			parameterSection.GetParameterBounds(parameterIndex, out iterator, out endOfParameter);

			while (iterator < endOfParameter)
				{
				if (iterator.PrototypeParsingType == PrototypeParsingType.TypeModifier &&
					Languages.Parsers.SystemVerilog.IsOnParameterKeyword(iterator))
					{
					keywordPosition = iterator;
					return true;
					}

				if (!TryToSkipBlock(ref iterator, endOfParameter))
					{  iterator.Next();  }
				}

			keywordPosition = endOfParameter;
			return false;
			}


		/* Function: FindBaseDataType
		 * If the passed parameter contains a base data type it will return a <TokenIterator> at its position and return
		 * true.  Returns false otherwise.  Base data type tokens must be marked with <PrototypeParsingType.Type> or
		 * <PrototypeParsingType.TypeQualifier>.
		 */
		protected bool FindBaseDataType (ParameterSection parameterSection, int parameterIndex,
														  out TokenIterator baseDataTypePosition, out TokenIterator endOfParameter)
			{
			TokenIterator iterator;
			parameterSection.GetParameterBounds(parameterIndex, out iterator, out endOfParameter);

			while (iterator < endOfParameter)
				{
				if (iterator.PrototypeParsingType == PrototypeParsingType.Type ||
					iterator.PrototypeParsingType == PrototypeParsingType.TypeQualifier)
					{
					baseDataTypePosition = iterator;
					return true;
					}

				if (!TryToSkipBlock(ref iterator, endOfParameter))
					{  iterator.Next();  }
				}

			baseDataTypePosition = endOfParameter;
			return false;
			}


		/* Function: FindOtherModifiers
		 * If the passed parameter contains modifiers that aren't signing or packed dimensions it will return a
		 * <TokenIterator> at its position and return true.
		 */
		protected bool FindOtherModifiers (ParameterSection parameterSection, int parameterIndex,
														  out TokenIterator modifierPosition, out TokenIterator endOfParameter)
			{
			TokenIterator iterator;
			bool isStructUnion = false;
			bool isEnum = false;

			if (FindBaseDataType(parameterSection, parameterIndex, out iterator, out endOfParameter))
				{
				// If it has a base data type, check if it's a struct, union, or enum since they'll need special handling.
				isStructUnion = Languages.Parsers.SystemVerilog.IsOnAnyKeyword(iterator, "struct", "union");
				isEnum = Languages.Parsers.SystemVerilog.IsOnKeyword(iterator, "enum");

				// Move past the rest of the type name
				do
					{  iterator.Next();  }
				while (iterator < endOfParameter &&
						 (iterator.PrototypeParsingType == PrototypeParsingType.Type ||
						  iterator.PrototypeParsingType == PrototypeParsingType.TypeQualifier));

				// Move past any unmarked whitespace after the type
				while (iterator < endOfParameter &&
						 iterator.PrototypeParsingType == PrototypeParsingType.Null &&
						 (iterator.FundamentalType == FundamentalType.Whitespace ||
						  iterator.FundamentalType == FundamentalType.LineBreak))
					{  iterator.Next();  }
				}
			else
				{
				// If it doesn't have a base data type we don't have to worry about special handling.  Position the iterator
				// at the beginning of the parameter.
				parameterSection.GetParameterBounds(parameterIndex, out iterator, out endOfParameter);
				}

			if (iterator.PrototypeParsingType == PrototypeParsingType.TypeModifier)
				{
				if (Languages.Parsers.SystemVerilog.IsOnSigningKeyword(iterator))
					{
					// Structs, unions, and enums can have signing but we want to treat them as "other modifiers" instead,
					// so return true.
					if (isStructUnion || isEnum)
						{
						modifierPosition = iterator;
						return true;
						}
					// For everything else signing doesn't get put under "other modifiers", so return false.
					else
						{
						modifierPosition = endOfParameter;
						return false;
						}
					}
				// If it's a modifier but not a signing keyword it's definitely part of "other modifiers".
				else
					{
					modifierPosition = iterator;
					return true;
					}
				}

			else if (iterator.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier)
				{
				if (iterator.Character == '[')
					{
					// Enums can have packed dimensions on the base type or after the body.  We want to treat ones on the
					// base type as "other modifiers".
					// - For "enum bit [7:0] { ... }" we would find "bit" before the dimensions and not hit this code block.
					// - For "enum [7:0] { ... }" we would find the dimensions immediately after the base type.
					// - For "enum { ... } [7:0]" we would find the body before the dimensions and not hit this code block.
					// Thus we can say if we're here it's always the second option and so we should return true, that it's part
					// of "other modifiers".
					if (isEnum)
						{
						modifierPosition = iterator;
						return true;
						}
					// If it's not on an enum then it's a packed dimension which we don't want to inlude in "other modifiers".
					else
						{
						modifierPosition = endOfParameter;
						return false;
						}
					}
				// If it's an opening modifier keyword but not a dimension then it's definitely part of "other modifiers".
				else
					{
					modifierPosition = iterator;
					return true;
					}
				}

			// If it's not a modifier at all we can return false.
			else
				{
				modifierPosition = endOfParameter;
				return false;
				}
			}


		/* Function: FindSigning
		 *
		 * If the passed parameter contains a signing keyword (signed, unsigned) it will return a <TokenIterator> at its
		 * position and return true.  Returns false otherwise.  Signing keywords must be marked with
		 * <PrototypeParsingType.TypeModifier>.
		 *
		 * Note that this will return false for signed structs and unions.  This is because those appear in the middle of
		 * "other modifiers" so are treated as part of them.
		 */
		protected bool FindSigning (ParameterSection parameterSection, int parameterIndex,
												out TokenIterator signingPosition, out TokenIterator endOfParameter)
			{
			TokenIterator iterator;

			if (FindBaseDataType(parameterSection, parameterIndex, out iterator, out endOfParameter))
				{
				// If it has a base data type, check if it's a struct, union, or enum.  They can have signing keywords
				// but we treat them as "other modifiers" so this function should return false.
				if (Languages.Parsers.SystemVerilog.IsOnAnyKeyword(iterator, "struct", "union", "enum"))
					{
					signingPosition = endOfParameter;
					return false;
					}
				}
			else
				{
				// If it doesn't have a base data type, position the iterator at the beginning of the parameter.
				parameterSection.GetParameterBounds(parameterIndex, out iterator, out endOfParameter);
				}

			while (iterator < endOfParameter)
				{
				if (iterator.PrototypeParsingType == PrototypeParsingType.TypeModifier &&
					Languages.Parsers.SystemVerilog.IsOnSigningKeyword(iterator))
					{
					signingPosition = iterator;
					return true;
					}

				if (!TryToSkipBlock(ref iterator, endOfParameter))
					{  iterator.Next();  }
				}

			signingPosition = endOfParameter;
			return false;
			}


		/* Function: FindPackedDimensions
		 * If the passed parameter contains one or more packed dimensions it will return a <TokenIterator> at its position
		 * and return true.  Returns false otherwise.  Packed dimensions must be marked with
		 * <PrototypeParsingType.OpeningTypeModifier> and <PrototypeParsingType.ClosingTypeModifier>.  They also
		 * cannot appear after a <PrototypeParsingType.Name> token.
		 */
		protected bool FindPackedDimensions (ParameterSection parameterSection, int parameterIndex,
																out TokenIterator packedDimensionsPosition, out TokenIterator endOfParameter)
			{
			TokenIterator iterator;
			bool isEnum = false;

			if (FindBaseDataType(parameterSection, parameterIndex, out iterator, out endOfParameter))
				{
				// If it has a base data type, check if it's "enum" since that will need special handling.
				isEnum = Languages.Parsers.SystemVerilog.IsOnKeyword(iterator, "enum");
				}
			else
				{
				// If it doesn't have a base data type we don't have to worry about special handling.  Position the iterator
				// at the beginning of the parameter.
				parameterSection.GetParameterBounds(parameterIndex, out iterator, out endOfParameter);
				}

			bool afterBody = false;

			while (iterator < endOfParameter)
				{
				if (iterator.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier)
					{
					if (iterator.Character == '[')
						{
						// If we're at dimensions, make sure either it isn't an enum, in which case we want to return true, or
						// if it is an enum, it appears after the body.  We don't want to return dimensions before the body like
						// "enum bit [7:0] { ... }" since those go in "other identifiers".  We do want to return ones after it like
						// "enum bit { ... } [7:0]".
						if (isEnum == false ||
							afterBody == true)
							{
							packedDimensionsPosition = iterator;
							return true;
							}
						}
					else if (iterator.Character == '{')
						{
						afterBody = true;
						}
					}

				else if (iterator.PrototypeParsingType == PrototypeParsingType.Name)
					{  break;  }

				if (!TryToSkipBlock(ref iterator, endOfParameter))
					{  iterator.Next();  }
				}

			packedDimensionsPosition = endOfParameter;
			return false;
			}


		/* Function: FindUnpackedDimensions
		 * If the passed parameter contains one or more unpacked dimensions it will return a <TokenIterator> at its
		 * position and return true.  Returns false otherwise.  Unpacked dimensions must be marked with
		 * <PrototypeParsingType.OpeningParamModifier> and <PrototypeParsingType.ClosingParamModifier>.  They also
		 * must appear after a <PrototypeParsingType.Name> token.
		 */
		protected bool FindUnpackedDimensions (ParameterSection parameterSection, int parameterIndex,
																   out TokenIterator unpackedDimensionsPosition, out TokenIterator endOfParameter)
			{
			TokenIterator iterator;
			parameterSection.GetParameterBounds(parameterIndex, out iterator, out endOfParameter);


			// Skip all the tokens before the name

			while (iterator < endOfParameter &&
					 iterator.PrototypeParsingType != PrototypeParsingType.Name)
				{
				if (!TryToSkipBlock(ref iterator, endOfParameter))
					{  iterator.Next();  }
				}


			// If there's no name, there's no unpacked dimensions

			if (iterator.PrototypeParsingType != PrototypeParsingType.Name)
				{
				unpackedDimensionsPosition = endOfParameter;
				return false;
				}


			// Skip the name

			do
				{  iterator.Next();  }
			while (iterator.PrototypeParsingType == PrototypeParsingType.Name);


			// Now look for param modifiers after the name

			while (iterator < endOfParameter)
				{
				if (iterator.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier &&
					iterator.Character == '[')
					{
					unpackedDimensionsPosition = iterator;
					return true;
					}

				if (!TryToSkipBlock(ref iterator, endOfParameter))
					{  iterator.Next();  }
				}

			unpackedDimensionsPosition = endOfParameter;
			return false;
			}


		/* Function: AppendDirection
		 * If the passed parameter contains a direction keyword (input, output, etc.) it will append it to the <TypeBuilder>
		 * and return true.  Returns false otherwise.  Direction keywords must be marked with
		 * <PrototypeParsingType.TypeModifier>.
		 */
		protected bool AppendDirection (ParameterSection parameterSection, int parameterIndex, TypeBuilder typeBuilder)
			{
			TokenIterator directionKeyword, endOfParameter;

			if (FindDirection(parameterSection, parameterIndex, out directionKeyword, out endOfParameter))
				{
				typeBuilder.AddToken(directionKeyword);
				return true;
				}
			else
				{  return false;  }
			}


		/* Function: AppendParameterKeyword
		 * If the passed parameter contains a parameter keyword (parameter, localparam) it will append it to the <TypeBuilder>
		 * and return true.  Returns false otherwise.  Parameter keywords must be marked with
		 * <PrototypeParsingType.TypeModifier>.
		 */
		protected bool AppendParameterKeyword (ParameterSection parameterSection, int parameterIndex, TypeBuilder typeBuilder)
			{
			TokenIterator parameterKeyword, endOfParameter;

			if (FindParameterKeyword(parameterSection, parameterIndex, out parameterKeyword, out endOfParameter))
				{
				typeBuilder.AddToken(parameterKeyword);
				return true;
				}
			else
				{  return false;  }
			}


		/* Function: AppendBaseDataType
		 * If the passed parameter contains a base data type it will add it to the <TypeBuilder> and return true.  Returns false
		 * otherwise.  Base data type tokens must be marked with <PrototypeParsingType.Type> or
		 * <PrototypeParsingType.TypeQualifier>.
		 */
		protected bool AppendBaseDataType (ParameterSection parameterSection, int parameterIndex, TypeBuilder typeBuilder)
			{
			TokenIterator iterator, endOfParameter;

			if (FindBaseDataType(parameterSection, parameterIndex, out iterator, out endOfParameter))
				{
				bool isTypeReference = iterator.MatchesToken("type");

				typeBuilder.AddToken(iterator);
				iterator.Next();

				// Add any consecutive type or type qualifier tokens.
				while (iterator < endOfParameter)
					{
					if (iterator.PrototypeParsingType == PrototypeParsingType.Type ||
						iterator.PrototypeParsingType == PrototypeParsingType.TypeQualifier)
						{
						typeBuilder.AddToken(iterator);
						iterator.Next();
						}

					// If there's null whitespace tokens, move past them to see if there's any more tokens we care about on the
					// other side.  The type builder will handle spacing so we don't have to worry about adding them.
					else if (iterator.PrototypeParsingType == PrototypeParsingType.Null &&
							   (iterator.FundamentalType == FundamentalType.Whitespace ||
								iterator.FundamentalType == FundamentalType.LineBreak) )
						{
						iterator.Next();
						}
					else
						{  break;  }
					}

				return true;
				}
			else
				{  return false;  }
			}


		/* Function: AppendOtherModifiers
		 * If the passed parameter contains modifiers that aren't signing or packed dimensions it will add them to the
		 * <TypeBuilder> and return true.  Returns false otherwise.
		 */
		protected bool AppendOtherModifiers (ParameterSection parameterSection, int parameterIndex, TypeBuilder typeBuilder)
			{
			TokenIterator iterator, endOfParameter;

			if (!FindOtherModifiers(parameterSection, parameterIndex, out iterator, out endOfParameter))
				{  return false;  }

			TokenIterator closingSymbol, endOfBlock;
			bool afterBody = false;

			while (iterator < endOfParameter)
				{
				if (iterator.PrototypeParsingType == PrototypeParsingType.TypeModifier)
					{
					typeBuilder.AddToken(iterator);
					iterator.Next();
					}

				else if (iterator.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier)
					{
					// Stop at a packed dimension.  However, don't stop if we're on an enum and the packed dimension appears before
					// the body ("enum bit [7:0] { ... }") instead of after ("enum bit { ... } [7:0]").
					if (iterator.Character == '[')
						{
						if (afterBody)
							{  break;  }

						TokenIterator baseType, ignore;

						if (FindBaseDataType(parameterSection, parameterIndex, out baseType, out ignore) &&
							Languages.Parsers.SystemVerilog.IsOnKeyword(baseType, "enum"))
							{  /* continue */  }
						else
							{  break;  }
						}

					if (iterator.Character == '{')
						{  afterBody = true;  }

					GetEndOfBlock(iterator, endOfParameter, out closingSymbol, out endOfBlock);

					typeBuilder.AddTokens(iterator, endOfBlock);
					iterator = endOfBlock;
					}

				else
					{  break;  }

				// If there's null whitespace tokens, move past them to see if there's any more properties on the other side.
				// The type builder will handle spacing so we don't have to worry about adding them.
				while (iterator.PrototypeParsingType == PrototypeParsingType.Null &&
						 iterator < endOfParameter &&
						 (iterator.FundamentalType == FundamentalType.Whitespace ||
						  iterator.FundamentalType == FundamentalType.LineBreak) )
					{
					iterator.Next();
					}
				}

			return true;
			}


		/* Function: AppendSigning
		 *
		 * If the passed parameter contains a signing keyword (signed, unsigned) it will add it to the <TypeBuilder> and return
		 * true.  Returns false otherwise.  Signing keywords must be marked with <PrototypeParsingType.TypeModifier>.
		 *
		 * Note that this will return false for signed structs and unions.  This is because those appear in the middle of
		 * "other modifiers" so are treated as part of them.
		 */
		protected bool AppendSigning (ParameterSection parameterSection, int parameterIndex, TypeBuilder typeBuilder)
			{
			TokenIterator signingKeyword, endOfParameter;

			if (FindSigning(parameterSection, parameterIndex, out signingKeyword, out endOfParameter))
				{
				typeBuilder.AddToken(signingKeyword);
				return true;
				}
			else
				{  return false;  }
			}


		/* Function: AppendPackedDimensions
		 * If the passed parameter contains one or more packed dimensions it will add them to the <TypeBuilder> and return true.
		 * Returns false otherwise.  Packed dimensions must be marked with <PrototypeParsingType.OpeningTypeModifier> and
		 * <PrototypeParsingType.ClosingTypeModifier>.  They also cannot appear after a <PrototypeParsingType.Name> token.
		 */
		protected bool AppendPackedDimensions (ParameterSection parameterSection, int parameterIndex, TypeBuilder typeBuilder)
			{
			TokenIterator iterator, endOfParameter;

			if (FindPackedDimensions(parameterSection, parameterIndex, out iterator, out endOfParameter))
				{
				TokenIterator closingSymbol, endOfBlock;
				GetEndOfBlock(iterator, endOfParameter, out closingSymbol, out endOfBlock);

				typeBuilder.AddTokens(iterator, endOfBlock);
				iterator = endOfBlock;

				// Add any consecutive packed dimensions.
				while (iterator < endOfParameter)
					{
					if (iterator.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier &&
						iterator.Character == '[')
						{
						GetEndOfBlock(iterator, endOfParameter, out closingSymbol, out endOfBlock);

						typeBuilder.AddTokens(iterator, endOfBlock);
						iterator = endOfBlock;
						}

					// If there's null whitespace tokens, move past them to see if there's any more dimensions on the other side.
					// The type builder will handle spacing so we don't have to worry about adding them.
					else if (iterator.PrototypeParsingType == PrototypeParsingType.Null &&
								(iterator.FundamentalType == FundamentalType.Whitespace ||
								iterator.FundamentalType == FundamentalType.LineBreak) )
						{
						iterator.Next();
						}
					else
						{  break;  }
					}

				return true;
				}
			else
				{  return false;  }
			}


		/* Function: AppendUnpackedDimensions
		 *
		 * If the passed parameter contains one or more unpacked dimensions it will add them to the <TypeBuilder> and return true.
		 * Returns false otherwise.  Unpacked dimensions must be marked with <PrototypeParsingType.OpeningParamModifier> and
		 * <PrototypeParsingType.ClosingParamModifier>.  They also must appear after a <PrototypeParsingType.Name> token.
		 *
		 * In order to distinguish packed from unpacked dimensions, you can set it to add a stand-in token in place of the name.
		 * This means the following declarations will be converted to the following types:
		 *
		 * --- SystemVerilog ---
		 * logic varA             // type is "logic"
		 * logic [7:0] varB       // type is "logic [7:0]"
		 * logic varC[0:3]        // type is "_[0:3]" to show it is unpacked
		 * logic [7:0] varD[0:3]  // type is "logic [7:0] _[0:3]" to show the latter is unpacked
		 * ---
		 *
		 * The stand-in token is defined in <NameStandInToken>.
		 */
		protected bool AppendUnpackedDimensions (ParameterSection parameterSection, int parameterIndex, TypeBuilder typeBuilder,
																		bool addStandInForName)
			{
			TokenIterator iterator, endOfParameter;

			if (FindUnpackedDimensions(parameterSection, parameterIndex, out iterator, out endOfParameter))
				{
				if (addStandInForName)
					{  typeBuilder.AddToken(NameStandInToken.FirstToken);  }

				TokenIterator closingSymbol, endOfBlock;
				GetEndOfBlock(iterator, endOfParameter, out closingSymbol, out endOfBlock);

				typeBuilder.AddTokens(iterator, endOfBlock);
				iterator = endOfBlock;

				// Add any consecutive unpacked dimensions.
				while (iterator < endOfParameter)
					{
					if (iterator.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier &&
						iterator.Character == '[')
						{
						GetEndOfBlock(iterator, endOfParameter, out closingSymbol, out endOfBlock);

						typeBuilder.AddTokens(iterator, endOfBlock);
						iterator = endOfBlock;
						}

					// If there's null whitespace tokens, move past them to see if there's any more dimensions on the other side.
					// The type builder will handle spacing so we don't have to worry about adding them.
					else if (iterator.PrototypeParsingType == PrototypeParsingType.Null &&
								(iterator.FundamentalType == FundamentalType.Whitespace ||
								iterator.FundamentalType == FundamentalType.LineBreak) )
						{
						iterator.Next();
						}
					else
						{  break;  }
					}

				return true;
				}
			else
				{  return false;  }
			}



		// Group: Overridden Functions
		// __________________________________________________________________________


		/* Function: GetParameter
		 */
		override public bool GetParameter (int parameterIndex, out TokenIterator parameterStart, out TokenIterator parameterEnd)
			{
			ParameterSection containingSection;
			int containingSectionParameterIndex;

			if (ConvertParameterIndex(parameterIndex, out containingSection, out containingSectionParameterIndex))
				{
				return containingSection.GetParameterBounds(containingSectionParameterIndex,
																				   out parameterStart, out parameterEnd);
				}
			else
				{
				parameterStart = tokenizer.EndOfTokens;
				parameterEnd = tokenizer.EndOfTokens;
				return false;
				}
			}


		/* Function: GetParameterName
		 */
		override public bool GetParameterName (int parameterIndex, out TokenIterator parameterNameStart,
																 out TokenIterator parameterNameEnd)
			{
			ParameterSection containingSection;
			int containingSectionParameterIndex;

			if (ConvertParameterIndex(parameterIndex, out containingSection, out containingSectionParameterIndex))
				{
				return containingSection.GetParameterName(containingSectionParameterIndex,
																				out parameterNameStart, out parameterNameEnd);
				}
			else
				{
				parameterNameStart = tokenizer.EndOfTokens;
				parameterNameEnd = tokenizer.EndOfTokens;
				return false;
				}
			}


		/* Function: BuildFullParameterType
		 */
		override public bool BuildFullParameterType (int parameterIndex, out TokenIterator fullTypeStart,
																	  out TokenIterator fullTypeEnd, bool impliedTypes = true)
			{
			ParameterSection containingSection;
			int containingSectionParameterIndex;

			if (!ConvertParameterIndex(parameterIndex, out containingSection, out containingSectionParameterIndex))
				{
				fullTypeStart = tokenizer.EndOfTokens;
				fullTypeEnd = tokenizer.EndOfTokens;
				return false;
				}

			TypeBuilder typeBuilder;

			switch (GetParameterSectionType(containingSection))
				{
				case ParameterSectionType.ANSIParameterPorts:
					typeBuilder = BuildFullANSIParameterPortType(containingSection, containingSectionParameterIndex, impliedTypes);
					break;

				case ParameterSectionType.ANSIPorts:
					typeBuilder = BuildFullANSIPortType(containingSection, containingSectionParameterIndex, impliedTypes);
					break;

				case ParameterSectionType.NonANSIPorts:
					// xxx temporary
					return base.BuildFullParameterType(parameterIndex, out fullTypeStart, out fullTypeEnd, impliedTypes);

				default:
					throw new NotImplementedException();
				}

			if (!typeBuilder.IsEmpty)
				{
				var fullTypeTokenizer = typeBuilder.ToTokenizer();
				fullTypeStart = fullTypeTokenizer.FirstToken;
				fullTypeEnd = fullTypeTokenizer.EndOfTokens;
				return true;
				}
			else
				{
				fullTypeStart = tokenizer.EndOfTokens;
				fullTypeEnd = tokenizer.EndOfTokens;
				return false;
				}
			}


		/* Function: GetBaseParameterType
		 */
		override public bool GetBaseParameterType (int parameterIndex, out TokenIterator baseTypeStart,
																		out TokenIterator baseTypeEnd, bool impliedTypes = true)
			{
			ParameterSection containingSection;
			int containingSectionParameterIndex;

			if (!ConvertParameterIndex(parameterIndex, out containingSection, out containingSectionParameterIndex))
				{
				baseTypeStart = tokenizer.EndOfTokens;
				baseTypeEnd = tokenizer.EndOfTokens;
				return false;
				}

			switch (GetParameterSectionType(containingSection))
				{
				case ParameterSectionType.ANSIParameterPorts:
					return GetBaseANSIParameterPortType(containingSection, containingSectionParameterIndex,
																			 out baseTypeStart, out baseTypeEnd, impliedTypes);

				case ParameterSectionType.ANSIPorts:
					return GetBaseANSIPortType(containingSection, containingSectionParameterIndex,
															  out baseTypeStart, out baseTypeEnd, impliedTypes);

				case ParameterSectionType.NonANSIPorts:
					// xxx temporary
					return base.GetBaseParameterType(parameterIndex, out baseTypeStart, out baseTypeEnd, impliedTypes);

				default:
					throw new NotImplementedException();
				}
			}


		/* Function: GetParameterDefaultValue
		 */
		override public bool GetParameterDefaultValue (int parameterIndex, out TokenIterator defaultValueStart,
																			 out TokenIterator defaultValueEnd)
			{
			ParameterSection containingSection;
			int containingSectionParameterIndex;

			if (ConvertParameterIndex(parameterIndex, out containingSection, out containingSectionParameterIndex))
				{
				return containingSection.GetParameterDefaultValue(containingSectionParameterIndex,
																						 out defaultValueStart, out defaultValueEnd);
				}
			else
				{
				defaultValueStart = tokenizer.EndOfTokens;
				defaultValueEnd = tokenizer.EndOfTokens;
				return false;
				}
			}



		// Group: Overridden Properties
		// __________________________________________________________________________


		/* Property: NumberOfParameters
		 */
		override public int NumberOfParameters
			{
			get
				{
				int numberOfParameters = 0;

				for (int i = mainSectionIndex; i < sections.Count; i++)
					{
					if (sections[i] is ParameterSection)
						{  numberOfParameters += (sections[i] as ParameterSection).NumberOfParameters;  }
					}

				return numberOfParameters;
				}
			}



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: NameStandinToken
		 * When building a full type that contains an unpacked dimension, this is the stand-in token to go in place of
		 * the variable name.  See <AppendUnpackedDimensions()>.
		 */
		static protected Tokenizer NameStandInToken;

		}
	}
