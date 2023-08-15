using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace gdsCiteSeer
{
    public class CSFileReader
    {
        private ArrayList   itemListValue = new ArrayList();
        private CultureInfo cultureInfoUS = new CultureInfo("en-US");

        public IList Items
        {
            get { return itemListValue; }
        }

        public void Load(string filename)
        {
            if (filename == null || filename.Length == 0)
                throw new ArgumentException("Filename not specified");

            // Load document
            StreamReader xmlStream = null;
            XmlDocument xmlDoc = null;

            try
            {
                xmlStream = new StreamReader(filename);
                xmlDoc = new XmlDocument();
                xmlDoc.LoadXml("<records>" + xmlStream.ReadToEnd() + "</records>");

                // Configure namespaces for document
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace("oai_citeseer",  "http://copper.ist.psu.edu/oai/oai_citeseer/");
                nsmgr.AddNamespace("dc",            "http://purl.org/dc/elements/1.1/");

                // Parse all record nodes
                this.itemListValue.Clear();
                XmlNodeList recordNodes = xmlDoc.SelectNodes(@"records/record", nsmgr);
                foreach (XmlNode recordNode in recordNodes)
                {
                    XmlNode valueNode = null;
                    CSFileItem indexItem = new CSFileItem();

                    // Get identifier
                    valueNode = recordNode.SelectSingleNode(@"header/identifier", nsmgr);
                    if (valueNode == null)
                        throw new Exception("No identifier found");
                    string csIdentifier = valueNode.InnerText;

                    // Get title
                    valueNode = recordNode.SelectSingleNode(@"metadata/oai_citeseer:oai_citeseer/dc:title", nsmgr);
                    if (valueNode != null)
                        indexItem.Title = valueNode.InnerText;
                    //throw new Exception("No title found in document #" + csIdentifier);

                    // Get date
                    valueNode = recordNode.SelectSingleNode(@"metadata/oai_citeseer:oai_citeseer/dc:date", nsmgr);
                    if (valueNode != null)
                        indexItem.PublicationDate = DateTime.ParseExact(valueNode.InnerText, "yyyy-MM-dd", cultureInfoUS);
                    //throw new Exception("No date found in document #" + csIdentifier);

                    // Get CiteSeer URL
                    valueNode = recordNode.SelectSingleNode(@"metadata/oai_citeseer:oai_citeseer/dc:identifier", nsmgr);
                    if (valueNode == null)
                        throw new Exception("No URL found in document #" + csIdentifier);
                    indexItem.URL = valueNode.InnerText;

                    // Get author details
                    XmlNodeList authorNodes = recordNode.SelectNodes(@"metadata/oai_citeseer:oai_citeseer/oai_citeseer:author", nsmgr);
                    foreach (XmlNode authorNode in authorNodes)
                    {
                        // Author name
                        XmlAttribute authorName = authorNode.Attributes["name"];
                        if (authorName == null)
                            throw new Exception("No author name found in document #" + csIdentifier);
                        indexItem.AddAuthorName(authorName.Value);
                    
                        // Author affiliation
                        valueNode = authorNode.SelectSingleNode(@"affiliation", nsmgr);
                        if (valueNode != null)
                            indexItem.AddAuthorAffiliation(valueNode.InnerText);
                        else
                            indexItem.AddAuthorAffiliation(String.Empty);
                    }

                    itemListValue.Add(indexItem);
                }
            }
            finally
            {
                if (xmlStream != null)
                {
                    xmlStream.Close();
                    xmlStream = null;
                }
                xmlDoc = null;
            }
        }
	}
}
