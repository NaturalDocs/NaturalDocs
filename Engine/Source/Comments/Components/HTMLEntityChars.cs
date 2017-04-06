/* 
 * Class: CodeClear.NaturalDocs.Engine.Comments.Components.HTMLEntityChars
 * ____________________________________________________________________________
 * 
 * A static class to handle entity chars that may appear in HTML or XML.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.Comments.Components
	{
	public static class HTMLEntityChars
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: IsEntityChar
		 * Whether the passed string is a recognized entity char.
		 */
		public static bool IsEntityChar (string text)
			{
			return (DecodeSingle(text) != '\0');
			}


		/* Function: IsEntityChar
		 * Whether the passed string segment is a recognized entity char.
		 */
		public static bool IsEntityChar (string text, int index, int length)
			{
			return (DecodeSingle(text, index, length) != '\0');
			}


		/* Function: DecodeSingle
		 * If the entire passed string is an entity char, returns its decoded value.  Will return '\0' if not.
		 */
		public static char DecodeSingle (string text)
			{
			return DecodeSingle(text, 0, text.Length);
			}


		/* Function: DecodeSingle
		 * If the passed string segment is an entity char, returns its decoded value.  Will return '\0' if not.
		 */
		public static char DecodeSingle (string text, int index, int length)
			{
			if (length <= 2 || text[index] != '&' || text[index + length - 1] != ';')
				{  return '\0';  }

			if (length >= 4 && text[index + 1] == '#')
				{
				int value = 0;

				if (length >= 5 && (text[index + 2] == 'x' || text[index + 2] == 'X'))
					{
					for (int i = index + 3; i < index + length - 1; i++)
						{
						value <<= 4;

						if (text[i] >= '0' && text[i] <= '9')
							{  value += text[i] - '0';  }
						else if (text[i] >= 'a' && text[i] <= 'f')
							{  value += 10 + (text[i] - 'a');  }
						else if (text[i] >= 'A' && text[i] <= 'F')
							{  value += 10 + (text[i] - 'A');  }
						else
							{  return '\0';  }
						}
					}
				else
					{
					for (int i = index + 2; i < index + length - 1; i++)
						{
						value *= 10;

						if (text[i] >= '0' && text[i] <= '9')
							{  value += text[i] - '0';  }
						else
							{  return '\0';  }
						}
					}

				return (char)value;
				}
			else
				{
				return Lookup(text.Substring(index, length));
				}
			}


		/* Function: DecodeAll
		 * Returns the string with any entity chars inside decoded.
		 */
		public static string DecodeAll (string text)
			{
			return DecodeAll(text, 0, text.Length);
			}


		/* Function: DecodeAll
		 * Returns the string segment with any entity chars inside decoded.
		 */
		public static string DecodeAll (string text, int index, int length)
			{
			int endIndex = index + length;
			int ampIndex = text.IndexOf('&', index, length);

			if (ampIndex == -1)
				{  return text;  }

			StringBuilder decodedText = new StringBuilder();

			for (;;)
				{
				if (ampIndex > index)
					{  
					decodedText.Append(text, index, ampIndex - index);  
					index = ampIndex;
					}

				int semicolonIndex = text.IndexOf(';', ampIndex + 1, endIndex - (ampIndex + 1));

				if (semicolonIndex == -1)
					{  break;  }

				char decodedChar = DecodeSingle(text, ampIndex, semicolonIndex + 1 - ampIndex);

				if (decodedChar != '\0')
					{  decodedText.Append(decodedChar);  }
				else
					{  decodedText.Append(text, ampIndex, semicolonIndex + 1 - ampIndex);  }

				index = semicolonIndex + 1;

				if (index >= endIndex)
					{  break;  }

				ampIndex = text.IndexOf('&', index, endIndex - index);

				if (ampIndex == -1)
					{  break;  }
				}

			if (index < endIndex)
				{  decodedText.Append(text, index, endIndex - index);  }

			return decodedText.ToString();
			}


		private static char Lookup (string text)
			{
			// According to StackOverflow using a switch statement this way causes the compiler to generate a hash lookup table instead of 
			// a series of if-else statements, so although this is ugly it's the most efficient way to have a compile-time constant lookup table.

			// http://stackoverflow.com/questions/268084/creating-a-constant-dictionary-in-c-sharp
			// http://stackoverflow.com/questions/395618/is-there-any-significant-difference-between-using-if-else-and-switch-case-in-c

			// Microsoft provides some native functions to do this, but they either require .NET 4.0+ (we target 3.5 because it comes with
			// Windows 7 and won't require a download) or require the full 3.5 profile instead of the client profile (resulting in a several hundred
			// meg download for Windows XP users.)

			switch (text)
				{
				case "&nbsp;": return '\u00A0';
				case "&iexcl;": return '\u00A1';
				case "&cent;": return '\u00A2';
				case "&pound;": return '\u00A3';
				case "&curren;": return '\u00A4';
				case "&yen;": return '\u00A5';
				case "&brvbar;": return '\u00A6';
				case "&sect;": return '\u00A7';
				case "&uml;": return '\u00A8';
				case "&copy;": return '\u00A9';
				case "&ordf;": return '\u00AA';
				case "&laquo;": return '\u00AB';
				case "&not;": return '\u00AC';
				case "&shy;": return '\u00AD';
				case "&reg;": return '\u00AE';
				case "&macr;": return '\u00AF';
				case "&deg;": return '\u00B0';
				case "&plusmn;": return '\u00B1';
				case "&sup2;": return '\u00B2';
				case "&sup3;": return '\u00B3';
				case "&acute;": return '\u00B4';
				case "&micro;": return '\u00B5';
				case "&para;": return '\u00B6';
				case "&middot;": return '\u00B7';
				case "&cedil;": return '\u00B8';
				case "&sup1;": return '\u00B9';
				case "&ordm;": return '\u00BA';
				case "&raquo;": return '\u00BB';
				case "&frac14;": return '\u00BC';
				case "&frac12;": return '\u00BD';
				case "&frac34;": return '\u00BE';
				case "&iquest;": return '\u00BF';
				case "&Agrave;": return '\u00C0';
				case "&Aacute;": return '\u00C1';
				case "&Acirc;": return '\u00C2';
				case "&Atilde;": return '\u00C3';
				case "&Auml;": return '\u00C4';
				case "&Aring;": return '\u00C5';
				case "&AElig;": return '\u00C6';
				case "&Ccedil;": return '\u00C7';
				case "&Egrave;": return '\u00C8';
				case "&Eacute;": return '\u00C9';
				case "&Ecirc;": return '\u00CA';
				case "&Euml;": return '\u00CB';
				case "&Igrave;": return '\u00CC';
				case "&Iacute;": return '\u00CD';
				case "&Icirc;": return '\u00CE';
				case "&Iuml;": return '\u00CF';
				case "&ETH;": return '\u00D0';
				case "&Ntilde;": return '\u00D1';
				case "&Ograve;": return '\u00D2';
				case "&Oacute;": return '\u00D3';
				case "&Ocirc;": return '\u00D4';
				case "&Otilde;": return '\u00D5';
				case "&Ouml;": return '\u00D6';
				case "&times;": return '\u00D7';
				case "&Oslash;": return '\u00D8';
				case "&Ugrave;": return '\u00D9';
				case "&Uacute;": return '\u00DA';
				case "&Ucirc;": return '\u00DB';
				case "&Uuml;": return '\u00DC';
				case "&Yacute;": return '\u00DD';
				case "&THORN;": return '\u00DE';
				case "&szlig;": return '\u00DF';
				case "&agrave;": return '\u00E0';
				case "&aacute;": return '\u00E1';
				case "&acirc;": return '\u00E2';
				case "&atilde;": return '\u00E3';
				case "&auml;": return '\u00E4';
				case "&aring;": return '\u00E5';
				case "&aelig;": return '\u00E6';
				case "&ccedil;": return '\u00E7';
				case "&egrave;": return '\u00E8';
				case "&eacute;": return '\u00E9';
				case "&ecirc;": return '\u00EA';
				case "&euml;": return '\u00EB';
				case "&igrave;": return '\u00EC';
				case "&iacute;": return '\u00ED';
				case "&icirc;": return '\u00EE';
				case "&iuml;": return '\u00EF';
				case "&eth;": return '\u00F0';
				case "&ntilde;": return '\u00F1';
				case "&ograve;": return '\u00F2';
				case "&oacute;": return '\u00F3';
				case "&ocirc;": return '\u00F4';
				case "&otilde;": return '\u00F5';
				case "&ouml;": return '\u00F6';
				case "&divide;": return '\u00F7';
				case "&oslash;": return '\u00F8';
				case "&ugrave;": return '\u00F9';
				case "&uacute;": return '\u00FA';
				case "&ucirc;": return '\u00FB';
				case "&uuml;": return '\u00FC';
				case "&yacute;": return '\u00FD';
				case "&thorn;": return '\u00FE';
				case "&yuml;": return '\u00FF';
				case "&fnof;": return '\u0192';
				case "&Alpha;": return '\u0391';
				case "&Beta;": return '\u0392';
				case "&Gamma;": return '\u0393';
				case "&Delta;": return '\u0394';
				case "&Epsilon;": return '\u0395';
				case "&Zeta;": return '\u0396';
				case "&Eta;": return '\u0397';
				case "&Theta;": return '\u0398';
				case "&Iota;": return '\u0399';
				case "&Kappa;": return '\u039A';
				case "&Lambda;": return '\u039B';
				case "&Mu;": return '\u039C';
				case "&Nu;": return '\u039D';
				case "&Xi;": return '\u039E';
				case "&Omicron;": return '\u039F';
				case "&Pi;": return '\u03A0';
				case "&Rho;": return '\u03A1';
				case "&Sigma;": return '\u03A3';
				case "&Tau;": return '\u03A4';
				case "&Upsilon;": return '\u03A5';
				case "&Phi;": return '\u03A6';
				case "&Chi;": return '\u03A7';
				case "&Psi;": return '\u03A8';
				case "&Omega;": return '\u03A9';
				case "&alpha;": return '\u03B1';
				case "&beta;": return '\u03B2';
				case "&gamma;": return '\u03B3';
				case "&delta;": return '\u03B4';
				case "&epsilon;": return '\u03B5';
				case "&zeta;": return '\u03B6';
				case "&eta;": return '\u03B7';
				case "&theta;": return '\u03B8';
				case "&iota;": return '\u03B9';
				case "&kappa;": return '\u03BA';
				case "&lambda;": return '\u03BB';
				case "&mu;": return '\u03BC';
				case "&nu;": return '\u03BD';
				case "&xi;": return '\u03BE';
				case "&omicron;": return '\u03BF';
				case "&pi;": return '\u03C0';
				case "&rho;": return '\u03C1';
				case "&sigmaf;": return '\u03C2';
				case "&sigma;": return '\u03C3';
				case "&tau;": return '\u03C4';
				case "&upsilon;": return '\u03C5';
				case "&phi;": return '\u03C6';
				case "&chi;": return '\u03C7';
				case "&psi;": return '\u03C8';
				case "&omega;": return '\u03C9';
				case "&thetasym;": return '\u03D1';
				case "&upsih;": return '\u03D2';
				case "&piv;": return '\u03D6';
				case "&bull;": return '\u2022';
				case "&hellip;": return '\u2026';
				case "&prime;": return '\u2032';
				case "&Prime;": return '\u2033';
				case "&oline;": return '\u203E';
				case "&frasl;": return '\u2044';
				case "&weierp;": return '\u2118';
				case "&image;": return '\u2111';
				case "&real;": return '\u211C';
				case "&trade;": return '\u2122';
				case "&alefsym;": return '\u2135';
				case "&larr;": return '\u2190';
				case "&uarr;": return '\u2191';
				case "&rarr;": return '\u2192';
				case "&darr;": return '\u2193';
				case "&harr;": return '\u2194';
				case "&crarr;": return '\u21B5';
				case "&lArr;": return '\u21D0';
				case "&uArr;": return '\u21D1';
				case "&rArr;": return '\u21D2';
				case "&dArr;": return '\u21D3';
				case "&hArr;": return '\u21D4';
				case "&forall;": return '\u2200';
				case "&part;": return '\u2202';
				case "&exist;": return '\u2203';
				case "&empty;": return '\u2205';
				case "&nabla;": return '\u2207';
				case "&isin;": return '\u2208';
				case "&notin;": return '\u2209';
				case "&ni;": return '\u220B';
				case "&prod;": return '\u220F';
				case "&sum;": return '\u2211';
				case "&minus;": return '\u2212';
				case "&lowast;": return '\u2217';
				case "&radic;": return '\u221A';
				case "&prop;": return '\u221D';
				case "&infin;": return '\u221E';
				case "&ang;": return '\u2220';
				case "&and;": return '\u2227';
				case "&or;": return '\u2228';
				case "&cap;": return '\u2229';
				case "&cup;": return '\u222A';
				case "&int;": return '\u222B';
				case "&there4;": return '\u2234';
				case "&sim;": return '\u223C';
				case "&cong;": return '\u2245';
				case "&asymp;": return '\u2248';
				case "&ne;": return '\u2260';
				case "&equiv;": return '\u2261';
				case "&le;": return '\u2264';
				case "&ge;": return '\u2265';
				case "&sub;": return '\u2282';
				case "&sup;": return '\u2283';
				case "&nsub;": return '\u2284';
				case "&sube;": return '\u2286';
				case "&supe;": return '\u2287';
				case "&oplus;": return '\u2295';
				case "&otimes;": return '\u2297';
				case "&perp;": return '\u22A5';
				case "&sdot;": return '\u22C5';
				case "&lceil;": return '\u2308';
				case "&rceil;": return '\u2309';
				case "&lfloor;": return '\u230A';
				case "&rfloor;": return '\u230B';
				case "&lang;": return '\u2329';
				case "&rang;": return '\u232A';
				case "&loz;": return '\u25CA';
				case "&spades;": return '\u2660';
				case "&clubs;": return '\u2663';
				case "&hearts;": return '\u2665';
				case "&diams;": return '\u2666';
				case "&quot;": return '\u0022';
				case "&amp;": return '\u0026';
				case "&lt;": return '\u003C';
				case "&gt;": return '\u003E';
				case "&OElig;": return '\u0152';
				case "&oelig;": return '\u0153';
				case "&Scaron;": return '\u0160';
				case "&scaron;": return '\u0161';
				case "&Yuml;": return '\u0178';
				case "&circ;": return '\u02C6';
				case "&tilde;": return '\u02DC';
				case "&ensp;": return '\u2002';
				case "&emsp;": return '\u2003';
				case "&thinsp;": return '\u2009';
				case "&zwnj;": return '\u200C';
				case "&zwj;": return '\u200D';
				case "&lrm;": return '\u200E';
				case "&rlm;": return '\u200F';
				case "&ndash;": return '\u2013';
				case "&mdash;": return '\u2014';
				case "&lsquo;": return '\u2018';
				case "&rsquo;": return '\u2019';
				case "&sbquo;": return '\u201A';
				case "&ldquo;": return '\u201C';
				case "&rdquo;": return '\u201D';
				case "&bdquo;": return '\u201E';
				case "&dagger;": return '\u2020';
				case "&Dagger;": return '\u2021';
				case "&permil;": return '\u2030';
				case "&lsaquo;": return '\u2039';
				case "&rsaquo;": return '\u203A';
				case "&euro;": return '\u20AC';
				default: return '\0';
				}
			}

		}
	}