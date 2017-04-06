/* 
 * Class: CodeClear.NaturalDocs.Engine.Exceptions.RelativeCompareOfIteratorsNotOnSameBase
 * ____________________________________________________________________________
 * 
 * Thrown when two iterators were compared relatively (such as a < b) but were not on the same base object.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Exceptions
	{
	public class RelativeCompareOfIteratorsNotOnSameBase : Exception
		{
		public RelativeCompareOfIteratorsNotOnSameBase()
			: base ("Attempted a relative compare on two iterators which were not on the same base object.")
			{
			}
		}
	}