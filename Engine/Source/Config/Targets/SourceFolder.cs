/*
 * Class: CodeClear.NaturalDocs.Engine.Config.Targets.SourceFolder
 * ____________________________________________________________________________
 *
 * The configuration of a source folder input target.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Engine.Config.Targets
	{
	public partial class SourceFolder : Targets.Input
		{

		// Group: Functions
		// __________________________________________________________________________


		public SourceFolder (PropertyLocation propertyLocation) : base (Files.InputType.Source, propertyLocation)
			{
			folder = null;
			name = null;

			folderPropertyLocation = PropertySource.NotDefined;
			namePropertyLocation = PropertySource.NotDefined;

			repositoryName = null;
			repositoryBranch = null;
			repositoryProjectURL = null;
			repositorySourceURLTemplate = null;

			repositoryNamePropertyLocation = PropertySource.NotDefined;
			repositoryBranchPropertyLocation = PropertySource.NotDefined;
			repositoryProjectURLPropertyLocation = PropertySource.NotDefined;
			repositorySourceURLTemplatePropertyLocation = PropertySource.NotDefined;
			}

		public SourceFolder (SourceFolder toCopy) : base (toCopy)
			{
			folder = toCopy.folder;
			name = toCopy.name;

			folderPropertyLocation = toCopy.folderPropertyLocation;
			namePropertyLocation = toCopy.namePropertyLocation;

			repositoryName = toCopy.repositoryName;
			repositoryBranch = toCopy.repositoryBranch;
			repositoryProjectURL = toCopy.repositoryProjectURL;
			repositorySourceURLTemplate = toCopy.repositorySourceURLTemplate;

			repositoryNamePropertyLocation = toCopy.repositoryNamePropertyLocation;
			repositoryBranchPropertyLocation = toCopy.repositoryBranchPropertyLocation;
			repositoryProjectURLPropertyLocation = toCopy.repositoryProjectURLPropertyLocation;
			repositorySourceURLTemplatePropertyLocation = toCopy.repositorySourceURLTemplatePropertyLocation;
			}

		override public Input Duplicate ()
			{
			return new SourceFolder(this);
			}

		override public bool IsSameTarget (Input other)
			{
			if ((other is SourceFolder) == false)
				{  return false;  }

			return (folder == (other as SourceFolder).folder);
			}

		public override bool Validate (ErrorList errorList, int targetIndex)
			{
			bool valid = true;

			if (System.IO.Directory.Exists(folder) == false)
				{
				errorList.Add( Locale.Get("NaturalDocs.Engine", "Project.txt.SourceFolderDoesNotExist(folder)", folder),
									 folderPropertyLocation,
									 "InputTargets[" + targetIndex + "].Folder" );
				valid = false;
				}

			if (HasCharacterEncodingRules)
				{
				foreach (var encodingRule in CharacterEncodingRules)
					{
					if (encodingRule.ValidateAndLookupID(errorList) == false)
						{  valid = false;  }

					if (encodingRule.Folder != null &&
						encodingRule.Folder != folder &&
						folder.Contains(encodingRule.Folder) == false)
						{
						errorList.Add( Locale.Get("NaturalDocs.Engine", "Project.txt.EncodingFolderNotPartOfSourceFolder(ruleFolder, sourceFolder)",
															 encodingRule.Folder, folder),
											 encodingRule.PropertyLocation );
						valid = false;
						}
					}
				}

			return valid;
			}

		public void GenerateDefaultName ()
			{
			string prefix;
			List<string> segments;

			folder.Split(out prefix, out segments);

			if (segments.Count > 0)
				{
				// The name gets set to the last segment by default.  However, we also walk down the list to see if there are any
				// that don't match the ignored segment regex.  If there is, we use that instead.
				int nameIndex = segments.Count - 1;
				name = segments[nameIndex];

				while (nameIndex >= 0 && IsIgnoredSourceFolderSegmentRegex().IsMatch(segments[nameIndex]))
					{  nameIndex--;  }

				if (nameIndex >= 0)
					{  name = segments[nameIndex];  }
				else
					{  name = segments[segments.Count - 1];  }
				}
			else
				{  name = folder;  }

			namePropertyLocation = PropertySource.SystemGenerated;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Folder
		 * The <AbsolutePath> that should be searched for source files.
		 */
		public AbsolutePath Folder
			{
			get
				{  return folder;  }
			set
				{  folder = value;  }
			}


		/* Property: Name
		 * The name of the input target, or null if it isn't defined.  Names are used to distinguish multiple file sources in user-visible
		 * places such as menus.  They should ideally be unique.
		 */
		public string Name
			{
			get
				{  return name;  }
			set
				{  name = value;  }
			}


		/* Property: HasRepositoryInfo
		 * A simple way to determine if any of the repository properties are defined.
		 */
		public bool HasRepositoryInfo
			{
			get
				{
				// This is the one that must be defined, so this is all we need to check.
				return repositoryProjectURLPropertyLocation.IsDefined;
				}
			}


		/* Property: RepositoryName
		 * The name of the repository the source code is in, or null if it's not set.  This is the name of the site, such as GitHub, rather
		 * than the name of the project.
		 */
		public string RepositoryName
			{
			get
				{  return repositoryName;  }
			set
				{  repositoryName = value;  }
			}


		/* Property: RepositoryBranch
		 * The name of the repository branch to use for links, or null if it's not set.
		 */
		public string RepositoryBranch
			{
			get
				{  return repositoryBranch;  }
			set
				{  repositoryBranch = value;  }
			}


		/* Property: RepositoryProjectURL
		 * The URL for the project in the repository, or null if it's not set.
		 */
		public string RepositoryProjectURL
			{
			get
				{  return repositoryProjectURL;  }
			set
				{  repositoryProjectURL = value;  }
			}


		/* Property: RepositorySourceURLTemplate
		 * The URL template for source files in the repository, or null if it's not set.  The template should mark substitution points with
		 * <RepositorySubstitutions.FilePath> and <RepositorySubstitutions.LineNumber>.
		 */
		public string RepositorySourceURLTemplate
			{
			get
				{  return repositorySourceURLTemplate;  }
			set
				{
				repositorySourceURLTemplate = value;
				}
			}



		// Group: Property Locations
		// __________________________________________________________________________


		/* Property: FolderPropertyLocation
		 * Where <Folder> is defined.
		 */
		public PropertyLocation FolderPropertyLocation
			{
			get
				{  return folderPropertyLocation;  }
			set
				{  folderPropertyLocation = value;  }
			}


		/* Property: NamePropertyLocation
		 * Where <Name> is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation NamePropertyLocation
			{
			get
				{  return namePropertyLocation;  }
			set
				{  namePropertyLocation = value;  }
			}


		/* Property: RepositoryNamePropertyLocation
		 * Where the <RepositoryName> property is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation RepositoryNamePropertyLocation
			{
			get
				{  return repositoryNamePropertyLocation;  }
			set
				{  repositoryNamePropertyLocation = value;  }
			}


		/* Property: RepositoryBranchPropertyLocation
		 * Where the <RepositoryBranch> property is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation RepositoryBranchPropertyLocation
			{
			get
				{  return repositoryBranchPropertyLocation;  }
			set
				{  repositoryBranchPropertyLocation = value;  }
			}


		/* Property: RepositoryProjectURLPropertyLocation
		 * Where the <RepositoryProjectURL> property is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation RepositoryProjectURLPropertyLocation
			{
			get
				{  return repositoryProjectURLPropertyLocation;  }
			set
				{  repositoryProjectURLPropertyLocation = value;  }
			}


		/* Property: RepositorySourceURLTemplatePropertyLocation
		 * Where the <RepositorySourceURLTemplate> property is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation RepositorySourceURLTemplatePropertyLocation
			{
			get
				{  return repositorySourceURLTemplatePropertyLocation;  }
			set
				{  repositorySourceURLTemplatePropertyLocation = value;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		protected AbsolutePath folder;
		protected string name;

		protected PropertyLocation folderPropertyLocation;
		protected PropertyLocation namePropertyLocation;

		protected string repositoryName;
		protected string repositoryBranch;
		protected string repositoryProjectURL;
		protected string repositorySourceURLTemplate;

		protected PropertyLocation repositoryNamePropertyLocation;
		protected PropertyLocation repositoryBranchPropertyLocation;
		protected PropertyLocation repositoryProjectURLPropertyLocation;
		protected PropertyLocation repositorySourceURLTemplatePropertyLocation;



		// Group: Regular Expressions
		// __________________________________________________________________________


		/* Regex: IsIgnoredSourceFolderSegmentRegex
		 * Will match if the string is an individual folder name that should be excluded when generating a default source
		 * folder name.  For example, a source folder "C:\Projects\My Project\src" should be called "My Project" instead
		 * of "src".  This regex should be matched against individual folder names ("src") and not on the entire path.
		 */
		[GeneratedRegex("""^(?:source|src|content)$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static private partial Regex IsIgnoredSourceFolderSegmentRegex();

		}
	}
