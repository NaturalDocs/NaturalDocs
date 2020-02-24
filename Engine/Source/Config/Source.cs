/* 
 * Enum: CodeClear.NaturalDocs.Engine.Config.Source
 * ____________________________________________________________________________
 * 
 * A configuration source.
 * 
 * Values:
 * 
 *		NotDefined - The property is not defined.
 *		SystemDefault - The property was not defined and is using the system default value.
 *		SystemGenerated - The property was not defined and the system generated a value.  This differs from <SystemDefault>
 *								   in that it should be saved to the config file even though it was not specified by the user.
 *		CommandLine - The property is defined on the command line.
 *		Combined - The properties were combined from multiple sources.  This should only be used for entire configurations and
 *						not for individual properties.
 *		PreviousRun - The property comes from the previous run, such as being stored in <Project.nd>.
 *		ProjectFile - The property is defined in <Project.txt>.
 *		OldMenuFile - The property is defined in <Menu.txt>.
 *		ProjectLanguageFile - The property is defined in the project's <Languages.txt> file.
 *		SystemLanguageFile - The property is defined in the system's <Languages.txt> file.
 *		ProjectTopicsFile - The property is defined in the project's <Comments.txt> file.
 *		SystemTopicsFile - The property is defined in the system's <Comments.txt> file.
 *		
 * Meta-Values:
 * 
 *		LowestFileValue - Any value between or equal to this and <HighestFileValue> comes from a file such as <Project.txt> as 
 *								opposed to something like the command line.
 *		HighestFileValue - Any value between or equal to this and <LowestFileValue> comes from a file such as <Project.txt> as 
 *								opposed to something like the command line.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Config
	{
	public enum Source : byte
		{
		NotDefined = 0,
		SystemDefault,
		SystemGenerated,
		CommandLine,
		Combined,
		PreviousRun,

		ProjectFile,
		OldMenuFile,
		ProjectLanguageFile,
		SystemLanguageFile,
		ProjectTopicsFile,
		SystemTopicsFile,

		LowestFileValue = ProjectFile,
		HighestFileValue = SystemTopicsFile
		}
	}