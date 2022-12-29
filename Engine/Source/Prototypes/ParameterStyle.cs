/*
 * Enum: CodeClear.NaturalDocs.Engine.Prototypes.ParameterStyle
 * ____________________________________________________________________________
 *
 * An enum representing the parameter style a language or a section of a prototype is using.
 *
 *		Unknown - The style hasn't been determined yet.
 *		C - A C-style prototype with parameters in a form similar to "int x = 12".
 *		Pascal - A Pascal-style prototype with parameters in a form similar to "x: int := 12".
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Prototypes
	{
	public enum ParameterStyle
		{  Unknown = 0, C, Pascal  }
	}
