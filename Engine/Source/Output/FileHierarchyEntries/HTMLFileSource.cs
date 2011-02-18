/* 
 * Class: GregValure.NaturalDocs.Engine.Output.FileHierarchyEntries.HTMLFileSource
 * ____________________________________________________________________________
 * 
 * Represents a file source in a <HTMLFileHierarchy>.  Extra fields are added to help output generation.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;


namespace GregValure.NaturalDocs.Engine.Output.FileHierarchyEntries
	{
	public class HTMLFileSource : FileSource, IHTMLEntry
		{

		// Group: Functions
		// __________________________________________________________________________

		public HTMLFileSource (Files.FileSource newFileSource) : base (newFileSource)
			{
			jsonName = null;
			jsonPath = null;
			}

		public void PrepareJSON (Builders.HTML htmlBuilder)
			{
			// JSON Name

			if (!MergeWithRoot)
				{
				jsonName = '"' + TextConverter.EscapeStringChars(TextConverter.TextToHTML(WrappedFileSource.Name)) + '"';
				}


			// JSON Path

			Path fullPath = htmlBuilder.OutputPath(WrappedFileSource, pathFragment, 
																				 Builders.HTML.SourcePathType.FolderOnly,
																				 Builders.HTML.OutputPathType.RelativeToRootOutputFolder);
			jsonPath = '"' + TextConverter.EscapeStringChars(fullPath.ToURL()) + '"';
			}


		public void AppendJSON (StringBuilder output)
			{
			for (var parent = Parent; parent != null; parent = parent.Parent)
				{  output.Append("   ");  }  // xxx

			output.Append('[');
			
			if (!MergeWithRoot)
				{  
				output.Append((int)Builders.HTML.FileHierarchyEntryType.InlineFolder);  
				output.Append(',');
				output.Append(jsonName);
				}
			else
				{  
				output.Append((int)Builders.HTML.FileHierarchyEntryType.RootFolder);
				output.Append(',');
				output.Append( (Parent as FileHierarchyEntries.RootFolder).ID );
				}

			output.Append(',');
			output.Append(jsonPath);
			output.Append(',');
			output.Append("[xxx]");
			output.Append(']');
			output.AppendLine();//xxx
			}

		// Group: Properties
		// __________________________________________________________________________

		/* Property: MergeWithRoot
		 * If this is true, this class will generate a root folder JSON tag instead of an inline folder tag.  This is
		 * used when the root folder only has one file source and it's not worth making a separate entry for.
		 */
		public bool MergeWithRoot
			{
			get
				{  return ((Parent as HTMLRootFolder).MergeWithFileSource);  }
			}

		public int JSONTagLength
			{
			get
				{  
				if (!MergeWithRoot)
					{
					// [#,[name],[path],[members]] = 6 for id, commas, and brackets
					// +2 for member brackets, + separating commas
					return jsonName.Length + jsonPath.Length + 8 + (Members.Count - 1);
					}
				else
					{
					// [#,#,[path],[members]] = 7 for id, root id, commas, and brackets
					// +2 for member brackets, + separating commas
					return jsonPath.Length + 9 + (Members.Count - 1);
					}
				}
			}


		// Group: Variables
		// __________________________________________________________________________

		protected string jsonName;
		protected string jsonPath;

		}
	}