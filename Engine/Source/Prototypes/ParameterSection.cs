/*
 * Class: CodeClear.NaturalDocs.Engine.Prototypes.ParameterSection
 * ____________________________________________________________________________
 *
 * A class that wraps a section of a <Tokenizer> which has been marked with <PrototypeParsingTypes> and represents a parameter
 * list.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Prototypes
	{
	public class ParameterSection : Section
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: ParameterSection
		 * Pass the start and end of the entire section to be covered, including the before and after parameters part.  This function will
		 * automatically call <RecalculateParameters()> so you don't have to.
		 */
		public ParameterSection (TokenIterator start, TokenIterator end,
											 ParameterStyle parameterStyle = ParameterStyle.Unknown,
											 bool supportsImpliedTypes = true)
			: base (start, end)
			{
			beforeParameters = null;
			afterParameters = null;
			parameters = null;

			this.parameterStyle = parameterStyle;
			this.supportsImpliedTypes = supportsImpliedTypes;

			RecalculateParameters();
			}


		/* Function: GetName
		 * Returns the bounds of the name if one is marked by <PrototypeParsingType.Name> tokens, or false if it couldn't find it.
		 */
		override public bool GetName (out TokenIterator nameStart, out TokenIterator nameEnd)
			{
			if (beforeParameters != null)
				{  return beforeParameters.GetName(out nameStart, out nameEnd);  }
			else
				{
				nameStart = end;
				nameEnd = end;
				return false;
				}
			}


		/* Function: GetAccessLevel
		 * Returns the <Languages.AccessLevel> if it can be determined.  This should only be used with basic language support
		 * as it's not as reliable as the results from the dedicated language parsers.
		 */
		override public Languages.AccessLevel GetAccessLevel ()
			{
			if (beforeParameters != null)
				{  return beforeParameters.GetAccessLevel();  }
			else
				{  return Languages.AccessLevel.Unknown;  }
			}


		/* Function: GetBeforeParameters
		 * Returns the bounds of the section of the prototype prior to the parameters and whether it exists.  If it does
		 * exist, the bounds will include the opening symbol of the parameter list such as the opening parenthesis.
		 */
		public bool GetBeforeParameters (out TokenIterator beforeParametersStart, out TokenIterator beforeParametersEnd)
			{
			if (beforeParameters != null)
				{
				beforeParametersStart = beforeParameters.Start;
				beforeParametersEnd = beforeParameters.End;
				return true;
				}
			else
				{
				beforeParametersStart = end;
				beforeParametersEnd = end;
				return false;
				}
			}


		/* Function: GetAfterParameters
		 * Returns the bounds of the section of the prototype after the parameters and whether it exists.  If it does
		 * exist, the bounds will include the closing symbol of the parameter list such as the closing parenthesis.
		 */
		public bool GetAfterParameters (out TokenIterator afterParametersStart, out TokenIterator afterParametersEnd)
			{
			if (afterParameters != null)
				{
				afterParametersStart = afterParameters.Start;
				afterParametersEnd = afterParameters.End;
				return true;
				}
			else
				{
				afterParametersStart = end;
				afterParametersEnd = end;
				return false;
				}
			}


		/* Function: GetOpeningParameterSymbol
		 * Returns the bounds of the opening parameter symbol, such as "(", "#(", or "{", and whether it exists.
		 */
		public bool GetOpeningParameterSymbol (out TokenIterator symbolStart, out TokenIterator symbolEnd)
			{
			if (beforeParameters != null)
				{
				symbolEnd = beforeParameters.End;


				// Remove any trailing whitespace as long as it's insignifcant

				TokenIterator lookbehind = symbolEnd;
				lookbehind.Previous();

				while (lookbehind >= beforeParameters.Start)
					{
					if (lookbehind.FundamentalType == FundamentalType.Whitespace &&
						lookbehind.PrototypeParsingType == PrototypeParsingType.Null)
						{
						symbolEnd = lookbehind;
						lookbehind.Previous();
						}
					else
						{  break;  }
					}


				// Move past any opening symbols

				symbolStart = symbolEnd;

				while (lookbehind >= beforeParameters.Start)
					{
					if (lookbehind.PrototypeParsingType == PrototypeParsingType.StartOfParams ||
						lookbehind.PrototypeParsingType == PrototypeParsingType.OpeningExtensionSymbol)
						{
						symbolStart = lookbehind;
						lookbehind.Previous();
						}
					else
						{  break;  }
					}

				return (symbolStart < symbolEnd);
				}
			else
				{
				symbolStart = end;
				symbolEnd = end;
				return false;
				}
			}



		// Group: Parameter Functions
		// __________________________________________________________________________


		/* Function: RecalculateParameters
		 * Scans this section for <PrototypeParsingType.StartOfParams>, <PrototypeParsingType.EndOfParams>, and
		 * <PrototypeParsingTypes.ParamSeparator> tokens to determine how many parameters there are and allow easy access to them
		 * individually.  This is automatically called by the constructor so you only need to call this manually if you made changes to these
		 * token types after creating this object.
		 */
		public void RecalculateParameters ()
			{
			TokenIterator iterator = start;


			// Before parameters

			while (iterator < end &&
					 iterator.PrototypeParsingType != PrototypeParsingType.StartOfParams)
				{  iterator.Next();  }

			if (iterator.PrototypeParsingType == PrototypeParsingType.StartOfParams)
				{
				iterator.Next();

				// Note that it could be more than one token, such as "(*".
				while (iterator.PrototypeParsingType == PrototypeParsingType.OpeningExtensionSymbol)
					{  iterator.Next();  }
				}

			beforeParameters = new Section(start, iterator);

			// Only trim whitespace if it's insignificant
			if (iterator.FundamentalType == FundamentalType.Whitespace &&
				iterator.PrototypeParsingType == PrototypeParsingType.Null)
				{  iterator.Next();  }


			// Count the parameters so we don't allocate more memory than we need for the list.  Also, there may not be any parameters
			// at all since it could be a pair of empty parentheses.

			if (iterator < end &&
				iterator.PrototypeParsingType != PrototypeParsingType.EndOfParams)
				{
				TokenIterator startOfFirstParam = iterator;
				int paramCount = 1;

				do
					{
					if (iterator.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
						{  paramCount++;  }

					iterator.Next();
					}
				while (iterator < end &&
						 iterator.PrototypeParsingType != PrototypeParsingType.EndOfParams);


				// Get the actual parameters

				parameters = new List<Section>(paramCount);

				iterator = startOfFirstParam;
				TokenIterator startOfParam = iterator;

				for (;;)
					{
					if (iterator >= end ||
						iterator.PrototypeParsingType == PrototypeParsingType.EndOfParams)
						{
						TokenIterator lookbehind = iterator;
						lookbehind.Previous();

						if (lookbehind.FundamentalType == FundamentalType.Whitespace &&
							lookbehind.PrototypeParsingType == PrototypeParsingType.Null &&
							lookbehind >= startOfParam)
							{
							parameters.Add( new Section(startOfParam, lookbehind) );
							}
						else
							{
							parameters.Add( new Section(startOfParam, iterator) );
							}

						break;
						}
					else if (iterator.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
						{
						iterator.Next();
						parameters.Add( new Section(startOfParam, iterator) );

						if (iterator.FundamentalType == FundamentalType.Whitespace &&
							iterator.PrototypeParsingType == PrototypeParsingType.Null)
							{  iterator.Next();  }

						startOfParam = iterator;
						}
					else
						{  iterator.Next();  }
					}
				}


			// After parameters

			if (iterator < end)
				{
				afterParameters = new Section(iterator, end);
				}


			// Remove the last parameter if it's empty.  This could happen when formatting things like structs where there's
			// a separator after each member and not just between them.  However, don't remove it if there's only one.

			if (parameters != null &&
				parameters.Count > 1)
				{
				var lastParameter = parameters[parameters.Count - 1];
				bool lastParameterIsEmpty = true;

				iterator = lastParameter.Start;

				while (iterator < lastParameter.End)
					{
					if (iterator.FundamentalType != FundamentalType.Whitespace)
						{
						lastParameterIsEmpty = false;
						break;
						}
					}

				if (lastParameterIsEmpty)
					{
					parameters.RemoveAt(parameters.Count - 1);
					}
				}
			}


		/* Function: GetParameterBounds
		 * Returns the bounds of the parameter, or false if it doesn't exist.
		 */
		public bool GetParameterBounds (int index, out TokenIterator parameterStart, out TokenIterator parameterEnd)
			{
			if (parameters != null && index >= 0 && index < parameters.Count)
				{
				parameters[index].GetBounds(out parameterStart, out parameterEnd);
				return true;
				}
			else
				{
				parameterStart = end;
				parameterEnd = end;
				return false;
				}
			}


		/* Function: GetParameterName
		 * Returns the bounds of the parameter name if one is marked by <PrototypeParsingType.Name> tokens, or false if it
		 * couldn't find it.
		 */
		public bool GetParameterName (int index, out TokenIterator nameStart, out TokenIterator nameEnd)
			{
			if (parameters != null && index >= 0 && index < parameters.Count)
				{  return parameters[index].GetName(out nameStart, out nameEnd);  }
			else
				{
				nameStart = end;
				nameEnd = end;
				return false;
				}
			}


		/* Function: GetBaseParameterType
		 *
		 * Returns the bounds of the parameter's base type if one is marked by <PrototypeParsingType.Type> tokens, or false if it
		 * couldn't find it.  It will also include type qualifiers ("Package.Class") but exclude modifiers (so "unsigned int*[]" would just
		 * be "int".)
		 *
		 * If implied types is set and <SupportsImpliedTypes> is true this will return "int" for y in "int x, y".  If it is not set or
		 * <SupportsImpliedTypes> is false then it will return false for y.
		 */
		public bool GetBaseParameterType (int index, out TokenIterator baseTypeStart, out TokenIterator baseTypeEnd, bool impliedTypes = true)
			{
			if (parameters != null && index >= 0 && index < parameters.Count)
				{
				// If the parameter has its own type, we can use the existing Build function.
				if (parameters[index].HasType)
					{  return parameters[index].GetBaseType(out baseTypeStart, out baseTypeEnd);  }

				// If not, find the closest parameter that defines one.
				int impliedTypeIndex;
				if (impliedTypes && supportsImpliedTypes && GetImpliedTypeIndex(index, out impliedTypeIndex))
					{  return parameters[impliedTypeIndex].GetBaseType(out baseTypeStart, out baseTypeEnd);  }
				}

			// Couldn't find the type or invalid parameters.
			baseTypeStart = end;
			baseTypeEnd = end;
			return false;
			}


		/* Function: BuildFullParameterType
		 *
		 * Returns the full type if one is marked by <PrototypeParsingType.Type> tokens, combining all its modifiers and qualifiers into
		 * one continuous string.
		 *
		 * If the type and all its modifiers and qualifiers are continuous in the original <Tokenizer> it will return <TokenIterators> based
		 * on it.  However, if the type and all its modifiers and qualifiers are NOT continuous it will create a separate <Tokenizer> to hold
		 * a continuous version of it.  The returned bounds will be <TokenIterators> based on that rather than on the original <Tokenizer>.
		 * The new <Tokenizer> will still contain the same <PrototypeParsingTypes> and <SyntaxHighlightingTypes> of the original.
		 *
		 * If implied types is set and <SupportsImpliedTypes> is true this will return "int" for y in "int x, y".  If it is not set or
		 * <SupportsImpliedTypes> is false then it will return false for y.
		 */
		public bool BuildFullParameterType (int index, out TokenIterator fullTypeStart, out TokenIterator fullTypeEnd, bool impliedTypes = true)
			{
			if (parameters != null && index >= 0 && index < parameters.Count)
				{
				// If the parameter has its own type, we can use the existing Build function.

				if (parameters[index].HasType)
					{  return parameters[index].BuildFullType(out fullTypeStart, out fullTypeEnd);  }


				// If not, build one from the closest parameter that defines one.

				int impliedTypeIndex;
				if (impliedTypes && supportsImpliedTypes && GetImpliedTypeIndex(index, out impliedTypeIndex))
					{
					TypeBuilder typeBuilder = new TypeBuilder();


					// If it's a Pascal-style parameter, first include any param modifiers that appear before the name, such as
					// "out" in "out a[12], b: integer".  We don't want to do this for C-style parameters because "int a, *b"
					// would show up as "*int" instead of "int*".

					TokenIterator iterator, end, name;

					if (ParameterStyle == ParameterStyle.Pascal)
						{
						iterator = parameters[index].Start;
						end = parameters[index].End;

						while (iterator < end &&
								  iterator.PrototypeParsingType != PrototypeParsingType.Name &&
								  iterator.PrototypeParsingType != PrototypeParsingType.KeywordName)
							{
							if (iterator.PrototypeParsingType == PrototypeParsingType.ParamModifier)
								{
								typeBuilder.AddToken(iterator);
								iterator.Next();
								}
							else if (iterator.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier)
								{
								TokenIterator closingToken, endOfBlock;
								ParsedPrototype.GetEndOfBlock(iterator, end, out closingToken, out endOfBlock);

								typeBuilder.AddTokens(iterator, endOfBlock);

								iterator = endOfBlock;
								}
							else if (!ParsedPrototype.TryToSkipBlock(ref iterator, end))
								{
								iterator.Next();
								}
							}

						name = iterator;
						}
					else
						{  name = default(TokenIterator);  }  // to make the compiler shut up


					// Next add the implied type and modifiers

					iterator = parameters[impliedTypeIndex].Start;
					end = parameters[impliedTypeIndex].End;

					while (iterator < end)
						{
						if (iterator.PrototypeParsingType == PrototypeParsingType.Type ||
							iterator.PrototypeParsingType == PrototypeParsingType.TypeModifier ||
							iterator.PrototypeParsingType == PrototypeParsingType.TypeQualifier)
							{
							typeBuilder.AddToken(iterator);
							iterator.Next();
							}
						else if (iterator.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier ||
								   iterator.PrototypeParsingType == PrototypeParsingType.StartOfTuple)
							{
							TokenIterator closingToken, endOfBlock;
							ParsedPrototype.GetEndOfBlock(iterator, end, out closingToken, out endOfBlock);

							typeBuilder.AddTokens(iterator, endOfBlock);

							iterator = endOfBlock;
							}
						else if (!ParsedPrototype.TryToSkipBlock(ref iterator, end))
							{
							iterator.Next();
							}
						}


					// Next get any param modifiers.  For Pascal-style parameters we'll start after the name to get things like
					// "[12]" in "out a[12], b: integer".  For C-style parameters we'll start at the beginning because we didn't add
					// any before.

					if (ParameterStyle == ParameterStyle.Pascal)
						{  iterator = name;  }
					else
						{  iterator = parameters[index].Start;  }

					end = parameters[index].End;

					while (iterator < end)
						{
						if (iterator.PrototypeParsingType == PrototypeParsingType.ParamModifier)
							{
							typeBuilder.AddToken(iterator);
							iterator.Next();
							}
						else if (iterator.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier)
							{
							TokenIterator closingToken, endOfBlock;
							ParsedPrototype.GetEndOfBlock(iterator, end, out closingToken, out endOfBlock);

							typeBuilder.AddTokens(iterator, endOfBlock);

							iterator = endOfBlock;
							}
						else if (!ParsedPrototype.TryToSkipBlock(ref iterator, end))
							{
							iterator.Next();
							}
						}

					Tokenizer fullTypeTokenizer = typeBuilder.ToTokenizer();
					fullTypeStart = fullTypeTokenizer.FirstToken;
					fullTypeEnd = fullTypeTokenizer.EndOfTokens;
					return true;
					}
				}

			// Couldn't find one or parameters were invalid
			fullTypeStart = end;
			fullTypeEnd = end;
			return false;
			}


		/* Function: GetParameterDefaultValue
		 * Returns the bounds of the parameter's default value as marked by <PrototypeParsingType.DefaultValueSeparator> and
		 * <PrototypeParsingType.DefaultValue, or false if it couldn't find it.
		 */
		public bool GetParameterDefaultValue (int index, out TokenIterator defaultValueStart, out TokenIterator defaultValueEnd)
			{
			if (parameters != null && index >= 0 && index < parameters.Count)
				{  return parameters[index].GetDefaultValue(out defaultValueStart, out defaultValueEnd);  }
			else
				{
				defaultValueStart = end;
				defaultValueEnd = end;
				return false;
				}
			}


		/* Function: GetImpliedTypeIndex
		 * If the parameter at the passed index doesn't define its own type, returns the index of the closest parameter that does.
		 * It will search backwards for C-style parameters ("int x, y") and forwards for Pascal-style parameters ("x, y: integer").  It
		 * will return false if it can't find one.
		 */
		protected bool GetImpliedTypeIndex (int parameterIndex, out int impliedTypeIndex)
			{
			impliedTypeIndex = parameterIndex;

			for (;;)
				{
				if (ParameterStyle == ParameterStyle.C ||
					ParameterStyle == ParameterStyle.SystemVerilog)
					{
					impliedTypeIndex--;

					if (impliedTypeIndex < 0)
						{
						impliedTypeIndex = -1;
						return false;
						}
					}
				else if (ParameterStyle == ParameterStyle.Pascal)
					{
					impliedTypeIndex++;

					if (impliedTypeIndex >= NumberOfParameters)
						{
						impliedTypeIndex = -1;
						return false;
						}
					}
				else
					{  throw new NotImplementedException();  }

				if (parameters[impliedTypeIndex].HasType)
					{  return true;  }
				}
			}



		// Group: Irrelevant Functions
		// __________________________________________________________________________
		//
		// These functions are no longer relevant for parameter sections and always return false.  You should use the <Parameter
		// Functions> instead.


		/* Function: GetBaseType
		 * This function isn't relevant to this section and will always return false.  Call on one of the individual parameters instead.
		 */
		override public bool GetBaseType (out TokenIterator baseTypeStart, out TokenIterator baseTypeEnd)
			{
			baseTypeStart = end;
			baseTypeEnd = end;
			return false;
			}


		/* Function: BuildFullType
		 * This function isn't relevant to this section and will always return false.  Call on one of the individual parameters instead.
		 */
		override public bool BuildFullType (out TokenIterator fullTypeStart, out TokenIterator fullTypeEnd)
			{
			fullTypeStart = end;
			fullTypeEnd = end;
			return false;
			}


		/* Function: GetDefaultValue
		 * This function isn't relevant to this section and will always return false.  Call on one of the individual parameters instead.
		 */
		override public bool GetDefaultValue (out TokenIterator defaultValueStart, out TokenIterator defaultValueEnd)
			{
			defaultValueStart = end;
			defaultValueEnd = end;
			return false;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: NumberOfParameters
		 */
		public int NumberOfParameters
			{
			get
				{
				if (parameters == null)
					{  return 0;  }
				else
					{  return parameters.Count;  }
				}
			}


		/* Property: ParameterStyle
		 */
		public ParameterStyle ParameterStyle
			{
			get
				{  return parameterStyle;  }
			set
				{  parameterStyle = value;  }
			}


		/* Property: SupportsImpliedTypes
		 * Whether the prototype's language supports implied types.
		 */
		public bool SupportsImpliedTypes
			{
			get
				{  return supportsImpliedTypes;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: beforeParameters
		 * The part of the section before the first parameter, including <PrototypeParsingTypes.StartOfParams>, or null if none.
		 */
		protected Section beforeParameters;

		/* var: afterParameters
		 * The part of the section after the last parameter, including <PrototypeParsingTypes.EndOfParams>, or null if none.
		 */
		protected Section afterParameters;

		/* var: parameters
		 * A separate section for each parameter, or null if there aren't any.
		 */
		protected List<Section> parameters;

		/* var: parameterStyle
		 * The format of the parameters, such as C-style ("int x") or Pascal-style ("x: int").
		 */
		protected ParameterStyle parameterStyle;

		/* var: supportsImpliedTypes
		 * Whether the prototype's language supports implied types.
		 */
		protected bool supportsImpliedTypes;

		}
	}
