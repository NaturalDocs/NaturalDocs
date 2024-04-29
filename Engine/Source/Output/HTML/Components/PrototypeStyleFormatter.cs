/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeStyleFormatter
 * ____________________________________________________________________________
 *
 * A base class for classes such as <PrototypeStyleFormatters.C> and <PrototypeStyleFormatters.Pascal>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	abstract public class PrototypeStyleFormatter
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: ColumnSpacing
		 *
		 * Any spacing rules that should be applied to a column to make it consistent.
		 *
		 * Normal - No spacing changes should be made.
		 * AlwaysLeading - There should always be a space before the column's content.
		 * AlwaysTrailing - There should always be a space after the column's content.
		 * AlwaysBoth - There should always be a space on both sides of the column's content.
		 * BothUnlessColon - There should be a space on both sides of the column's content, unless the content is a colon, in which
		 *								case there should only be a space on the right side.
		 */
		public enum ColumnSpacing
			{  Normal, AlwaysLeading, AlwaysTrailing, AlwaysBoth, SpacedUnlessColon  }



		// Group: Functions
		// __________________________________________________________________________


		/* Function: CalculateCells
		 * Takes a <ParameterSection> and generates a table of <PrototypeCellLayouts>.  Each row represents a parameter, and each
		 * cell is a column in <ColumnOrder>.
		 */
		abstract public PrototypeCellLayout[,] CalculateCells (ParameterSection parameters);


		/* Function: ColumnSpacingOf
		 * Returns the spacing of the passed column type.
		 */
		virtual public ColumnSpacing ColumnSpacingOf (PrototypeColumnType columnType)
			{
			return ColumnSpacing.Normal;
			}


		/* Function: ColumnSpacingOf
		 * Returns the spacing of the column at the passed index.
		 */
		public ColumnSpacing ColumnSpacingOf (int index)
			{
			return ColumnSpacingOf(ColumnOrder[index]);
			}



		// Group: Parsing Functions
		// __________________________________________________________________________


		/* Function: SkipModifierBlock
		 * If the iterator is on a <PrototypeParsingType.OpeningTypeModifier> or <PrototypeParsingType.OpeningParamModifier>
		 * token, moves the token iterator past the entire block, including any nested blocks.
		 */
		protected void SkipModifierBlock (ref TokenIterator iterator, TokenIterator limit)
			{
			if (iterator < limit &&
				(iterator.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier ||
				 iterator.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier))
				{
				int level = 1;
				iterator.Next();

				while (iterator < limit && level > 0)
					{
					if (iterator.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier ||
						iterator.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier)
						{  level++;  }
					else if (iterator.PrototypeParsingType == PrototypeParsingType.ClosingTypeModifier ||
							   iterator.PrototypeParsingType == PrototypeParsingType.ClosingParamModifier)
						{  level--;  }

					iterator.Next();
					}

				while (iterator < limit &&
						  iterator.PrototypeParsingType == PrototypeParsingType.ClosingExtensionSymbol)
					{  iterator.Next();  }
				}
			}


		/* Function: SkipTuple
		 * If the iterator is on a <PrototypeParsingType.StartOfTuple> token, moves the token iterator past the entire tuple,
		 * including any nested tuples.
		 */
		protected void SkipTuple (ref TokenIterator iterator, TokenIterator limit)
			{
			if (iterator < limit &&
				iterator.PrototypeParsingType == PrototypeParsingType.StartOfTuple)
				{
				int level = 1;
				iterator.Next();

				while (iterator < limit && level > 0)
					{
					if (iterator.PrototypeParsingType == PrototypeParsingType.StartOfTuple)
						{  level++;  }
					else if (iterator.PrototypeParsingType == PrototypeParsingType.EndOfTuple)
						{  level--;  }

					iterator.Next();
					}

				while (iterator < limit &&
						  iterator.PrototypeParsingType == PrototypeParsingType.ClosingExtensionSymbol)
					{  iterator.Next();  }
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: ColumnOrder
		 * An array of <PrototypeColumnTypes> representing the order in which columns should appear.
		 */
		abstract public PrototypeColumnType[] ColumnOrder { get; }


		/* Property: ColumnCount
		 * The number of possible columns.
		 */
		public int ColumnCount
			{
			get
				{  return ColumnOrder.Length;  }
			}

		}
	}
