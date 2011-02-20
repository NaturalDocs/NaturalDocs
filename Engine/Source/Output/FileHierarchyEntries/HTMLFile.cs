/* 
 * Class: GregValure.NaturalDocs.Engine.Output.FileHierarchyEntries.HTMLFile
 * ____________________________________________________________________________
 * 
 * Represents a file in a <HTMLFileHierarchy>.  Extra fields are added to help output generation.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;


namespace GregValure.NaturalDocs.Engine.Output.FileHierarchyEntries
	{
	public class HTMLFile : File, IHTMLEntry
		{

		// Group: Functions
		// __________________________________________________________________________

		public HTMLFile (Path newFileName) : base (newFileName)
			{
			json = null;
			}

		public void PrepareJSON (Builders.HTML htmlBuilder)
			{
			StringBuilder output = new StringBuilder();

			string htmlName = TextConverter.TextToHTML(FileName);
			string path = Builders.HTML.OutputFileName(FileName);
			Builders.HTML.FileHierarchyEntryType type;

			string htmlNameTranslation = htmlName.Replace('.', '-') + ".html";
			if (htmlNameTranslation == path)
				{  type = Builders.HTML.FileHierarchyEntryType.ImplicitFile;  }
			else
				{  type = Builders.HTML.FileHierarchyEntryType.ExplicitFile;  }

			output.Append('[');
			output.Append((int)type);
			output.Append(",\"");
			output.Append( TextConverter.EscapeStringChars(htmlName) );
			output.Append('"');

			if (type == Builders.HTML.FileHierarchyEntryType.ExplicitFile)
				{
				output.Append(",\"");
				output.Append( TextConverter.EscapeStringChars(path) );
				output.Append('"');
				}

			output.Append(']');

			json = output.ToString();
			}

		public void AppendJSON (StringBuilder output, List<FileHierarchyEntries.HTMLRootFolder> rootFolders)
			{
			#if DONT_SHRINK_FILES
				HTMLFileHierarchy.AppendJSONIndent(this, output);
			#endif

			output.Append(json);
			}


		// Group: Properties
		// __________________________________________________________________________

		public int JSONTagLength
			{
			get
				{  return json.Length;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		protected string json;

		}
	}