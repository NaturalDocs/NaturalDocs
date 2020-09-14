/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Paths.Image
 * ____________________________________________________________________________
 * 
 * Path functions relating to image files in HTML output.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Paths
	{
	static public class Image
		{

		/* Function: OutputFile
		 * Returns the output file of the image file.  The file path must be relative to the file source.
		 */
		static public Path OutputFile (Path targetOutputFolder, int fileSourceNumber, Files.InputType fileSourceType, Path relativeFilePath)
			{
			#if DEBUG
			if (relativeFilePath.IsAbsolute)
				{  throw new Exception ("You must pass relative file paths to HTML.Paths.Image.OutputFile.");  }
			#endif

			string fileName = relativeFilePath.NameWithoutPath.ToString();
			string outputFileName = Utilities.Sanitize(fileName);
			string outputFolder = OutputFolder(targetOutputFolder, fileSourceNumber, fileSourceType, relativeFilePath.ParentFolder);
			
			return outputFolder + '/' + outputFileName;
			}


		/* Function: OutputFolder
		 * 
		 * Returns the output folder of the passed output target, file source number and type, and optionally a subfolder within it.
		 * If the subfolder is null it returns the root output folder for the target and file source number.
		 * 
		 * Examples:
		 * 
		 *		targetOutputFolder + fileSourceNumber - C:\Project\Documentation\files
		 *		targetOutputFolder + fileSourceNumber + subfolder - C:\Project\Documentation\files\Folder1\Folder2
		 */
		static public Path OutputFolder (Path targetOutputFolder, int fileSourceNumber, Files.InputType fileSourceType,
													  Path subfolder = default(Path))
			{
			if (fileSourceType == Files.InputType.Source)
				{
				return Paths.SourceFile.OutputFolder(targetOutputFolder, fileSourceNumber, subfolder);
				}
			else if (fileSourceType == Files.InputType.Image)
				{
				StringBuilder result = new StringBuilder(targetOutputFolder);
				result.Append("/images");  

				if (fileSourceNumber != 1)
					{  result.Append(fileSourceNumber);  }
					
				if (subfolder != null)
					{
					result.Append('/');
					result.Append(Utilities.Sanitize(subfolder));
					}

				return result.ToString();
				}
			else
				{  throw new InvalidOperationException();  }
			}

		}
	}
