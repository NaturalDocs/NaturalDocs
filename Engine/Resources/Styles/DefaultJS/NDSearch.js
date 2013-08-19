﻿/*
	Include in output:

	This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
	Natural Docs is licensed under version 3 of the GNU Affero General Public
	License (AGPL).  Refer to License.txt or www.naturaldocs.org for the
	complete details.

	This file may be distributed with documentation files generated by Natural Docs.
	Such documentation is not covered by Natural Docs' copyright and licensing,
	and may have its own copyright and distribution terms as decided by its author.


	Substitutions:

		`PrefixObject_Prefix = 0
		`PrefixObject_KeywordObjects = 1
		`PrefixObject_Ready = 2
		`PrefixObject_DOMLoaderID = 3

		`KeywordObject_HTMLName = 0
		`KeywordObject_SearchText = 1
		`KeywordObject_MemberObjects = 2

		`MemberObject_HTMLQualifier = 0
		`MemberObject_HTMLName = 1
		`MemberObject_SearchText = 2
		`MemberObject_TopicType = 3
		`MemberObject_FileHashPath = 4
		`MemberObject_ClassHashPath = 5

		`UpdateSearchDelay = 350
		`MaxAutoExpand = 10
		`MoreResultsThreshold = 25

*/

"use strict";


/* Class: NDSearch
	___________________________________________________________________________

*/
var NDSearch = new function ()
	{

	// Group: Functions
	// ________________________________________________________________________


	/* Function: Start
	*/
	this.Start = function ()
		{

		// UI variables

		this.domSearchField = document.getElementById("NDSearchField");

		this.domResults = document.createElement("div");
		this.domResults.id = "NDSearchResults";
		this.domResults.style.display = "none";
		this.domResults.style.position = "fixed";

		if (NDCore.IEVersion() == 6)
			{  this.domResults.style.position = "absolute";  }

		this.domResultsContent = document.createElement("div");
		this.domResultsContent.id = "SeContent";

		this.domResults.appendChild(this.domResultsContent);
		document.body.appendChild(this.domResults);

		// this.updateTimeout = undefined;
		this.topLevelEntryCount = 0;
		this.visibleEntryCount = 0;
		this.openParents = [ ];
		this.keyboardSelectionIndex = -1;
		this.moreResultsThreshold = `MoreResultsThreshold;


		// Search data variables

		// We delay loading search/index.js until the search field is activated

		// this.allPrefixes = undefined;
		this.allPrefixesStatus = `NotLoaded;
		this.prefixObjects = { };


		// Event handlers

		this.domSearchField.onfocus = function () {  NDSearch.OnSearchFieldFocus();  };
		this.domSearchField.onblur = function () {  NDSearch.OnSearchFieldBlur();  };
		this.domSearchField.onkeydown = function (event) {  NDSearch.OnSearchFieldKey(event);  };

		this.domResults.onfocus = function () {  NDSearch.OnResultsFocus();  };
		this.domResults.onblur = function () {  NDSearch.OnResultsBlur();  };
		this.domResults.onkeydown = function (event) {  NDSearch.OnResultsKey(event);  };


		// Initialization

		this.DeactivateSearchField();
		};


	/* Function: Update
	*/
	this.Update = function ()
		{
		// This may be called by prefix data loaders after the field was deactivated so we have to check.
		if (!this.SearchFieldIsActive())
			{  return;  }

		var searchInterpretations = this.GetSearchInterpretations();

		if (searchInterpretations.length == 0)
			{
			this.ClearResults();
			return;
			}

		if (this.allPrefixesStatus != `Ready)
			{
			this.ClearResults(true);
			this.domResultsContent.innerHTML = this.BuildSearchingStatus();
			this.ShowResults();
			return;
			}

		var searchInterpretationPrefixes = this.GetMatchingPrefixes(searchInterpretations);

		this.RemoveUnusedPrefixObjects(searchInterpretationPrefixes);

		if (searchInterpretationPrefixes.length == 0)
			{
			this.ClearResults(true);
			this.domResultsContent.innerHTML = this.BuildNoMatchesStatus();
			this.ShowResults();
			return;
			}

		var location = new NDLocation(window.location.hash);
		var favorClasses = (location.type == "Class" || location.type == "Database");

		// Don't fonce expansion if it returns undefined because more needs to be loaded.
		var forceExpansion = (this.TotalMatchesGreaterThan(searchInterpretations, searchInterpretationPrefixes, `MaxAutoExpand) === false);

		var buildResults = this.BuildResults(searchInterpretations, searchInterpretationPrefixes, favorClasses, forceExpansion);
		
		var oldScrollTop = this.domResults.scrollTop;

		this.domResultsContent.innerHTML = buildResults.html;
		this.ShowResults();

		this.domResults.scrollTop = oldScrollTop;

		if (this.keyboardSelectionIndex != -1)
			{
			var domSelectedEntry = document.getElementById("SeSelectedEntry");

			if (domSelectedEntry != undefined)
				{  this.ScrollEntryIntoView(domSelectedEntry, false);  }
			}

		if (buildResults.prefixDataToLoad != undefined)
			{  this.LoadPrefixData(buildResults.prefixDataToLoad);  }
		};


	/* Function: ClearResults
		Clears the search results and all internal data related to it.  If internalOnly is set, it will not hide the results in the 
		DOM.
	*/
	this.ClearResults = function (internalOnly)
		{
		if (this.updateTimeout != undefined)
			{
			clearTimeout(this.updateTimeout);
			this.updateTimeout = undefined;
			}

		if (!internalOnly)
			{  this.HideResults();  }

		this.topLevelEntryCount = 0;
		this.visibleEntryCount = 0;
		this.openParents = [ ];
		this.keyboardSelectionIndex = -1;
		this.moreResultsThreshold = `MoreResultsThreshold;

		this.prefixObjects = { };
		};


	/* Function: ToggleParent
	*/
	this.ToggleParent = function (topLevelIndex, fromKeyboard)
		{
		var openParentsIndex = this.openParents.indexOf(topLevelIndex);
		var opening = (openParentsIndex == -1);

		if (opening)
			{  this.openParents.push(topLevelIndex);  }
		else // closing
			{  this.openParents.splice(openParentsIndex, 1);  }

		if (!fromKeyboard)
			{  
			this.keyboardSelectionIndex = -1;
			this.domSearchField.focus();
			}

		this.Update();

		// Update() will scroll the selected entry into view, but if the parent was opened we want to make sure all the children are 
		// in view as well.
		if (opening)
			{
			// Find the DOM element by the top level index.  We can't just use it as an index into ResultsContent.children
			// because SeEntryChildren count towards that but not topLevelIndex.

			var children = this.domResultsContent.children;
			var topLevelCount = 0;
			var domToggledElement = undefined;
			
			for (var i = 0; i < children.length; i++)
				{
				if (NDCore.HasClass(children[i], "SeEntry"))
					{
					if (topLevelCount == topLevelIndex)
						{  
						domToggledElement = children[i];
						break;
						}
					else
						{  topLevelCount++;  }
					}
				}

			if (domToggledElement != undefined)
				{  this.ScrollEntryIntoView(domToggledElement, true);  }
			}

		// Chrome 28 has a weird bug where if you open or close a parent and the scroll level wasn't at the top, all the swatches
		// and icons will be wrong.  They jump back into place as soon as you scroll some more, so do that automatically.
		if (navigator.userAgent.indexOf("KHTML") != -1 && this.domResults.scrollTop > 0)
			{
			// Have to scroll up instead of down or it won't work reliably when scrolled all the way to the bottom.
			this.domResults.scrollTop--;
			}
		};


	/* Function: LoadMoreResults
	*/
	this.LoadMoreResults = function ()
		{
		this.moreResultsThreshold = this.visibleEntryCount + `MoreResultsThreshold;
		this.Update();
		};



	// Group: Event Handlers
	// ________________________________________________________________________


	/* Function: OnSearchFieldFocus
	*/
	this.OnSearchFieldFocus = function ()
		{
		if (!this.SearchFieldIsActive())
			{  
			this.ActivateSearchField();  

			// Start loading the prefix index as soon as the search field is first activated.  We don't want to wait
			// until they start typing.
			if (this.allPrefixesStatus == `NotLoaded)
				{
				this.allPrefixesStatus = `Loading;
				NDCore.LoadJavaScript("search/index.js");
				}
			}
		// Otherwise it might be receiving focus back from the search results
		};


	/* Function: OnSearchFieldBlur
	*/
	this.OnSearchFieldBlur = function ()
		{
		// IE switches focus to the results if you click on a scroll bar
//		if (document.activeElement == undefined || document.activeElement.id != "NDSearchResults")
//			{  this.Deactivate();  }
		};


	/* Function: OnSearchFieldKey
	*/
	this.OnSearchFieldKey = function (event)
		{
		if (event === undefined)
			{  event = window.event;  }

		if (event.keyCode == 27)  // ESC
			{  
			this.ClearResults();
			this.DeactivateSearchField();

			// Set focus to the content page iframe so that keyboard scrolling works without clicking over to it.
			document.getElementById("CFrame").contentWindow.focus();
			}

		else if (event.keyCode == 38)  // Up
			{
			// If it's -1 (no selection) or 0 (first entry) wrap to the last item
			if (this.keyboardSelectionIndex <= 0)
				{
				// Will result in -1 if count is 0, which is exactly what we want.
				this.keyboardSelectionIndex = this.visibleEntryCount - 1;
				}
			else
				{  this.keyboardSelectionIndex--;  }

			this.UpdateSelection();
			}

		else if (event.keyCode == 40)  // Down
			{
			if (this.visibleEntryCount == 0)
				{  this.keyboardSelectionIndex = -1;  }
			else if (this.keyboardSelectionIndex >= this.visibleEntryCount - 1)
				{  this.keyboardSelectionIndex = 0;  }
			else
				{
				// Will result in 0 if it was previously -1, which is exactly what we want.
				this.keyboardSelectionIndex++;
				}

			this.UpdateSelection();
			}

		else if (event.keyCode == 13)  // Enter
			{
			// Figure out which element to activate, if any.
			var domSelectedEntry = undefined;

			// Was there a keyboard selection?
			if (this.keyboardSelectionIndex != -1)
				{  domSelectedEntry = document.getElementById("SeSelectedEntry");  }

			// If not, was there only one entry left in the results?
			else if (this.visibleEntryCount == 1)
				{  domSelectedEntry = this.domResultsContent.firstChild;  }

			// If not, wer there only two entries left in the results and the first was a group?  This will happen in
			// this scenario:
			//
			// Search: [CSS]
			// > CSS
			//     [] CSS Structure
			else if (this.visibleEntryCount == 2 && NDCore.HasClass(this.domResultsContent.firstChild, "SeParent"))
				{  domSelectedEntry = this.domResultsContent.childNodes[1].firstChild;  }

			// If we found something we can activate it.  If we didn't because there's a lot of results visible and no
			// keyboard selection, just ignore the enter press.
			if (domSelectedEntry != undefined)
				{
				var address = domSelectedEntry.getAttribute("href");

				if (address.substr(0, 11) == "javascript:")
					{  
					address = address.substr(11);

					// Change false to true to let ToggleParent() know we're doing it from the keyboard.
					// DEPENDENCY: This depends on the exact JavaScript BuildKeyword() generates for parents.
					address = address.replace(/^(NDSearch.ToggleParent\([0-9]+,)false(.*)$/, "$1true$2");

					eval(address);
					}
				else
					{  location.href = address;  }
				}
			}

		else  // Everything else
			{
			this.keyboardSelectionIndex = -1;

			if (this.updateTimeout == undefined)
				{
				this.updateTimeout = setTimeout(
					function ()
						{
						clearTimeout(NDSearch.updateTimeout);
						NDSearch.updateTimeout = undefined;

						NDSearch.Update();
						},
					`UpdateSearchDelay);
				}
			}
		}


	/* Function: OnResultsFocus
	*/
	this.OnResultsFocus = function ()
		{
		};


	/* Function: OnResultsBlur
	*/
	this.OnResultsBlur = function ()
		{
		};


	/* Function: OnResultsKey
	*/
	this.OnResultsKey = function (event)
		{
		this.OnSearchFieldKey(event);
		}


	/* Function: OnUpdateLayout
	*/
	this.OnUpdateLayout = function ()
		{
		// Check for undefined because this may be called before Start().
		if (this.domResults != undefined)
			{  
			this.PositionResults();

			if (this.keyboardSelectionIndex != -1)
				{  
				var domSelectedEntry = document.getElementById("SeSelectedEntry");

				if (domSelectedEntry != undefined)
					{  this.ScrollEntryIntoView(domSelectedEntry, false);  }
				}
			}
		};

	
	
	// Group: Search Functions
	// ________________________________________________________________________


	/* Function: GetSearchInterpretations
		Reads the contents of <domSearchField> and returns it as an array of normalized interpretations compatible with
		the search text Natural Docs generates for the data files .  Usually there will only be one, but there may be more 
		if the search text is ambiguous.  There may also be none, in which case it will return an empty array.
	*/
	this.GetSearchInterpretations = function ()
		{
		// DEPENDENCY: This must match what is done in Engine.SearchIndex.Entry.Normalize().

		var interpretations = [ ];
		var normalizedSearchText = this.domSearchField.value.toLowerCase();

		// Trim and condense whitespace
		normalizedSearchText = normalizedSearchText.replace(/\s+/g, " ");
		normalizedSearchText = normalizedSearchText.replace(/^ /, "");
		normalizedSearchText = normalizedSearchText.replace(/ $/, "");

		// Remove spaces unless between two alphanumeric/underscore characters
		normalizedSearchText = normalizedSearchText.replace(/([^a-z0-9_]) /g, "$1");  // Substitution because JavaScript has no (?<=) for lookbehinds
		normalizedSearchText = normalizedSearchText.replace(/ (?=[^a-z0-9_])/g, "");

		// Normalize separators
		normalizedSearchText = normalizedSearchText.replace(/::|->/g, ".");
		normalizedSearchText = normalizedSearchText.replace(/\\/g, "/");

		// Remove leading separators.  We don't have to worry about whitespace between them and the rest.
		normalizedSearchText = normalizedSearchText.replace(/^[./]+/, "");

		if (normalizedSearchText == "")
			{  return interpretations;  }

		interpretations.push(normalizedSearchText);


		// If the search text ends with : or - it's possible that it's the first character of :: or ->.  Provide an alternate
		// search string so relevant results don't disappear until the second character is added.

		var lastChar = normalizedSearchText.charAt(normalizedSearchText.length - 1);

		if (lastChar == ":" || lastChar == "-")
			{  interpretations.push(normalizedSearchText.substr(0, normalizedSearchText.length - 1) + ".");  }


		return interpretations;
		};


	/* Function: GetMatchingPrefixes
		Returns an array of prefixes from <allPrefixes> that apply to the passed search text array.
	*/
	this.GetMatchingPrefixes = function (searchTextArray)
		{
		var matchingPrefixes = [ ];

		if (this.allPrefixesStatus != `Ready)
			{  return matchingPrefixes;  }


		// Add each prefix to the array

		for (var i = 0; i < searchTextArray.length; i++)
			{
			var searchText = searchTextArray[i];
			var searchPrefix = this.MakePrefix(searchText);

			if (searchPrefix != undefined && searchPrefix != "")
				{
				var prefixIndex = this.GetAllPrefixesIndex(searchPrefix);

				while (prefixIndex < this.allPrefixes.length)
					{
					if (this.allPrefixes[prefixIndex].length >= searchPrefix.length &&
						this.allPrefixes[prefixIndex].substr(0, searchPrefix.length) == searchPrefix)
						{  
						matchingPrefixes.push(this.allPrefixes[prefixIndex]);  
						prefixIndex++;
						}
					else
						{  break;  }
					}
				}
			}


		if (searchTextArray.length <= 1)
			{  return matchingPrefixes;  }


		// If there was more than one, sort the combined array and remove duplicates.

		matchingPrefixes.sort();

		for (var i = 1; i < matchingPrefixes.length; /* no auto-increment */)
			{
			if (matchingPrefixes[i] == matchingPrefixes[i - 1])
				{  matchingPrefixes.splice(i, 1);  }
			else
				{  i++;  }
			}

		return matchingPrefixes;
		};

	
	/* Function: GetAllPrefixesIndex
		Returns the index at which the passed prefix appears or should appear in <allPrefixes>.  If it's not found 
		it will return the index it would be inserted at if it were to be added.
	*/
	this.GetAllPrefixesIndex = function (prefix)
		{
		if (this.allPrefixesStatus != `Ready)
			{  return undefined;  }
		if (this.allPrefixes.length == 0)
			{  return 0;  }

		var firstIndex = 0;
		var lastIndex = this.allPrefixes.length - 1;  // lastIndex is inclusive

		for (;;)
			{
			var testIndex = (firstIndex + lastIndex) >> 1;

			if (prefix == this.allPrefixes[testIndex])
				{  return testIndex;  }

			else if (prefix < this.allPrefixes[testIndex])
				{  
				if (testIndex == firstIndex)
					{  return testIndex;  }
				else
					{  
					// Not testIndex - 1 because even though prefix is lower, that may be the position it would be 
					// inserted at.
					lastIndex = testIndex;
					}
				}

			else // prefix > this.allPrefixes[testIndex]
				{
				if (testIndex == lastIndex)
					{  return lastIndex + 1;  }
				else
					{  firstIndex = testIndex + 1;  }
				}
			}
		};


	/* Function: KeywordMatchesInterpretations
		Returns whether the keyword matches any of the passed interpretations.
	*/
	this.KeywordMatchesInterpretations = function (keywordObject, interpretations)
		{
		for (var i = 0; i < interpretations.length; i++)
			{
			var interpretation = interpretations[i];

			// Searching for "acc" in keyword "Access"...
			if (interpretation.length <= keywordObject[`KeywordObject_SearchText].length)
				{
				if (keywordObject[`KeywordObject_SearchText].indexOf(interpretation) != -1)
					{  return true;  }
				}

			// Reverse it to search for "access levels" under keyword "Access"...
			else
				{
				if (interpretation.indexOf(keywordObject[`KeywordObject_SearchText]) != -1)
					{  return true;  }
				}
			}

		return false;
		};


	/* Function: MemberMatchesInterpretations
		Returns whether the keyword member matches any of the passed interpretations.
	*/
	this.MemberMatchesInterpretations = function (memberObject, interpretations)
		{
		for (var i = 0; i < interpretations.length; i++)
			{
			var interpretation = interpretations[i];

			if (memberObject[`MemberObject_SearchText].indexOf(interpretation) != -1)
				{  return true;  }
			}

		return false;
		};

	
	/* Function: TotalMatchesGreaterThan

		Returns whether the total number of entries that match the search interpretations is greater than the
		passed maximum.  It will return true or false, or undefined if more data needs to be loaded in order
		to find out.
	*/
	this.TotalMatchesGreaterThan = function (searchInterpretations, searchInterpretationPrefixes, maximum)
		{
		var totalMatches = 0;

		for (var p = 0; p < searchInterpretationPrefixes.length; p++)
			{
			var prefix = searchInterpretationPrefixes[p];

			if (this.prefixObjects[prefix] == undefined ||
				this.prefixObjects[prefix][`PrefixObject_Ready] == false)
				{
				return undefined;
				}

			var keywordObjects = this.prefixObjects[prefix][`PrefixObject_KeywordObjects];

			for (var k = 0; k < keywordObjects.length; k++)
				{
				var keywordObject = keywordObjects[k];

				if (this.KeywordMatchesInterpretations(keywordObject, searchInterpretations))
					{
					var memberObjects = keywordObject[`KeywordObject_MemberObjects];

					for (var m = 0; m < memberObjects.length; m++)
						{
						var memberObject = memberObjects[m];

						if (this.MemberMatchesInterpretations(memberObject, searchInterpretations))
							{  
							totalMatches++;

							if (totalMatches > maximum)
								{  return true;  }
							}
						}
					}

				}  // keywordObjects
			}  // searchInterpretationPrefixes
		
		return false;
		};



	// Group: Build Functions
	// ________________________________________________________________________


	/* Function: BuildResults

		Builds the search results in HTML.  If a prefix data it needs is not loaded yet it will build what it can and return 
		the next one that needs in the results.  This will also set <topLevelEntryCount> and <visibleEntryCount>.

		Flags:

			favorClasses - If set, links will use the class/database view whenever possible.
			forceExpansion - If set, all parent entries will be expanded regardless of <openParents>.

		Returns:

			{ html, prefixDataToLoad }
	*/
	this.BuildResults = function (searchInterpretations, searchInterpretationPrefixes, favorClasses, forceExpansion)
		{
		var results = {
			// prefixDataToLoad: undefined,
			html: ""
			};
		
		this.topLevelEntryCount = 0;
		this.visibleEntryCount = 0;

		var addSearchingStatus = false;

		for (var p = 0; p < searchInterpretationPrefixes.length; p++)
			{
			var prefix = searchInterpretationPrefixes[p];

			if (this.prefixObjects[prefix] == undefined)
				{
				if (this.visibleEntryCount < this.moreResultsThreshold)
					{
					results.prefixDataToLoad = prefix;
					addSearchingStatus = true;
					}
				else
					{
					results.html += this.BuildMoreResultsEntry();
					}

				break;
				}
			else if (this.prefixObjects[prefix][`PrefixObject_Ready] == false)
				{
				addSearchingStatus = true;
				break;
				}

			var keywordObjects = this.prefixObjects[prefix][`PrefixObject_KeywordObjects];

			for (var k = 0; k < keywordObjects.length; k++)
				{  results.html += this.BuildKeyword(keywordObjects[k], searchInterpretations, favorClasses, forceExpansion);  }
			}
		
		if (addSearchingStatus)
			{  results.html += this.BuildSearchingStatus();  }
		else if (results.html == "")
			{  results.html += this.BuildNoMatchesStatus();  }

		return results;
		};


	/* Function: BuildKeyword

		Builds the results for a keyword and returns the HTML.  The results will be filtered based on <searchText>.
		
		Flags:

			favorClasses - If set, links will use the class/database view whenever possible.
			forceExpansion - If set, all parent entries will be expanded regardless of <openParents>.
	*/
	this.BuildKeyword = function (keywordObject, searchInterpretations, favorClasses, forceExpansion)
		{
		if (this.KeywordMatchesInterpretations(keywordObject, searchInterpretations) == false)
			{  return "";  }

		var memberMatches = 0;
		var lastMatchingMemberObject;

		for (var i = 0; i < keywordObject[`KeywordObject_MemberObjects].length; i++)
			{
			var memberObject = keywordObject[`KeywordObject_MemberObjects][i];

			if (this.MemberMatchesInterpretations(memberObject, searchInterpretations))
				{
				lastMatchingMemberObject = memberObject;
				memberMatches++;  
				}
			}

		if (memberMatches == 0)
			{  return "";  }

		else if (memberMatches == 1 &&
				   lastMatchingMemberObject[`MemberObject_SearchText] == keywordObject[`KeywordObject_SearchText])
			{
			var selected = (this.keyboardSelectionIndex == this.visibleEntryCount);
			var topicType = lastMatchingMemberObject[`MemberObject_TopicType];
			var target;

			if (favorClasses && lastMatchingMemberObject[`MemberObject_ClassHashPath] != undefined)
				{  target = lastMatchingMemberObject[`MemberObject_ClassHashPath];  }
			else
				{  target = lastMatchingMemberObject[`MemberObject_FileHashPath];  }

			var html = "<a class=\"SeEntry T" + topicType + "\" " + (selected ? "id=\"SeSelectedEntry\" " : "") +
								"href=\"#" + target + "\">" +
								"<div class=\"SeEntryIcon\"></div>" +
								lastMatchingMemberObject[`MemberObject_HTMLName];

			if (lastMatchingMemberObject[`MemberObject_HTMLQualifier] != undefined)
				{  html += "<span class=\"SeQualifier\">, " + lastMatchingMemberObject[`MemberObject_HTMLQualifier] + "</span>";  }
			
			html += "</a>";

			this.topLevelEntryCount++;
			this.visibleEntryCount++;

			return html;
			}

		else
			{
			var selected = (this.keyboardSelectionIndex == this.visibleEntryCount);
			var openClosed;

			if (forceExpansion || this.openParents.indexOf(this.topLevelEntryCount) != -1)
				{  openClosed = "open";  }
			else
				{  openClosed = "closed";  }
			
			// DEPENDENCY: OnSearchFieldKey depends on the ToggleParent JavaScript to process the Enter key.
			var html = "<a class=\"SeEntry SeParent " + openClosed + "\" " + (selected ? "id=\"SeSelectedEntry\" " : "") +
								"href=\"javascript:NDSearch.ToggleParent(" + this.topLevelEntryCount + ",false)\">" + 
								"<div class=\"SeEntryIcon\"></div>" +
								keywordObject[`KeywordObject_HTMLName] + 
								" <span class=\"SeChildCount\">(" + memberMatches + ")</span>" +
							"</a>";

			this.topLevelEntryCount++;
			this.visibleEntryCount++;

			if (openClosed == "open")
				{
				html += "<div class=\"SeChildren\">";

				for (var i = 0; i < keywordObject[`KeywordObject_MemberObjects].length; i++)
					{
					var memberObject = keywordObject[`KeywordObject_MemberObjects][i];

					if (this.MemberMatchesInterpretations(memberObject, searchInterpretations))
						{
						var selected = (this.keyboardSelectionIndex == this.visibleEntryCount);
						var topicType = memberObject[`MemberObject_TopicType];
						var target;

						if (favorClasses && memberObject[`MemberObject_ClassHashPath] != undefined)
							{  target = memberObject[`MemberObject_ClassHashPath];  }
						else
							{  target = memberObject[`MemberObject_FileHashPath];  }

						html += "<a class=\"SeEntry T" + topicType + "\" " + (selected ? "id=\"SeSelectedEntry\" " : "") +
										"href=\"#" + target + "\">" + 
										"<div class=\"SeEntryIcon\"></div>" +
										memberObject[`MemberObject_HTMLName];

						if (memberObject[`MemberObject_HTMLQualifier] != undefined)
							{  html += "<span class=\"SeQualifier\">, " + memberObject[`MemberObject_HTMLQualifier] + "</span>";  }

						html += "</a>";

						this.visibleEntryCount++;
						}
					}

				html += "</div>";
				}

			return html;
			}
		};

	
	/* Function: BuildSearchingStatus
	*/
	this.BuildSearchingStatus = function ()
		{
		return "<div class=\"SeStatus Searching\">" + `Locale{HTML.SearchingStatus} + "</div>";
		};


	/* Function: BuildNoMatchesStatus
	*/
	this.BuildNoMatchesStatus = function ()
		{
		return "<div class=\"SeStatus NoResults\">" + `Locale{HTML.NoMatchesStatus} + "</div>";
		};

	
	/* Function: BuildMoreResultsEntry
	*/
	this.BuildMoreResultsEntry = function ()
		{
		var selected = (this.keyboardSelectionIndex == this.visibleEntryCount);

		var html = "<a class=\"SeEntry MoreResults\" " + (selected ? "id=\"SeSelectedEntry\" " : "") +
							"href=\"javascript:NDSearch.LoadMoreResults();\">" + 
							"<div class=\"SeEntryIcon\"></div>" +
							`Locale{HTML.MoreResults} + 
						 "</a>";

		this.visibleEntryCount++;
		this.topLevelEntryCount++;

		return html;
		}



	// Group: Prefix Functions
	// ________________________________________________________________________


	/* Function: MakePrefix
		Returns the prefix of an individual normalized search string.
	*/
	this.MakePrefix = function (searchText)
		{
		var prefix = "";

		for (var i = 0; i < 3; i++)
			{
			if (i >= searchText.length)
				{  break;  }

			var char = searchText.charAt(i);

			if (char == " " || char == "." || char == "/")
				{  break;  }

			prefix += char;
			}

		if (prefix.length > 0)
			{  return prefix;  }
		else
			{  return undefined;  }
		};


	/* Function: PrefixToHex
	*/
	this.PrefixToHex = function (prefix)
		{
		var hex = "";

		for (var i = 0; i < prefix.length; i++)
			{
			var charValue = "0000" + prefix.charCodeAt(i).toString(16);
			hex += charValue.substr(charValue.length - 4, 4);
			}

		return hex;
		};


	/* Function: PrefixToDataFile
	*/
	this.PrefixToDataFile = function (prefix)
		{
		return "search/keywords/" + this.PrefixToHex(prefix) + ".js";
		};

	
	
	// Group: UI Functions
	// ________________________________________________________________________

	
	/* Function: ActivateSearchField
	*/
	this.ActivateSearchField = function ()
		{
		this.domSearchField.value = "";
		NDCore.RemoveClass(this.domSearchField, "DefaultText");
		};

	
	/* Function: DeactivateSearchField
	*/
	this.DeactivateSearchField = function ()
		{
		NDCore.AddClass(this.domSearchField, "DefaultText");
		this.domSearchField.value = `Locale{HTML.DefaultSearchText};
		};


	/* Function: SearchFieldIsActive
	*/
	this.SearchFieldIsActive = function ()
		{
		return (NDCore.HasClass(this.domSearchField, "DefaultText") == false);
		};


	/* Function: ShowResults
	*/
	this.ShowResults = function ()
		{
		this.domResults.style.display = "block";
		this.PositionResults();
		};


	/* Function: HideResults
	*/
	this.HideResults = function ()
		{
		this.domResults.style.display = "none";
		};
	

	/* Function: PositionResults
	*/
	this.PositionResults = function ()
		{
		this.domResults.style.visibility = "hidden";
		var oldScrollTop = this.domResults.scrollTop;


		// First set the position to 0,0 and the width and height back to auto so it will be sized naturally to its content

		NDCore.SetToAbsolutePosition(this.domResults, 0, 0, undefined, undefined);
		this.domResults.style.width = "";
		this.domResults.style.height = "";

		
		// Figure out our desired upper right coordinates

		var urX = this.domSearchField.offsetLeft + this.domSearchField.offsetWidth;
		var urY = this.domSearchField.offsetTop + this.domSearchField.offsetHeight + 5;


		// Figure out our maximum width/height so we don't go off the screen.  We include the footer height not because
		// we care about covering the footer, but because it serves as a good estimate for the URL popup you get in
		// Firefox and Chrome.

		var footer = document.getElementById("NDFooter");

		var maxWidth = urX;
		var maxHeight = NDCore.WindowClientHeight() - urY - (footer.offsetHeight * 2);


		// Resize

		if (this.domResults.offsetHeight > maxHeight)
			{  NDCore.SetToAbsolutePosition(this.domResults, undefined, undefined, undefined, maxHeight);  }
		if (this.domResults.offsetWidth > maxWidth)
			{  NDCore.SetToAbsolutePosition(this.domResults, undefined, undefined, maxWidth, undefined);  }
		else
			{
			// Firefox and Chrome will sometimes not set the automatic width correctly, leaving a horizontal scroll bar where 
			// one isn't necessary.  Weird.  Fix it up for them.  This also fixes the positioning for IE 6 and 7.
			if (this.domResults.scrollWidth > this.domResults.clientWidth)
				{
				var newWidth = this.domResults.offsetWidth + 
									 (this.domResults.scrollWidth - this.domResults.clientWidth) + 5;

				if (newWidth > maxWidth)
					{  newWidth = maxWidth;  }

				NDCore.SetToAbsolutePosition(this.domResults, undefined, undefined, newWidth, undefined);
				}

			// Also make sure the results are at least as wide as the search box.
			if (this.domResults.offsetWidth < this.domSearchField.offsetWidth)
				{
				NDCore.SetToAbsolutePosition(this.domResults, undefined, undefined, this.domSearchField.offsetWidth, undefined);
				}
			}


		// Reposition

		NDCore.SetToAbsolutePosition(this.domResults, urX - this.domResults.offsetWidth, urY, 
												 undefined, undefined);


		this.domResults.scrollTop = oldScrollTop;
		this.domResults.style.visibility = "visible";
		};


	/* Function: UpdateSelection
		Updates the SeSelectedEntry element in the results to match <keyboardSelectionIndex> without regenerating the HTML.
	*/
	this.UpdateSelection = function ()
		{
		var domCurrentSelection = document.getElementById("SeSelectedEntry");
		var domNewSelection = undefined;

		if (this.keyboardSelectionIndex != -1)
			{  domNewSelection = NDCore.GetElementsByClassName(this.domResultsContent, "SeEntry", "a")[this.keyboardSelectionIndex];  }

		if (domCurrentSelection != undefined)
			{  domCurrentSelection.id = undefined;  }

		if (domNewSelection != undefined)
			{
			domNewSelection.id = "SeSelectedEntry";
			this.ScrollEntryIntoView(domNewSelection, false);
			}
		};


	/* Function: ScrollEntryIntoView
	*/
	this.ScrollEntryIntoView = function (domEntry, includeChildren)
		{
		var itemTop = domEntry.offsetTop;
		var itemBottom;

		if (includeChildren && NDCore.HasClass(domEntry, "open"))
			{
			var domSelectedChildren = domEntry.nextSibling;
			itemBottom = domSelectedChildren.offsetTop + domSelectedChildren.offsetHeight;
			}
		else
			{  itemBottom = itemTop + domEntry.offsetHeight;  }

		var windowTop = this.domResults.scrollTop;
		var windowBottom = windowTop + this.domResults.clientHeight;

		var offset = 0;

		if (windowBottom < itemBottom)
			{  offset = itemBottom - windowBottom;  }

		// Separate "if" statement instead of an "else" so we can handle when scrolling the bottom of the child list into
		// view would scroll the top of the list out of view.  In this case we want the top to stay in view.

		if (windowTop + offset > itemTop)
			{  offset = itemTop - windowTop;  }

		if (offset != 0)
			{  this.domResults.scrollTop += offset;  }
		};



	// Group: Search Data Functions
	// ________________________________________________________________________

	
	/* Function: OnPrefixIndexLoaded
	*/
	this.OnPrefixIndexLoaded = function (prefixes)
		{
		this.allPrefixes = prefixes;
		this.allPrefixesStatus = `Ready;

		this.Update();
		};

	
	/* Function: LoadPrefixData
		Starts loading the prefix data file associated with the passed prefix if it isn't already loaded or in the process of loading.
	*/
	this.LoadPrefixData = function (prefix)
		{
		if (this.prefixObjects[prefix] == undefined)
			{
			var prefixObject = [ ];

			prefixObject[`PrefixObject_Prefix] = prefix;
			// prefixObject[`PrefixObject_KeywordObjects] = undefined;
			prefixObject[`PrefixObject_Ready] = false;
			prefixObject[`PrefixObject_DOMLoaderID] = "NDPrefixLoader_" + this.PrefixToHex(prefix);

			this.prefixObjects[prefix] = prefixObject;

			NDCore.LoadJavaScript(this.PrefixToDataFile(prefix), prefixObject[`PrefixObject_DOMLoaderID]);
			}
		};


	/* Function: OnPrefixDataLoaded
		Called by the prefix data file when it has finished loading.
	*/
	this.OnPrefixDataLoaded = function (prefix, topicTypes, keywordObjects)
		{
		var prefixObject = this.prefixObjects[prefix];

		// The data file might have been requested but then purged as no longer needed before it came in.  If that's the
		// case then we can just discard the data.
		if (prefixObject == undefined)
			{  return;  }

		// Undo the data deduplication that was applied to the content.
		for (var k = 0; k < keywordObjects.length; k++)
			{
			var keywordObject = keywordObjects[k];

			if (keywordObject[`KeywordObject_SearchText] == undefined)
				{  keywordObject[`KeywordObject_SearchText] = keywordObject[`KeywordObject_HTMLName].toLowerCase();  }

			for (var m = 0; m < keywordObject[`KeywordObject_MemberObjects].length; m++)
				{
				var memberObject = keywordObject[`KeywordObject_MemberObjects][m];

				var topicTypeIndex = memberObject[`MemberObject_TopicType];
				memberObject[`MemberObject_TopicType] = topicTypes[topicTypeIndex];

				if (memberObject[`MemberObject_HTMLName] == undefined)
					{  memberObject[`MemberObject_HTMLName] = keywordObject[`KeywordObject_HTMLName];  }
				if (memberObject[`MemberObject_SearchText] == undefined)
					{  memberObject[`MemberObject_SearchText] = memberObject[`MemberObject_HTMLName].toLowerCase();  }
				}
			}

		prefixObject[`PrefixObject_KeywordObjects] = keywordObjects;
		prefixObject[`PrefixObject_Ready] = true;

		// We don't need the loader anymore.
		NDCore.RemoveScriptElement(prefixObject[`PrefixObject_DOMLoaderID]);

		//	Replace with this line to simulate latency:
		// setTimeout("NDSearch.Update()", 3000);
		this.Update();
		};


	/* Function: RemoveUnusedPrefixObjects
		Removes all entries from <prefixObjects> that are not in the passed prefix list.
	*/
	this.RemoveUnusedPrefixObjects = function (usedPrefixes)
		{
		if (usedPrefixes.length == 0)
			{  
			this.prefixObjects = { };
			return;
			}

		for (var prefix in this.prefixObjects)
			{
			if (usedPrefixes.indexOf(prefix) == -1)
				{
				// Set it to undefined instead of using delete so we don't potentially screw up the for..in iteration.
				this.prefixObjects[prefix] = undefined;
				}
			}
		};


	
	// Group: DOM Elements
	// ________________________________________________________________________


	/* var: domSearchField
		The search field DOM element.
	*/

	/* var: domResults
		The search results DOM element.
	*/

	/* var: domResultsContent
		The SeContent section of <domResults>.
	*/



	// Group: Timers
	// ________________________________________________________________________


	/* var: updateTimeout
		A timeout to manage the delay between when the user stops typing and when the search results
		update.
	*/



	// Group: UI Variables
	// ________________________________________________________________________


	/* var: visibleEntryCount
		The total number of visible entries in the search results.  This includes <topLevelEntryCount> plus any expanded 
		children.  It does not include children from parents that aren't expanded.
	*/

	/* var: topLevelEntryCount
		The total number of top-level entries in the search results.  This is only SeEntry items, so it excludes SeChildren
		and everything within them.
	*/

	/* var: openParents
		An array of indexes for all the SeParents which are open, in no particular order.  These indexes are based on
		<topLevelEntryCount>, not <visibleEntryCount>.
	*/

	/* var: keyboardSelectionIndex
		The index into the entries of the keyboard selection, or -1 if there isn't one.  This is based on <visibleEntryCount>,
		not <topLevelEntryCount>.
	*/

	/* var: moreResultsThreshold
		The number of results that must be loaded before the "Load More" message appears.  This will be adjusted
		upwards every time Load More is clicked.
	*/



	// Group: Search Data Variables
	// ________________________________________________________________________


	/* var: allPrefixes
		A sorted array of all the search text prefixes that have data files associated with them.  This is
		what was stored in search/index.js.  This variable is only available if <allPrefixesStatus> is set
		to `Ready.
	*/

	/* var: allPrefixesStatus

		The state of <allPrefixes>, which may be:

		`NotLoaded - search/index.js has not been loaded yet, or even had it's script element added.
		`Loading - search/index.js has had a script element added but the data hasn't returned yet.
		`Ready - search/index.js has been loaded and <allPrefixes> is ready to use.
	*/
		/* Substitutions:
			`NotLoaded = 1
			`Loading = 2
			`Ready = 3
		*/

	/* var: prefixObjects
		A hash mapping prefixes to prefix data objects.
	*/

	};
