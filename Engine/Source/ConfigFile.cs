/*
 * Class: CodeClear.NaturalDocs.Engine.ConfigFile
 * ____________________________________________________________________________
 *
 * A class to handle reading standard Natural Docs text-based configuration files.  It also provides the static function
 * <TryToAnnotateWithErrors()> but is otherwise not used for writing.
 *
 *
 * Topic: File Format
 *
 *		All configuration files are text files in UTF-8.  They may appear with or without the Unicode BOM and using
 *		any line break format.  The parsing behavior can be tweaked on a file by file basis by setting the <FileFormatFlags>.
 *
 *
 *		Comments:
 *
 *		Lines that start with # are ignored.  Comments cannot appear on the same line as content, the only
 *		exception being after a brace if braces are supported.
 *
 *
 *		Identifiers:
 *
 *		Identifiers can contain any characters except #, colons, and braces.
 *
 *
 *		Identifier/Value Pairs:
 *
 *		Most lines will be in the format "[identifier]: [value]".  The value can contain any characters, including colons,
 *		#, and unless they're supported by the configuration file, braces.
 *
 *
 *		Braces:
 *
 *		If the configuration file supports braces they can appear throughout the file and are treated as if they're
 *		surrounded by line breaks.  This means a brace can appear on the same line as content and it immediately
 *		ends the content line.  It also means comments can appear after it on the same line, which normally isn't
 *		allowed.
 *
 *
 *		Raw Value Lines:
 *
 *		If supported by the configuration file, lines that do not contain a colon, brace, or start with # are considered
 *		value lines without identifiers.
 *
 *
 *		Null Value Lines:
 *
 *		If supported by the configuration file, identifier lines can appear without a value.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.IO;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Engine
	{
	public class ConfigFile : IDisposable
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: FileFormatFlags
		 *
		 * SupportsBraces - The configuration file supports braces as described in the <File Format>.
		 * SupportsRawValueLines - The configuration file supports values without identifiers.
		 * SupportsNullValueLines - The configuration file supports identifiers without values.
		 *
		 * CondenseIdentifierWhitespace - All consecutive whitespace characters in the identifier are replaced with a single
		 *												   space.
		 *	CondenseValueWhitespace - All consecutive whitespace characters in the value are replaced with a single space.
		 *
		 * MakeIdentifiersLowercase - Identifiers will be converted to all lowercase before being returned.
		 */
		[Flags]
		public enum FileFormatFlags : byte
			{
			SupportsBraces = 0x01,
			SupportsRawValueLines = 0x02,
			SupportsNullValueLines = 0x04,

			CondenseIdentifierWhitespace = 0x08,
			CondenseValueWhitespace = 0x10,

			MakeIdentifiersLowercase = 0x20
			}



		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: ConfigFile
		 * Creates the object.  Does not open a file.
		 */
		public ConfigFile ()
			{
			file = null;
			fileName = null;
			propertySource = Config.PropertySource.NotDefined;
			version = null;
			fileFormatFlags = 0;
			lineNumber = 0;
			restOfLine = null;
			errorList = null;
			}


		/* Function: Dispose
		 */
		public void Dispose ()
			{
			if (file != null)
				{
				file.Dispose();
				file = null;
				}
			}


		/* Function: Open
		 *
		 * Attempts to open the passed configuration file and returns whether it was successful.  Any errors
		 * encountered in trying to open the file will be added to the errors array.
		 *
		 * Parameters:
		 *
		 *		fileName - The <Path> of the configuration file.
		 *		propertySource - The <Config.PropertySource> this file represents.
		 *		fileFormatFlags - The <FileFormatFlags> to use when parsing the file.
		 *		errorList - A list where the parser will put any errors.
		 */
		public bool Open (Path fileName, Config.PropertySource propertySource, FileFormatFlags fileFormatFlags, ErrorList errorList)
			{
			if (IsOpen)
				{  throw new Engine.Exceptions.FileAlreadyOpen(fileName, this.fileName);  }

			if (!File.Exists(fileName))
				{
				errorList.Add(
					Locale.Get("NaturalDocs.Engine", "ConfigFile.DoesNotExist(name)", fileName)
					);

				return false;
				}

			try
				{  file = new StreamReader(fileName, System.Text.Encoding.UTF8, true);  }
			catch (Exception e)
				{
				errorList.Add(
					Locale.Get("NaturalDocs.Engine", "ConfigFile.CouldNotOpen(name, exception)", fileName, e.Message)
					);

				return false;
				}

			this.fileName = fileName;
			this.propertySource = propertySource;
			this.fileFormatFlags = fileFormatFlags;
			this.errorList = errorList;
			this.lineNumber = 0;

			string identifier, valueString;

			if (!Get(out identifier, out valueString) || identifier.ToLower() != "format")
				{
				AddError( Locale.Get("NaturalDocs.Engine", "ConfigFile.DidntStartWithFormat(name)", fileName) );
				}
			else
				{
				version = valueString;

				if (version == null)
					{
					AddError( Locale.Get("NaturalDocs.Engine", "ConfigFile.FormatNotAValidVersionString(versionString)", valueString) );
					}
				}

			if (version != null)
				{  return true;  }
			else
				{
				Close();
				return false;
				}
			}


		/* Function: Close
		 * Closes the file if one was open.
		 */
		public void Close ()
			{
			if (IsOpen)
				{
				Dispose();

				file = null;
				fileName = null;
				propertySource = Config.PropertySource.NotDefined;
				version = null;
				fileFormatFlags = 0;
				lineNumber = 0;
				restOfLine = null;
				errorList = null;
				}
			}


		/* Function: Get
		 *
		 * Gets the next identifier/value pair, if any.  Returns whether it was successful.
		 *
		 * If the file supports braces, they will be returned as the identifier with null as the value.  If the file supports
		 * raw value lines, they will be returned as the value with null as the identifier.
		 */
		public bool Get (out string identifier, out string value)
			{
			string line;

			for (;;)
				{
				line = GetLine();

				if (line == null)
					{
					identifier = null;
					value = null;
					return false;
					}

				line = line.Trim();

				if ( (fileFormatFlags & FileFormatFlags.SupportsBraces) != 0 &&
					 (line == "{" || line == "}"))
					{
					identifier = line;
					value = null;
					return true;
					}

				else if (line != "" && line[0] != '#')
					{
					int index = line.IndexOf(':');

					if (index == -1)
						{
						if ( (fileFormatFlags & FileFormatFlags.SupportsRawValueLines) != 0)
							{
							if ( (fileFormatFlags & FileFormatFlags.CondenseValueWhitespace) != 0)
								{
								line = line.CondenseWhitespace();
								}

							identifier = null;
							value = line;
							return true;
							}
						else
							{
							AddError( Engine.Locale.Get("NaturalDocs.Engine", "ConfigFile.LineNotInIdentifierValueFormat") );
							// Continue parsing
							}
						}
					else
						{
						string tempIdentifier;
						string tempValue;

						if (index == 0)
							{  tempIdentifier = null;  }
						else
							{
							 tempIdentifier = line.Substring(0, index).TrimEnd();
							 if (tempIdentifier == "")
								{  tempIdentifier = null;  }
							}

						if (index == line.Length - 1)
							{  tempValue = null;  }
						else
							{
							tempValue = line.Substring(index + 1).TrimStart();
							if (tempValue == "")
								{  tempValue = null;  }
							}

						if (tempIdentifier == null ||
							(tempValue == null && (fileFormatFlags & FileFormatFlags.SupportsNullValueLines) == 0) )
							{
							AddError( Engine.Locale.Get("NaturalDocs.Engine", "ConfigFile.LineNotInIdentifierValueFormat") );
							// Continue parsing
							}
						else
							{
							if ((fileFormatFlags & FileFormatFlags.CondenseIdentifierWhitespace) != 0)
								{
								tempIdentifier = tempIdentifier.CondenseWhitespace();
								}
							if ((fileFormatFlags & FileFormatFlags.CondenseValueWhitespace) != 0 && tempValue != null)
								{
								tempValue = tempValue.CondenseWhitespace();
								}

							if ( (fileFormatFlags & FileFormatFlags.MakeIdentifiersLowercase) != 0 )
								{  tempIdentifier = tempIdentifier.ToLower();  }

							identifier = tempIdentifier;
							value = tempValue;

							return true;
							}
						}

					}  // Line not empty or comment
				}  // Loop
			}


		/* Function: AddError
		 * Adds an error to the list, automatically filling in the file and line number properties based on the last call to <Get()>.
		 */
		public void AddError (string errorMessage, string property = null)
			{
			errorList.Add(errorMessage, fileName, lineNumber, propertySource, property);
			}



		// Group: Protected Functions
		// __________________________________________________________________________


		/* Function: GetLine
		 *
		 * Returns the next line of the file, splitting braces into separate lines if supported.  The line will not have
		 * line break characters at the end of it.  Otherwise it does not process the line in any way, so comments
		 * and blank lines will be returned and all whitespace will be intact.  Does advance <lineNumber> though.
		 *
		 * Will return null if there are no more lines.
		 */
		protected string GetLine ()
			{
			string line = null;

			if (String.IsNullOrEmpty(restOfLine))
				{
				line = file.ReadLine();

				if (line == null)
					{  return null;  }

				lineNumber++;
				}
			else
				{
				line = restOfLine;
				}

			if ( (fileFormatFlags & FileFormatFlags.SupportsBraces) == 0)
				{
				return line;
				}
			else
				{
				int braceIndex = line.IndexOfAny(BracesChars);

				if (braceIndex == -1)
					{
					restOfLine = null;
					return line;
					}
				else if (braceIndex == 0)
					{
					restOfLine = line.Substring(1);
					return line.Substring(0, 1);
					}
				else
					{
					restOfLine = line.Substring(braceIndex);
					return line.Substring(0, braceIndex);
					}
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: IsOpen
		 * Whether the class has a file open.
		 */
		public bool IsOpen
			{
			get
				{  return (file != null);  }
			}


		/* Property: Version
		 * The <Engine.Version> of the file if one is open, null otherwise.
		 */
		public Engine.Version Version
			{
			get
				{  return version;  }
			}


		/* Property: FileName
		 * The <Path> to the open file, or null if none.
		 */
		public Path FileName
			{
			get
				{  return fileName;  }
			}


		/* Property: LineNumber
		 * Returns the line number of the last line returned by <Get()>, or zero if it hasn't been called yet.
		 */
		public int LineNumber
			{
			get
				{  return lineNumber;  }
			}


		/* Property: PropertySource
		 * Returns the <Config.PropertySource> of the open file, or <Config.Property.NotDefined> if none.
		 */
		public Config.PropertySource PropertySource
			{
			get
				{  return propertySource;  }
			}


		/* Property: PropertyLocation
		 * Returns a <Config.PropertyLocation> created from <PropertySource>, <FileName>, and <LineNumber>.
		 */
		public Config.PropertyLocation PropertyLocation
			{
			get
				{  return new Config.PropertyLocation(propertySource, fileName, lineNumber);  }
			}



		// Group: Static Functions
		// __________________________________________________________________________


		/* Function: TryToAnnotateWithErrors
		 *
		 * Attempts to provide comment annotations for all the config files with errors appearing in a passed <ErrorList>.  Only
		 * errors that have <Error.File> and <Error.LineNumber> set will be applied.
		 *
		 * This function will possibly change the line numbers where errors occur.  It will update the <ErrorList> if it does.
		 * However, this means if you were going to present the error information another way as well, such as to the console,
		 * you must call this function beforehand so the line numbers are accurate.
		 *
		 * This function does *not* throw exceptions if it can't open any of the files for writing, hence the "Try" in the name.
		 * It will simply continue to the next file.
		 */
		static public void TryToAnnotateWithErrors (ErrorList errorList)
			{
			IList<Path> files = errorList.ConfigFiles;


			foreach (Path file in files)
				{
				string[] originalLines = null;

				try
					{
					originalLines = File.ReadAllLines(file, System.Text.Encoding.UTF8);
					}
				catch
					{
					// We don't care if it doesn't work.  Just skip to the next file.
					}

				if (originalLines != null)
					{
					IList<Error> errors = errorList.FromFile(file);

					int originalLineIndex = 0;
					int errorIndex = 0;

					int originalLineNumber = 1;
					int newLineNumber = 1;

					StreamWriter output = null;

					try
						{
						output = File.CreateText(file);
						}
					catch
						{
						// We don't care if it doesn't work.  Just skip to the next file.
						}

					if (output != null)
						{
						using (output)
							{

							// Write an error header first.

							if (errors.Count == 1)
								{  output.WriteLine("# " + Locale.Get("NaturalDocs.Engine", "ConfigFile.ErrorInThisFile"));  }
							else
								{  output.WriteLine("# " + Locale.Get("NaturalDocs.Engine", "ConfigFile.ErrorsInThisFile(count)", errors.Count));  }

							output.WriteLine();
							newLineNumber += 2;


							// Skip the prior error header if it exists.

							if (originalLineIndex < originalLines.Length &&
								System.Text.RegularExpressions.Regex.IsMatch(originalLines[originalLineIndex],
																									  Locale.Get("NaturalDocs.Engine", "ConfigFile.ErrorsInThisFileRegex") ))
								{
								originalLineIndex++;
								originalLineNumber++;

								// Skip the blank line after it too.

								if (originalLineIndex < originalLines.Length && originalLines[originalLineIndex].Trim() == "")
									{
									originalLineIndex++;
									originalLineNumber++;
									}
								}


							// Start writing lines and errors.

							string errorPrefix = "# " + Locale.Get("NaturalDocs.Engine", "ConfigFile.ErrorPrefix") + ' ';

							while (originalLineIndex < originalLines.Length)
								{

								// Write the errors.  There may be more than one for a line.  We also use <= instead of == because
								// some errors may be set to line 0 or -1.

								int errorsForThisLine = 0;
								while (errorIndex < errors.Count && errors[errorIndex].LineNumber <= originalLineNumber)
									{
									output.WriteLine( errorPrefix + errors[errorIndex].Message );
									newLineNumber++;
									errorIndex++;
									errorsForThisLine++;
									}


								// Update the line numbers for any errors that occurred here.  We have to do this separately from the
								// loop above to give them all the same value.  Skip any that were less than 1.

								if (errorsForThisLine > 0)
									{
									errorIndex -= errorsForThisLine;

									do
										{
										if (errors[errorIndex].LineNumber >= 1)
											{  errors[errorIndex].LineNumber = newLineNumber;  }
										errorIndex++;
										errorsForThisLine--;
										}
									while (errorsForThisLine > 0);
									}


								// Write the line.  Skip it if it's an existing error line.

								if (!originalLines[originalLineIndex].StartsWith(errorPrefix))
									{
									output.WriteLine( originalLines[originalLineIndex] );
									newLineNumber++;
									}

								originalLineIndex++;
								originalLineNumber++;
								}


							// Write out any errors that are left over.

							while (errorIndex < errors.Count)
								{
								output.WriteLine( errorPrefix + errors[errorIndex].Message );
								errors[errorIndex].LineNumber = newLineNumber + errors.Count;
								errorIndex++;
								}

							}  // using output
						}  // output != null
					}  // originalLines != null
				}  // foreach config file
			}


		/* Function: TryToRemoveErrorAnnotations
		 *
		 * Attempts to remove any error annotations from the passed config file.  This is provided for system config files that don't
		 * normally rewrite themselves.
		 *
		 * This function does *not* throw exceptions if it can't open the file for writing, hence the "Try" in the name.
		 */
		static public void TryToRemoveErrorAnnotations (Path file)
			{
			string[] originalLines = null;
			StreamWriter output = null;

			try
				{  originalLines = File.ReadAllLines(file, System.Text.Encoding.UTF8);    }
			catch
				{
				// We don't care if it doesn't work.
				return;
				}

			if (originalLines.Length > 0 &&
				System.Text.RegularExpressions.Regex.IsMatch(originalLines[0],
																					  Locale.Get("NaturalDocs.Engine", "ConfigFile.ErrorsInThisFileRegex") ) == false)
				{  return;  }

			try
				{  output = File.CreateText(file);  }
			catch
				{
				// We don't care if it doesn't work.
				return;
				}

			using (output)
				{
				int lineIndex = 1;

				// Skip the blank line after the header too.

				if (lineIndex < originalLines.Length && originalLines[lineIndex].Trim() == "")
					{  lineIndex++;  }


				// Start writing lines and errors.

				string errorPrefix = "# " + Locale.Get("NaturalDocs.Engine", "ConfigFile.ErrorPrefix");

				while (lineIndex < originalLines.Length)
					{
					// Write the line.  Skip it if it's an existing error line.

					if (!originalLines[lineIndex].StartsWith(errorPrefix))
						{  output.WriteLine( originalLines[lineIndex] );  }

					lineIndex++;
					}
				}
			}


		/* Function: SaveIfDifferent
		 *
		 * Saves the passed content to the file if it's meaningfully different than what's already there.  Checking the existing content
		 * prevents unnecessary writes and timestamp changes.  Returns whether it was successful.
		 *
		 * If noErrorOnFail is set, it will return false if it fails but perform no other action.  If it is not set, an error message will be
		 * added to the list as well.
		 */
		public static bool SaveIfDifferent (Path filename, string newContent, bool noErrorOnFail, Errors.ErrorList errorList = null)
			{
			string existingContent = null;
			try
				{  existingContent = System.IO.File.ReadAllText(filename, System.Text.Encoding.UTF8);  }
			catch
				{
				// Ignore.  If we can't read it, chances are we can't write to it either so let that error handling take care of it.
				}

			// We don't want to rewrite the file just for line break differences.  This would cause unnecessary file changes when
			// using version control systems across multiple platforms.
			string newContentNormalized = newContent.NormalizeLineBreaks();
			string existingContentNormalized = (existingContent == null ? null : existingContent.NormalizeLineBreaks());

			// We also don't want to rewrite the file just for Format: line differences.  This prevents unnecessary file changes in
			// version control systems when the contents of two files are the same except for "Format: 2.0" versus "Format: 2.0.1".
			newContentNormalized = FormatLineRegex.Replace(newContentNormalized, "");
			existingContentNormalized = (existingContent == null ? null : FormatLineRegex.Replace(existingContentNormalized, ""));

			if (newContentNormalized == existingContentNormalized)
				{  return true;  }
			else
				{
				try
					{
					// Write out the regular content, NOT the normalized content.
					System.IO.File.WriteAllText(filename, newContent, System.Text.Encoding.UTF8);
					}
				catch (Exception e)
					{
					if (!noErrorOnFail)
						{
						errorList.Add(
							Locale.Get("NaturalDocs.Engine", "ConfigFile.CouldNotWriteTo(name, exception)", filename, e.Message)
							);
						}

					return false;
					}

				return true;
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: file
		 * The currently open file, or null if none.
		 */
		protected StreamReader file;

		/* var: fileName
		 * The <Path> of the file currently being parsed, or null if none.
		 */
		protected Path fileName;

		/* var: propertySource
		 * The <Config.PropertySource> represented by the file currently being parsed, or <Config.PropertySource.NotDefined>
		 * if none.
		 */
		protected Config.PropertySource propertySource;

		/* var: version
		 * The version of the file if one is open, null otherwise.
		 */
		protected Engine.Version version;

		/* var: fileFormatFlags
		 * The file format flags of the currently open file.
		 */
		protected FileFormatFlags fileFormatFlags;

		/* var: lineNumber
		 * The line number of the last value returned, or zero if none.
		 */
		protected int lineNumber;

		/* var: restOfLine
		 * If a line is being split because of braces, this string will hold the rest of it between calls.  Null otherwise.
		 */
		protected string restOfLine;

		/* var: errorList
		 * A reference to the list of errors to add to if we encounter any.
		 */
		protected ErrorList errorList;



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: BracesChars
		 * An array of braces characters for use with IndexOfAny(char[]).
		 */
		static protected char[] BracesChars = new char[] { '{', '}' };

		static public Regex.Config.FormatLine FormatLineRegex = new Regex.Config.FormatLine();

		}
	}
