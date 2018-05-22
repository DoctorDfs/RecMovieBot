using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Data;


namespace LuisBot.CommandPattern
{
    public static class ReceiverQuery
    {
        public static bool GenericInsertInto(string query, SqlConnection dbReference)
        {
          

            try
            {
                SqlCommand command = new SqlCommand(query, dbReference);
                
                command.ExecuteNonQuery();
                command.Dispose();
            }
            catch (Exception e) {
                Debug.Print($"Insert non riuscita!: {e}");
                Debug.Print("query:" + query);
                return false;
            }

            return true;
        }

        
        public static string GenericSelect(string query, SqlConnection dbReferences)
        {
            string result = string.Empty;
            SqlCommand command;
            SqlDataReader reader;
            try
            {
                command = new SqlCommand(query, dbReferences);
                reader = command.ExecuteReader();
                DataTable table = reader.GetSchemaTable();
                int tablelength = table.Rows.Count;
                
                while (reader.Read())
                {
                    for (int i = 0; i < tablelength; i++)
                    {
                        result = result + reader.GetValue(i) + " ";
                    }
                    result = result + "\n";
                }
                command.Dispose();
                reader.Close();
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
              
            }
            return result;
        }
    }
}