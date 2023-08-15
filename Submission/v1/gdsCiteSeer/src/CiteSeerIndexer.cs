using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using GoogleDesktopSearchAPILib;
using System.Windows.Forms;

namespace gdsCiteSeer
{
	public class CiteSeerIndexer
	{
        private const string    GUID_CITESEER_INDEX             = "{18E56A87-091A-4db6-92E1-C56FBF8E376C}";
        private const UInt32    E_COMPONENT_NOT_REGISTERED      = 0x80040002;
        private const UInt32    E_COMPONENT_DISABLED            = 0x80040005;
        private const UInt32    E_COMPONENT_ALREADY_REGISTERED  = 0x80040006;
        private const UInt32    S_INDEXING_PAUSED               = 0x80050007;
        private const UInt32    E_EVENT_TOO_LARGE               = 0x80040008;
        private const UInt32    E_SERVICE_NOT_RUNNING           = 0x80040009;
        

        enum gdsEventFlags : int
        {
            // Set this flag on all events
            EventFlagIndexable   = 0x00000001,

            // Set this flag when the event is historical, i.e. generated from 
            // a crawl over files or other data generated in the past. This  
            // is as opposed to events generated in realtime from events 
            // presently occurring
            EventFlagHistorical  = 0x00000010
        };

        private GoogleDesktopSearchRegisterClass gdsRegistration = null;

        public void Register()
        {
            // Register the component
            try
            {
                this.gdsRegistration = new GoogleDesktopSearchRegisterClass();
                object [] componentDescriptionProperties = new object[6] {"Title", "CiteSeer Metadata", "Description", "Indexes CiteSeer metadata fields", "Icon", "no icon"};
                this.gdsRegistration.RegisterComponent(GUID_CITESEER_INDEX, componentDescriptionProperties);

            }
            catch(COMException e)
            {
                if ((UInt32) e.ErrorCode != E_COMPONENT_ALREADY_REGISTERED)
                    throw e;
            }
        }

        public void Unregister()
        {
            // Unregister the component
            try 
            {
                if (this.gdsRegistration != null)
                {
                    this.gdsRegistration.UnregisterComponent(GUID_CITESEER_INDEX);
                    this.gdsRegistration = null;
                }
            }
            catch(COMException e)
            {
                if ((UInt32)e.ErrorCode != E_COMPONENT_NOT_REGISTERED)
                    throw e;
            }
        }


        public void CiteSeerIndex(string filepath)
        {
            //  Index specified metadata file
            GoogleDesktopSearchClass gdsClass = new GoogleDesktopSearchClass();

            if (!File.Exists(filepath))
                throw new ArgumentException("'" + filepath + "' does not exist");

            CSFileReader rdr = new CSFileReader();
            StringBuilder content = new StringBuilder();

            rdr.Load(filepath);

            ArrayList indexItems = (ArrayList) rdr.Items;
            if (indexItems != null)
            {
                for (int idx=0; idx < indexItems.Count; idx++)
                {
                    while (true)
                    {
                        try
                        {
                            // Create an index event, add document properties and send to Google Desktop
                            CSFileItem indexItem = (CSFileItem) indexItems[idx];

                            IGoogleDesktopSearchEvent gdsEvent = (IGoogleDesktopSearchEvent) gdsClass.CreateEvent(GUID_CITESEER_INDEX, "Google.Desktop.WebPage");

                            // Put author names into content area
                            content.Length = 0;
                            ArrayList authorFirstNames   = indexItem.AuthorFirstNames;
                            ArrayList authorLastNames    = indexItem.AuthorLastNames;

                            // Add year and month to content area
                            string fieldPubYear = FieldBuilder.BuildField(FieldBuilder.EFieldNames.PubYear, indexItem.PublicationDate.ToString("yyyy"));
                            content.Append(FieldBuilder.EncodeField(fieldPubYear) + " ");

                            string fieldPubMonth = FieldBuilder.BuildField(FieldBuilder.EFieldNames.PubMonth, indexItem.PublicationDate.ToString("MM"));
                            content.Append(FieldBuilder.EncodeField(fieldPubMonth) + " ");

                            for (int authorIndex=0; authorIndex < authorFirstNames.Count; authorIndex++)
                            {
                                string fieldFirstName = FieldBuilder.BuildField(FieldBuilder.EFieldNames.FirstName, ((string) authorFirstNames[authorIndex]).Trim());
                                string fieldLastName  = FieldBuilder.BuildField(FieldBuilder.EFieldNames.LastName,  ((string) authorLastNames[authorIndex]).Trim());
                                string fieldFullName  = FieldBuilder.BuildField(FieldBuilder.EFieldNames.FullName,  ((string) authorFirstNames[authorIndex]).Trim() + " " + ((string) authorLastNames[authorIndex]).Trim());

                                content.Append(FieldBuilder.EncodeField(fieldFirstName) + " ");
                                content.Append(FieldBuilder.EncodeField(fieldLastName) + " ");
                                content.Append(FieldBuilder.EncodeField(fieldFullName) + " ");
                            }

                            // Fill out the Google schema and index
                            gdsEvent.AddProperty("content",             content.ToString());
                            gdsEvent.AddProperty("format",              "text/plain");
                            gdsEvent.AddProperty("uri",                 indexItem.URL);
                            gdsEvent.AddProperty("last_modified_time",  indexItem.PublicationDate.ToString("yyyy-MM-dd"));
                            gdsEvent.AddProperty("title",               indexItem.Title);

                            
                            gdsEvent.Send((int) gdsEventFlags.EventFlagIndexable);

                            break;
                        }
                        catch(System.IO.EndOfStreamException)
                        {
                            // This normally means the Indexer is busy,
                            // so retry index request again later
                            System.Threading.Thread.Sleep(500);
                        }
                        catch(System.Runtime.InteropServices.COMException e)
                        {
                            // If indexing is paused, wait and retry
                            if ((UInt32)e.ErrorCode == S_INDEXING_PAUSED)
                                System.Threading.Thread.Sleep(15000);
                            else
                                throw e;
                        }
                        catch(Exception e)
                        {
                            throw e;
                        }
                    }
                }
            }
        }    
    }
}
