/*
 * Struct: CodeClear.NaturalDocs.Engine.Version
 * ____________________________________________________________________________
 *
 * A struct for dealing with version information in its various formats.
 *
 *
 * Topic: String Format
 *
 *		Version numbers should be stored as strings, even in binary contexts.  The format of the strings will change
 *		over time and needs more flexibility than a set binary format can provide.
 *
 *		Full Releases:
 *
 *			> 2.0
 *			> 2.0.1
 *			> 2.2
 *
 *			Full releases are in major.minor.bugfix format.  The bugfix part can be omitted, but not the minor part.
 *			Both the minor and bugfix parts are integers as opposed to decimals, meaning 1.22 > 1.3.  Neither can be
 *			greater than 99.
 *
 *		Interim Releases:
 *
 *			> [version] ([status] [mm]-[dd]-[yyyy])
 *			> [version] ([status] [count])
 *			>
 *			> 2.0 (Development Release 01-01-2007)
 *			> 2.0.1 (Release Candidate 1)
 *			> 2.0.1 (Release Candidate 02-02-2007)
 *			> 2.1 (Beta 3)
 *			> 2.1 (Beta 03-03-2007)
 *
 *			Interim releases are the version number followed by the status and either a date in parentheses or a number.
 *			The date always has leading zeroes.  The status description can only be A-Z and spaces, no unicode or other
 *			stuff.  If it's unrecognized it's treated the same as a development release.
 *
 *			For pre-releases (development, beta, release candidate) the version number is what the release *aspires to be*,
 *			and the full release is always greater than it in comparisons.  "2.0" is greater than "2.0 (Release Candidate 2)".
 *
 *		Full Releases Prior to 2.0:
 *
 *			> 1.3
 *			> 1.35
 *
 *			Prior to 2.0 full releases were in major.minor format with the minor part acting as a decimal, so 1.3 > 1.22.
 *			These are treated as if the lowest digit is the bugfix part, so 1.35 = 1.3.5.
 *
 *		Development Releases Prior to 2.0:
 *
 *			> Development Release 07-09-2006 (1.35 base)
 *
 *			Prior to 2.0 development releases had the above format, with the version number being the full release it
 *			was based upon instead of the release it aspires to be.  Since this was only ever used with 1.35 these
 *			are treated as development releases for 1.4, so "Development Release 07-09-2006 (1.35 base)" =
 *			"1.4 (Development Release 07-09-2006)".
 *
 *		Full Releases Prior to 0.95:
 *
 *			> 1
 *
 *			Prior to 0.95 text files each had a separate file format version number that was used instead of the
 *			application version.  These were never changed between 0.85 and 0.91, so they are simply "1".
 *			Text version numbers that are "1" instead of "1.0" will be interpreted as 0.9.1.
 *
 *
 *
 * Topic: Integer Format
 *
 *		Version numbers are stored as unsigned 64-bit values internally and are designed to be easily comparable.
 *		*The integer format should be treated as opaque.*  It may change between releases so it should never be
 *		stored in binary form.  The following is just for internal documentation.
 *
 *		Implementation:
 *
 *			The integers use this bit distribution.
 *
 *			> [aaaaaaaa] [bbbbbbbb] [cccccccc] [tttttttt] [yyyyyyyy|yyyyyyyy] [mmmmmmmm] [dddddddd]
 *
 *			a - The major version number.
 *			b - The minor version number.
 *			c - The bugfix version number.
 *
 *			t - The type of release.
 *
 *			y - The year if greater than 1000, or the count if less than 1000, or zero for full releases.
 *			m - The month, or zero for full releases.
 *			d - The day, or zero for full releases.
 *
 *			The integers can be directly compared.  Since the release type is a higher order than the date fields, a
 *			full release will still be greater than any development versions even though the date fields are zero.
 */


// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;


namespace CodeClear.NaturalDocs.Engine
	{
	public partial struct Version
		{


		// Group: Types
		// __________________________________________________________________________


		/* Enum: ReleaseType
		 *
		 * The type of release.  They are listed below in descending order, so a full release will compare as higher
		 * than a release candidate, which will be higher than a beta, etc.
		 *
		 * Full - A standard release.
		 * ReleaseCandidate - A release candidate.
		 * Beta - A beta pre-release.
		 * Development - A development pre-release.
		 * Null - A null version string.
		 */
		public enum ReleaseType : byte
			{
			Full = 0xF0,
			ReleaseCanditate = 0xCA,
			Beta = 0xBE,
			Development = 0x0D,
			Null = 0x00
			}



		// Group: Static Functions
		// __________________________________________________________________________


		/* Function: Parse
		 * Attempts to convert a string into a Version object.  Will throw an exception if it fails.
		 */
		public static Version Parse (string versionString)
			{
			Version version;

			if (TryParse(versionString, out version))
				{  return version;  }
			else
				{  throw new Exceptions.StringNotInValidFormat(versionString, "Version");  }
			}


		/* Function: TryParse
		 * Attempts to convert a string into a Version object.  Returns whether it was successful.  Unlike <Parse()>, it will not
		 * throw an exception if it fails.
		 */
		public static bool TryParse (string versionString, out Version version)
			{
			// Null

			if (string.IsNullOrEmpty(versionString))
				{
				version = new Version();
				return true;
				}


			// Release format "x.y.z".  This will also catch the pre-2.0 release strings.

			Match match = IsReleaseVersionStringRegex().Match(versionString);

			if (match.Success)
				{
				byte majorVersion = byte.Parse( match.Groups[1].ToString() );
				byte minorVersion;
				byte bugfixVersion;

				string minorVersionString = match.Groups[2].ToString();
				string bugfixVersionString = match.Groups[3].ToString();

				// Convert old x.yy versions to x.y.z.
				if (majorVersion < 2)
					{
					// Old versions can't have a bugfix section attached.
					if (!string.IsNullOrEmpty(bugfixVersionString))
						{
						version = new Version();
						return false;
						}

					if (minorVersionString.Length == 1)
						{
						minorVersion = (byte)(minorVersionString[0] - '0');
						bugfixVersion = 0;
						}
					else
						{
						minorVersion = (byte)(minorVersionString[0] - '0');
						bugfixVersion = (byte)(minorVersionString[1] - '0');
						}
					}

				// We're at least at 2.0.
				else
					{
					minorVersion = byte.Parse(minorVersionString);

					if (!string.IsNullOrEmpty(bugfixVersionString))
						{  bugfixVersion = byte.Parse(bugfixVersionString);  }
					else
						{  bugfixVersion = 0;  }
					}

				var versionInt = ToVersionInt (majorVersion, minorVersion, bugfixVersion, ReleaseType.Full, 0, 0, 0, 0);

				version = new Version(versionInt);
				return (versionInt != 0);
				}


			// Development release format "x.y (type mm-dd-yyyy)"

			match = IsDevelopmentVersionStringWithDateRegex().Match(versionString);

			if (match.Success)
				{
				byte majorVersion = byte.Parse( match.Groups[1].ToString() );
				byte minorVersion = byte.Parse( match.Groups[2].ToString() );
				byte bugfixVersion;

				string bugfixVersionString = match.Groups[3].ToString();
				if (string.IsNullOrEmpty(bugfixVersionString))
					{  bugfixVersion = 0;  }
				else
					{  bugfixVersion = byte.Parse( bugfixVersionString );  }

				ReleaseType type = ReleaseType.Development;
				string typeString = match.Groups[4].ToString().ToLower(CultureInfo.InvariantCulture);

				if (typeString == "release candidate" || typeString == "rc")
					{  type = ReleaseType.ReleaseCanditate;  }
				else if (typeString == "beta")
					{  type = ReleaseType.Beta;  }

				byte month = byte.Parse( match.Groups[5].ToString() );
				byte day = byte.Parse( match.Groups[6].ToString() );
				ushort year = ushort.Parse( match.Groups[7].ToString() );

				var versionInt = ToVersionInt (majorVersion, minorVersion, bugfixVersion, type, 0, month, day, year);

				version = new Version(versionInt);
				return (versionInt != 0);
				}


			// Development release format "x.y (type count)"

			match = IsDevelopmentVersionStringWithCountRegex().Match(versionString);

			if (match.Success)
				{
				byte majorVersion = byte.Parse( match.Groups[1].ToString() );
				byte minorVersion = byte.Parse( match.Groups[2].ToString() );
				byte bugfixVersion;

				string bugfixVersionString = match.Groups[3].ToString();
				if (string.IsNullOrEmpty(bugfixVersionString))
					{  bugfixVersion = 0;  }
				else
					{  bugfixVersion = byte.Parse( bugfixVersionString );  }

				ReleaseType type = ReleaseType.Development;
				string typeString = match.Groups[4].ToString().ToLower(CultureInfo.InvariantCulture);

				if (typeString == "release candidate" || typeString == "rc")
					{  type = ReleaseType.ReleaseCanditate;  }
				else if (typeString == "beta")
					{  type = ReleaseType.Beta;  }

				ushort count = ushort.Parse( match.Groups[5].ToString() );

				var versionInt = ToVersionInt (majorVersion, minorVersion, bugfixVersion, type, count, 0, 0, 0);

				version = new Version(versionInt);
				return (versionInt != 0);
				}


			// 1.35 development release format "Development Release mm-dd-yyyy (1.35 base)"

			match = IsOldDevelopmentVersionStringWithDateRegex().Match(versionString);

			if (match.Success)
				{
				byte month = byte.Parse( match.Groups[1].ToString() );
				byte day = byte.Parse( match.Groups[2].ToString() );
				ushort year = ushort.Parse( match.Groups[3].ToString() );

				var versionInt = ToVersionInt (1, 4, 0, ReleaseType.Development, 0, month, day, year);

				version = new Version(versionInt);
				return (versionInt != 0);
				}


			// Old "1" format treated as 0.9.1

			if (versionString == "1")
				{
				var versionInt = ToVersionInt (0, 9, 1, ReleaseType.Full, 0, 0, 0, 0);

				version = new Version(versionInt);
				return true;
				}


			// Oh well.

			version = new Version();
			return false;
			}


		/* Function: ToString
		 * Returns the version as a string.
		 */
		public override string ToString ()
			{
			string primary = PrimaryVersionString;
			string secondary = SecondaryVersionString;

			if (secondary == null)
				{  return primary;  }
			else
				{  return primary + ' ' + secondary;  }
			}


		/* Function: ToVersionInt
		 * Takes a version's component parts and returns them encoded in the <Integer Format>.  Also sanity-checks
		 * the values so impossible combinations will result in zero.
		 */
		static private ulong ToVersionInt (byte majorVersion, byte minorVersion, byte bugfixVersion,
													 ReleaseType type, ushort count, byte month, byte day, ushort year)
			{
			ulong result = 0;

			// Either the major or the minor version must be set.
			if (majorVersion == 0 && minorVersion == 0)
				{  return 0;  }

			// All three also need to be under 100, but the regular expression already limits them to two digits for us.

			result |= (ulong)majorVersion << 56;
			result |= (ulong)minorVersion << 48;
			result |= (ulong)bugfixVersion << 40;

			result |= (ulong)type << 32;

			// Full releases cannot have date or count fields.
			if (type == ReleaseType.Full)
				{
				if (count != 0 || month != 0 || day != 0 || year != 0)
					{  return 0;  }
				}

			// All other release types must have count or date fields.
			else
				{
				if (count != 0)
					{
					if (count >= 1000 || month != 0 || day != 0 || year != 0)
						{  return 0;  }

					result |= (ulong)count << 16;
					}
				else
					{
					if (month < 1 || month > 12 || day < 1 || day > 31 || year < 1990 || year > 2100)
						{  return 0;  }

					result |= (ulong)year << 16;
					result |= (ulong)month << 8;
					result |= day;
					}
				}

			return result;
			}



		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Version (string)
		 * Creates the structure initialized to the string.  It must be in a valid <String Format> or an exception will be
		 * thrown.
		 */
		public Version (string newVersionString)
			{
			this = Parse(newVersionString);
			}


		/* Constructor: Version (ulong)
		 * Creates a structure initialized to the version int.  This is private because it assumes the value is valid, and no
		 * external code should be storing the integer format anyway.
		 */
		private Version (ulong newVersionInt)
			{
			this.versionInt = newVersionInt;
			}


		/* Function: IsAtLeastRelease
		 * Returns whether this version is a full release and is greater than or equal to the passed one, which must also be a full
		 * release.  It will return false if either of them are pre-releases, regardless of whether it is greater.
		 */
		public bool IsAtLeastRelease (Version minimum)
			{
			if (this.Type != ReleaseType.Full || minimum.Type != ReleaseType.Full)
				{  return false;  }
			else
				{  return (this.versionInt >= minimum.versionInt);  }
			}


		/* Function: IsSamePreRelease
		 * Returns whether this version is the same as the passed one and they are are both pre-releases, meaning anything other
		 * than <ReleaseType.Full>.  It will return false if either of them are full releases, even if they are the same.
		 */
		public bool IsSamePreRelease (Version other)
			{
			if (this.Type == ReleaseType.Full || other.Type == ReleaseType.Full)
				{  return false;  }
			else
				{  return (this.versionInt == other.versionInt);  }
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: MajorVersion
		 * The major version number, such as 2 in 2.0.1.
		 */
		public int MajorVersion
			{
			get
				{
				return (int)((versionInt & 0xFF00000000000000) >> 56);
				}
			}


		/* Property: MinorVersion
		 * The minor version number, such as 0 in 2.0.1.
		 */
		public int MinorVersion
			{
			get
				{
				return (int)((versionInt & 0x00FF000000000000) >> 48);
				}
			}


		/* Property: BugFixVersion
		 * The bugfix version number, such as 1 in 2.0.1.
		 */
		public int BugFixVersion
			{
			get
				{
				return (int)((versionInt & 0x0000FF0000000000) >> 40);
				}
			}


		/* Property: Type
		 * The <ReleaseType> of the version structure.
		 */
		public ReleaseType Type
			{
			get
				{
				return (ReleaseType)((versionInt & 0x000000FF00000000) >> 32);
				}
			}


		/* Property: CountOrYear
		 * The count or year variable.  Must be interpreted.
		 */
		private int CountOrYear
			{
			get
				{
				return (int)((versionInt & 0x00000000FFFF0000) >> 16);
				}
			}


		/* Property: Count
		 * The prerelease count, such as 2 in "2.0 (Beta 2)".  Will be zero for full releases or releases with a date.
		 */
		public int Count
			{
			get
				{
				var countOrYear = this.CountOrYear;

				if (countOrYear < 1000)
					{  return countOrYear;  }
				else
					{  return 0;  }
				}
			}


		/* Property: Year
		 * The year of non full-releases, such as "2.0 (Development Release 01-01-2013".  Will be zero for full releases or
		 * releases with a count.
		 */
		public int Year
			{
			get
				{
				var countOrYear = this.CountOrYear;

				if (countOrYear >= 1000)
					{  return countOrYear;  }
				else
					{  return 0;  }
				}
			}


		/* Property: Month
		 * The numeric month of non-full releases, such as "2.0 (Development Release 01-01-2013".  Will be zero for full releases
		 * or releases with a count.
		 */
		public int Month
			{
			get
				{
				return (int)((versionInt & 0x000000000000FF00) >> 8);
				}
			}


		/* Property: Day
		 * The numeric day of non-full releases, such as "2.0 (Development Release 01-01-2013".  Will be zero for full releases or
		 * releases with a count.
		 */
		public int Day
			{
			get
				{
				return (int)(versionInt & 0x00000000000000FF);
				}
			}


		/* Property: PrimaryVersionString
		 * The primary part of the version string.  For full releases this will be the same as the full string.  For other releases it
		 * will exclude the parenthetical, so for "2.0 (Development Release 01-01-2013)" it will return only "2.0".
		 */
		public string PrimaryVersionString
			{
			get
				{
				if (versionInt == 0)
					{  throw new InvalidOperationException("Tried to convert a null version to a string.");  }

				StringBuilder stringBuilder = new StringBuilder();

				if (MajorVersion >= 2)
					{
					stringBuilder.Append(MajorVersion);
					stringBuilder.Append('.');
					stringBuilder.Append(MinorVersion);

					if (BugFixVersion > 0)
						{
						stringBuilder.Append('.');
						stringBuilder.Append(BugFixVersion);
						}

					return stringBuilder.ToString();
					}
				else // MajorVersion < 2
					{
					if (Type == ReleaseType.Development)
						{
						// The old "Development Release 01-01-2007 (1.35 base)" format
						return string.Format("Development Release {0:00}-{1:00}-{2:0000}", this.Month, this.Day, this.Year);
						}
					else
						{
						stringBuilder.Append(MajorVersion);
						stringBuilder.Append('.');
						stringBuilder.Append(MinorVersion);

						if (BugFixVersion > 0)
							{  stringBuilder.Append(BugFixVersion);  }

						return stringBuilder.ToString();
						}
					}
				}
			}


		/* Property: SecondaryVersionString
		 * The secondary part of the version string, or null if there is none.  For full releases this will always be null.  For other
		 * releases it will be the part in parentheses, so for "2.0 (Development Release 01-01-2013)" it will be
		 * "(Development Release 01-01-2013)", parentheses included.
		 */
		public string SecondaryVersionString
			{
			get
				{
				if (versionInt == 0)
					{  throw new InvalidOperationException("Tried to convert a null version to a string.");  }

				if (Type == ReleaseType.Full)
					{  return null;  }

				if (MajorVersion >= 2)
					{
					string typeName;

					if (Type == ReleaseType.ReleaseCanditate)
						{  typeName = "Release Candidate";  }
					else if (Type == ReleaseType.Beta)
						{  typeName = "Beta";  }
					else
						{  typeName = "Development Release";  }

					if (this.Year != 0)
						{  return String.Format("({0} {1:00}-{2:00}-{3:0000})", typeName, this.Month, this.Day, this.Year);  }
					else
						{  return String.Format("({0} {1})", typeName, this.Count);  }
					}
				else
					{
					// The only time non-full version numbers were used prior to 2.0 was in the format "Development Release 01-01-2007 (1.35 base)".
					return "(1.35 base)";
					}
				}
			}



		// Group: Operators
		// __________________________________________________________________________


		/* Function: operator Version (string)
		 * A cast operator that allows the structure to be set to a string.  The string must be in a valid <String
		 * Format> or it will throw an exception.
		 */
		public static implicit operator Version (string versionString)
			{
			return Parse(versionString);
			}

		/* Function: operator == (Version)
		 * Compares one version to another.
		 */
		public static bool operator == (Version version1, Version version2)
			{
			return (version1.versionInt == version2.versionInt);
			}

		/* Function: operator != (Version)
		 * Compares one version to another.
		 */
		public static bool operator != (Version version1, Version version2)
			{
			return (version1.versionInt != version2.versionInt);
			}

		/* Function: operator > (Version)
		 * Compares one version to another.
		 */
		public static bool operator > (Version version1, Version version2)
			{
			return (version1.versionInt > version2.versionInt);
			}

		/* Function: operator >= (Version)
		 * Compares one version to another.
		 */
		public static bool operator >= (Version version1, Version version2)
			{
			return (version1.versionInt >= version2.versionInt);
			}

		/* Function: operator < (Version)
		 * Compares one version to another.
		 */
		public static bool operator < (Version version1, Version version2)
			{
			return (version1.versionInt < version2.versionInt);
			}

		/* Function: operator <= (Version)
		 * Compares one version to another.
		 */
		public static bool operator <= (Version version1, Version version2)
			{
			return (version1.versionInt <= version2.versionInt);
			}


		/* Function: operator string (Version)
		 * A cast operator that returns the version as a string.
		 */
		public static explicit operator string (Version version)
			{
			return version.ToString();
			}



		// Group: Standard Object Functions
		// I have to override some of this stuff or the compiler will complain.
		// __________________________________________________________________________


		/* Function: Equals
		 * Compares to the object as a uint.  If the object cannot be cast to a uint, returns false.
		 */
		public override bool Equals (object obj)
			{
			if (obj is Version)
				{  return (this == (Version)obj);  }
			else
				{  return false;  }
			}

		/* Function: GetHashCode
		 * Gets the hash code of the structure.
		 */
		public override int GetHashCode ()
			{
			return versionInt.GetHashCode();
			}



		// Group: Variables
		// __________________________________________________________________________

		/* var: versionInt
		 * The structure's version in <Integer Format>.
		 */
		private ulong versionInt;



		// Group: Regular Expressions
		// __________________________________________________________________________


		/* Regex: IsReleaseVersionStringRegex
		 *
		 * Will match if the entire string is a release version string such as "2.0" or "2.0.1".
		 *
		 * Capture Groups:
		 *
		 *		1 - The major version number, such as "2" in "2.0.1".
		 *		2 - The minor version number, such as "0" in "2.0.1".
		 *		3 - The bugfix release number, if it exists, such as "1" in "2.0.1".
		 */
		[GeneratedRegex("""^([0-9]{1,2})\.([0-9]{1,2})(?:\.([0-9]{1,2}))?$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsReleaseVersionStringRegex();


		/* Regex: IsDevelopmentVersionStringWithDateRegex
		 *
		 * Will match if the entire string is a date-based development version string such as "2.0.1 (Development Release 07-01-2025)".
		 *
		 * Capture Groups:
		 *
		 *		1 - The major version number, such as "2" in "2.0.1 (Development Release 07-01-2025)".
		 *		2 - The minor version number, such as "0" in "2.0.1 (Development Release 07-01-2025)".
		 *		3 - The bugfix release number, if it exists, such as "1" in "2.0.1 (Development Release 07-01-2025)".
		 *		4 - The release type, such as "Development Release" in "2.0.1 (Development Release 07-01-2025)".
		 *		5 - The numeric month, such as "07" in "2.0.1 (Development Release 07-01-2025)".
		 *		6 - The numeric day, such as "01" in "2.0.1 (Development Release 07-01-2025)".
		 *		7 - The numeric year, such as "2025" in "2.0.1 (Development Release 07-01-2025)".
		 */
		[GeneratedRegex("""^([0-9]{1,2})\.([0-9]{1,2})(?:\.([0-9]{1,2}))? \(([a-z ]+?) ([0-9]{1,2})-([0-9]{1,2})-([0-9]{4})\)$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static private partial Regex IsDevelopmentVersionStringWithDateRegex();


		/* Regex: IsDevelopmentVersionStringWithCountRegex
		 *
		 * Will match if the entire string is a date-based development version string such as "2.0.1 (Beta 1)" or
		 * "2.0.1 (Release Candidate 2)".
		 *
		 * Capture Groups:
		 *
		 *		1 - The major version number, such as "2" in "2.0.1 (Beta 3)".
		 *		2 - The minor version number, such as "0" in "2.0.1 (Beta 3)".
		 *		3 - The bugfix release number, if it exists, such as "1" in "2.0.1 (Beta 3)".
		 *		4 - The release type, such as "Beta" in "2.0.1 (Beta 3)".
		 *		5 - The count, such as "3" in "2.0.1 (Beta 3)".
		 */
		[GeneratedRegex("""^([0-9]{1,2})\.([0-9]{1,2})(?:\.([0-9]{1,2}))? \(([a-z ]+?) ([0-9]{1,3})\)$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static private partial Regex IsDevelopmentVersionStringWithCountRegex();


		/* Regex: IsOldDevelopmentVersionStringWithDateRegex
		 *
		 * Will match if the entire string is an old date-based development version string such as
		 * "Development Release 02-10-2007 (1.35 base)".  This format was only ever used with 1.35 as the base.
		 *
		 * Capture Groups:
		 *
		 *		1 - The numeric month, such as "02" in "Development Release 02-10-2007 (1.35 base)".
		 *		2 - The numeric day, such as "10" in "Development Release 02-10-2007 (1.35 base)".
		 *		3 - The numeric year, such as "2007" in "Development Release 02-10-2007 (1.35 base)".
		 */
		[GeneratedRegex("""^Development Release ([0-9]{1,2})-([0-9]{1,2})-([0-9]{4}) \(1\.35 base\)$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static private partial Regex IsOldDevelopmentVersionStringWithDateRegex();


		}
	}
