
/*
	Title: Qualifiers
	_______________________________________________________________________________

	Qualifiers are things like namespace levels and path folders that should be made a little less 
	prominent in titles in order to emphasize the ending class, function, or file name.  This is 
	especially important in languages like Java and C# where fully qualified names can be very 
	long (i.e. GregValure.NaturalDocs.Engine.Output.Builders.HTML.)
	
	Zero width spaces (&#8203;) should also be inserted so long titles are allowed to wrap and 
	don't hurt the layout.  When separating the qualifiers from the ending symbol via a span tag,
	the zero width space must appear after the closing tag because Chrome won't break on it if
	it appears inside.

*/

/*
	Class: Qualifier.Qualifier.Class

	Class: Qualifier::Qualifier::Class

	Function: Qualifier::Qualifier->Function

	File: Folder/Folder/FileName.MoreFileName.js
		The folders should be treated as qualifiers, but both dots in the file name should be ignored.

	Topic: Generic/Text Topic.And::More.
		This isn't a code topic so no qualifiers should apply.

	File: Folder/Folder/Folder///
		Separators at the end of a title should be ignored.

*/