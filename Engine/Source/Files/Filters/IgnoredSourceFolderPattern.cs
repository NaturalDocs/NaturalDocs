/* 
 * Class: CodeClear.NaturalDocs.Engine.Files.Filters.IgnoredSourceFolderPattern
 * ____________________________________________________________________________
 * 
 * An ignored source folder pattern.  If the pattern matches any folder name in the path, it will be ignored.
 * Patterns can use ? to represent any single character, and * to represent zero or more characters.  The
 * pattern must match the full folder name, so "cli" will not match "client" although "cli*" will.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Files.Filters
	{
	public class IgnoredSourceFolderPattern : IgnoredSourceFolderRegex
		{
		
		// Group: Functions
		// __________________________________________________________________________
		

		/* Constructor: IgnoredSourceFolderPattern
		 * Creates a new object from the passed pattern
		 */
		public IgnoredSourceFolderPattern (string newPattern) : base (null) // Set the regex later
			{
			string pattern = newPattern;
			
			pattern = System.Text.RegularExpressions.Regex.Replace(pattern, @"^\*?[/\\]", "");
			pattern = System.Text.RegularExpressions.Regex.Replace(pattern, @"[/\\]\*?$", "");
			pattern = System.Text.RegularExpressions.Regex.Replace(pattern, @"[/\\]", Engine.SystemInfo.PathSeparatorCharacter.ToString());
				
			pattern = System.Text.RegularExpressions.Regex.Escape(pattern);
			
			pattern = pattern.Replace("\\?", ".");
			pattern = pattern.Replace("\\*", ".*");				
			pattern = @"(?:^|[/\\])" + pattern + @"(?:$|[/\\])";
			
			System.Text.RegularExpressions.RegexOptions options = System.Text.RegularExpressions.RegexOptions.Compiled;
			
			if (Engine.SystemInfo.IgnoreCaseInPaths)
				{  options |= System.Text.RegularExpressions.RegexOptions.IgnoreCase;  }
				
			regex = new System.Text.RegularExpressions.Regex(pattern, options);
			}
					
		}
	}