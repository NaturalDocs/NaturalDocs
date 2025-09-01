
// Topic: Regular Expressions
// ____________________________________________________________________________
//
// Source-generated regular expressions in C# are defined using attributes.  However, attributes get included in the
// HTML output, and we don't want that in this particular case.  Regular expressions can be long and their contents are
// an implementation detail, much like the value of a constant, so they really shouldn't appear in the HTML.  This tests
// that the details of GeneratedRegex attributes are removed, and *only* for GeneratedRegex.
//

[GeneratedRegex("regular(expression)with\"quoted\"string")]
static private partial Regex RegularExpressionWithQuotedString();

[GeneratedRegex(@"regular(expression)with""verbatim""string", RegexOptions.CultureInvariant)]
static private partial Regex RegularExpressionWithVerbatimString();

[GeneratedRegex("""regular(expression)with"raw text\"string""",
						 RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
static private partial Regex RegularExpressionWithRawTextString();

[Attribute]
[GeneratedRegex("regular(expression)with\"quoted\"string")]
[AnotherAttribute("some other string")]
static private partial Regex RegularExpressionWithOtherAttributes();

[ GeneratedRegex ( "regular ( expression ) with spaces" ) ]
static private partial Regex RegularExpressionWithSpaces();

[Attribute("GeneratedRegex")]
static private partial Regex RegularExpressionWithFakeoutAttribute();

[Attribute(@"GeneratedRegex("fake regex content")")]
static private partial Regex RegularExpressionWithFakeoutAttribute2();

[Attribute(@" [GeneratedRegex("fake regex content")] ")]
static private partial Regex RegularExpressionWithFakeoutAttribute3();

static private partial Regex GeneratedRegex(string x = "fakeout");