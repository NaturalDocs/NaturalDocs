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
			hashPath = null;
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


			// Hash Path

			string pathString = PathFragment;
			Entry parent = Parent;

			// Build the full path by walking all the way down to the file source.  We have to handle both folders and root folder 
			// entries added to make dynamic folders.
			while ((parent is FileSource) == false)
				{
				if (parent is Folder)
					{  pathString = (parent as Folder).PathFragment + '/' + pathString;  }

				parent = parent.Parent;
				}

			FileSource fileSource = (FileSource)parent;
			
			if (fileSource.PathFragment != null)
				{  pathString = fileSource.PathFragment + '/' + pathString;  }

			hashPath = htmlBuilder.Source_OutputFolderHashPath(fileSource.WrappedFileSource.Number, pathString);
			hashPath = '"' + TextConverter.EscapeStringChars(hashPath) + '"';
			}


		public void AppendJSON (StringBuilder output, Stack<FileHierarchyEntries.HTMLRootFolder> rootFolders)
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
			output.Append(hashPath);
			output.Append(',');

			if (IsDynamicFolder)
				{  
				output.Append(DynamicMembersID);  
				rootFolders.Push((HTMLRootFolder)Members[0]);
				}
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

		public int JSONTagLength
			{
			get
				{  
				// [#,[name],[path],[members]] = 6 for id, commas, and brackets
				// +2 for member brackets or number
				int tagLength = jsonName.Length + hashPath.Length + 8;

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
		protected string hashPath;

		}
	}