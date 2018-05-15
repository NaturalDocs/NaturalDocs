/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.Project_nd
 * ____________________________________________________________________________
 * 
 * A class to handle loading and saving <Project.nd>.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 *		
 * 
 * File: Project.nd
 * 
 *		A binary file which stores some of the previous settings of <Project.txt>.  Only settings relevant to the global operation of 
 *		the program are stored.  Information that is only relevant to the output builders is not because whether a change is significant
 *		and what its effects are are dependent on the builders themselves.  They are expected to track any changes that are relevant
 *		themselves.
 *		
 *		Format:
 *		
 *			> [[Binary Header]]
 *		
 *			The file starts with the standard binary file header as managed by <BinaryFile>.
 *			
 *			> [Int32: Tab Width]
 *			> [Byte: Documented Only (0 or 1)]
 *			> [Byte: Auto Group (0 or 1)]
 *			> [Byte: Shrink Files (0 or 1)]
 *
 *			Global properties.
 *			
 *			> [String: Target Type]
 *			> [[Properties]]
 *			> ...
 *			> [String: null]
 *			
 *			A segment of data for each target.  They each start with a type string and the following properties and their encodings
 *			are specific to the type.  Segments continue until a null identifier is reached.
 *			
 *			> [String: Target Type="Source Folder"]
 *			> [String: Absolute Path]
 *			> [Int32: Number]
 *			
 *			The Name property isn't stored because that's only used for presentation in the output.
 *		
 *			> [String: Target Type="Image Folder"]
 *			> [String: Absolute Path]
 *			> [String: Number]
 *			
 *			Filter targets are not stored.  When a filter is changed from one run to the next the effects will be reflected in the file
 *			scans, so there is no need to detect it separately.
 *			
 *			> [String: Target Type="HTML Output Folder"]
 *			> [String: Absolute Path]
 *			> [Int32: Number]
 *			
 *			A new output target requires a full rebuild, and knowing its number is important for allowing it to keep intermediate data.
 *			Project information like Title is not stored, either at the global or output target level, as it is only relevant to the output 
 *			builders.
 *			
 * 
 *		Revisions:
 *		
 *			2.0:
 *			
 *				- The file is introduced.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using CodeClear.NaturalDocs.Engine.Config;


namespace CodeClear.NaturalDocs.Engine.Config
	{
	public class Project_nd
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Project_nd
		 */
		public Project_nd ()
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
			projectConfig = new ProjectConfig(Source.PreviousRun);
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
					projectConfig.TabWidthPropertyLocation = Source.PreviousRun;

					projectConfig.DocumentedOnly = (binaryFile.ReadByte() == 1);
					projectConfig.DocumentedOnlyPropertyLocation = Source.PreviousRun;

					projectConfig.AutoGroup = (binaryFile.ReadByte() == 1);
					projectConfig.AutoGroupPropertyLocation = Source.PreviousRun;

					projectConfig.ShrinkFiles = (binaryFile.ReadByte() == 1);
					projectConfig.ShrinkFilesPropertyLocation = Source.PreviousRun;
					
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
				projectConfig = new ProjectConfig(Source.PreviousRun);
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

			var target = new Targets.SourceFolder(Source.PreviousRun, Files.InputType.Source);

			target.Folder = binaryFile.ReadString();
			target.FolderPropertyLocation = Source.PreviousRun;

			target.Number = binaryFile.ReadInt32();
			target.NumberPropertyLocation = Source.PreviousRun;

			projectConfig.InputTargets.Add(target);
			}


		protected void LoadImageFolder ()
			{
			// [String: Absolute Path]
			// [Int32: Number]

			var target = new Targets.SourceFolder(Source.PreviousRun, Files.InputType.Image);

			target.Folder = binaryFile.ReadString();
			target.FolderPropertyLocation = Source.PreviousRun;

			target.Number = binaryFile.ReadInt32();
			target.NumberPropertyLocation = Source.PreviousRun;

			projectConfig.InputTargets.Add(target);
			}


		protected void LoadHTMLOutputFolder ()
			{
			// [String: Absolute Path]
			// [Int32: Number]

			var target = new Targets.HTMLOutputFolder(Source.PreviousRun);

			target.Folder = binaryFile.ReadString();
			target.FolderPropertyLocation = Source.PreviousRun;

			target.Number = binaryFile.ReadInt32();
			target.NumberPropertyLocation = Source.PreviousRun;

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
		                {  
						if (target.Type == Files.InputType.Source)
							{  SaveSourceFolder((Targets.SourceFolder)target, output);  }
						else if (target.Type == Files.InputType.Image)
							{  SaveImageFolder((Targets.SourceFolder)target, output);  }
						else
							{  throw new NotImplementedException();  }
						}
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


		protected void SaveImageFolder (Targets.SourceFolder target, BinaryFile output)
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