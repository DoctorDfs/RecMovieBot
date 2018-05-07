using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Diagnostics;

namespace LuisBot.DatabasesConnection
{
    
    public class DbAccess
    {
        private static SqlConnection connection;
        private static  DbAccess access;
        private static bool connectionOpen = false;

        private DbAccess(){}

        public static DbAccess GetInstanceOfDbAccess()
        {
            if (access == null)
            {
                access = new DbAccess();
                connection = new SqlConnection();
                connection.ConnectionString = "";
                //sostituire la connection string
            }

            return access;
        }

        public bool OpenConnection() {
            try
            {
                connection.Open();
                connectionOpen = true;
                return true;
            }
            catch (Exception e) {
                Debug.Print(e.Message);
                return false;
            }
        }

        public bool CloseConnection() {
            try
            {
                if (connectionOpen) { 
                    connection.Close();
                    connectionOpen = false;
                }
             return true;
            }catch (Exception e) {
                Debug.Print(e.Message);
                return false;
            } 
        }

        public SqlConnection GetConnection() {
            if (access != null)
                return connection;
            else
                return null;
        }

        public bool ConnectionIsOpen() {
            return connectionOpen;
        }
        
    }
}