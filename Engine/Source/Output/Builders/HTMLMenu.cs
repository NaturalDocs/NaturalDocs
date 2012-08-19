/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTMLMenu
 * ____________________________________________________________________________
 * 
 * A reusable helper class to build the JavaScript menu data for <Output.Builders.HTML>.  See <JavaScript Menu Data> 
 * for the output format.
 * 
 * 
 * Usage:
 * 
 *		- Fill in the menu data as described in the documentation for <Menu>.
 *		- Call <Build()>.
 *		- Ta da.
 *	
 * 
 * Threading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.  It has an internal state that is used during a call to
 *		<Build()>, and another <Build()> should not be started until it's completed.  Instead each thread should create its 
 *		own object.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Output.MenuEntries;


namespace GregValure.NaturalDocs.Engine.Output.Builders
	{
	public class HTMLMenu : Menu
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: HTMLMenu
		 */
		public HTMLMenu (Builders.HTML htmlBuilder) : base ()
			{
			this.htmlBuilder = htmlBuilder;
			}


		/* Function: Build
		 * Generates JSON files for all entries in the menu.  It returns a <StringTable> mapping the file type strings ("files", 
		 * "classes", etc.) to a <IDObjects.NumberSet> representing all the files that were generated.  So "files.js", "files2.js",
		 * and "files3.js" would map to "files" -> [1-3].
		 */
		public StringTable<IDObjects.NumberSet> Build ()
			{
			StringTable<IDObjects.NumberSet> outputFiles = new StringTable<IDObjects.NumberSet>(false, false);

			if (RootFileMenu != null)
				{  
				GenerateJSON(RootFileMenu);


				// Assign the root the first files ID.

				IDObjects.NumberSet numberSet = new IDObjects.NumberSet();
				numberSet.Add(1);

				outputFiles.Add("files", numberSet);

				(RootFileMenu.ExtraData as ContainerEntryExtraData).DataFileName = htmlBuilder.Menu_DataFileNameOnly("files", 1);


				// Segment and build.

				SegmentMenu(RootFileMenu, "files", outputFiles);
				BuildOutput(RootFileMenu);
				}

			return outputFiles;
			}


		/* Function: GenerateJSON
		 * Generates JSON for all the entries in the passed container.
		 */
		protected void GenerateJSON (MenuEntries.Base.Container container)
			{
			ContainerEntryExtraData containerExtraData = new ContainerEntryExtraData(container);
			container.ExtraData = containerExtraData;

			containerExtraData.GenerateJSON(htmlBuilder, this);

			foreach (var member in container.Members)
				{
				if (member is MenuEntries.Base.Target)
					{
					TargetEntryExtraData targetExtraData = new TargetEntryExtraData((MenuEntries.Base.Target)member);
					member.ExtraData = targetExtraData;

					targetExtraData.GenerateJSON(htmlBuilder, this);
					}
				else if (member is MenuEntries.Base.Container)
					{
					GenerateJSON((MenuEntries.Base.Container)member);
					}
				}
			}


		/* Function: SegmentMenu
		 * Segments the menu into smaller pieces and generates data file names.
		 */
		protected void SegmentMenu (MenuEntries.Base.Container container, string dataFileType, 
																  StringTable<IDObjects.NumberSet> usedDataFiles)
			{
			// xxx
			}


		/* Function: BuildOutput
		 * Generates the output file for the container.  It must have <ContainerEntryExtraData.DataFileName> set.  If it finds
		 * any sub-containers that also have that set, it will recursively generate files for them as well.
		 */
		protected void BuildOutput (MenuEntries.Base.Container container)
			{
			#if DEBUG
			if (container.ExtraData == null || (container.ExtraData as ContainerEntryExtraData).StartsNewDataFile == false)
				{  throw new Exception ("BuildOutput() can only be called on containers with DataFileName set.");  }
			#endif

			Stack<MenuEntries.Base.Container> containersToBuild = new Stack<MenuEntries.Base.Container>();
			containersToBuild.Push(container);

			while (containersToBuild.Count > 0)
				{
				MenuEntries.Base.Container containerToBuild = containersToBuild.Pop();
				string fileName = (containerToBuild.ExtraData as ContainerEntryExtraData).DataFileName;
				
				StringBuilder output = new StringBuilder();
				output.Append("NDMenu.OnSectionLoaded(\"");
				output.StringEscapeAndAppend(fileName);
				output.Append("\",[");

				#if DONT_SHRINK_FILES
				output.AppendLine();
				#endif
				
				AppendMembers(containerToBuild, output, 1, containersToBuild);

				#if DONT_SHRINK_FILES
				output.Append(' ', IndentSpaces);
				#endif

				output.Append("]);");

				System.IO.File.WriteAllText(htmlBuilder.Menu_DataFolder + "/" + fileName, output.ToString());
				}
			}


		/* Function: AppendMembers
		 * A support function for <BuildOutput()>.  Appends the output of the container's members to the string, recursively 
		 * going through sub-containers as well.  This will not include the surrounding brackets, only the comma-separated
		 * member entries.  If it finds any sub-containers that start a new data file, it will add them to containersToBuild.
		 */
		protected void AppendMembers (MenuEntries.Base.Container container, StringBuilder output, int indent, 
																  Stack<MenuEntries.Base.Container> containersToBuild)
			{
			for (int i = 0; i < container.Members.Count; i++)
				{
				var member = container.Members[i];

				#if DONT_SHRINK_FILES
				output.Append(' ', indent * IndentSpaces);
				#endif

				if (member is MenuEntries.Base.Target)
					{
					TargetEntryExtraData targetExtraData = (TargetEntryExtraData)member.ExtraData;
					output.Append(targetExtraData.JSON);
					}
				else if (member is MenuEntries.Base.Container)
					{
					ContainerEntryExtraData containerExtraData = (ContainerEntryExtraData)member.ExtraData;
					output.Append(containerExtraData.JSONBeforeMembers);

					if (containerExtraData.StartsNewDataFile)
						{
						output.Append('"');
						output.StringEscapeAndAppend(containerExtraData.DataFileName);
						output.Append('"');

						containersToBuild.Push((MenuEntries.Base.Container)member);
						}
					else
						{
						output.Append('[');

						#if DONT_SHRINK_FILES
						output.AppendLine();
						#endif

						AppendMembers((MenuEntries.Base.Container)member, output, indent + 1, containersToBuild);

						#if DONT_SHRINK_FILES
						output.Append(' ', (indent + 1) * IndentSpaces);
						#endif

						output.Append(']');
						}

					output.Append(containerExtraData.JSONAfterMembers);
					}
				#if DEBUG
				else
					{  throw new Exception ("Can't append JSON for menu entry " + member.Title + ".");  }
				#endif

				if (i < container.Members.Count - 1)
					{  output.Append(',');  }

				#if DONT_SHRINK_FILES
				output.AppendLine();
				#endif
				}
			}



		// Group: Variables
		// __________________________________________________________________________
			
		/* var: htmlBuilder
			* The <Builders.HTML> object associated with this menu.
			*/
		protected Builders.HTML htmlBuilder;


		// Group: Constants
		// __________________________________________________________________________

		/* Constant: IndentSpaces
		 * The number of spaces to indent each level by when building the output with <DONT_SHRINK_FILES>.
		 */
		protected int IndentSpaces = 3;




		/* ____________________________________________________________________________
		 * 
		 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTMLMenu.TargetEntryExtraData
		 * ____________________________________________________________________________
		 * 
		 * A class used to store extra information needed by <HTMLMenu> in each menu entry via the 
		 * ExtraData property.
		 * 
		 */
		private class TargetEntryExtraData
			{

			// Group: Functions
			// _________________________________________________________________________

			/* Function: TargetEntryExtraData
			 */
			public TargetEntryExtraData (MenuEntries.Base.Target menuEntry)
				{
				this.menuEntry = menuEntry;
				this.json = null;
				}

			/* Function: GenerateJSON
			 */
			public void GenerateJSON (Builders.HTML htmlBuilder, HTMLMenu menu)
				{
				#if DEBUG
				if ((menuEntry is MenuEntries.File.File) == false)
					{  throw new Exception("HTMLMenu can only generate JSON for target entries that are files.");  }
				#endif

				StringBuilder output = new StringBuilder();

				output.Append("[1,\"");

				string htmlTitle = menuEntry.Title.ToHTML();
				output.StringEscapeAndAppend(htmlTitle);

				output.Append('"');

				string hashPath = htmlBuilder.Source_OutputFileNameOnlyHashPath( (menuEntry as MenuEntries.File.File).WrappedFile.FileName );

				if (hashPath != htmlTitle)
					{
					output.Append(",\"");
					output.StringEscapeAndAppend(hashPath);
					output.Append('"');
					}

				output.Append(']');

				json = output.ToString();
				}


			// Group: Properties
			// _________________________________________________________________________

			/* Property: JSON
			 * After <GenerateJSON()> is called, this is the JSON output for this entry.
			 */
			public string JSON
				{
				get
					{  return json;  }
				}


			// Group: Variables
			// _________________________________________________________________________

			/* var: menuEntry
			 * The menu entry associated with this object.
			 */
			protected MenuEntries.Base.Target menuEntry;

			/* var: json
			 * The generated JSON for this entry.
			 */
			protected string json;

			}



		/* ____________________________________________________________________________
		 * 
		 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTMLMenu.ContainerEntryExtraData
		 * ____________________________________________________________________________
		 * 
		 * A class used to store extra information needed by <HTMLMenu> in each menu entry via the 
		 * ExtraData property.
		 * 
		 */
		private class ContainerEntryExtraData
			{

			// Group: Functions
			// _________________________________________________________________________

			public ContainerEntryExtraData (MenuEntries.Base.Container menuEntry)
				{
				this.menuEntry = menuEntry;
				this.jsonBeforeMembers = null;
				this.jsonAfterMembers = null;
				this.dataFileName = null;
				}

			/* Function: GenerateJSON
			 */
			public void GenerateJSON (Builders.HTML htmlBuilder, HTMLMenu menu)
				{
				#if DEBUG
				if (menuEntry is MenuEntries.File.Folder && menuEntry.Parent == null)
					{  throw new Exception("Parent must be defined before generating JSON for a folder.");  }
				#endif

				StringBuilder output = new StringBuilder();

				output.Append("[2,");


				// Title

				if (menuEntry.CondensedTitles == null)
					{
					// xxx This is for single file sources that don't have titles.  This shouldn't be necessary once condensing is turned on.
					if (menuEntry.Title == null)
						{  output.Append("undefined");  }
					else
						{
						output.Append('"');
						output.StringEscapeAndAppend(menuEntry.Title.ToHTML());
						output.Append('"');
						}
					}
				else
					{
					output.Append('[');

					foreach (string condensedTitle in menuEntry.CondensedTitles)
						{
						output.Append('"');
						output.StringEscapeAndAppend(condensedTitle.ToHTML());
						output.Append("\",");
						}

					output.Append('"');
					output.StringEscapeAndAppend(menuEntry.Title.ToHTML());
					output.Append("\"]");
					}


				// Hash path

				output.Append(",\"");

				if (menuEntry is MenuEntries.File.FileSource)
					{
					MenuEntries.File.FileSource fileSourceEntry = (MenuEntries.File.FileSource)menuEntry;
					output.StringEscapeAndAppend( htmlBuilder.Source_OutputFolderHashPath( fileSourceEntry.WrappedFileSource.Number ));
					}
				else if (menuEntry is MenuEntries.File.Folder)
					{
					MenuEntries.Base.Container container = menuEntry.Parent;

					while ((container is MenuEntries.File.FileSource) == false)
						{
						container = container.Parent;

						#if DEBUG
						if (container == null)
							{  throw new Exception ("Couldn't find a file source among the folder's parents when generating JSON.");  }
						#endif
						}

					MenuEntries.File.Folder folderEntry = (MenuEntries.File.Folder)menuEntry;
					MenuEntries.File.FileSource fileSourceEntry = (MenuEntries.File.FileSource)container;

					output.StringEscapeAndAppend( 
						htmlBuilder.Source_OutputFolderHashPath( fileSourceEntry.WrappedFileSource.Number, folderEntry.PathFromFileSource )
						);
					}
				else if (menuEntry == menu.RootFileMenu)
					{
					output.StringEscapeAndAppend(Builders.HTML.Source_HashPathPrefix());
					}
				#if DEBUG
				else
					{  throw new Exception ("Could not generate JSON for container " + menuEntry.Title + ".");  }
				#endif

				output.Append("\",");

				jsonBeforeMembers = output.ToString();
				jsonAfterMembers = "]";
				}


			// Group: Properties
			// _________________________________________________________________________

			/* Property: StartsNewDataFile
			 * Whether this container starts a new data file.  This property is read-only.  If you need to change
			 * it, set <DataFileName> instead.
			 */
			public bool StartsNewDataFile
				{
				get
					{  return (dataFileName != null);  }
				}

			/* Property: DataFileName
			 * If this container starts a new data file this will be its file name, such as "files2.js" or "classes.js".  It will
			 * not include a path.  If this container doesn't start a new data file, this will be null.
			 */
			public string DataFileName
				{
				get
					{  return dataFileName;  }
				set
					{  dataFileName = value;  }
				}

			/* Property: JSONBeforeMembers
			 * After <GenerateJSON()> is called, this will be the JSON output of this entry up to the point where its members
			 * would appear.
			 */
			public string JSONBeforeMembers
				{
				get
					{  return jsonBeforeMembers;  }
				}

			/* Property: JSONAfterMembers
			 * After <GenerateJSON()> is called, this will be the JSON output of this entry after the point where its members
			 * would appear.
			 */
			public string JSONAfterMembers
				{
				get
					{  return jsonAfterMembers;  }
				}


			// Group: Variables
			// _________________________________________________________________________

			/* var: menuEntry
			 * The menu entry associated with this object.
			 */
			protected MenuEntries.Base.Container menuEntry;

			/* var: jsonBeforeMembers
			 * The generated JSON for this entry, up to the point where its members would be inserted.
			 */
			protected string jsonBeforeMembers;

			/* var: jsonAfterMembers
			 * The generated JSON for this entry, after the point where its members would be inserted.
			 */
			protected string jsonAfterMembers;

			/* var: dataFileName
			 * If this container starts a new data file this will be its file name, such as "files2.js" or "classes.js".  It will
			 * not include a path.  If this container doesn't start a new data file, this will be null.
			 */
			protected string dataFileName;

			}

		}
	}

