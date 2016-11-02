/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Styles.FileSource
 * ____________________________________________________________________________
 * 
 * A file source that handles monitoring all the style files, both project and system.
 * 
 * All output files must register the style classes they use to work with this one, since it only submits files
 * from styles in use to <Files.Manager>.  Since style references across all output builders are pooled, you
 * cannot rely on file deletion notices when a style is removed from a particular builder.  It may be referenced
 * by other builders and thus still be seen as a valid part of the project by <Files.Manager>.  Also, when a
 * new style is added you should tell the class to force a reparse of everything, since the style may already
 * be in use by another builder and thus you won't get the new/change notice automatically.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2016 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Output.Styles
	{
	public class FileSource : Engine.Files.FileSource
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: FileSource
		 * Instance constructor.  If the path is relative it will be made absolute using the current working folder.
		 */
		public FileSource (Output.Manager manager) : base (manager.EngineInstance.Files)
			{
			styles = new List<Style>();
			forceReparse = false;
			}

		/* Function: AddStyle
		 * Adds a <Style> class to the list of styles to monitor the files of.
		 */
		public void AddStyle (Style newStyle)
			{
			foreach (Style style in styles)
				{
				if (style.IsSameFundamentalStyle(newStyle))
					{  return;  }
				}

			styles.Add(newStyle);
			}
			
		/* Function: Contains
		 * Returns whether this file source contains the passed file.
		 */
		override public bool Contains (Path file)
		   {
			foreach (Style style in styles)
				{
				if (style.Contains(file))
					{  return true;  }
				}

			return false;
		   }
			
		/* Function: MakeRelative
		 * If the passed absolute <Path> is contained by this file source, returns a relative path to it.  Otherwise returns null.
		 */
		override public Path MakeRelative (Path file)
		   {
			foreach (Style style in styles)
				{
				if (style.Contains(file))
					{  return style.MakeRelative(file);  }
				}

			return null;
		   }

		/* Function: AddAllFiles
		 * Calls <Files.Manager.AddOrUpdateFile()> for every file in the folder and its subfolders.
		 */
		override public void AddAllFiles (CancelDelegate cancelDelegate)
			{ 
			addAllFilesStatus.Reset();

			// String stack instead of Path stack because the IO functions will return strings and there's no need to normalize
			// them all or otherwise use Path functions on them.
			Stack<string> foldersToSearch = new Stack<string>();
			foldersToSearch.Push(Manager.EngineInstance.Config.SystemStyleFolder);
			foldersToSearch.Push(Manager.EngineInstance.Config.ProjectConfigFolder);
			
			while (foldersToSearch.Count > 0)
			   {
			   string folder = foldersToSearch.Pop();
			   string[] subfolders = System.IO.Directory.GetDirectories(folder);
				
			   if (cancelDelegate())
			      {  return;  }
			
			   foreach (string subfolder in subfolders)
			      {  foldersToSearch.Push(subfolder);  }

			   string[] files = System.IO.Directory.GetFiles(folder);
				
			   if (cancelDelegate())
			      {  return;  }

			   // This is deliberately not batched to increase parallelism.  Reading all the file modification times could potentially be
			   // a long, IO intensive operation if there are a lot of files in a folder.  It would be more efficient in a single threaded
			   // application to put off triggering the change notifications for each one, but in a multithreaded application it's 
			   // preventing other file sources from searching and/or parsers from working on the files already found.
			
			   foreach (string file in files)
			      {
			      if (cancelDelegate())
			         {  return;  }

					Path filePath = file;

					foreach (Style style in styles)
						{
						if (style.Contains(filePath))
							{  
							Manager.AddOrUpdateFile(filePath, Files.FileType.Style, System.IO.File.GetLastWriteTimeUtc(file), forceReparse);
							break;
							}
						}
			      }
			   }

			addAllFilesStatus.Completed = true;
			}



		// Group: Properties
		// __________________________________________________________________________

		/* Property: UniqueIDString
		 * A string that uniquely identifies this FileSource among all others of its <Type>, including FileSources based on other
		 * classes.
		 */
		override public string UniqueIDString
			{
			get 
				{  
				// Since we only have one FileSource for all the combined styles in all the output builders, we don't need to append
				// any sort of path or style name information.
				return "Styles:";  
				}
			}

		/* Property: Type
		 * The type of files this FileSource provides.
		 */
		override public Files.InputType Type
			{
			get 
				{  return Files.InputType.Style;  }
			}


		/* Property: ForceReparse
		 * Whether to force this class to reparse all style files found.  This is useful when adding a new style to a
		 * builder because you are guaranteed to get a change notice for every file in it.  Otherwise if another
		 * builder already referenced the new style, you wouldn't get any notices for it.
		 *
		 * You can only set this property to true.  It cannot be set back to false once turned on.
		 */
		public bool ForceReparse
			{
			get
				{  return forceReparse;  }
			set
				{
				if (value == true)
					{  forceReparse = true;  }
				else
					{  throw new InvalidOperationException();  }
				}
			}

		
		
		// Group: Variables
		// __________________________________________________________________________


		/* var: styles
		 * A list of all the <Styles> that are referenced by the builders.
		 */
		protected List<Style> styles;

		/* var: forceReparse
		 * Whether to force reparsing of all style files found.
		 */
		protected bool forceReparse;
			
		}
	}