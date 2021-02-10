/* 
 * Class: CodeClear.NaturalDocs.Engine.Languages.ConfigFiles.TextFilePrototypeEnders
 * ____________________________________________________________________________
 * 
 * A class encapsulating information about a language as it appears in <Languages.txt>.  This differs from <Language> in 
 * that its meant to represent its entry in a config file rather than the final combined settings of a language.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Languages.ConfigFiles
	{
	public class TextFilePrototypeEnders
		{
		public TextFilePrototypeEnders()
			{
			CommentTypeName = null;
			EnderStrings = null;
			}
			
		public string CommentTypeName;

		/* var: EnderStrings
			* An array of ender strings which may be symbols and/or "\n".
			*/
		public string[] EnderStrings;
		}
	
	}