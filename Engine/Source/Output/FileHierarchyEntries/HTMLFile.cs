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

			output.Append('[');
			output.Append((int)Builders.HTML.FileHierarchyEntryType.File);
			output.Append(",\"");
			output.Append( TextConverter.EscapeStringChars(TextConverter.TextToHTML(FileName)) );
			output.Append("\",\"");
			output.Append( TextConverter.EscapeStringChars(Builders.HTML.OutputFileName(FileName)) );
			output.Append("\"]");

			json = output.ToString();
			}

		public void AppendJSON (StringBuilder output)
			{
			for (var parent = Parent; parent != null; parent = parent.Parent)
				{  output.Append("   ");  }  // xxx

			output.Append(json);

			output.AppendLine(); //xxx
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