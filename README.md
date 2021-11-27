Tesseract.Sitecore.ContentSearch
=====================================

This is an extension for Sitecore.ContentSearch that indexed Media items content with OCR support.

More information about Tesseract for .NET can be found here : https://github.com/charlesw/tesseract
Tesseract documentation: https://tesseract-ocr.github.io/



## Installation

- Build project and copy **bin** folder to your website.
- Copy **App_Config\Include\Tesseract.Sitecore.ContentSearch** folder to your website.
- Copy **tessdata** folder (with *.traineddata* files) to **App_Data\tessdata** folder of your website. 


## Sitecore versions support

Tested for *Sitecore 8.2 - 9.3*, should also work for *Sitecore 10* and higher *(no sitecore dependencies)*.

## How to add new language support

- Navagate to https://tesseract-ocr.github.io/tessdoc/Data-Files.
- Download language that you need for OCR support.
- Add needed language in **zzzSitecore.ContentSearch.Tesseract.config**:

Example for **rus** language (download *rus.traineddata* to **App_Data\tessdata** folder) and mapping for Sitecore ru-ru language:
```
<?xml version="1.0" encoding="utf-8" ?>
<configuration xmlns:patch="http://www.sitecore.net/xmlconfig/" xmlns:set="http://www.sitecore.net/xmlconfig/set/" xmlns:role="http://www.sitecore.net/xmlconfig/role/"  xmlns:search="http://www.sitecore.net/xmlconfig/search/">
  <sitecore role:require="Standalone or ContentManagement or ContentDelivery">

      <settings>
          <!-- Tessdata folder location -->
          <setting name="TessdataFolder" value="App_Data\\tessdata" />
      </settings>

      <!-- Language mapping format: <language name="{sitecore_language_name}">{tesstata_file_language_name}</language> -->
       <tesseract>
           <language name="en">eng</language>
           <language name="ru-ru">rus</language>
       </tesseract>
      

  </sitecore>
</configuration>
```

# Feedback #
If you are faced with any issues or have questions/suggestions you can contact me in sitecore slack channel @x3mxray.


