/* 
 * Enum: GregValure.NaturalDocs.Engine.Config.PropertyResult
 * ____________________________________________________________________________
 * 
 * Accepted - The property and value are used by this <Entry> and are valid.
 * InvalidProperty - The property does not belong to this <Entry>.
 * InvalidValue - The property belongs to this <Entry> but the value is invalid.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2008 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Config
	{

	public enum PropertyResult : byte
		{  Accepted, InvalidProperty, InvalidValue  }

	}