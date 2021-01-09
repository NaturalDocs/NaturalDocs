/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Paths.Style
 * ____________________________________________________________________________
 * 
 * Path functions relating to styles in HTML output.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Paths
	{
	static public class Style
		{

		/* Function: OutputFile
		 * Returns the output file name of the style file.  The path to the style file must be relative to the style's folder.
		 */
		static public Path OutputFile (Path targetOutputFolder, string styleName, Path relativePath)
			{
			#if DEBUG
			if (relativePath.IsAbsolute)
				{  throw new Exception ("You must pass relative file paths to HTML.Paths.Style.OutputFile.");  }
			#endif

			return targetOutputFolder + "/styles/" + Paths.Utilities.Sanitize(styleName) + "/" + Paths.Utilities.Sanitize(relativePath);
			}


		/* Function: OutputFolder
		 * 
		 * Returns the output folder for style files.  If you include the style name, it will be the output folder for that
		 * particular style.  If you do not, it will be the root output folder for all styles.
		 * 
		 * Examples:
		 * 
		 *		targetOutputFolder - C:\Project\Documentation\styles
		 *		targetOutputFolder + styleName - C:\Project\Documentation\styles\Red
		 */
		static public Path OutputFolder (Path targetOutputFolder, string styleName = null)
			{
			StringBuilder result = new StringBuilder(targetOutputFolder);
			result.Append("/styles");

			if (styleName != null)
				{  
				result.Append('/');
				result.Append(Paths.Utilities.Sanitize(styleName));
				}
			
			return result.ToString();
			}

		}
	}
