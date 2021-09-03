/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.ConfigFiles.BinaryFileParser
 * ____________________________________________________________________________
 * 
 * A class to handle loading and saving <Project.nd>.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using CodeClear.NaturalDocs.Engine.Config;


namespace CodeClear.NaturalDocs.Engine.Config.ConfigFiles
	{
	public class BinaryFileParser
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: BinaryFileParser
		 */
		public BinaryFileParser ()
			{
			projectConfig = null;
			binaryFile = null;
			}



		// Group: Loading Functions
		// __________________________________________________________________________


		/* Function: Load
		 * Loads the information in <Project.nd>, returning whether it was successful.
		 */
		public bool Load (Path filename, out ProjectConfig projectConfig)
			{
			projectConfig = new ProjectConfig(PropertySource.PreviousRun);
			this.projectConfig = projectConfig;

			binaryFile = new BinaryFile();
			bool result = true;
			
			try
				{
				// There were no file format changes between 2.0 and 2.0.2 but there were parsing changes and
				// bug fixes that require a full rebuild.
				if (binaryFile.OpenForReading(filename, "2.0.2") == false)
					{
					result = false;
					}
				else
					{
					
					// [Int32: Tab Width]
					// [Byte: Documented Only (0 or 1)]
					// [Byte: Auto Group (0 or 1)]
					// [Byte: Shrink Files (0 or 1)]

					projectConfig.TabWidth = binaryFile.ReadInt32();
					projectConfig.TabWidthPropertyLocation = PropertySource.PreviousRun;

					projectConfig.DocumentedOnly = (binaryFile.ReadByte() == 1);
					projectConfig.DocumentedOnlyPropertyLocation = PropertySource.PreviousRun;

					projectConfig.AutoGroup = (binaryFile.ReadByte() == 1);
					projectConfig.AutoGroupPropertyLocation = PropertySource.PreviousRun;

					projectConfig.ShrinkFiles = (binaryFile.ReadByte() == 1);
					projectConfig.ShrinkFilesPropertyLocation = PropertySource.PreviousRun;
					
					// [String: Identifier]
					// [[Properties]]
					// ...
					// [String: null]

					string identifier = binaryFile.ReadString();

					while (identifier != null)
						{
						LoadTarget(identifier);
						identifier = binaryFile.ReadString();
						}
					}
				}
			catch
				{
				result = false;

				// Reset everything.
				projectConfig = new ProjectConfig(PropertySource.PreviousRun);
				}
			finally
				{
				binaryFile.Close();
				}
				
			return result;
			}


		/* Function: LoadTarget
		 * Creates a target object from the passed identifier and adds it to <projectConfig>.  Reads any additional properties it
		 * may have from <binaryFile>, which will be in position right after the identifier string.
		 */
		protected void LoadTarget (string identifier)
			{
			if (identifier == "Source Folder")
				{  LoadSourceFolder();  }
			else if (identifier == "Image Folder")
				{  LoadImageFolder();  }
			else if (identifier == "HTML Output Folder")
				{  LoadHTMLOutputFolder();  }
			else
				{  throw new Exception("Unknown Project.nd entry " + identifier);  }
			}


		protected void LoadSourceFolder ()
			{
			// [String: Absolute Path]
			// [Int32: Number]

			var target = new Targets.SourceFolder(PropertySource.PreviousRun);

			target.Folder = (AbsolutePath)binaryFile.ReadString();
			target.FolderPropertyLocation = PropertySource.PreviousRun;

			target.Number = binaryFile.ReadInt32();
			target.NumberPropertyLocation = PropertySource.PreviousRun;

			projectConfig.InputTargets.Add(target);
			}


		protected void LoadImageFolder ()
			{
			// [String: Absolute Path]
			// [Int32: Number]

			var target = new Targets.ImageFolder(PropertySource.PreviousRun);

			target.Folder = (AbsolutePath)binaryFile.ReadString();
			target.FolderPropertyLocation = PropertySource.PreviousRun;

			target.Number = binaryFile.ReadInt32();
			target.NumberPropertyLocation = PropertySource.PreviousRun;

			projectConfig.InputTargets.Add(target);
			}


		protected void LoadHTMLOutputFolder ()
			{
			// [String: Absolute Path]
			// [Int32: Number]

			var target = new Targets.HTMLOutputFolder(PropertySource.PreviousRun);

			target.Folder = (AbsolutePath)binaryFile.ReadString();
			target.FolderPropertyLocation = PropertySource.PreviousRun;

			target.Number = binaryFile.ReadInt32();
			target.NumberPropertyLocation = PropertySource.PreviousRun;

			projectConfig.OutputTargets.Add(target);
			}



		// Group: Saving Functions
		// __________________________________________________________________________


		/* Function: Save
		 * Saves the passed <ProjectConfig> into <Project.nd>.  Throws an exception if unsuccessful.
		 */
		public void Save (Path filename, ProjectConfig projectConfig)
		    {
		    var output = new BinaryFile();
		    output.OpenForWriting(filename);

		    try
		        {

		        // [Int32: Tab Width]
		        // [Byte: Documented Only (0 or 1)]
		        // [Byte: Auto Group (0 or 1)]
		        // [Byte: Shrink Files (0 or 1)]

		        output.WriteInt32(projectConfig.TabWidth);
		        output.WriteByte( (byte)((bool)projectConfig.DocumentedOnly == false ? 0 : 1) );
		        output.WriteByte( (byte)((bool)projectConfig.AutoGroup == false ? 0 : 1) );
		        output.WriteByte( (byte)((bool)projectConfig.ShrinkFiles == false ? 0 : 1) );
				
		        // [String: Identifier]
		        // [[Properties]]
		        // ...
		        // [String: null]
				
		        foreach (var target in projectConfig.InputTargets)
		            {
		            if (target is Targets.SourceFolder)
						{  SaveSourceFolder((Targets.SourceFolder)target, output);  }
		            else if (target is Targets.ImageFolder)
						{  SaveImageFolder((Targets.ImageFolder)target, output);  }
					else
						{  throw new NotImplementedException();  }
		            }

				// We don't save filter targets

				foreach (var target in projectConfig.OutputTargets)
					{
					if (target is Targets.HTMLOutputFolder)
						{  SaveHTMLOutputFolder((Targets.HTMLOutputFolder)target, output);  }
					else
						{  throw new NotImplementedException();  }
					}
					
		        output.WriteString(null);
		        }
				
		    finally
		        {
		        output.Close();
		        }
		    }


		protected void SaveSourceFolder (Targets.SourceFolder target, BinaryFile output)
		    {
		    // [String: Identifier="Source Folder"]
		    // [String: Absolute Path]
		    // [Int32: Number]
				
		    output.WriteString("Source Folder");
		    output.WriteString(target.Folder);
		    output.WriteInt32(target.Number);
		    }


		protected void SaveImageFolder (Targets.ImageFolder target, BinaryFile output)
		    {
		    // [String: Identifier="Image Folder"]
		    // [String: Absolute Path]
		    // [String: Number]
				
		    output.WriteString("Image Folder");
		    output.WriteString(target.Folder);
		    output.WriteInt32(target.Number);
		    }


		protected void SaveHTMLOutputFolder (Targets.HTMLOutputFolder target, BinaryFile output)
			{
			// [String: Identifier="HTML Output Folder"]
			// [String: Absolute Path]
			// [Int32: Number]

			output.WriteString("HTML Output Folder");
			output.WriteString(target.Folder);
			output.WriteInt32(target.Number);
			}



		// Group: Variables
		// __________________________________________________________________________

		protected ProjectConfig projectConfig;
		protected BinaryFile binaryFile;

		}
	}