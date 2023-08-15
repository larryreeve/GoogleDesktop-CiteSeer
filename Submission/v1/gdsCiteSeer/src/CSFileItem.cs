using System;
using System.Collections;

namespace gdsCiteSeer
{
	public class CSFileItem
	{
        private ArrayList   authorFirstNamesValue   = new ArrayList();
        private ArrayList   authorLastNamesValue    = new ArrayList();
        private ArrayList   authorAffiliationsValue = new ArrayList();
        private DateTime    publicationDateValue    = DateTime.MinValue;
        private string      title                   = String.Empty;
        private string      urlValue                = String.Empty;

        public ArrayList AuthorFirstNames
        {
            get { return this.authorFirstNamesValue; }
        }

        public ArrayList AuthorLastNames
        {
            get { return this.authorLastNamesValue; }
        }

        public ArrayList AuthorAffiliations
        {
            get { return this.authorAffiliationsValue; }
        }

        public DateTime PublicationDate
        {
            get { return this.publicationDateValue; }
            set { publicationDateValue = value;     }
        }

        public string Title
        {
            get { return this.title;    }
            set { this.title = value;   }
        }

        public string URL
        {
            get { return this.urlValue;     }
            set { this.urlValue = value;    }
        }

        public void AddAuthorAffiliation(string authorAffiliation)
        {
            this.authorAffiliationsValue.Add(authorAffiliation);
        }

        public void AddAuthorName(string authorFullName)
        {
            string [] names = authorFullName.Split(" ".ToCharArray());

            this.authorFirstNamesValue.Add(names[0]);
            this.authorLastNamesValue.Add(names[names.Length-1]);
        }
    }
}
