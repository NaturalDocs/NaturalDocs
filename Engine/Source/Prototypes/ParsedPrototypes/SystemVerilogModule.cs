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
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Prototypes.ParsedPrototypes
	{
	public class SystemVerilogModule : ParsedPrototype
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: SystemVerilogModule
		 */
		public SystemVerilogModule (Tokenizer prototype, int languageID, int commentTypeID)
			: base (prototype, languageID, commentTypeID, parameterStyle: ParameterStyle.SystemVerilog, supportsImpliedTypes: true)
			{
			// The base constructor will call RecalculateSections() and the overridden version will set secondarySectionIndex.
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

			if (ConvertParameterIndex(parameterIndex, out containingSection, out containingSectionParameterIndex))
				{
				return containingSection.BuildFullParameterType(containingSectionParameterIndex,
																					 out fullTypeStart, out fullTypeEnd,
																					(impliedTypes && supportsImpliedTypes));
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

			if (ConvertParameterIndex(parameterIndex, out containingSection, out containingSectionParameterIndex))
				{
				return containingSection.GetBaseParameterType(containingSectionParameterIndex,
																					 out baseTypeStart, out baseTypeEnd,
																					(impliedTypes && supportsImpliedTypes));
				}
			else
				{
				baseTypeStart = tokenizer.EndOfTokens;
				baseTypeEnd = tokenizer.EndOfTokens;
				return false;
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


		/* Function: RecalculateSections
		 */
		override public void RecalculateSections ()
			{
			base.RecalculateSections();
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

		}
	}
