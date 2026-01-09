/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeStyleFormatters.Unknown
 * ____________________________________________________________________________
 *
 * A stub class for prototypes set to <ParameterStyle.Unknown>.  This can happen when the prototype has an empty set of parentheses,
 * which makes it a prototype with parameters but an unknown style.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Prototypes;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeStyleFormatters
	{
	public class Unknown : PrototypeStyleFormatter
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: CalculateCells
		 */
		override public PrototypeCellLayout[,] CalculateCells (ParameterSection parameters)
			{
			return null;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: ColumnOrder
		 */
		override public PrototypeColumnType[] ColumnOrder
			{
			get
				{  return ColumnOrderValues;  }
			}



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: ColumnOrderValues
		 */
		readonly static protected PrototypeColumnType[] ColumnOrderValues = { };

		}
	}
