/* 
 * Enum: GregValure.NaturalDocs.Engine.Tokenization.PreviousPastWhitespaceMode
 * ____________________________________________________________________________
 * 
 * The method to use when using <PreviousPastWhitespace()>.
 * 
 * EndingBounds - The iterator is treated as an ending bounds, a limit to another iterator.
 * Iterator - The iterator is treated as an independent iterator.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Tokenization
	{
	public enum PreviousPastWhitespaceMode : byte
		{  EndingBounds, Iterator  }
	}