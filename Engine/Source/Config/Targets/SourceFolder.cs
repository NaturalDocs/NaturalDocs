/*
 * Class: CodeClear.NaturalDocs.Engine.Config.Targets.SourceFolder
 * ____________________________________________________________________________
 *
 * The configuration of a source folder input target.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Engine.Config.Targets
	{
	public class SourceFolder : Targets.Input
		{

		// Group: Functions
		// __________________________________________________________________________


		public SourceFolder (PropertyLocation propertyLocation) : base (Files.InputType.Source, propertyLocation)
			{
			folder = null;
			name = null;
			url = null;

			folderPropertyLocation = PropertySource.NotDefined;
			namePropertyLocation = PropertySource.NotDefined;
			urlPropertyLocation = PropertySource.NotDefined;
			}

		public SourceFolder (SourceFolder toCopy) : base (toCopy)
			{
			folder = toCopy.folder;
			name = toCopy.name;
			url = toCopy.url;

			folderPropertyLocation = toCopy.folderPropertyLocation;
			namePropertyLocation = toCopy.namePropertyLocation;
			urlPropertyLocation = toCopy.urlPropertyLocation;
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

				Regex.Config.DefaultNameIgnoredSegment ignoredSegmentRegex = new Regex.Config.DefaultNameIgnoredSegment();

				while (nameIndex >= 0 && ignoredSegmentRegex.IsMatch(segments[nameIndex]))
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

		/* Property: Url
		 * 
		 */
		public string Url
			{
			get
				{  return url;  }
			set
				{  url = value;  }
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

		/* Property: UrlPropertyLocation
		 * Where <Url> is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation UrlPropertyLocation
		    {
		    get
		        {  return urlPropertyLocation;  }
		    set
		        {  urlPropertyLocation = value;  }
		    }


		// Group: Variables
		// __________________________________________________________________________


		protected AbsolutePath folder;
		protected string name;
		protected string url;

		protected PropertyLocation folderPropertyLocation;
		protected PropertyLocation namePropertyLocation;
		protected PropertyLocation urlPropertyLocation;

		}
	}
