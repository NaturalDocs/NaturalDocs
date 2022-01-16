/*
 * Class: CodeClear.NaturalDocs.Engine.Links.ImageLink
 * ____________________________________________________________________________
 *
 * A class encapsulating all the information available about an image link, like "(see image.jpg)".
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Symbols;


namespace CodeClear.NaturalDocs.Engine.Links
	{
	public class ImageLink
		{

		// Group: Types
		// __________________________________________________________________________

		/* Enum: IgnoreFields
		 *
		 * When querying links from the database, not all fields may be needed in all circumstances.  This is a
		 * bitfield that allows you to specify which fields can be ignored.  This is also stored in the object so that,
		 * in debug builds, if you try to access any of these fields an exception will be thrown.
		 */
		[Flags]
		public enum IgnoreFields : byte
			{
			None = 0x00,

			ImageLinkID = 0x01,
			OriginalText = 0x02,
			Path = 0x04,
			FileID = 0x80,
			ClassString = 0x10,
			ClassID = 0x20,
			TargetFileID = 0x40,
			TargetScore = 0x80
			}



		// Group: Functions
		// __________________________________________________________________________


		public ImageLink ()
			{
			imageLinkID = 0;
			originalText = null;
			path = default(Path);
			fileID = 0;
			classString = new ClassString();
			classID = 0;
			targetFileID = 0;
			targetScore = 0;
			ignoredFields = IgnoreFields.None;
			}


		/* Function: SameIdentifyingPropertiesAs
		 * Returns whether the identifying properties of the link (<OriginalText>, <FileID>, <ClassString>) are the same as
		 * the passed one.
		 */
		public bool SameIdentifyingPropertiesAs (ImageLink other)
			{
			return (CompareIdentifyingPropertiesTo(other) == 0);
			}


		/* Function: CompareIdentifyingPropertiesTo
		 *
		 * Compares the identifying properties of the link (<OriginalText>, <FileID>, <ClassString>) and returns a value similar
		 * to a string  comparison result which is suitable for sorting a list of ImageLinks.  It will return zero if all the properties
		 * are equal.
		 */
		public int CompareIdentifyingPropertiesTo (ImageLink other)
			{
			// DEPENDENCY: What CopyNonIdentifyingPropertiesFrom() does depends on what this compares.

			int result = originalText.CompareTo(other.originalText);

			if (result != 0)
				{  return result;  }

			if (fileID != other.fileID)
				{  return (fileID - other.fileID);  }

			if (classString == null)
				{
				if (other.classString == null)
					{  return 0;  }
				else
					{  return -1;  }
				}
			else
				{
				if (other.classString == null)
					{  return 1;  }
				else
					{  return classString.ToString().CompareTo(other.classString.ToString());  }
				}
			}


		/* Function: CopyNonIdentifyingPropertiesFrom
		 * Makes this link copy all the properties not tested by <CompareIdentifyingPropertiesTo()> and
		 * <SameIdentifyingPropertiesAs()> from the passed link.
		 */
		public void CopyNonIdentifyingPropertiesFrom (ImageLink other)
			{
			// DEPENDENCY: What this copies depends on what CompareIdentifyingPropertiesTo() does not.

			imageLinkID = other.imageLinkID;
			path = other.path;
			classID = other.classID;
			targetFileID = other.targetFileID;
			targetScore = other.targetScore;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: ImageLinkID
		 * The link's ID number, or zero if it hasn't been set.
		 */
		public int ImageLinkID
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.ImageLinkID) != 0)
					{  throw new InvalidOperationException("Tried to access ImageLinkID when that field was ignored.");  }
				#endif

				return imageLinkID;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.ImageLinkID) != 0)
					{  throw new InvalidOperationException("Tried to access ImageLinkID when that field was ignored.");  }
				#endif

				imageLinkID = value;
				}
			}


		/* Property: OriginalText
		 * The plain text of the link, such as "(see image.jpg)".
		 */
		public string OriginalText
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.OriginalText) != 0)
					{  throw new InvalidOperationException("Tried to access OriginalText when that field was ignored.");  }
				#endif

				return originalText;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.OriginalText) != 0)
					{  throw new InvalidOperationException("Tried to access OriginalText when that field was ignored.");  }
				#endif

				originalText = value;
				}
			}


		/* Property: Path
		 * The path of the link.  For "(see folder\image.jpg)", this would be "folder\image.jpg".
		 */
		public Path Path
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Path) != 0)
					{  throw new InvalidOperationException("Tried to access Path when that field was ignored.");  }
				#endif

				return path;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Path) != 0)
					{  throw new InvalidOperationException("Tried to access Path when that field was ignored.");  }
				#endif

				path = value;
				}
			}


		/* Property: FileName
		 * The file name of the link.  For "(see folder\image.jpg)", this would be "image.jpg".
		 */
		public Path FileName
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.Path) != 0)
					{  throw new InvalidOperationException("Tried to access FileName when Path was ignored.");  }
				#endif

				return path.NameWithoutPath;
				}
			}


		/* Property: FileID
		 * The ID number of the file that defines this link
		 */
		public int FileID
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.FileID) != 0)
					{  throw new InvalidOperationException("Tried to access FileID when that field was ignored.");  }
				#endif

				return fileID;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.FileID) != 0)
					{  throw new InvalidOperationException("Tried to access FileID when that field was ignored.");  }
				#endif

				fileID = value;
				}
			}


		/* Property: ClassString
		 * The class the link appears in.
		 */
		public Symbols.ClassString ClassString
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.ClassString) != 0)
					{  throw new InvalidOperationException("Tried to access ClassString when that field was ignored.");  }
				#endif

				return classString;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.ClassString) != 0)
					{  throw new InvalidOperationException("Tried to access ClassString when that field was ignored.");  }
				#endif

				classString = value;
				}
			}


		/* Property: ClassIDKnown
		 * Whether <ClassID> is known, which basically tests whether <ClassID> is zero when <ClassString> is not null.
		 */
		public bool ClassIDKnown
			{
			get
				{  return (classID != 0 || classString == null);  }
			}


		/* Property: ClassID
		 * The ID of the class that defines this link.
		 */
		public int ClassID
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.ClassID) != 0)
					{  throw new InvalidOperationException("Tried to access ClassID when that field was ignored.");  }
				#endif

				return classID;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.ClassID) != 0)
					{  throw new InvalidOperationException("Tried to access ClassID when that field was ignored.");  }
				#endif

				classID = value;
				}
			}


		/* Property: IsResolved
		 * Whether the link has a target topic.  Is equivalent to testing whether <TargetFileID> is zero.
		 */
		public bool IsResolved
			{
			get
				{  return (targetFileID != 0);  }
			}


		/* Property: TargetFileID
		 * The ID number of the file the link resolves to, or zero if none.
		 */
		public int TargetFileID
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TargetFileID) != 0)
					{  throw new InvalidOperationException("Tried to access TargetFileID when that field was ignored.");  }
				#endif

				return targetFileID;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TargetFileID) != 0)
					{  throw new InvalidOperationException("Tried to access TargetFileID when that field was ignored.");  }
				#endif

				targetFileID = value;
				}
			}


		/* Property: TargetScore
		 * If <TargetFileID> is set, the numeric score of the match.
		 */
		public int TargetScore
			{
			get
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TargetScore) != 0)
					{  throw new InvalidOperationException("Tried to access TargetScore when that field was ignored.");  }
				#endif

				return targetScore;
				}
			set
				{
				#if DEBUG
				if ((ignoredFields & IgnoreFields.TargetScore) != 0)
					{  throw new InvalidOperationException("Tried to access TargetScore when that field was ignored.");  }
				#endif

				targetScore = value;
				}
			}


		/* Property: IgnoredFields
		 *
		 * When querying links from the database, not all fields may be needed in all situations.  The database
		 * may accept <IgnoreFields> flags to skip retrieving parts of them.  If that's done, the flags should also
		 * be set here so that in debug builds an exception will be thrown if you try to access those properties.
		 *
		 * IgnoredFields defaults to <IgnoreFields.None> so that links created by parsing don't have to worry
		 * about them.
		 *
		 */
		public IgnoreFields IgnoredFields
			{
			get
				{  return ignoredFields;  }
			set
				{  ignoredFields = value;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: imageLinkID
		 * The links's ID number, or zero if not specified.
		 */
		protected int imageLinkID;

		/* var: originalText
		 * The plain text of the link, such as "(see image.jpg)"
		 */
		protected string originalText;

		/* var: path
		 * The path of the link.  In "(see folder\image.jpg)" this would be "folder\image.jpg".
		 */
		protected Path path;

		/* var: fileID
		 * The ID number of the file that defines this link.
		 */
		protected int fileID;

		/* var: classString
		 * The class this link appears in.
		 */
		protected Symbols.ClassString classString;

		/* var: classID
		 * The ID of <classString> if known, or zero if not.
		 */
		protected int classID;

		/* var: targetFileID
		 * The ID number of the file the link resolves to, or zero if none.
		 */
		protected int targetFileID;

		/* var: targetScore
		 * If <targetFileID> is set, the numeric score of the match.
		 */
		protected int targetScore;

		/* var: ignoredFields
		 * The <IgnoreFields> applied to this object.
		 */
		protected IgnoreFields ignoredFields;
		}
	}
