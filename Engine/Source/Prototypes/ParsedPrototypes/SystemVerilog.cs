/*
 * Class: CodeClear.NaturalDocs.Engine.Prototypes.ParsedPrototypes.SystemVerilog
 * ____________________________________________________________________________
 *
 * A specialized <ParsedPrototype> for SystemVerilog.  It differs from <ParsedPrototype> in that multiple parameter
 * sections will be used for things like <GetParameterName()> and <NumberOfParameters>.  This is because modules
 * can have both #() and () parameters and we want to include them both.
 *
 * This class treats them as if they are one continuous set of parameters, so in:
 *
 * --- SV Code ---
 * module MyModule #(int A, int B) (int C)
 * ---
 *
 * it behaves as if there are three parameters, with int B at parameter index 1 and int C at parameter index 2.  The
 * benefit of this is it allows types to be automatically retrieved from both sets of parameters when documenting them
 * in definition lists.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Prototypes.ParsedPrototypes
	{
	public class SystemVerilog : ParsedPrototype
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: SystemVerilog
		 */
		public SystemVerilog (Tokenizer prototype, int languageID, int commentTypeID)
			: base (prototype, languageID, commentTypeID, parameterStyle: ParameterStyle.SystemVerilog, supportsImpliedTypes: true)
			{
			// The base constructor will call RecalculateSections() and the overridden version will set secondarySectionIndex.
			}


		/* Function: GetParameter
		 */
		override public bool GetParameter (int parameterIndex, out TokenIterator parameterStart, out TokenIterator parameterEnd)
			{
			if (secondarySectionIndex != -1 &&
				parameterIndex >= NumberOfMainParameters)
				{
				return (sections[secondarySectionIndex] as ParameterSection).GetParameterBounds(
					parameterIndex - NumberOfMainParameters, out parameterStart, out parameterEnd);
				}
			else
				{
				return base.GetParameter(parameterIndex, out parameterStart, out parameterEnd);
				}
			}


		/* Function: GetParameterName
		 */
		override public bool GetParameterName (int parameterIndex, out TokenIterator parameterNameStart,
																   out TokenIterator parameterNameEnd)
			{
			if (secondarySectionIndex != -1 &&
				parameterIndex >= NumberOfMainParameters)
				{
				return (sections[secondarySectionIndex] as ParameterSection).GetParameterName(
					parameterIndex - NumberOfMainParameters, out parameterNameStart, out parameterNameEnd);
				}
			else
				{
				return base.GetParameterName(parameterIndex, out parameterNameStart, out parameterNameEnd);
				}
			}


		/* Function: BuildFullParameterType
		 */
		override public bool BuildFullParameterType (int parameterIndex, out TokenIterator fullTypeStart,
																		 out TokenIterator fullTypeEnd, bool impliedTypes = true)
			{
			if (secondarySectionIndex != -1 &&
				parameterIndex >= NumberOfMainParameters)
				{
				return (sections[secondarySectionIndex] as ParameterSection).BuildFullParameterType(
					parameterIndex - NumberOfMainParameters, out fullTypeStart, out fullTypeEnd,
					(impliedTypes && supportsImpliedTypes));
				}
			else
				{
				return base.BuildFullParameterType(parameterIndex, out fullTypeStart, out fullTypeEnd, impliedTypes);
				}
			}


		/* Function: GetBaseParameterType
		 */
		override public bool GetBaseParameterType (int parameterIndex, out TokenIterator start, out TokenIterator end,
																		bool impliedTypes = true)
			{
			if (secondarySectionIndex != -1 &&
				parameterIndex >= NumberOfMainParameters)
				{
				return (sections[secondarySectionIndex] as ParameterSection).GetBaseParameterType(
					parameterIndex - NumberOfMainParameters, out start, out end, (impliedTypes && supportsImpliedTypes));
				}
			else
				{
				return base.GetBaseParameterType(parameterIndex, out start, out end, impliedTypes);
				}
			}


		/* Function: GetParameterDefaultValue
		 */
		override public bool GetParameterDefaultValue (int parameterIndex, out TokenIterator defaultValueStart,
																			 out TokenIterator defaultValueEnd)
			{
			if (secondarySectionIndex != -1 &&
				parameterIndex >= NumberOfMainParameters)
				{
				return (sections[secondarySectionIndex] as ParameterSection).GetParameterDefaultValue(
					parameterIndex - NumberOfMainParameters, out defaultValueStart, out defaultValueEnd);
				}
			else
				{
				return base.GetParameterDefaultValue(parameterIndex, out defaultValueStart, out defaultValueEnd);
				}
			}


		/* Function: RecalculateSections
		 */
		override public void RecalculateSections ()
			{
			base.RecalculateSections();

			if (sections[mainSectionIndex] is ParameterSection &&
				mainSectionIndex + 1 < sections.Count &&
				sections[mainSectionIndex + 1] is ParameterSection)
				{
				secondarySectionIndex = mainSectionIndex + 1;
				}
			else
				{
				secondarySectionIndex = -1;
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: SecondarySectionIndex
		 * If the prototype contains two relevant sets of parameters, such as a module with #() and (), this is the index
		 * into <ParsedPrototype.sections> for the second one.  It will be -1 if there isn't one.
		 */
		public int SecondarySectionIndex
			{
			get
				{  return secondarySectionIndex;  }
			set
				{  secondarySectionIndex = value;  }
			}


		/* Property: NumberOfParameters
		 */
		override public int NumberOfParameters
			{
			get
				{
				return (NumberOfMainParameters + NumberOfSecondaryParameters);
				}
			}


		/* Property: NumberOfMainParameters
		 */
		public int NumberOfMainParameters
			{
			get
				{
				if (sections[mainSectionIndex] is ParameterSection)
					{  return (sections[mainSectionIndex] as ParameterSection).NumberOfParameters;  }
				else
					{  return 0;  }
				}
			}


		/* Property: NumberOfSecondaryParameters
		 */
		public int NumberOfSecondaryParameters
			{
			get
				{
				if (secondarySectionIndex != -1)
					{  return (sections[secondarySectionIndex] as ParameterSection).NumberOfParameters;  }
				else
					{  return 0;  }
				}
			}



		// Group: Variables
		// __________________________________________________________________________

		/* var: secondarySectionIndex
		 * If the prototype contains two relevant sets of parameters, such as a module with #() and (), this is the index
		 * into <ParsedPrototype.sections> for the second one.  It will be -1 if there isn't one.
		 */
		protected int secondarySectionIndex;

		}
	}
