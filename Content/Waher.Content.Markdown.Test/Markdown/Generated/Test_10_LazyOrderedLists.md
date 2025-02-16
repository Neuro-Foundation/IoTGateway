﻿Lazy numbering can be accomplished by prefixing items with `#.`\:

#.	Bird
#.	McHale
#.	Parish

To make lists look nice, you can wrap items with hanging indents\:

#.	Lorem ipsum dolor sit amet, consectetuer adipiscing elit\. Aliquam hendrerit mi posuere lectus\. Vestibulum enim wisi, viverra nec, fringilla in, laoreet vitae, risus\.
#.	Donec sit amet nisl\. Aliquam semper ipsum sit amet velit\. Suspendisse id sem consectetuer libero luctus adipiscing\.

But if you want to be lazy, you don’t have to\:

#.	Lorem ipsum dolor sit amet, consectetuer adipiscing elit\. Aliquam hendrerit mi posuere lectus\. Vestibulum enim wisi, viverra nec, fringilla in, laoreet vitae, risus\.
#.	Donec sit amet nisl\. Aliquam semper ipsum sit amet velit\. Suspendisse id sem consectetuer libero luctus adipiscing\.

On separate lines\:

#.	Bird
	

#.	Magic
	


List items may consist of multiple paragraphs\. Each subsequent paragraph in a list item must be indented by either 4 spaces or one tab\:

#.	This is a list item with two paragraphs\. Lorem ipsum dolor sit amet, consectetuer adipiscing elit\. Aliquam hendrerit mi posuere lectus\.
	
	Vestibulum enim wisi, viverra nec, fringilla in, laoreet vitae, risus\. Donec sit amet nisl\. Aliquam semper ipsum sit amet velit\.
	

#.	Suspendisse id sem consectetuer libero luctus adipiscing\.
	


It looks nice if you indent every line of the subsequent paragraphs, but here again, Markdown will allow you to be lazy\:

#.	This is a list item with two paragraphs\.
	
	This is the second paragraph in the list item\. You’re only required to indent the first line\. Lorem ipsum dolor sit amet, consectetuer adipiscing elit\.
	

#.	Another item in the same list\.
	


To put a blockquote within a list item, the blockquote’s \> delimiters need to be indented\:

#.	A list item with a blockquote\:
	
	>	This is a blockquote inside a list item\.
	>	
	


To put a code block within a list item, the code block needs to be indented twice — 8 spaces or two tabs\:

#.	A list item with a code block\:
	
	```
	[code goes here]
	```
	


Nested lists\:

#.	Item 1
#.	Item 2 
	
	+	Item 2a
	+	Item 2b
	


Nested lists 2\:

#.	Item 1
#.	Item 2 
	
	#.	Item 2a
	#.	Item 2b
	


Nested lists 3\:

#.	Item 1
#.	Item 2 
	
	2.	Item 2a
	3.	Item 2b
	


Three levels\:

#.	Item 1
#.	Item 2 
	
	#.	Item 2a
	#.	Item 2b 
		
		#.	Item 2bi
		#.	Item 2bii
		#.	Item 2biii
		
	
	#.	Item 2c
	

#.	Item 3

Multi\-paragraph, multi\-level lists\:

#.	Item 1
	
	Second paragraph of item 1\.
	
	```
	This is some code of item 1.
	```
	
	Third paragraph of item 1\.
	
	#.	Item 1\.1
		
		Second paragraph of item 1\.1\.
		
		```
		This is some code of item 1.1.
		```
		
		Third paragraph of item 1\.1\.
		
		#.	Item 1\.1\.1
			
			Second paragraph of item 1\.1\.1\.
			
			```
			This is some code of item 1.1.1.
			```
			
			Third paragraph of item 1\.1\.1\.
			
		
		#.	Item 1\.1\.2
			
			Second paragraph of item 1\.1\.2\.
			
			```
			This is some code of item 1.1.2.
			```
			
			Third paragraph of item 1\.1\.2\.
			
		
		
	
	#.	Item 1\.2
		
		Second paragraph of item 1\.2\.
		
		```
		This is some code of item 1.2.
		```
		
		Third paragraph of item 1\.2\.
		
		#.	Item 1\.2\.1
			
			Second paragraph of item 1\.2\.1\.
			
			```
			This is some code of item 1.2.1.
			```
			
			Third paragraph of item 1\.2\.1\.
			
		
		#.	Item 1\.2\.2
			
			Second paragraph of item 1\.2\.2\.
			
			```
			This is some code of item 1.2.2.
			```
			
			Third paragraph of item 1\.2\.2\.
			
		
		
	
	


Starting with simple items, continuing with block items\:

#.	Item 1\.
#.	Item 2\.
#.	Item 3\.
#.	Item 4\.
	
	Item 4 has a second paragraph\.
	

#.	Item 5\.
#.	Item 6\.

Multi\-paragraph, multi\-level lists\:

#.	Unchecked
	
	Second paragraph of item 1
	

#.	Checked
	
	Second paragraph of item 2
	
	#.	Unchecked subitem
		
		```
		Some code
		```
		
		Second paragraph of item 2\.1
		
	
	#.	Checked subitem
		
		Second paragraph of item 2\.2
		
		#.	Unchecked subsubitem
		#.	Checked subsubitem
			
			```
			Some code
			```
			
			Second paragraph of item 2\.2\.2
			
		
		#.	Second checked subsubitem
		#.	Second unchecked subsubitem
		
	
	#.	Second checked subitem
		
		Second paragraph of item 2\.3
		
	
	#.	Second unchecked subitem
		
		Second paragraph of item 2\.4
		
	
	

#.	Also checked
	
	Second paragraph of item 3
	


Nested indentation\:

#.	#.	Header 1
		
		#.	Header 1\.1
		#.	Header 1\.2
		#.	Header 1\.3
		#.	Header 1\.4
		
	
	
	#.	Header 2
		
		#.	Header 2\.1
		#.	Header 2\.2
		#.	Header 2\.3
		
	
	


