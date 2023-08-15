using System;
using System.Security.Cryptography;
using System.Text;

namespace gdsCiteSeer
{
	public class FieldBuilder
	{
        public enum EFieldNames 
        {
            FirstName,
            LastName,
            FullName,
            PubMonth,
            PubYear
        }

        public static string EncodeField(string field)
        {
            if (field == null || field.Length == 0)
                return String.Empty;

            StringBuilder encodedField = new StringBuilder();

            byte[] dataToEncode = Encoding.UTF8.GetBytes(field);

            SHA1 hasher = new SHA1CryptoServiceProvider(); 
            //MD5  hasher = new MD5CryptoServiceProvider();
            byte [] hashResult = hasher.ComputeHash(dataToEncode);

            // Note: BitConverter.ToString separates hex values with dashes
            for (int idx=0; idx < hashResult.Length; idx++)
                encodedField.Append(hashResult[idx].ToString("X2"));

            return encodedField.ToString();
        }

        public static string BuildField(
            EFieldNames fieldName, 
            string fieldValue)
        {
            string fieldValueReturned = null;

            switch(fieldName)
            {
                case EFieldNames.FirstName:
                    fieldValueReturned = fieldValue.Trim().ToLower();
                    return "first:" + fieldValueReturned;
                    
                case EFieldNames.LastName:
                    fieldValueReturned = fieldValue.Trim().ToLower();
                    return "last:" + fieldValueReturned;

                case EFieldNames.FullName:
                    fieldValueReturned = fieldValue.Trim().ToLower();
                    return "full:" + fieldValueReturned;

                case EFieldNames.PubMonth:
                    fieldValueReturned = fieldValue.Trim();

                    if (fieldValueReturned.Length > 2)
                        fieldValueReturned = fieldValueReturned.Substring(0, 2);
                    else if (fieldValueReturned.Length < 2) 
                        fieldValueReturned = fieldValueReturned.PadLeft(2, '0');

                    return "pubmonth:" + fieldValueReturned;
                    
                case EFieldNames.PubYear:
                    fieldValueReturned = fieldValue.Trim();

                    if (fieldValueReturned.Length > 4)
                        fieldValueReturned = fieldValueReturned.Substring(0, 4);
                    else if (fieldValueReturned.Length < 4) 
                        fieldValueReturned = fieldValueReturned.PadLeft(4, '0');

                    return "pubyear:" + fieldValueReturned;
            }

            return String.Empty;
        }
	}
}
