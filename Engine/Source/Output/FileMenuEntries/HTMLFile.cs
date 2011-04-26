/* 
 * Class: GregValure.NaturalDocs.Engine.Output.FileMenuEntries.HTMLFile
 * ____________________________________________________________________________
 * 
 * Represents a file in a <HTMLFileMenu>.  Extra fields are added to help output generation.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;


namespace GregValure.NaturalDocs.Engine.Output.FileMenuEntries
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
			string hashPath = htmlBuilder.Source_OutputFileNameOnlyHashPath(FileName);

			Builders.HTML.FileMenuEntryType type;

			if (hashPath == htmlName)
				{  type = Builders.HTML.FileMenuEntryType.ImplicitFile;  }
			else
				{  type = Builders.HTML.FileMenuEntryType.ExplicitFile;  }

			output.Append('[');
			output.Append((int)type);
			output.Append(",\"");
			output.Append( TextConverter.EscapeStringChars(htmlName) );
			output.Append('"');

			if (type == Builders.HTML.FileMenuEntryType.ExplicitFile)
				{
				output.Append(",\"");
				output.Append( TextConverter.EscapeStringChars(hashPath) );
				output.Append('"');
				}

			output.Append(']');

			json = output.ToString();
			}

		public void AppendJSON (StringBuilder output, Stack<FileMenuEntries.HTMLRootFolder> rootFolders)
			{
			#if DONT_SHRINK_FILES
				HTMLFileMenu.AppendJSONIndent(this, output);
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