/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.ConfigFiles.FileSourceConfig
 * ____________________________________________________________________________
 *
 * A simple class for storing information about the previous run's <Files.FileSources>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.ConfigFiles
	{

	public class FileSourceConfig
		{

		// Group: Functions
		// __________________________________________________________________________

		public FileSourceConfig ()
			{
			Number = 0;
			Type = Files.InputType.Source;
			UniqueIDString = null;
			Name = null;
			}

		public bool IsSameFundamentalFileSource (Files.FileSource other)
			{
			return (Number == other.Number &&
					   Type == other.Type &&
					   UniqueIDString == other.UniqueIDString);
			}

		public void CopyFrom (Files.FileSource other)
			{
			Number = other.Number;
			Type = other.Type;
			UniqueIDString = other.UniqueIDString;
			Name = other.Name;
			}


		// Group: Variables
		// __________________________________________________________________________

		public int Number;
		public Files.InputType Type;
		public string UniqueIDString;
		public string Name;

		}
	}
