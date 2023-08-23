/*
 * Class: CodeClear.NaturalDocs.Engine.Comments.NaturalDocs.ConfigFiles.TextFileParser
 * ____________________________________________________________________________
 *
 * A class to handle loading <Parser.txt>.  Unlike most other config files, this one is not resaved by Natural Docs so
 * there is no save function.
 *
 *
 * Multithreading: Not Thread Safe
 *
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Globalization;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Regex;
using CodeClear.NaturalDocs.Engine.Regex.Comments.NaturalDocs;


namespace CodeClear.NaturalDocs.Engine.Comments.NaturalDocs.ConfigFiles
	{
	public class TextFileParser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: TextFileParser
		 */
		public TextFileParser ()
			{
		    arrowSeparatorRegex = new CondensedWhitespaceArrowSeparator();
			acceptableURLProtocolCharactersRegex = new AcceptableURLProtocolCharacters();
			}



		// Group: Loading Functions
		// __________________________________________________________________________


		/* Function: Load
		 *
		 * Loads the contents of a <Parser.txt> file into a <Config>, returning whether it was successful.  If it was unsuccessful
		 * config will be null and it will place errors on the errorList.
		 *
		 * Parameters:
		 *
		 *		filename - The <Path> where the file is located.
		 *		propertySource - The <Engine.Config.PropertySource> associated with the file.
		 *		errorList - If it couldn't successfully parse the file it will add error messages to this list.
		 *		config - The contents of the file as a <Config>.
		 */
		public bool Load (Path filename, Engine.Config.PropertySource propertySource, Errors.ErrorList errorList,
								 out Config config)
			{
		    int previousErrorCount = errorList.Count;

		    using (ConfigFile file = new ConfigFile())
		        {
		        bool openResult = file.Open(filename,
														 Engine.Config.PropertySource.ParserConfigurationFile,
														 ConfigFile.FileFormatFlags.MakeIdentifiersLowercase |
		                                                 ConfigFile.FileFormatFlags.CondenseValueWhitespace |
		                                                 ConfigFile.FileFormatFlags.SupportsRawValueLines,
		                                                 errorList);

		        if (openResult == false)
		            {
					config = null;
					return false;
					}

				config = new Config();

		        string identifier = null;
		        string value = null;

		        // If this is true, identifier and value are already filled but not processed, so Get shouldn't be called again on the next
		        // iteration.
		        bool alreadyHaveNextLine = false;

		        while (alreadyHaveNextLine || file.Get(out identifier, out value))
		            {
		            alreadyHaveNextLine = false;

		            if (identifier == null)
		                {
		                file.AddError(
		                    Locale.Get("NaturalDocs.Engine", "ConfigFile.LineNotInIdentifierValueFormat")
		                    );
		                continue;
		                }


		            //
		            // Sets
		            //

					if (identifier == "set")
						{
						string lcSetName = value.ToLowerInvariant();
						StringSet set = null;
						bool urlProtocols = false;

						if (lcSetName == "start block keywords")
							{  set = config.StartBlockKeywords;  }
						else if (lcSetName == "end block keywords")
							{  set = config.EndBlockKeywords;  }
						else if (lcSetName == "see image keywords")
							{  set = config.SeeImageKeywords;  }
						else if (lcSetName == "at link keywords")
							{  set = config.AtLinkKeywords;  }
						else if (lcSetName == "acceptable link suffixes")
							{  set = config.AcceptableLinkSuffixes;  }
						else if (lcSetName == "url protocols")
							{
							set = config.URLProtocols;
							urlProtocols = true;
							}
						else
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidIdentifier(identifier)", value)
								);
							// Continue anyway.
							}

						while (file.Get(out identifier, out value))
							{
							if (identifier != null)
								{
								alreadyHaveNextLine = true;
								break;
								}

							if (urlProtocols && acceptableURLProtocolCharactersRegex.IsMatch(value) == false)
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidValue(value)", value)
									);
								}

							if (set != null)
								{  set.Add(value);  }
							}
						}


					//
					// Tables
					//

					else if (identifier == "table")
						{
						string lcTableName = value.ToLowerInvariant();

						bool inBlockTypes = false;
						bool inSpecialHeadings = false;
						bool inAccessLevel = false;

						if (lcTableName == "block types")
							{  inBlockTypes = true;  }
						else if (lcTableName == "special headings")
							{  inSpecialHeadings = true;  }
						else if (lcTableName == "access level")
							{  inAccessLevel = true;  }
						else
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidIdentifier(identifier)", value)
								);
							// Continue anyway.
							}

						while (file.Get(out identifier, out value))
							{
							if (identifier != null)
								{
								alreadyHaveNextLine = true;
								break;
								}

							string lcValue = value.ToLowerInvariant();
							string[] split = arrowSeparatorRegex.Split(lcValue, 2);


							// Block Types

							if (inBlockTypes)
								{
								NaturalDocs.Parser.BlockType blockType = default;

								if (split[1] == "generic")
									{  blockType = NaturalDocs.Parser.BlockType.Generic;  }
								else if (split[1] == "code")
									{  blockType = NaturalDocs.Parser.BlockType.Code;  }
								else if (split[1] == "prototype")
									{  blockType = NaturalDocs.Parser.BlockType.Prototype;  }
								else
									{
									file.AddError(
										Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidValue(value)", split[1])
										);
									// Continue anyway.
									}

								config.BlockTypes.Add(split[0], blockType);
								}


							// Special Headings

							if (inSpecialHeadings)
								{
								NaturalDocs.Parser.HeadingType headingType = default;

								if (split[1] == "generic")
									{  headingType = NaturalDocs.Parser.HeadingType.Generic;  }
								else if (split[1] == "parameters")
									{  headingType = NaturalDocs.Parser.HeadingType.Parameters;  }
								else
									{
									file.AddError(
										Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidValue(value)", split[1])
										);
									// Continue anyway.
									}

								config.SpecialHeadings.Add(split[0], headingType);
								}


							// Access Level

							if (inAccessLevel)
								{
								Languages.AccessLevel accessLevel = default;

								if (split[1] == "public")
									{  accessLevel = Languages.AccessLevel.Public;  }
								else if (split[1] == "private")
									{  accessLevel = Languages.AccessLevel.Private;  }
								else if (split[1] == "protected")
									{  accessLevel = Languages.AccessLevel.Protected;  }
								else if (split[1] == "internal")
									{  accessLevel = Languages.AccessLevel.Internal;  }
								else if (split[1] == "protectedinternal")
									{  accessLevel = Languages.AccessLevel.ProtectedInternal;  }
								else if (split[1] == "privateprotected")
									{  accessLevel = Languages.AccessLevel.PrivateProtected;  }
								else
									{
									file.AddError(
										Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidValue(value)", split[1])
										);
									// Continue anyway.
									}

								config.AccessLevel.Add(split[0], accessLevel);
								}
							}
						}


					//
					// Conversion Lists
					//

					else if (identifier == "conversion list")
						{
						string lcListName = value.ToLowerInvariant();
						List<KeyValuePair<string, string>> list = null;

						if (lcListName == "plural conversions")
							{  list = config.PluralConversions;  }
						else if (lcListName == "possessive conversions")
							{  list = config.PossessiveConversions;  }
						else
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidIdentifier(identifier)", value)
								);
							// Continue anyway.
							}

						while (file.Get(out identifier, out value))
							{
							if (identifier != null)
								{
								alreadyHaveNextLine = true;
								break;
								}

							if (list != null)
								{
								string[] split = arrowSeparatorRegex.Split(value, 2);

								string left = split[0].ToLower(CultureInfo.InvariantCulture).Normalize(System.Text.NormalizationForm.FormC);
								string right = (String.IsNullOrEmpty(split[1]) ? null : split[1].ToLower(CultureInfo.InvariantCulture).Normalize(System.Text.NormalizationForm.FormC));

								list.Add( new KeyValuePair<string, string>(left, right) );
								}
							}
						}

		            else
		                {
		                file.AddError(
		                    Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidIdentifier(identifier)", identifier)
		                    );

		                // Skip to the next identifier
		                while (file.Get(out identifier, out value))
		                    {
		                    if (identifier != null)
		                        {
		                        alreadyHaveNextLine = true;
		                        break;
		                        }
		                    }
		                }
		            }
				}


		    if (errorList.Count == previousErrorCount)
		        {  return true;  }
		    else
		        {
				config = null;
		        return false;
		        }
			}


		// Group: Variables
		// __________________________________________________________________________

	    protected CondensedWhitespaceArrowSeparator arrowSeparatorRegex;
		protected AcceptableURLProtocolCharacters acceptableURLProtocolCharactersRegex;


		}
	}
