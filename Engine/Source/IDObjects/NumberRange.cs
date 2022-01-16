/*
 * Struct: CodeClear.NaturalDocs.Engine.IDObjects.NumberRange
 * ____________________________________________________________________________
 *
 * A simple struct representing a range of numbers for use with <NumberSet>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.IDObjects
	{
	public struct NumberRange
		{

		/* var: Low
		 * The lower bounds of the range.  It may be the same as <High> if the range represents a single number.
		 */
		public int Low;

		/* var: High
		 * The upper bounds of the range.  It may be the same as <Low> if the range represents a single number.
		 */
		public int High;

		}
	}
