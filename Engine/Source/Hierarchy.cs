/* 
 * Enum: CodeClear.NaturalDocs.Engine.Hierarchy
 * ____________________________________________________________________________
 * 
 * Which hierarchy a <Symbols.ClassString> or other element appears in.
 * 
 * Class - The class hierarchy.
 * Database - The database hierarchy.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;


namespace CodeClear.NaturalDocs.Engine
	{
	public enum Hierarchy : byte
		{  
		Class = 1, 
		Database = 2  
		}
	}