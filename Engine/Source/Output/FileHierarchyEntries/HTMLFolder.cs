/* 
 * Class: GregValure.NaturalDocs.Engine.Output.FileHierarchyEntries.HTMLFolder
 * ____________________________________________________________________________
 * 
 * Represents a folder or group of folders in a <FileHierarchy>.  It will only represent a group of folders
 * ("FolderA/FolderB") if the parent folder contains nothing other than the child folder.  Extra fields are added 
 * to help output generation.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;


namespace GregValure.NaturalDocs.Engine.Output.FileHierarchyEntries
	{
	public class HTMLFolder : Folder, IHTMLEntry
		{

		// Group: Functions
		// __________________________________________________________________________

		public HTMLFolder (Path newPathFragment) : base (newPathFragment)
			{
			jsonName = null;
			jsonPath = null;
			}

		public void PrepareJSON (Builders.HTML htmlBuilder)
			{
			// JSON Name

			string pathPrefix;
			List<string> pathSegments;
			PathFragment.Split(out pathPrefix, out pathSegments);

			StringBuilder nameBuilder = new StringBuilder();

			if (pathSegments.Count > 1)
				{  nameBuilder.Append('[');  }

			for (int i = 0; i < pathSegments.Count; i++)
				{
				if (i > 0)
					{  nameBuilder.Append(',');  }

				nameBuilder.Append('"');
				nameBuilder.Append( TextConverter.EscapeStringChars(TextConverter.TextToHTML(pathSegments[i])) );
				nameBuilder.Append('"');
				}

			if (pathSegments.Count > 1)
				{  nameBuilder.Append(']');  }

			jsonName = nameBuilder.ToString();


			// JSON Path

			Path fullPath = PathFragment;
			Entry parent = Parent;

			// Build the full path by walking all the way down to the file source.  We have to handle both folders and root folder 
			// entries added to make dynamic folders.
			while ((parent is FileSource) == false)
				{
				if (parent is Folder)
					{  fullPath = (parent as Folder).PathFragment + '/' + fullPath;  }

				parent = parent.Parent;
				}

			FileSource fileSource = (FileSource)parent;
			
			if (fileSource.PathFragment != null)
				{  fullPath = fileSource.PathFragment + '/' + fullPath;  }

			fullPath = htmlBuilder.OutputPath(fileSource.WrappedFileSource, fullPath, 
																		 Builders.HTML.SourcePathType.FolderOnly,
																		 Builders.HTML.OutputPathType.RelativeToRootOutputFolder);
			jsonPath = '"' + TextConverter.EscapeStringChars(fullPath.ToURL()) + '"';
			}


		public void AppendJSON (StringBuilder output, List<FileHierarchyEntries.HTMLRootFolder> rootFolders)
			{
			#if DONT_SHRINK_FILES
				HTMLFileHierarchy.AppendJSONIndent(this, output);
			#endif

			output.Append('[');
			
			if (IsDynamicFolder)
				{  output.Append((int)Builders.HTML.FileHierarchyEntryType.DynamicFolder);  }
			else  // Inline
				{  output.Append((int)Builders.HTML.FileHierarchyEntryType.InlineFolder);  }

			output.Append(',');
			output.Append(jsonName);
			output.Append(',');
			output.Append(jsonPath);
			output.Append(',');

			if (IsDynamicFolder)
				{  output.Append(DynamicMembersID);  }
			else // Inline
				{
				output.Append('[');

				#if DONT_SHRINK_FILES
					output.AppendLine();
				#endif

				for (int i = 0; i < Members.Count; i++)
					{
					if (i > 0)
						{
						output.Append(',');

						#if DONT_SHRINK_FILES
							output.AppendLine();
						#endif
						}

					(Members[i] as IHTMLEntry).AppendJSON(output, rootFolders);
					}

				#if DONT_SHRINK_FILES
					output.AppendLine();
					HTMLFileHierarchy.AppendJSONIndent(Members[0], output);
				#endif

				output.Append(']');
				}

			output.Append(']');
			}


		// Group: Properties
		// __________________________________________________________________________

		/* Property: IsDynamicFolder
		 * Whether this folder dynamically loads its members as opposed to storing them inline.  The folder will
		 * be dynamic if it only contains a single entry and it's a <RootFolder>.
		 */
		public bool IsDynamicFolder
			{
			get
				{  
				return (Members.Count == 1 && Members[0] is RootFolder);
				}
			}

		/* Property: IsInlineFolder
		 * Whether this folder stores its members inline as opposed to loading them dynamically.
		 */
		public bool IsInlineFolder
			{
			get
				{  return !IsDynamicFolder;  }
			}

		/* Property: DynamicMembersID
		 * If <IsDynamicFolder> is true, the ID number that should be used in place of the members.
		 */
		public int DynamicMembersID
			{
			get
				{  
				if (IsDynamicFolder)
					{  return (Members[0] as RootFolder).ID;  }
				else
					{  return 0;  }
				}
			}

		public int JSONTagLength
			{
			get
				{  
				// [#,[name],[path],[members]] = 6 for id, commas, and brackets
				// +2 for member brackets or number
				int tagLength = jsonName.Length + jsonPath.Length + 8;

				if (IsInlineFolder)
					{
					// Separating commas  
					tagLength += Members.Count - 1;
					}

				return tagLength;
				}
			}



		// Group: Variables
		// __________________________________________________________________________

		protected string jsonName;
		protected string jsonPath;

		}
	}