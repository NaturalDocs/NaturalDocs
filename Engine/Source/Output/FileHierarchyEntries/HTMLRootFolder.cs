/* 
 * Class: GregValure.NaturalDocs.Engine.Output.FileHierarchyEntries.HTMLRootFolder
 * ____________________________________________________________________________
 * 
 * Represents the root folder in a <HTMLFileHierarchy>.  This may be the bottom root that contains the
 * entire hierarchy, or additional roots added to create dynamic folders.  Extra fields are added to help 
 * output generation.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;


namespace GregValure.NaturalDocs.Engine.Output.FileHierarchyEntries
	{
	public class HTMLRootFolder : RootFolder, IHTMLEntry
		{

		// Group: Functions
		// __________________________________________________________________________

		public HTMLRootFolder () : base ()
			{
			jsonPath = null;
			}

		public void PrepareJSON (Builders.HTML htmlBuilder)
			{
			// The JSON path is only relevant for root folders used to create dynamic folders.  It will remain null for the
			// bottom root folder.
			if (Parent != null)
				{
				Path fullPath = new Path();
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
			}


		public void AppendJSON (StringBuilder output)
			{
			if (MergeWithFileSource)
				{  return;  }

			for (var parent = Parent; parent != null; parent = parent.Parent)
				{  output.Append("   ");  }  // xxx

			output.Append('[');
			output.Append((int)Builders.HTML.FileHierarchyEntryType.RootFolder);
			output.Append(',');
			output.Append(ID);
			output.Append(',');

			if (jsonPath != null)
				{  output.Append(jsonPath);  }
			else
				{  output.Append("undefined");  }

			output.Append(',');
			output.Append("[xxx]");
			output.Append(']');

			output.AppendLine();//xxx
			}


		// Group: Properties
		// __________________________________________________________________________

		/* Property: MergeWithFileSource
		 * If true, the folder only contains one entry and it's a <HTMLFileSource>, and thus this entry shouldn't generate
		 * a JSON tag.  The <HTMLFileSource> will so that the properties are combined.
		 */
		public bool MergeWithFileSource
			{
			get
				{  return (Members.Count == 1 && Members[0] is HTMLFileSource);  }
			}

		public int JSONTagLength
			{
			get
				{  
				if (MergeWithFileSource)
					{  return 0;  }
				else
					{
					// [#,#,[path],[members]] = 7 for id, root id, commas, and brackets
					// +2 for member brackets, + separating commas
					int length = 9 + (Members.Count - 1);

					if (jsonPath != null)
						{  length += jsonPath.Length;  }
					else
						{  
						// +9 for "undefined"
						length += 9;
						}

					return length;
					}
				}
			}


		// Group: Variables
		// __________________________________________________________________________

		protected string jsonPath;

		}
	}