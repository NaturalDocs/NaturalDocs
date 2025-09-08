/*
 * Struct: CodeClear.NaturalDocs.Engine.Output.HTML.FileSourceInfo
 * ____________________________________________________________________________
 *
 * A simple struct for storing information about the previous run's <Files.FileSources>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{

	public struct FileSourceInfo
		{
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

		public int Number;
		public Files.InputType Type;
		public string UniqueIDString;
		public string Name;
		}

	}
