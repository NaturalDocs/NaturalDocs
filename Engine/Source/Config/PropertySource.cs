/*
 * Enum: CodeClear.NaturalDocs.Engine.Config.PropertySource
 * ____________________________________________________________________________
 *
 * The source of a configuration property.
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
 *		ParserConfigurationFile - The property is defined in <Parser.txt>.
 *		ProjectLanguagesFile - The property is defined in the project's <Languages.txt> file.
 *		SystemLanguagesFile - The property is defined in the system's <Languages.txt> file.
 *		ProjectCommentsFile - The property is defined in the project's <Comments.txt> file.
 *		SystemCommentsFile - The property is defined in the system's <Comments.txt> file.
 *		StyleConfigurationFile - The property is defined in a <Style.txt> file.
 *
 * Meta-Values:
 *
 *		LowestFileValue - Any value between or equal to this and <HighestFileValue> comes from a file such as <Project.txt> as
 *								opposed to something like the command line.
 *		HighestFileValue - Any value between or equal to this and <LowestFileValue> comes from a file such as <Project.txt> as
 *								opposed to something like the command line.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Config
	{
	public enum PropertySource : byte
		{
		NotDefined = 0,
		SystemDefault,
		SystemGenerated,
		CommandLine,
		Combined,
		PreviousRun,

		ProjectFile,
		OldMenuFile,
		ParserConfigurationFile,
		ProjectLanguagesFile,
		SystemLanguagesFile,
		ProjectCommentsFile,
		SystemCommentsFile,
		StyleConfigurationFile,

		LowestFileValue = ProjectFile,
		HighestFileValue = StyleConfigurationFile
		}
	}
