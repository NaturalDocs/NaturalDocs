/* 
 * Class: GregValure.NaturalDocs.Engine.Files.Filters.IgnoredSourceFolderRegex
 * ____________________________________________________________________________
 * 
 * A filter that uses a regular expression to determine which source folders to ignore.  If the regex matches any part
 * of the path, the folder will be ignored.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Files.Filters
	{
	public class IgnoredSourceFolderRegex : Filter
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: IgnoredSourceFolderRegex
		 * Creates a new filter from the passed regular expression object.
		 */
		public IgnoredSourceFolderRegex (System.Text.RegularExpressions.Regex newRegex)
			{
			regex = newRegex;
			}

		public override bool IgnoreSourceFolder (Path path)
			{
			return regex.IsMatch(path);
			}

			
		
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: regex
		 * The compiled regular expression created from the pattern.
		 */
		protected System.Text.RegularExpressions.Regex regex;
		
		}
	}